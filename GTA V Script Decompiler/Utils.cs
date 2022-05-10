using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
    static class Utils
    {
        static SHA256Managed crypt = new SHA256Managed();

        public static uint GetJoaat(string str)
        {
            uint hash, i;
            char[] key = str.ToLower().ToCharArray();
            for (hash = i = 0; i < key.Length; i++)
            {
                hash += key[i];
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            return hash;
        }

        public static string Represent(long value, Stack.DataType type)
        {
            switch (type)
            {
                case Stack.DataType.Float:
                    return BitConverter.ToSingle(BitConverter.GetBytes(value), 0).ToString() + "f";
                case Stack.DataType.Bool:
                    return value == 0 ? "false" : "true"; // still need to fix bools
                case Stack.DataType.FloatPtr:
                case Stack.DataType.IntPtr:
                case Stack.DataType.StringPtr:
                case Stack.DataType.UnkPtr:
                    return "NULL";
            }
            if (value > Int32.MaxValue && value <= UInt32.MaxValue)
                return ((int)((uint)value)).ToString();
            return value.ToString();
        }

        public static string FormatHexHash(uint hash)
        {
            return $"0x{hash:X8}";
        }

        public static float SwapEndian(float num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public static uint SwapEndian(uint num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public static int SwapEndian(int num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public static ulong SwapEndian(ulong num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public static long SwapEndian(long num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public static ushort SwapEndian(ushort num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static short SwapEndian(short num)
        {
            byte[] data = BitConverter.GetBytes(num);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public static bool IntParse(string temp, out int value)
        {
            //fixes when a string push also has the same index as a function location and the decompiler adds /*func_loc*/ to the string
            if (temp.Contains("/*") && temp.Contains("*/"))
            {
                int index = temp.IndexOf("/*");
                int index2 = temp.IndexOf("*/", index + 1);
                if (index2 == -1)
                {
                    value = -1;
                    return false;
                }
                temp = temp.Substring(0, index) + temp.Substring(index2 + 2);
            }
            //fixes the rare case when a string push has the same index as a known hash
            if (temp.StartsWith("joaat(\""))
            {
                temp = temp.Remove(temp.Length - 2).Substring(7);
                uint val = GetJoaat(temp);
                value = unchecked((int)val);
                return true;
            }
            if (Program.IntStyle == Program.IntType._hex)
            {
                return int.TryParse(temp.Substring(2), System.Globalization.NumberStyles.HexNumber, new System.Globalization.CultureInfo("en-gb"), out value);
            }
            else
                return int.TryParse(temp, out value);
        }

        public static UInt64 RotateRight(UInt64 x, int n)
        {
            return (((x) >> (n)) | ((x) << (64 - (n))));
        }

        public static UInt64 RotateLeft(UInt64 x, int n)
        {
            return (((x) << (n)) | ((x) >> (64 - (n))));
        }

        public static string GetAbsolutePath(string path, string basePath = null)
        {
            if (path == null) return null;
            basePath = (basePath == null) ? Path.GetFullPath(".") : GetAbsolutePath(null, basePath);

            String finalPath = path;
            if (!Path.IsPathRooted(path) || "\\".Equals(Path.GetPathRoot(path)))
            {
                if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    finalPath = Path.Combine(Path.GetPathRoot(basePath), path.TrimStart(Path.DirectorySeparatorChar));
                else
                    finalPath = Path.Combine(basePath, path);
            }
            return Path.GetFullPath(finalPath);
        }

        public static int CountLines(string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (str == string.Empty) return 0;

            int index = -1, count = 0;
            while (-1 != (index = str.IndexOf(Environment.NewLine, index + 1)))
                count++;
            return count + 1;
        }

        static StringBuilder Sb = new StringBuilder();
        public static string SHA256(string value)
        {
            Sb.Clear();
            foreach (Byte b in crypt.ComputeHash(Encoding.UTF8.GetBytes(value)))
                Sb.Append(b.ToString("x2"));
            return Sb.ToString();
        }

    }
}
