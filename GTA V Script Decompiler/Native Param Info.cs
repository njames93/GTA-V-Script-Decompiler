using System;
using System.Collections.Generic;
using System.IO;	  
using System.Windows.Forms;

namespace Decompiler
{
	class NativeParamInfo
	{
		Dictionary<uint, Tuple<Stack.DataType, Stack.DataType[]>> Natives;

		public NativeParamInfo()
		{
			loadfile();
		}

		public void savefile()
		{
			Stream natfile =
				File.Create(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"nativeinfo.dat"));
			IO.Writer writer = new IO.Writer(natfile);
			foreach (KeyValuePair<uint, Tuple<Stack.DataType, Stack.DataType[]>> native in Natives)
			{
				writer.Write(native.Key);
				writer.Write(Types.indexof(native.Value.Item1));
				writer.Write((byte) native.Value.Item2.Length);
				for (int i = 0; i < native.Value.Item2.Length; i++)
				{
					writer.Write(Types.indexof(native.Value.Item2[i]));
				}
			}
			writer.Close();
		}


		public void updatenative(uint hash, Stack.DataType returns, params Stack.DataType[] param)
		{
			lock (Program.ThreadLock)
			{
				if (!Natives.ContainsKey(hash))
				{
					Natives.Add(hash, new Tuple<Stack.DataType, Stack.DataType[]>(returns, param));
					return;
				}
			}

			Stack.DataType curret = Natives[hash].Item1;
			Stack.DataType[] curpar = Natives[hash].Item2;

			if (Types.gettype(curret).precedence < Types.gettype(returns).precedence)
			{
				curret = returns;
			}
			for (int i = 0; i < curpar.Length; i++)
			{
				if (Types.gettype(curpar[i]).precedence < Types.gettype(param[i]).precedence)
				{
					curpar[i] = param[i];
				}
			}
			Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(curret, curpar);
		}

		public bool updateparam(uint hash, Stack.DataType type, int paramindex)
		{
			if (!Natives.ContainsKey(hash))
				return false;
			Stack.DataType[] paramslist = Natives[hash].Item2;
			paramslist[paramindex] = type;
			Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(Natives[hash].Item1, paramslist);
			return true;
		}

		public Stack.DataType getrettype(uint hash)
		{
			if (!Natives.ContainsKey(hash))
				return Stack.DataType.Unk;
			return Natives[hash].Item1;
		}

		public Stack.DataType getparamtype(uint hash, int index)
		{
			if (!Natives.ContainsKey(hash))
				return Stack.DataType.Unk;
			return Natives[hash].Item2[index];
		}

		public void updaterettype(uint hash, Stack.DataType returns, bool over = false)
		{
			if (!Natives.ContainsKey(hash))
				return;
			if (Types.gettype(Natives[hash].Item1).precedence < Types.gettype(returns).precedence || over)
			{
				Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(returns, Natives[hash].Item2);
			}
		}

