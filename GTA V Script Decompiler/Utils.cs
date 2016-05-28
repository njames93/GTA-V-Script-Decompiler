using System;
using System.Collections.Generic;
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
		public static string formathexhash(uint hash)
		{
			string hashres = hash.ToString("X");
			while (hashres.Length < 8)
				hashres = "0" + hashres;
			return hashres;
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
		public static bool intparse(string temp, out int value)
		{
			if (Program.getIntType == Program.IntType._hex)
			{
				return int.TryParse(temp.Substring(2), System.Globalization.NumberStyles.HexNumber, new System.Globalization.CultureInfo("en-gb"), out value);
			}
			else
				return int.TryParse(temp, out value);
		}

	}
}
