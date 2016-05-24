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
        public Vars_Info(ListType type, int varcount)
        {
            Listtype = type;
            Vars = new List<Var>();
            for (int i = 0; i < varcount; i++)
            {
                Vars.Add(new Var(i));
            }
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
            Vars.Add(new Var(Vars.Count, (int) value));
        }
        public void checkvars()
        {
            unusedcheck();
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
                /*switch (var.DataType)
                {
                    case Stack.DataType.Unk: name = "u"; break;
                    case Stack.DataType.Bool: name = "b"; break;
                    case Stack.DataType.Float: name = "f"; break;
                    case Stack.DataType.Int: name = "i"; break;
                    case Stack.DataType.String: throw new Exception("Strings should be handled already");
                    case Stack.DataType.StringPtr: name = "s"; break;
                }*/
            }
            else if (var.Immediatesize == 3)
            {
                name = "v";
            }

            switch (Listtype)
            {
                case ListType.Statics: name += "Local_"; break;
                case ListType.Vars: name += "Var"; break;
                case ListType.Params: name += "Param"; break;
            }

            if (Program.Shift_Variables) return name + VarRemapper[(int)index].ToString();
            else
            {
                return name + index.ToString();
            }

        }
		public string[] GetDeclaration()
		{
			List<string> Working = new List<string>();
			string varlocation = "";
			string datatype = "";
			switch (Listtype)
			{
				case ListType.Statics: varlocation = "Local_"; break;
				case ListType.Vars: varlocation = "Var"; break;
				case ListType.Params: throw new DecompilingException("Parameters have different declaration");
			}
			int i = 0;
			foreach (Var var in Vars)
			{
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
				if (!var.Is_Array)
				{
					if (var.DataType == Stack.DataType.String)
					{
						datatype = "char[" + (var.Immediatesize * 4).ToString() + "] c";
					}
					else if (var.Immediatesize == 1)
						datatype = Types.gettype(var.DataType).vardec;
					/*switch (var.DataType)
					{

						case Stack.DataType.Unk: datatype = "var u"; break;
						case Stack.DataType.Bool: datatype = "bool b"; break;
						case Stack.DataType.Float: datatype = "float f"; break;
						case Stack.DataType.Int: datatype = "int i"; break;
						case Stack.DataType.String: throw new Exception("Strings should have this declared");
						case Stack.DataType.StringPtr: datatype = "*string s"; break;
					}*/
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
					/*switch (var.DataType)
					{
						case Stack.DataType.Unk: datatype = "var[] u"; break;
						case Stack.DataType.Bool: datatype = "bool[] b"; break;
						case Stack.DataType.Float: datatype = "float[] f"; break;
						case Stack.DataType.Int: datatype = "int[] i"; break;
						case Stack.DataType.String: throw new Exception("Strings should have this declared");
						case Stack.DataType.StringPtr: datatype = "*string[] s"; break;
					}*/
					else if (var.Immediatesize == 3)
					{
						datatype = "vector3[] v";
					}
					else datatype = "struct<" + var.Immediatesize.ToString() + ">[] ";
				}
				string value = "";
				if (var.Is_Array)
				{
					value = " = new " + datatype.Remove(datatype.IndexOf("]")) + var.Value.ToString() + "]";
				}
				else
					value = (Listtype == ListType.Statics ? " = " + var.Value.ToString() : "");
				Working.Add(datatype + varlocation + i.ToString() + value + ";");
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
					continue;
				string datatype = "";
				if (!var.Is_Array)
				{
					if (var.DataType == Stack.DataType.String)
					{
						datatype = "char[" + (var.Immediatesize * 4).ToString() + "] c";
					}
					else if (var.Immediatesize == 1)
						datatype = Types.gettype(var.DataType).vardec;
					/*switch (var.DataType)
					{
						case Stack.DataType.Unk: datatype = "var u"; break;
						case Stack.DataType.Bool: datatype = "bool b"; break;
						case Stack.DataType.Float: datatype = "float f"; break;
						case Stack.DataType.Int: datatype = "int i"; break;
						case Stack.DataType.Ped: datatype = "ped p"; break;
						case Stack.DataType.Player: datatype = "player P"; break;
						case Stack.DataType.Object: datatype = "object o"; break;
						case Stack.DataType.Entity: datatype = "entity e"; break;
						case Stack.DataType.Vehicle: datatype = "vehicle v"; break;
						case Stack.DataType.String: throw new Exception("Strings should have this declared");
						case Stack.DataType.StringPtr: datatype = "*string s"; break;
					}*/
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
					/*switch (var.DataType)
					{
						case Stack.DataType.Unk: datatype = "var[] u"; break;
						case Stack.DataType.Bool: datatype = "bool[] b"; break;
						case Stack.DataType.Float: datatype = "float[] f"; break;
						case Stack.DataType.Int: datatype = "int[] i"; break;
						case Stack.DataType.Player: datatype = "player[] P"; break;
						case Stack.DataType.Object: datatype = "object[] o"; break;
						case Stack.DataType.Entity: datatype = "entity[] e"; break;
						case Stack.DataType.Vehicle: datatype = "vehicle[] v"; break;
						case Stack.DataType.String: throw new Exception("Strings should have this declared");
						case Stack.DataType.StringPtr: datatype = "*string[] s"; break;
					}*/
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
            public Var(int index, int Value)
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
            int value;
            int immediatesize;
            bool isArray;
            bool is_used;
            bool isstruct;
            bool iscalled = false;
            Stack.DataType Datatype;
            public int Index { get { return index; } }
            public int Value { get { return value; } set { this.value = value; } }
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
