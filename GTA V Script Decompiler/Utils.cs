using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
	static class Utils
	{
		public static uint jenkins_one_at_a_time_hash(string str)
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
		public static string FormatHexHash(uint hash)
		{
			return $"0x{hash:X8}";
		}
		public static float SwapEndian(float num)
		{
            var b = BitConverter.GetBytes(num);

            return (float)((b[3] << 0) | (b[2] << 8) | (b[1] << 16) | (b[0] << 24));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToSingle(data, 0);
            */
        }
		public static uint SwapEndian(uint num)
		{
            var b = BitConverter.GetBytes(num);

            return (uint)((b[3] << 0) | (b[2] << 8) | (b[1] << 16) | (b[0] << 24));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToUInt32(data, 0);
            */
        }
		public static int SwapEndian(int num)
		{
            var b = BitConverter.GetBytes(num);

            return (int)((b[3] << 0) | (b[2] << 8) | (b[1] << 16) | (b[0] << 24));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToInt32(data, 0);
            */
        }
		public static ulong SwapEndian(ulong num)
		{
            var b = BitConverter.GetBytes(num);

            return (ulong)((b[7] << 0) | (b[6] << 8) | (b[5] << 16) | (b[4] << 24) |
                (b[3] << 32) | (b[2] << 40) | (b[1] << 48) | (b[0] << 56));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToUInt64(data, 0);
            */
        }
		public static long SwapEndian(long num)
		{
            var b = BitConverter.GetBytes(num);
            
            return (long)((b[7] << 0) | (b[6] << 8) | (b[5] << 16) | (b[4] << 24) |
                (b[3] << 32) | (b[2] << 40) | (b[1] << 48) | (b[0] << 56));

            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToInt64(data, 0);
            */
        }
		public static ushort SwapEndian(ushort num)
		{
            var b = BitConverter.GetBytes(num);

            return (ushort)((b[1] << 0) | (b[0] << 8));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToUInt16(data, 0);
            */
        }
		public static short SwapEndian(short num)
		{
            var b = BitConverter.GetBytes(num);

            return (short)((b[1] << 0) | (b[0] << 8));
            /*
			byte[] data = BitConverter.GetBytes(num);
			Array.Reverse(data);
			return BitConverter.ToInt16(data, 0);
            */
        }
		public static bool IntParse(string temp, out int value)
		{
            var isHex = (Program.getIntType == Program.IntType._hex);
            var numberStyle = (isHex) ? NumberStyles.HexNumber : NumberStyles.Integer;
            var str = (isHex) ? temp.Substring(2) : temp;

            return int.TryParse(str, numberStyle, new CultureInfo("en-US"), out value);
		}

	}
}
