using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Decompiler
{
    public class Hashes
    {
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
                string line = reader.ReadLine();
                if (line.Contains(":"))
                {
                    string[] split = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 2)
                        continue;

                    int hash = Convert.ToInt32(split[0]);
                    if (hash != 0 && !hashes.ContainsKey(hash))
                        hashes.Add(hash, split[1]); // Dont use ToLower(), use whatever is defined in Entities.
                }
                else if (line.Trim().Length > 0)
                {
                    int hash = (int) Utils.jenkins_one_at_a_time_hash(line.ToLower());
                    if (hash != 0 && !hashes.ContainsKey(hash))
                        hashes.Add(hash, line); // Dont use ToLower(), use whatever is defined in Entities.
                }
            }
        }

        public string GetHash(int value, string temp = "")
        {
            if (!Program.ReverseHashes)
                return inttohex(value);
            if (hashes.ContainsKey(value))
                return "joaat(\"" + hashes[value] + "\")";
            return inttohex(value) + temp;
        }

        public string GetHash(uint value, string temp = "")
        {
            if (!Program.ReverseHashes)
                return value.ToString();
            int intvalue = (int)value;
            if (hashes.ContainsKey(intvalue))
                return "joaat(\"" + hashes[intvalue] + "\")";
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
            string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Program.RDROpcodes ? "rdrgxr.dat" : "vgxt.dat");

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

                    int hash = split[0].StartsWith("0x") ? Convert.ToInt32(split[0], 16) : (int)Utils.jenkins_one_at_a_time_hash(split[0]);
                    if (hash != 0 && !entries.ContainsKey(hash))
                        entries.Add(hash, ToLiteral(split[1]));
                }
            }
        }

        public bool IsKnownGXT(int value)
        {
            return entries.ContainsKey(value);
        }

        public string GetEntry(int value)
        {
            return " /* GXTEntry: " + entries[value] + " */";
        }

        public bool IsKnownGXT(string value)
        {
            int tmp;
            return int.TryParse(value, out tmp) && IsKnownGXT(tmp);
        }

        public string GetEntry(string value)
        {
            int tmp;
            if (int.TryParse(value, out tmp) && IsKnownGXT(tmp))
            {
                return GetEntry(tmp);
            }
            return "";
        }
    }
}
