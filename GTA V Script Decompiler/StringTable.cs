using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
	/// <summary>
	/// Generates a dictionary of indexes and the strings at those given indexed for use with the PushString instruction
	/// </summary>
	public class StringTable : Dictionary<int, string>
	{
		public StringTable(Stream scriptFile, int[] stringtablelocs, int blockcount, int wholesize)
		{
			int index = 0;
			for (int i = 0; i < blockcount; i++)
			{
				int tablesize = ((i + 1) * 0x4000 >= wholesize) ? wholesize % 0x4000 : 0x4000;
				scriptFile.Position = stringtablelocs[i];
				string working = "";
				byte data;
				int j = 0;
				while (j < tablesize)
				{
					data = (byte)scriptFile.ReadByte();
					if (data == 0)
					{
						Add(index, working);
						index += working.Length + 1;
						working = "";
					}
					else
					{
						//Fix \r and \n causing an actual new line in the string output
						switch ((char)data)
						{
							case '\n': working += "\\n"; break;
							case '\r': working += "\\r"; break;
							default: working += (char)data; break;
						}
					}
					j++;
				}
			}

		}
		public void dispose()
		{
			base.Clear();
		}
	}
}
