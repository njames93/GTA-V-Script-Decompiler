using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
	class NativeFile : Dictionary<uint, string>
	{
		public string nativefromhash(uint hash)
		{
			if (Program.nativefile.ContainsKey(hash))
			{
				return this[hash];
			}
			else
			{
				string temps = hash.ToString("X");
				while (temps.Length < 8)
					temps = "0" + temps;
				return "unk_0x" + temps;
			}
		}
		public Dictionary<string, uint> revmap = new Dictionary<string, uint>();
		public NativeFile(Stream Nativefile) : base()
		{
			StreamReader sr = new StreamReader(Nativefile);
			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				if (line.Length > 1)
				{
					string[] data = line.Split(':');
					if (data.Length != 3)
						continue;
					string val = data[0];
					string nat = (Program.Show_Nat_Namespace ? (data[1] + "::") : "") + data[2];
					uint value;
					if (uint.TryParse(val, out value))
					{
						if (!ContainsKey(value))
						{
							Add(value, nat);
							revmap.Add(nat, value);
						}
					}
				}
			}
			sr.Close();
		}
	}
	class x64NativeFile : Dictionary<ulong, string>
	{
		public ulong TranslateHash(ulong hash)
		{
			while (TranslationTable.ContainsKey(hash))
			{
				hash = TranslationTable[hash];
			}
			return hash;
		}
		public string nativefromhash(ulong hash)
		{
			if (Program.x64nativefile.ContainsKey(hash))
			{
				return this[hash];
			}
			else
			{
				string temps = hash.ToString("X");
				while (temps.Length < 8)
					temps = "0" + temps;
				return "unk_0x" + temps;
			}
		}
		public Dictionary<string, ulong> revmap = new Dictionary<string, ulong>();
		public Dictionary<ulong, ulong> TranslationTable = new Dictionary<ulong, ulong>();
		public x64NativeFile(Stream Nativefile)
			: base()
		{
			StreamReader sr = new StreamReader(Nativefile);
			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				if (line.Length > 1)
				{
					string[] data = line.Split(':');
					if (data.Length != 3)
						continue;
					string val = data[0];
					string nat = (Program.Show_Nat_Namespace ? (data[1] + "::") : "") + data[2];

                    // force uppercase
                    nat = nat.ToUpper();

					ulong value;
					if (ulong.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
					{
						if (!ContainsKey(value))
						{
							Add(value, nat);
							revmap.Add(nat, value);
						}
					}
				}
			}
			sr.Close();
			Stream Decompressed = new MemoryStream();
			Stream Compressed = new MemoryStream(Properties.Resources.native_translation);
			DeflateStream deflate = new DeflateStream(Compressed, CompressionMode.Decompress);
			deflate.CopyTo(Decompressed);
			deflate.Dispose();
			Decompressed.Position = 0;
			sr = new StreamReader(Decompressed);
			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				if (line.Length > 1)
				{
					string val = line.Remove(line.IndexOfAny(new char[] { ':', '=' }));
					string nat = line.Substring(val.Length + 1);
					ulong newer;
					ulong older;
					if (ulong.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out newer))
					{
						if (!ContainsKey(newer))
						{
							if (ulong.TryParse(nat, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out older))
							{
								if (!TranslationTable.ContainsKey(newer))
								{
									TranslationTable.Add(newer, older);
								}
							}
						}
					}
				}
			}
			sr.Close();
		}
	}
}
