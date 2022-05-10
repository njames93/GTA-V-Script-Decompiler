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
        public static readonly string EntryName = "__EntryFunction__";

        List<byte> CodeTable;
        public OpcodeSet CodeSet { get; private set; }

        public StringTable StringTable;
        public X64NativeTable X64NativeTable;
        private int offset = 0;

        public List<Function> Functions; public Dictionary<int, Function> FunctionLoc;
        public List<Function> AggFunctions; public Dictionary<int, Function> AggregateLoc;

        public Dictionary<string, Tuple<int, int>> Function_loc = new Dictionary<string, Tuple<int, int>>();
        private Dictionary<ulong, HashSet<Function>> NativeXRef = new Dictionary<ulong, HashSet<Function>>();

        private Stream file;
        public ScriptHeader Header;
        internal Vars_Info Statics;
        internal bool CheckNative = true;

        public ScriptFile(Stream scriptStream, OpcodeSet opcodeSet)
        {
            file = scriptStream;
            CodeSet = opcodeSet;

            CodeTable = new List<byte>();
            Functions = new List<Function>(); FunctionLoc = new Dictionary<int, Function>();
            AggFunctions = new List<Function>(); AggregateLoc = new Dictionary<int, Function>();

            Header = ScriptHeader.Generate(scriptStream);
            StringTable = new StringTable(scriptStream, Header.StringTableOffsets, Header.StringBlocks, Header.StringsSize);
            X64NativeTable = new X64NativeTable(scriptStream, Header.NativesOffset + Header.RSC7Offset, Header.NativesCount, Header.CodeLength);
            for (int i = 0; i < Header.CodeBlocks; i++)
            {
                int tablesize = ((i + 1) * 0x4000 >= Header.CodeLength) ? Header.CodeLength % 0x4000 : 0x4000;
                byte[] working = new byte[tablesize];
                scriptStream.Position = Header.CodeTableOffsets[i];
                scriptStream.Read(working, 0, tablesize);
                CodeTable.AddRange(working);
            }
        }

        public override int GetHashCode() => Header.ScriptNameOffset.GetHashCode();

        public bool Dirty
        {
            get
            {
                bool dirty = false;
                foreach (Function func in Functions)
                    dirty = dirty || func.Dirty;
                return dirty;
            }
        }

        public bool PredecodeFunctions()
        {
            bool dirty = false;
            foreach (Function func in Functions)
            {
                if (func.Dirty)
                {
                    dirty = true;
                    func.Dirty = false;
                    func.decodeinsructionsforvarinfo();
                }
            }
            return dirty;
        }

        public ScriptFile Predecode()
        {
            GetStaticInfo();
            GetFunctions();
            foreach (Function func in Functions) func.PreDecode();
            Statics.checkvars();

            int count = 0;
            while (PredecodeFunctions())
            {
                count++;
                if (count > 10000)
                    throw new Exception("decodeinsructionsforvarinfo: infinite loop");
            }
            return this;
        }

        public ScriptFile BuildAggregation()
        {
            if (Program.AggregateFunctions)
            {
                AggFunctions.Clear();
                foreach (Function f in Functions)
                    AggFunctions.Add(f.CreateAggregate());
            }
            return this;
        }

        public ScriptFile Decode()
        {
            foreach (Function func in AggFunctions) func.Decode();
            foreach (Function func in Functions) func.Decode();
            return this;
        }

        public void CrossReferenceNative(ulong hash, Function f)
        {
            if (f.IsAggregate) return;

            if (!NativeXRef.ContainsKey(hash))
                NativeXRef.Add(hash, new HashSet<Function>(new Function[] { f }));
            else
                NativeXRef[hash].Add(f);
        }

        public void UpdateStaticType(uint index, Stack.DataType dataType)
        {
            Stack.DataType prev = Statics.GetTypeAtIndex(index);
            if (Statics.SetTypeAtIndex(index, dataType))
            {
                foreach (Function f in Functions)
                    f.Dirty = true;
            }
        }

        public void UpdateNativeReturnType(ulong hash, Stack.DataType datatype)
        {
            if (Program.X64npi.UpdateRetType(hash, datatype) && NativeXRef.ContainsKey(hash))
            {
                foreach (Function f in NativeXRef[hash])
                    f.Dirty = true;
            }
        }

        public void UpdateNativeParameter(ulong hash, Stack.DataType type, int index)
        {
            if (Program.X64npi.UpdateParam(hash, type, index))
            {
                foreach (Function f in NativeXRef[hash])
                    f.Dirty = true;
            }
        }

        public void Save(string filename)
        {
            Stream savefile = File.Create(filename);
            Save(savefile, true);
        }

        public void Save(Stream stream, bool close = false)
        {
            StreamWriter savestream = new StreamWriter(stream);
            try
            {
                int i = 1;
                if (Program.DeclareVariables)
                {
                    if (Header.StaticsCount > 0)
                    {
                        savestream.WriteLine("#region Local Var");
                        i++;
                        foreach (string s in Statics.GetDeclaration())
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
                    savestream.WriteLine(f.ToString());
                    Function_loc.Add(f.Name, new Tuple<int, int>(i, f.Location));
                    i += f.LineCount;
                }
            }
            finally
            {
                savestream.Flush();
                if (close)
                    savestream.Close();
            }
        }

        public void Close()
        {
            if (!Program.AggregateFunctions)
                foreach (Function func in Functions) func.Invalidate();
            file.Close();
        }

        public string[] GetStringTable()
        {
            List<string> table = new List<string>();
            foreach (KeyValuePair<int, string> item in StringTable)
                table.Add(item.Key.ToString() + ": " + item.Value);
            return table.ToArray();
        }

        public string[] GetNativeTable()
        {
            return X64NativeTable.GetNativeTable();
        }

        public string[] GetNativeHeader()
        {
            return X64NativeTable.GetNativeHeader();
        }

        public void GetFunctionCode()
        {
            for (int i = 0; i < Functions.Count - 1; i++)
            {
                int start = Functions[i].MaxLocation, end = Functions[i + 1].Location;
                Functions[i].CodeBlock = CodeTable.GetRange(start, end - start);
            }

            Functions[Functions.Count - 1].CodeBlock = CodeTable.GetRange(Functions[Functions.Count - 1].MaxLocation, CodeTable.Count - Functions[Functions.Count - 1].MaxLocation);
            foreach (Function func in Functions)
            {
                if (CodeSet.Map(func.CodeBlock[0]) != Instruction.ENTER && CodeSet.Map(func.CodeBlock[func.CodeBlock.Count - 3]) != Instruction.LEAVE)
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
                name = ScriptFile.EntryName;
            else
                name = Function.FunctionName + Functions.Count.ToString();

            int pcount = CodeTable[offset + 1];
            int tmp1 = CodeTable[offset + 2], tmp2 = CodeTable[offset + 3];
            int vcount = ((Program.SwapEndian) ? (tmp1 << 0x8) | tmp2 : (tmp2 << 0x8) | tmp1);
            if (vcount < 0)
            {
                throw new Exception("Well this shouldnt have happened");
            }
            int temp = start1 + 5 + namelen;
            while (CodeSet.Map(CodeTable[temp]) != Instruction.LEAVE)
            {
                switch (CodeSet.Map(CodeTable[temp]))
                {
                    case Instruction.PUSH_CONST_U8: temp += 1; break;
                    case Instruction.PUSH_CONST_U8_U8: temp += 2; break;
                    case Instruction.PUSH_CONST_U8_U8_U8: temp += 3; break;
                    case Instruction.PUSH_CONST_U32:
                    case Instruction.PUSH_CONST_F: temp += 4; break;
                    case Instruction.NATIVE: temp += 3; break;
                    case Instruction.ENTER: throw new Exception("Return Expected");
                    case Instruction.LEAVE: throw new Exception("Return Expected");
                    case Instruction.ARRAY_U8:
                    case Instruction.ARRAY_U8_LOAD:
                    case Instruction.ARRAY_U8_STORE:
                    case Instruction.LOCAL_U8:
                    case Instruction.LOCAL_U8_LOAD:
                    case Instruction.LOCAL_U8_STORE:
                    case Instruction.STATIC_U8:
                    case Instruction.STATIC_U8_LOAD:
                    case Instruction.STATIC_U8_STORE:
                    case Instruction.IADD_U8:
                    case Instruction.IMUL_U8:
                    case Instruction.IOFFSET_U8:
                    case Instruction.IOFFSET_U8_LOAD:
                    case Instruction.IOFFSET_U8_STORE: temp += 1; break;
                    case Instruction.PUSH_CONST_S16:
                    case Instruction.IADD_S16:
                    case Instruction.IMUL_S16:
                    case Instruction.IOFFSET_S16:
                    case Instruction.IOFFSET_S16_LOAD:
                    case Instruction.IOFFSET_S16_STORE:
                    case Instruction.ARRAY_U16:
                    case Instruction.ARRAY_U16_LOAD:
                    case Instruction.ARRAY_U16_STORE:
                    case Instruction.LOCAL_U16:
                    case Instruction.LOCAL_U16_LOAD:
                    case Instruction.LOCAL_U16_STORE:
                    case Instruction.STATIC_U16:
                    case Instruction.STATIC_U16_LOAD:
                    case Instruction.STATIC_U16_STORE:
                    case Instruction.GLOBAL_U16:
                    case Instruction.GLOBAL_U16_LOAD:
                    case Instruction.GLOBAL_U16_STORE:
                    case Instruction.J:
                    case Instruction.JZ:
                    case Instruction.IEQ_JZ:
                    case Instruction.INE_JZ:
                    case Instruction.IGT_JZ:
                    case Instruction.IGE_JZ:
                    case Instruction.ILT_JZ:
                    case Instruction.ILE_JZ: temp += 2; break;
                    case Instruction.CALL:
                    case Instruction.STATIC_U24:
                    case Instruction.STATIC_U24_LOAD:
                    case Instruction.STATIC_U24_STORE:
                    case Instruction.GLOBAL_U24:
                    case Instruction.GLOBAL_U24_LOAD:
                    case Instruction.GLOBAL_U24_STORE:
                    case Instruction.PUSH_CONST_U24: temp += 3; break;
                    case Instruction.SWITCH:
                    {
                        if (Program.RDROpcodes)
                        {
                            int length = (CodeTable[temp + 2] << 8) | CodeTable[temp + 1];
                            temp += 2 + 6 * (Program.SwapEndian ? Utils.SwapEndian(length) : length);
                        }
                        else
                            temp += 1 + 6 * CodeTable[temp + 1];
                        break;
                    }
                    case Instruction.TEXT_LABEL_ASSIGN_STRING:
                    case Instruction.TEXT_LABEL_ASSIGN_INT:
                    case Instruction.TEXT_LABEL_APPEND_STRING:
                    case Instruction.TEXT_LABEL_APPEND_INT: temp += 1; break;
                }
                temp += 1;
            }
            int rcount = CodeTable[temp + 2];
            int Location = start2;
            if (start1 == start2)
            {
                Functions.Add(new Function(this, name, pcount, vcount, rcount, Location, -1));
            }
            else
            {
                Functions.Add(new Function(this, name, pcount, vcount, rcount, Location, start1));
            }
        }

        void GetFunctions()
        {
            int returnpos = -3;
            while (offset < CodeTable.Count)
            {
                switch (CodeSet.Map(CodeTable[offset]))
                {
                    case Instruction.PUSH_CONST_U8: advpos(1); break;
                    case Instruction.PUSH_CONST_U8_U8: advpos(2); break;
                    case Instruction.PUSH_CONST_U8_U8_U8: advpos(3); break;
                    case Instruction.PUSH_CONST_U32:
                    case Instruction.PUSH_CONST_F: advpos(4); break;
                    case Instruction.NATIVE: advpos(3); break;
                    case Instruction.ENTER: AddFunction(offset, returnpos + 3); ; advpos(CodeTable[offset + 4] + 4); break;
                    case Instruction.LEAVE: returnpos = offset; advpos(2); break;
                    case Instruction.ARRAY_U8:
                    case Instruction.ARRAY_U8_LOAD:
                    case Instruction.ARRAY_U8_STORE:
                    case Instruction.LOCAL_U8:
                    case Instruction.LOCAL_U8_LOAD:
                    case Instruction.LOCAL_U8_STORE:
                    case Instruction.STATIC_U8:
                    case Instruction.STATIC_U8_LOAD:
                    case Instruction.STATIC_U8_STORE:
                    case Instruction.IADD_U8:
                    case Instruction.IMUL_U8:
                    case Instruction.IOFFSET_U8:
                    case Instruction.IOFFSET_U8_LOAD:
                    case Instruction.IOFFSET_U8_STORE: advpos(1); break;
                    case Instruction.PUSH_CONST_S16:
                    case Instruction.IADD_S16:
                    case Instruction.IMUL_S16:
                    case Instruction.IOFFSET_S16:
                    case Instruction.IOFFSET_S16_LOAD:
                    case Instruction.IOFFSET_S16_STORE:
                    case Instruction.ARRAY_U16:
                    case Instruction.ARRAY_U16_LOAD:
                    case Instruction.ARRAY_U16_STORE:
                    case Instruction.LOCAL_U16:
                    case Instruction.LOCAL_U16_LOAD:
                    case Instruction.LOCAL_U16_STORE:
                    case Instruction.STATIC_U16:
                    case Instruction.STATIC_U16_LOAD:
                    case Instruction.STATIC_U16_STORE:
                    case Instruction.GLOBAL_U16:
                    case Instruction.GLOBAL_U16_LOAD:
                    case Instruction.GLOBAL_U16_STORE:
                    case Instruction.J:
                    case Instruction.JZ:
                    case Instruction.IEQ_JZ:
                    case Instruction.INE_JZ:
                    case Instruction.IGT_JZ:
                    case Instruction.IGE_JZ:
                    case Instruction.ILT_JZ:
                    case Instruction.ILE_JZ: advpos(2); break;
                    case Instruction.CALL:
                    case Instruction.STATIC_U24:
                    case Instruction.STATIC_U24_LOAD:
                    case Instruction.STATIC_U24_STORE:
                    case Instruction.GLOBAL_U24:
                    case Instruction.GLOBAL_U24_LOAD:
                    case Instruction.GLOBAL_U24_STORE:
                    case Instruction.PUSH_CONST_U24: advpos(3); break;
                    case Instruction.SWITCH:
                    {
                        if (Program.RDROpcodes)
                        {
                            int length = (CodeTable[offset + 2] << 8) | CodeTable[offset + 1];
                            advpos(2 + 6 * (Program.SwapEndian ? Utils.SwapEndian(length) : length));
                        }
                        else
                            advpos(1 + 6 * CodeTable[offset + 1]);
                        break;
                    }
                    case Instruction.TEXT_LABEL_ASSIGN_STRING:
                    case Instruction.TEXT_LABEL_ASSIGN_INT:
                    case Instruction.TEXT_LABEL_APPEND_STRING:
                    case Instruction.TEXT_LABEL_APPEND_INT: advpos(1); break;
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
            IO.Reader reader = new IO.Reader(file);
            reader.BaseStream.Position = Header.StaticsOffset + Header.RSC7Offset;
            for (int count = 0; count < Header.StaticsCount; count++)
            {
                Statics.AddVar(Program.IsBit32 ? reader.CReadInt32() : reader.ReadInt64());
            }
        }

        /* Aggregate Function */
        public void CompileAggregate()
        {
            foreach (Function f in AggFunctions)
            {
                Agg.Instance.PushAggregate(this, f, f.ToString());
                f.Invalidate(); f.BaseFunction.Invalidate();
            }
        }
    }
}