		public void loadfile()
		{
			Natives = new Dictionary<uint, Tuple<Stack.DataType, Stack.DataType[]>>();
			string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				"nativeinfo.dat");
			if (!File.Exists(file))
				return;
			Stream natfile = File.OpenRead(file);
			IO.Reader reader = new IO.Reader(natfile, true);
			while (natfile.Position < natfile.Length)
			{
				uint native = reader.ReadUInt32();
				Stack.DataType returntype = Types.getatindex(reader.ReadByte());
				byte count = reader.ReadByte();
				Stack.DataType[] param = new Stack.DataType[count];
				for (byte i = 0; i < count; i++)
				{
					param[i] = Types.getatindex(reader.ReadByte());
				}
				Natives.Add(native, new Tuple<Stack.DataType, Stack.DataType[]>(returntype, param));
			}

		}

		public string getnativeinfo(uint hash)
		{
			if (!Natives.ContainsKey(hash))
			{
				throw new Exception("Native not found");
			}
			string dec = Types.gettype(Natives[hash].Item1).returntype + Program.nativefile.nativefromhash(hash) + "(";
			int max = Natives[hash].Item2.Length;
			if (max == 0)
				return dec + ");";
			for (int i = 0; i < max; i++)
			{
				dec += Types.gettype(Natives[hash].Item2[i]).vardec + i + ", ";
			}
			return dec.Remove(dec.Length - 2) + ");";
		}

		public void exportnativeinfo()
		{
			Stream natfile =
				File.Create(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"natives.h"));
			StreamWriter sw = new StreamWriter(natfile);
			sw.WriteLine("/*************************************************************");
			sw.WriteLine("****************** GTA V Native Header file ******************");
			sw.WriteLine("*************************************************************/\n");
			sw.WriteLine("#ifndef NATIVE_HEADER\n#define NATIVE_HEADER");
			sw.WriteLine("typedef unsigned int uint;");
			sw.WriteLine("typedef uint bool;");
			sw.WriteLine("typedef uint var;");
			sw.WriteLine("");
			List<Tuple<string, string>> natives = new List<Tuple<string, string>>();

			foreach (KeyValuePair<uint, Tuple<Stack.DataType, Stack.DataType[]>> native in Natives)
			{
				string type = Types.gettype(native.Value.Item1).returntype;
				string line = Program.nativefile.nativefromhash(native.Key) + "(";

				int max = native.Value.Item2.Length;
				if (max == 0)
				{
					natives.Add(new Tuple<string, string>(line + ");\n", type));
					continue;
				}
				for (int i = 0; i < max; i++)
				{
					line += Types.gettype(native.Value.Item2[i]).vardec + i + ", ";
				}
				natives.Add(new Tuple<string, string>(line.Remove(line.Length - 2) + ");\n", type));
			}
			natives.Sort();
			foreach (Tuple<string, string> native in natives)
			{
				sw.Write("extern " + native.Item2 + native.Item1);
			}
			sw.WriteLine("#endif");
			sw.Close();
		}

		public string TypeToString(Stack.DataType type)
		{
			return Types.gettype(type).singlename;
		}

		public Stack.DataType StringToType(string _string)
		{
			foreach (Types.DataTypes type in Types._types)
			{
				if (type.singlename == _string)
					return type.type;
			}
			throw new Exception("Type not found");
		}

		public bool StringTypeExists(string _string)
		{
			foreach (Types.DataTypes type in Types._types)
			{
				if (type.singlename == _string)
					return true;
			}
			return false;
		}

	}

	class x64BitNativeParamInfo
	{
		Dictionary<ulong, Tuple<Stack.DataType, Stack.DataType[]>> Natives;

		public x64BitNativeParamInfo()
		{
			loadfile();
		}

		public void savefile()
		{
			try
			{
				string loc = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				Stream natfile = File.Create(Path.Combine(loc, "x64nativeinfonew.dat"));
				IO.Writer writer = new IO.Writer(natfile);
				foreach (KeyValuePair<ulong, Tuple<Stack.DataType, Stack.DataType[]>> native in Natives)
				{
					writer.Write(native.Key);
					writer.Write(Types.indexof(native.Value.Item1));
					writer.Write((byte) native.Value.Item2.Length);
					for (int i = 0; i < native.Value.Item2.Length; i++)
					{
						writer.Write(Types.indexof(native.Value.Item2[i]));
					}
				}
				writer.Close();
				if (File.Exists(Path.Combine(loc, "x64nativeinfo.dat")))
				{
					File.Delete("x64nativeinfo.dat");
				}
				File.Move(Path.Combine(loc, "x64nativeinfonew.dat"), Path.Combine(loc, "x64nativeinfo.dat"));
			}
			catch (Exception Exception)
			{
				MessageBox.Show(Exception.Message);
			}
		}


		public void updatenative(ulong hash, Stack.DataType returns, params Stack.DataType[] param)
		{
			lock (Program.ThreadLock)
			{
				if (!Natives.ContainsKey(hash))
				{
					Natives.Add(hash, new Tuple<Stack.DataType, Stack.DataType[]>(returns, param));
					return;
				}
			}

			Stack.DataType curret = Natives[hash].Item1;
			Stack.DataType[] curpar = Natives[hash].Item2;
			if (param.Length != curpar.Length)
			{
				Stack.DataType[] Old = curpar;
				curpar = param;
				if (Old.Length < curpar.Length)
				{
					for (int i = 0; i < Old.Length; i++)
					{
						curpar[i] = Old[i];
					}
				}
			}

			if (Types.gettype(curret).precedence < Types.gettype(returns).precedence)
			{
				curret = returns;
			}
			for (int i = 0; i < curpar.Length; i++)
			{
				if (Types.gettype(curpar[i]).precedence < Types.gettype(param[i]).precedence)
				{
					curpar[i] = param[i];
				}
			}
			Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(curret, curpar);
		}

		public void updateparam(ulong hash, Stack.DataType type, int paramindex)
		{
			if (!Natives.ContainsKey(hash))
				return;
			Stack.DataType[] paramslist = Natives[hash].Item2;
			if (paramindex >= paramslist.Length)
			{
				Stack.DataType[] Old = paramslist;
				paramslist = new Stack.DataType[paramindex + 1];
				for (int i = 0; i < Old.Length; i++)
				{
					paramslist[i] = Old[i];
				}
			}
			paramslist[paramindex] = type;
			Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(Natives[hash].Item1, paramslist);
		}

		public Stack.DataType getrettype(ulong hash)
		{
			if (!Natives.ContainsKey(hash))
				return Stack.DataType.Unk;
			return Natives[hash].Item1;
		}

		public Stack.DataType getparamtype(ulong hash, int index)
		{
			if (!Natives.ContainsKey(hash))
				return Stack.DataType.Unk;
			if (Natives[hash].Item2.Length <= index)
				return Stack.DataType.Unk;
			return Natives[hash].Item2[index];
		}

		public void updaterettype(ulong hash, Stack.DataType returns, bool over = false)
		{
			if (!Natives.ContainsKey(hash))
				return;
			if (Types.gettype(Natives[hash].Item1).precedence < Types.gettype(returns).precedence || over)
			{
				Natives[hash] = new Tuple<Stack.DataType, Stack.DataType[]>(returns, Natives[hash].Item2);
			}
		}

		public void loadfile()
		{
			Natives = new Dictionary<ulong, Tuple<Stack.DataType, Stack.DataType[]>>();
			string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				"x64nativeinfo.dat");
			if (!File.Exists(file))
				return;
			Stream natfile = File.OpenRead(file);
			IO.Reader reader = new IO.Reader(natfile, false);
			while (natfile.Position < natfile.Length)
			{
				ulong native = reader.ReadUInt64();
				Stack.DataType returntype = Types.getatindex(reader.ReadByte());
				byte count = reader.ReadByte();
				Stack.DataType[] param = new Stack.DataType[count];
				for (byte i = 0; i < count; i++)
				{
					param[i] = Types.getatindex(reader.ReadByte());
				}
				Natives.Add(native, new Tuple<Stack.DataType, Stack.DataType[]>(returntype, param));
			}

		}

		public string getnativeinfo(ulong hash)
		{
			if (!Natives.ContainsKey(hash))
			{
				throw new Exception("Native not found");
			}
			string dec = Types.gettype(Natives[hash].Item1).returntype + Program.x64nativefile.nativefromhash(hash) + "(";
			int max = Natives[hash].Item2.Length;
			if (max == 0)
				return dec + ");";
			for (int i = 0; i < max; i++)
			{
				dec += Types.gettype(Natives[hash].Item2[i]).vardec + i + ", ";
			}
			return dec.Remove(dec.Length - 2) + ");";
		}

		public void exportnativeinfo()
		{
			Stream natfile =
				File.Create(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"natives.h"));
			StreamWriter sw = new StreamWriter(natfile);
			sw.WriteLine("/*************************************************************");
			sw.WriteLine("****************** GTA V Native Header file ******************");
			sw.WriteLine("*************************************************************/\n");
			sw.WriteLine("#define TRUE 1\n#define FALSE 0\n#define true 1\n#define false 0\n");
			sw.WriteLine("typedef unsigned int uint;");
			sw.WriteLine("typedef uint bool;");
			sw.WriteLine("typedef uint var;");
			sw.WriteLine("");
			List<Tuple<string, string>> natives = new List<Tuple<string, string>>();

			foreach (KeyValuePair<ulong, Tuple<Stack.DataType, Stack.DataType[]>> native in Natives)
			{
				string type = Types.gettype(native.Value.Item1).returntype;
				string line = Program.x64nativefile.nativefromhash(native.Key) + "(";

				int max = native.Value.Item2.Length;
				if (max == 0)
				{
					natives.Add(new Tuple<string, string>(line + ");\n", type));
					continue;
				}
				for (int i = 0; i < max; i++)
				{
					line += Types.gettype(native.Value.Item2[i]).vardec + i + ", ";
				}
				natives.Add(new Tuple<string, string>(line.Remove(line.Length - 2) + ");\n", type));
			}
			natives.Sort();
			foreach (Tuple<string, string> native in natives)
			{
				sw.Write("extern " + native.Item2 + native.Item1);
			}
			sw.Close();
		}

		public string TypeToString(Stack.DataType type)
		{
			return Types.gettype(type).singlename;
		}

		public Stack.DataType StringToType(string _string)
		{
			foreach (Types.DataTypes type in Types._types)
			{
				if (type.singlename == _string)
					return type.type;
			}
			throw new Exception("Type not found");
		}

		public bool StringTypeExists(string _string)
		{
			foreach (Types.DataTypes type in Types._types)
			{
				if (type.singlename == _string)
					return true;
			}
			return false;
		}

	}
}
