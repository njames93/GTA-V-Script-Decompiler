using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Decompiler
{
    public class ScriptFile
    {
        List<byte> CodeTable;
        public StringTable StringTable;
        public NativeTable NativeTable;
        public X64NativeTable X64NativeTable;
        private int offset = 0;
        public readonly bool ConsoleVer;
        public List<Function> Functions;
        public Dictionary<int, FunctionName> FunctionLoc;
        public static Hashes hashbank = new Hashes();
        private Stream file;
        public ScriptHeader Header;
        public string name;
        internal Vars_Info Statics;
        internal bool CheckNative = true;
        internal static NativeParamInfo npi = new NativeParamInfo();
        internal static x64BitNativeParamInfo X64npi = new x64BitNativeParamInfo();


        public Dictionary<string, Tuple<int, int>> Function_loc = new Dictionary<string, Tuple<int, int>>();

        public ScriptFile(Stream scriptStream, bool Console)
        {
            ConsoleVer = Console;
            file = scriptStream;
            Header = ScriptHeader.Generate(scriptStream, Console);
            StringTable = new StringTable(scriptStream, Header.StringTableOffsets, Header.StringBlocks, Header.StringsSize);
            if (Console)
                NativeTable = new NativeTable(scriptStream, Header.NativesOffset + Header.RSC7Offset, Header.NativesCount);
            else
                X64NativeTable = new X64NativeTable(scriptStream, Header.NativesOffset + Header.RSC7Offset, Header.NativesCount, Header.CodeLength);
            name = Header.ScriptName;
            CodeTable = new List<byte>();
            for (int i = 0; i < Header.CodeBlocks; i++)
            {
                int tablesize = ((i + 1) * 0x4000 >= Header.CodeLength) ? Header.CodeLength % 0x4000 : 0x4000;
                byte[] working = new byte[tablesize];
                scriptStream.Position = Header.CodeTableOffsets[i];
                scriptStream.Read(working, 0, tablesize);
                CodeTable.AddRange(working);
            }
            GetStaticInfo();
            Functions = new List<Function>();
            FunctionLoc = new Dictionary<int, FunctionName>();
            GetFunctions();
            foreach (Function func in Functions)
            {
                func.PreDecode();
            }
            Statics.checkvars();
            foreach (Function func in Functions)
            {
                func.Decode();
            }
        }

        public void Save(string filename)
        {
            Stream savefile = File.Create(filename);
            Save(savefile, true);
        }

        public void Save(Stream stream, bool close = false)
        {
            int i = 1;
            StreamWriter savestream = new StreamWriter(stream);
            if (Program.Declare_Variables)
            {
                if (Header.StaticsCount > 0)
                {
                    savestream.WriteLine("#region Local Var");
                    i++;
                    foreach (string s in Statics.GetDeclaration(ConsoleVer))
                    {
                        savestream.WriteLine("\t" + s);
                        i++;
                    }
                    savestream.WriteLine("#endregion");
                    savestream.WriteLine("");
                    i += 2;
                }
            }
            foreach (Function f in Functions)
            {
                string s = f.ToString();
                savestream.WriteLine(s);
                Function_loc.Add(f.Name, new Tuple<int, int>(i, f.Location));
                i += f.LineCount;
            }
            savestream.Flush();
            if (close)
                savestream.Close();
        }

        public void Close()
        {
            file.Close();
        }

        public string[] GetStringTable()
        {
            List<string> table = new List<string>();
            foreach (KeyValuePair<int, string> item in StringTable)
            {
                table.Add(item.Key.ToString() + ": " + item.Value);
            }
            return table.ToArray();
        }

        public string[] GetNativeTable()
        {
            if (ConsoleVer)
                return NativeTable.GetNativeTable();
            else
                return X64NativeTable.GetNativeTable();
        }

        public string[] GetNativeHeader()
        {
            if (ConsoleVer)
                return NativeTable.GetNativeHeader();
            else
                return X64NativeTable.GetNativeHeader();
        }

        public void GetFunctionCode()
        {
            for (int i = 0; i < Functions.Count - 1; i++)
            {
                int start = Functions[i].MaxLocation;
                int end = Functions[i + 1].Location;
                Functions[i].CodeBlock = CodeTable.GetRange(start, end - start);
            }
            Functions[Functions.Count - 1].CodeBlock = CodeTable.GetRange(Functions[Functions.Count - 1].MaxLocation, CodeTable.Count - Functions[Functions.Count - 1].MaxLocation);
            foreach (Function func in Functions)
            {
                if (func.CodeBlock[0] != 45 && func.CodeBlock[func.CodeBlock.Count - 3] != 46)
                    throw new Exception("Function has incorrect start/ends");
            }
        }

        void advpos(int pos)
        {
            offset += pos;
        }

        void AddFunction(int start1, int start2)
        {
            byte namelen = CodeTable[start1 + 4];
            string name = "";
            if (namelen > 0)
            {
                for (int i = 0; i < namelen; i++)
                {
                    name += (char)CodeTable[start1 + 5 + i];
                }
            }
            else if (start1 == 0)
            {
                name = "__EntryFunction__";
            }
            else name = "func_" + Functions.Count.ToString();
            int pcount = CodeTable[offset + 1];
            int tmp1 = CodeTable[offset + 2], tmp2 = CodeTable[offset + 3];
            int vcount = ((ConsoleVer) ? (tmp1 << 0x8) | tmp2 : (tmp2 << 0x8) | tmp1);
            if (vcount < 0)
            {
                throw new Exception("Well this shouldnt have happened");
            }
            int temp = start1 + 5 + namelen;
            while (CodeTable[temp] != 46)
            {
                switch (CodeTable[temp])
                {
                    case 37: temp += 1; break;
                    case 38: temp += 2; break;
                    case 39: temp += 3; break;
                    case 40:
                    case 41: temp += 4; break;
                    case 44: temp += 3; break;
                    case 45: throw new Exception("Return Expected");
                    case 46: throw new Exception("Return Expected");
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 64:
                    case 65:
                    case 66: temp += 1; break;
                    case 67:
                    case 68:
                    case 69:
                    case 70:
                    case 71:
                    case 72:
                    case 73:
                    case 74:
                    case 75:
                    case 76:
                    case 77:
                    case 78:
                    case 79:
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86:
                    case 87:
                    case 88:
                    case 89:
                    case 90:
                    case 91:
                    case 92: temp += 2; break;
                    case 93:
                    case 94:
                    case 95:
                    case 96:
                    case 97: temp += 3; break;
                    case 98: temp += 1 + CodeTable[temp + 1] * 6; break;
                    case 101:
                    case 102:
                    case 103:
                    case 104: temp += 1; break;
                }
                temp += 1;
            }
            int rcount = CodeTable[temp + 2];
            int Location = start2;
            if (start1 == start2)
            {
                Functions.Add(new Function(this, name, pcount, vcount, rcount, Location));
            }
            else
                Functions.Add(new Function(this, name, pcount, vcount, rcount, Location, start1));
        }

        void GetFunctions()
        {
            int returnpos = -3;
            while (offset < CodeTable.Count)
            {
                switch (CodeTable[offset])
                {
                    case 37: advpos(1); break;
                    case 38: advpos(2); break;
                    case 39: advpos(3); break;
                    case 40:
                    case 41: advpos(4); break;
                    case 44: advpos(3); break;
                    case 45: AddFunction(offset, returnpos + 3); ; advpos(CodeTable[offset + 4] + 4); break;
                    case 46: returnpos = offset; advpos(2); break;
                    case 52:
                    case 53:
                    case 54:
                    case 55:
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 64:
                    case 65:
                    case 66: advpos(1); break;
                    case 67:
                    case 68:
                    case 69:
                    case 70:
                    case 71:
                    case 72:
                    case 73:
                    case 74:
                    case 75:
                    case 76:
                    case 77:
                    case 78:
                    case 79:
                    case 80:
                    case 81:
                    case 82:
                    case 83:
                    case 84:
                    case 85:
                    case 86:
                    case 87:
                    case 88:
                    case 89:
                    case 90:
                    case 91:
                    case 92: advpos(2); break;
                    case 93:
                    case 94:
                    case 95:
                    case 96:
                    case 97: advpos(3); break;
                    case 98: advpos(1 + CodeTable[offset + 1] * 6); break;
                    case 101:
                    case 102:
                    case 103:
                    case 104: advpos(1); break;
                }
                advpos(1);
            }
            offset = 0;
            GetFunctionCode();
        }

        private void GetStaticInfo()
        {
            Statics = new Vars_Info(Vars_Info.ListType.Statics);
            Statics.SetScriptParamCount(Header.ParameterCount);
            IO.Reader reader = new IO.Reader(file, ConsoleVer);
            reader.BaseStream.Position = Header.StaticsOffset + Header.RSC7Offset;
            for (int count = 0; count < Header.StaticsCount; count++)
            {
                if (ConsoleVer)
                    Statics.AddVar(reader.SReadInt32());
                else
                    Statics.AddVar(reader.ReadInt64());
            }
        }
    }
}
