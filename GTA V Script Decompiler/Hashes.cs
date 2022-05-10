using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace Decompiler
{
    public class Hashes
    {
        public static readonly string HashPrefix = "joaat(\"";
        public static string LiteralHash(string x) => HashPrefix + x + "\")";

        Dictionary<int, string> hashes;

        public Hashes()
        {
            hashes = new Dictionary<int, string>();
            string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Entities.dat");

            StreamReader reader;
            if (File.Exists(file))
            {
                reader = new StreamReader(File.OpenRead(file));
            }
            else
            {
                reader = new StreamReader(new MemoryStream(Properties.Resources.Entities));
            }
            Populate(reader);
        }

        private void Populate(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var hash = (int)Utils.GetJoaat(line.ToLower());

                if (hash != 0 && !IsKnownHash(hash))
                    hashes.Add(hash, line.ToUpper());
            }
        }

        public string GetHash(int value, string temp = "")
        {
            if (!Program.ReverseHashes)
                return inttohex(value);
            if (hashes.ContainsKey(value))
                return Hashes.LiteralHash(hashes[value]);
            return inttohex(value) + temp;
        }

        public string GetHash(uint value, string temp = "")
        {
            if (!Program.ReverseHashes)
                return value.ToString();
            int intvalue = (int)value;
            if (hashes.ContainsKey(intvalue))
                return Hashes.LiteralHash(hashes[intvalue]);
            return value.ToString() + temp;
        }

        public bool IsKnownHash(int value)
        {
            return hashes.ContainsKey(value);
        }

        public static string inttohex(int value)
        {
            if (Program.IntStyle == Program.IntType._hex)
            {
                string s = value.ToString("X");
                while (s.Length < 8) s = "0" + s;
                return "0x" + s;
            }
            return value.ToString();
        }
    }

    public class GXTEntries
    {
        Dictionary<int, string> entries;

        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }

        public GXTEntries()
        {
            entries = new Dictionary<int, string>();
            string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Program.RDROpcodes ? "rdrgxt.dat" : "vgxt.dat");

            StreamReader reader;
            if (File.Exists(file))
            {
                reader = new StreamReader(File.OpenRead(file));
            }
            else
            {
                reader = new StreamReader(new MemoryStream(Program.RDROpcodes ? Properties.Resources.rdrgxt : Properties.Resources.vgxt));
            }
            Populate(reader);
        }

        private void Populate(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains(" // "))
                {
                    string[] split = line.Split(new string[] { " // " }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 2)
                        continue;

                    int hash = split[0].StartsWith("0x") ? Convert.ToInt32(split[0], 16) : (int)Utils.GetJoaat(split[0]);
                    if (hash != 0 && !entries.ContainsKey(hash))
                        entries.Add(hash, ToLiteral(split[1]));
                }
            }
        }


        public string GetEntry(int value, bool floatTranslate)
        {
            if (!Program.ShowEntryComments) return "";
            if (entries.ContainsKey(value)) return " /* GXTEntry: " + entries[value] + " */";

            /* This is a hack. There are many like it. But this one is mine. */
            if (floatTranslate && value != 1 && value != 0)
            {
                float f = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
                if (float.IsNaN(f) || float.IsInfinity(f) || f == 0f)
                    return "";

                string fs = f.ToString(CultureInfo.InvariantCulture);
                if (!fs.Contains("E") && (((int)f == f && Math.Abs(f) < 10000f) || fs.Length < 6))
                    return " /* Float: " + fs + "f */";
            }
            return "";
        }

        public string GetEntry(string value, bool floatTranslate)
        {
            int tmp;
            return int.TryParse(value, out tmp) ? GetEntry(tmp, floatTranslate) : "";
        }
    }
}
