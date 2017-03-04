using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler
{
	/// <summary>
	/// This is what i use for detecting if a variable is a int/float/bool/struct/array etc
	/// ToDo: work on proper detection of Vector3 and Custom script types(Entities, Ped, Vehicles etc)
	/// </summary>
	public class Vars_Info
    {
        ListType Listtype;//static/function_var/parameter
        List<Var> Vars;
        Dictionary<int, int> VarRemapper; //not necessary, just shifts variables up if variables before are bigger than 1 DWORD
		private int count;
		private int scriptParamCount = 0;
		private int scriptParamStart { get { return Vars.Count - scriptParamCount; } }
        public Vars_Info(ListType type, int varcount)
        {
            Listtype = type;
            Vars = new List<Var>();
            for (int i = 0; i < varcount; i++)
            {
                Vars.Add(new Var(i));
            }
			count = varcount;
        }
        public Vars_Info(ListType type)
        {
            Listtype = type;
			Vars = new List<Var>();
        }
        public void AddVar(int value)
        {
            Vars.Add(new Var(Vars.Count, value));//only used for static variables that are pre assigned
        }

        public void AddVar(long value)
        {
            Vars.Add(new Var(Vars.Count, value));
        }
        public void checkvars()
        {
            unusedcheck();
        }
		//This shouldnt be needed but in gamever 1.0.757.2
		//It seems a few of the scripts are accessing items from the
		//Stack frame that they havent reserver
		void broken_check(uint index)
		{
			if (index >= Vars.Count)
			{
				for (int i = Vars.Count; i <= index; i++)
				{
					Vars.Add(new Var(i));
				}
			}
		}
        public string GetVarName(uint index)
        {
	        string name = "";
            Var var = Vars[(int)index];
            if (var.DataType == Stack.DataType.String)
            {
                name = "c";
            }
            else if (var.Immediatesize == 1)
            {
                name = Types.gettype(var.DataType).varletter;
            }
            else if (var.Immediatesize == 3)
            {
                name = "v";
            }

            switch (Listtype)
            {
                case ListType.Statics: name += (index >= scriptParamStart ? "ScriptParam_" : "Local_"); break;
                case ListType.Vars: name += "Var"; break;
                case ListType.Params: name += "Param"; break;
            }

            if (Program.Shift_Variables) return name + VarRemapper[(int)index].ToString();
            else
            {
                return name + (Listtype == ListType.Statics && index >= scriptParamStart ? index - scriptParamStart : index).ToString();
            }

        }
		public void SetScriptParamCount(int count)
		{
			if (Listtype == ListType.Statics)
			{
				scriptParamCount = count;
			}
		}
		public string[] GetDeclaration(bool console)
		{
			List<string> Working = new List<string>();
			string varlocation = "";
			string datatype = "";

			int i = 0;
			int j = -1;
			foreach (Var var in Vars)
			{
				switch(Listtype)
				{
					case ListType.Statics:
						varlocation =  (i >= scriptParamStart ? "ScriptParam_" : "Local_");
						break;
					case ListType.Vars:
						varlocation = "Var";
						break;
					case ListType.Params:
						throw new DecompilingException("Parameters have different declaration");
				}
				j++;
				if (!var.Is_Used)
				{
					if (!Program.Shift_Variables)
						i++;
					continue;
				}
				if (Listtype == ListType.Vars && !var.Is_Called)
				{
					if (!Program.Shift_Variables)
						i++;
					continue;
				}
				if (var.Immediatesize == 1)
				{
					datatype = Types.gettype(var.DataType).vardec;
					
				}
				else if (var.Immediatesize == 3)
				{
					datatype = "vector3 v";
				}
				else if (var.DataType == Stack.DataType.String)
				{
					datatype = "char c";
				}
				else
				{
					datatype = "struct<" + var.Immediatesize.ToString() + "> ";
				}
				string value = "";
				if (!var.Is_Array)
				{
					if (Listtype == ListType.Statics)
					{
						if (var.Immediatesize == 1)
						{
							value = " = " + Utils.Represent(Vars[j].Value, var.DataType);
						}
						else if (var.DataType == Stack.DataType.String)
						{

							List<byte> data = new List<byte>();
							for (int l = 0; l < var.Immediatesize; l++)
							{
								data.AddRange(BitConverter.GetBytes(Vars[j + l].Value));
							}
							int len = data.IndexOf(0);
							data.RemoveRange(len, data.Count - len);
							value = " = \"" + Encoding.ASCII.GetString(data.ToArray()) + "\"";

						}
						else if (var.Immediatesize == 3)
						{

							value += " = { " + Utils.Represent(Vars[j].Value, Stack.DataType.Float) + ", ";
							value += Utils.Represent(Vars[j + 1].Value, Stack.DataType.Float) + ", ";
							value += Utils.Represent(Vars[j + 2].Value, Stack.DataType.Float) + " }";
						}
						else if (var.Immediatesize > 1)
						{
							value += " = { " + Utils.Represent(Vars[j].Value, Stack.DataType.Int);
							for (int l = 1; l < var.Immediatesize; l++)
							{
								value += ", " + Utils.Represent(Vars[j + l].Value, Stack.DataType.Int);
							}
							value += " } ";
						}
					}
				}
				else
				{
					if (Listtype == ListType.Statics)
					{
						if (var.Immediatesize == 1)
						{
							value = " = { ";
							for (int k = 0; k < var.Value; k++)
							{
								value += Utils.Represent(Vars[j + 1 + k].Value, var.DataType) + ", ";
							}
							if (value.Length > 2)
							{
								value = value.Remove(value.Length - 2);
							}
							value += " }";
						}
						else if (var.DataType == Stack.DataType.String)
						{
							value = " = { ";
							for (int k = 0; k < var.Value; k++)
							{
								List<byte> data = new List<byte>();
								for (int l = 0; l < var.Immediatesize; l++)
								{
									if (console)
									{
										data.AddRange(BitConverter.GetBytes((int)Vars[j + 1 + var.Immediatesize * k + l].Value));
									}
									else
									{
										data.AddRange(BitConverter.GetBytes(Vars[j + 1 + var.Immediatesize * k + l].Value));
									}
								}
								value += "\"" + Encoding.ASCII.GetString(data.ToArray()) + "\", ";
							}
							if (value.Length > 2)
							{
								value = value.Remove(value.Length - 2);
							}
							value += " }";
						}
						else if (var.Immediatesize == 3)
						{
							value = " = {";
							for (int k = 0; k < var.Value; k++)
							{
								value += "{ " + Utils.Represent(Vars[j + 1 + 3 * k].Value, Stack.DataType.Float) + ", ";
								value += Utils.Represent(Vars[j + 2 + 3 * k].Value, Stack.DataType.Float) + ", ";
								value += Utils.Represent(Vars[j + 3 + 3 * k].Value, Stack.DataType.Float) + " }, ";
							}
							if (value.Length > 2)
							{
								value = value.Remove(value.Length - 2);
							}
							value += " }";
						}
					}
				}
				string decl = datatype + varlocation + (Listtype == ListType.Statics && i >= scriptParamStart ? i - scriptParamStart : i).ToString();
				if (var.Is_Array)
				{
					decl += "[" + var.Value.ToString() + "]";
				}
				if (var.DataType == Stack.DataType.String)
				{
					decl += "[" + (var.Immediatesize*(console ? 4 : 8)).ToString() + "]";
				}
				Working.Add(decl + value + ";");
				i++;
			}
			return Working.ToArray();
		}
		public string GetPDec()
		{
			if (Listtype != ListType.Params)
				throw new DecompilingException("Only params use this declaration");
			string decl = "";
			int i = 0;
			foreach (Var var in Vars)
			{
				if (!var.Is_Used)
				{
					if (!Program.Shift_Variables)
					{
						i++;	 
					}
					continue;
				}			   
				string datatype = "";
				if (!var.Is_Array)
				{
					if (var.DataType == Stack.DataType.String)
					{
						datatype = "char[" + (var.Immediatesize * 4).ToString() + "] c";
					}
					else if (var.Immediatesize == 1)
						datatype = Types.gettype(var.DataType).vardec;
					else if (var.Immediatesize == 3)
					{
						datatype = "vector3 v";
					}
					else datatype = "struct<" + var.Immediatesize.ToString() + "> ";
				}
				else
				{
					if (var.DataType == Stack.DataType.String)
					{
						datatype = "char[" + (var.Immediatesize * 4).ToString() + "][] c";
					}
					else if (var.Immediatesize == 1)
						datatype = Types.gettype(var.DataType).vararraydec;
					else if (var.Immediatesize == 3)
					{
						datatype = "vector3[] v";
					}
					else datatype = "struct<" + var.Immediatesize.ToString() + ">[] ";
				}
				decl += datatype + "Param" + i.ToString() + ", ";
				i++;
			}
			if (decl.Length > 2)
				decl = decl.Remove(decl.Length - 2);
			return decl;
		}
        /// <summary>
        /// Remove unused vars from declaration, and shift var indexes down
        /// </summary>
        private void unusedcheck()
        {
            VarRemapper = new Dictionary<int, int>();
            for (int i = 0, k=0; i < Vars.Count; i++)
            {
                if (!Vars[i].Is_Used)
                    continue;
                if (Listtype == ListType.Vars && !Vars[i].Is_Called)
                    continue;
                if (Vars[i].Is_Array)
                {
                    for (int j = i + 1; j < i + 1 + Vars[i].Value * Vars[i].Immediatesize; j++)
                    {
                        Vars[j].dontuse();
                    }
                }
                else if (Vars[i].Immediatesize > 1)
                {
                    for (int j = i + 1; j < i + Vars[i].Immediatesize; j++)
                    {
	                    broken_check((uint)j);
                        Vars[j].dontuse();
                    }
                }
                VarRemapper.Add(i, k);
                k++;
            }
        }
        public Stack.DataType GetTypeAtIndex(uint index)
        {
            return Vars[(int)index].DataType;
        }
        public void SetTypeAtIndex(uint index, Stack.DataType type)
        {
            Vars[(int)index].DataType = type;
        }
        public Var GetVarAtIndex(uint index)
        {
	        broken_check(index);
            return Vars[(int)index];
        }

        public class Var
        {
            public Var(int index)
            {
                this.index = index;
                value = 0;
                immediatesize = 1;
                isArray = false;
                is_used = true;
                Datatype = Stack.DataType.Unk;
            }
            public Var(int index, long Value)
            {
                this.index = index;
                value = Value;
                immediatesize = 1;
                isArray = false;
                is_used = true;
                Datatype = Stack.DataType.Unk;
                isstruct = false;
            }
            int index;
			long value;
            int immediatesize;
            bool isArray;
            bool is_used;
            bool isstruct;
            bool iscalled = false;
            Stack.DataType Datatype;
            public int Index { get { return index; } }
            public long Value { get { return value; } set { this.value = value; } }
            public int Immediatesize { get { return immediatesize; } set { immediatesize = value; } }
            public void makearray()
            {
                if (!isstruct)
                isArray = true;
            }
            public void call()
            {
                iscalled = true;
            }
            public void makestruct()
            {
                DataType = Stack.DataType.Unk;
                isArray = false;
                isstruct = true;
            }
            public void dontuse()
            {
                is_used = false;
            }
            public bool Is_Used { get { return is_used; } }
            public bool Is_Called { get { return iscalled; } }
            public bool Is_Array { get { return isArray; } }
            public Stack.DataType DataType { get { return Datatype; } set { Datatype = value; } }
        }
        public enum ListType
        {
            Statics,
            Params,
            Vars
        }
    }
}
