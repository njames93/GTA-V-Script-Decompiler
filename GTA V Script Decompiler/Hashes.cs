using System;
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
			StreamReader reader;
			if (
				File.Exists(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"entities.dat")))
			{
				reader =
					new StreamReader(
						File.OpenRead(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
							"entities.dat")));
			}
			else
			{
				Stream Decompressed = new MemoryStream();
				Stream Compressed = new MemoryStream(Properties.Resources.Entities);
				DeflateStream deflate = new DeflateStream(Compressed, CompressionMode.Decompress);
				deflate.CopyTo(Decompressed);
				deflate.Dispose();
				Decompressed.Position = 0;
				reader = new StreamReader(Decompressed);
			}
			while (!reader.EndOfStream)
			{
				string line = reader.ReadLine();
				string[] split = line.Split(new char[] {':'}, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length != 2)
					continue;
				int hash = 0;
				try
				{
					hash = Convert.ToInt32(split[0]);
				}
				catch
				{
					hash = (int) Convert.ToUInt32(split[0]);
				}
				if (!hashes.ContainsKey(hash) && hash != 0)
					hashes.Add(hash, split[1]);
			}

		}

		public void Export_Entities()
		{
			Stream Decompressed =
				File.Create(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"entities_exp.dat"));
			Stream Compressed = new MemoryStream(Properties.Resources.Entities);
			DeflateStream deflate = new DeflateStream(Compressed, CompressionMode.Decompress);
			deflate.CopyTo(Decompressed);
			deflate.Dispose();
			Decompressed.Close();
			System.Diagnostics.Process.Start(
				Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)));

		}

		public string GetHash(int value, string temp = "")
		{
			if (!Program.Reverse_Hashes)
				return inttohex(value);
			if (hashes.ContainsKey(value))
				return "joaat(\"" + hashes[value] + "\")";
			return inttohex(value) + temp;
		}

		public string GetHash(uint value, string temp = "")
		{
			if (!Program.Reverse_Hashes)
				return value.ToString();
			int intvalue = (int) value;
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
			if (Program.getIntType == Program.IntType._hex)
			{
				string s = value.ToString("X");
				while (s.Length < 8) s = "0" + s;
				return "0x" + s;
			}
			return value.ToString();
		}
	}
}
