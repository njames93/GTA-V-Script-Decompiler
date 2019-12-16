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

        public bool IsAggregate { get; private set; } // Stateless variable information.

        public Vars_Info(ListType type, int varcount, bool isAggregate = false)
        {
            Listtype = type;
            Vars = new List<Var>();
            for (int i = 0; i < varcount; i++)
                Vars.Add(new Var(this, i));
            count = varcount;

            IsAggregate = isAggregate;
        }

        public Vars_Info(ListType type, bool isAggregate = false)
        {
            Listtype = type;
            Vars = new List<Var>();

            IsAggregate = isAggregate;
        }

        public void AddVar(int value)
        {
            Vars.Add(new Var(this, Vars.Count, value));//only used for static variables that are pre assigned
        }

        public void AddVar(long value)
        {
            Vars.Add(new Var(this, Vars.Count, value));
        }

        public void checkvars()
        {
            VarRemapper = new Dictionary<int, int>();
            for (int i = 0, k = 0; i < Vars.Count; i++)
            {
                if (!Vars[i].Is_Used)
                    continue;
                if (Listtype == ListType.Vars && !Vars[i].IsCalled)
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
        //This shouldnt be needed but in gamever 1.0.757.2
        //It seems a few of the scripts are accessing items from the
        //Stack frame that they havent reserver
        void broken_check(uint index)
        {
            if (index >= Vars.Count)
            {
                for (int i = Vars.Count; i <= index; i++)
                {
                    Vars.Add(new Var(this, i));
                }
            }
        }

        public string GetVarName(uint index)
        {
            if (IsAggregate)
            {
                switch (Listtype)
                {
                    case ListType.Statics: return (index >= scriptParamStart ? "ScriptParam_" : "Local_");
                    case ListType.Params: return "Param";
                    case ListType.Vars:
                    default:
                        return "Var";
                }
            }
            else
            {
                string name = "";
                Var var = Vars[(int)index];
                if (var.DataType == Stack.DataType.String)
                    name = "c";
                else if (var.Immediatesize == 1)
                    name = var.DataType.ShortName();
                else if (var.Immediatesize == 3)
                    name = "v";

                switch (Listtype)
                {
                    case ListType.Statics: name += (index >= scriptParamStart ? "ScriptParam_" : "Local_"); break;
                    case ListType.Vars: name += "Var"; break;
                    case ListType.Params: name += "Param"; break;
                }

                if (Program.Shift_Variables)
                {
                    if (VarRemapper.ContainsKey((int)index))
                        return name + VarRemapper[(int)index].ToString();
                    else
                        return name + "unknownVar";
                }
                else
                {
                    return name + (Listtype == ListType.Statics && index >= scriptParamStart ? index - scriptParamStart : index).ToString();
                }
            }
        }

        public void SetScriptParamCount(int count)
        {
            if (Listtype == ListType.Statics)
            {
                scriptParamCount = count;
            }
        }

        public string[] GetDeclaration()
        {
            List<string> Working = new List<string>();
            string varlocation = "";
            string datatype = "";

            int i = 0;
            int j = -1;
            foreach (Var var in Vars)
            {
                switch (Listtype)
                {
                    case ListType.Statics:
                        varlocation = (i >= scriptParamStart ? "ScriptParam_" : "Local_");
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
                if (Listtype == ListType.Vars && !var.IsCalled)
                {
                    if (!Program.Shift_Variables)
                        i++;
                    continue;
                }
                if (var.Immediatesize == 1)
                {
                    datatype = var.DataType.VarDeclaration();
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
                                    if (Program.IsBit32)
                                        data.AddRange(BitConverter.GetBytes((int)Vars[j + 1 + var.Immediatesize * k + l].Value));
                                    else
                                        data.AddRange(BitConverter.GetBytes(Vars[j + 1 + var.Immediatesize * k + l].Value));
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

                string decl;
                if (IsAggregate)
                    decl = datatype;
                else
                {
                    decl = datatype + varlocation + (Listtype == ListType.Statics && i >= scriptParamStart ? i - scriptParamStart : i).ToString();
                    if (var.Is_Array)
                        decl += "[" + var.Value.ToString() + "]";
                    if (var.DataType == Stack.DataType.String)
                        decl += "[" + (var.Immediatesize * (Program.IsBit32 ? 4 : 8)).ToString() + "]";
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
                        datatype = "char[" + (var.Immediatesize * 4).ToString() + "] c";
                    else if (var.Immediatesize == 1)
                        datatype = var.DataType.VarDeclaration();
                    else if (var.Immediatesize == 3)
                        datatype = "vector3 v";
                    else
                        datatype = "struct<" + var.Immediatesize.ToString() + "> ";
                }
                else
                {
                    if (var.DataType == Stack.DataType.String)
                        datatype = "char[" + (var.Immediatesize * 4).ToString() + "][] c";
                    else if (var.Immediatesize == 1)
                        datatype = var.DataType.VarArrayDeclaration();
                    else if (var.Immediatesize == 3)
                        datatype = "vector3[] v";
                    else
                        datatype = "struct<" + var.Immediatesize.ToString() + ">[] ";
                }

                if (IsAggregate)
                    decl += "Param, ";
                else
                    decl += datatype + "Param" + i.ToString() + ", ";
                i++;
            }
            if (decl.Length > 2)
                decl = decl.Remove(decl.Length - 2);
            return decl;
        }

        public Stack.DataType GetTypeAtIndex(uint index)
        {
            return Vars[(int)index].DataType;
        }

        public bool SetTypeAtIndex(uint index, Stack.DataType type)
        {
            Stack.DataType prev = Vars[(int)index].DataType;
            if (!type.IsUnknown() && (prev.IsUnknown() || prev.Precedence() < type.Precedence()))
            {
                Vars[(int)index].DataType = type;
                return true;
            }
            return false;
        }

        public Var GetVarAtIndex(uint index)
        {
            broken_check(index);
            return Vars[(int)index];
        }

        public class Var
        {
            private Vars_Info _parent;
            private Stack.DataType _datatype = Stack.DataType.Unk;
            private bool _fixed = false;

            public int Index { get; private set; }
            public long Value { get; set; }
            public int Immediatesize { get; set; } = 1;
            public Stack.DataType DataType {
                get => _datatype;
                set
                {
                    if (_fixed && (value.Precedence() <= _datatype.Precedence())) return;
                    _datatype = value;
                }
            }

            public bool IsStruct { get; private set; } = false;
            public bool Is_Array { get; private set; } = false;
            public bool Is_Used { get; private set; } = true;
            public bool IsCalled { get; private set; } = false;

            public Var(Vars_Info parent, int index)
            {
                _parent = parent;
                Index = index;
                Value = 0;
            }

            public Var(Vars_Info parent, int index, long value)
            {
                _parent = parent;
                Index = index;
                Value = value;
            }

            public Var Fixed()
            {
                if (_parent.VarRemapper == null) return this;
                _fixed = true; return this;
            }

            public void makearray()
            {
                if (!IsStruct)
                    Is_Array = true;
            }

            public void call() { IsCalled = true; }
            public void dontuse() { Is_Used = false; }
            public void makestruct()
            {
                DataType = Stack.DataType.Unk;
                Is_Array = false;
                IsStruct = true;
            }

            public string Name
            {
                get
                {
                    ListType Listtype = _parent.Listtype;
                    int scriptParamStart = _parent.scriptParamStart;

                    if (_parent.IsAggregate)
                    {
                        switch (Listtype)
                        {
                            case ListType.Statics: return (Index >= scriptParamStart ? "ScriptParam_" : "Local_");
                            case ListType.Params: return "Param";
                            case ListType.Vars:
                            default:
                                return "Var";
                        }
                    }
                    else
                    {
                        string name = "";
                        if (DataType == Stack.DataType.String)
                            name = "c";
                        else if (Immediatesize == 1)
                            name = DataType.ShortName();
                        else if (Immediatesize == 3)
                            name = "v";

                        switch (Listtype)
                        {
                            case ListType.Statics: name += (Index >= scriptParamStart ? "ScriptParam_" : "Local_"); break;
                            case ListType.Vars: name += "Var"; break;
                            case ListType.Params: name += "Param"; break;
                        }

                        if (Program.Shift_Variables && _parent.VarRemapper != null)
                        {
                            if (_parent.VarRemapper.ContainsKey((int)Index))
                                return name + _parent.VarRemapper[(int)Index].ToString();
                            else
                                return name + "unknownVar";
                        }
                        else
                        {
                            return name + (Listtype == ListType.Statics && Index >= scriptParamStart ? Index - scriptParamStart : Index).ToString();
                        }
                    }
                }
            }
        }
        public enum ListType
        {
            Statics,
            Params,
            Vars
        }
    }
}
