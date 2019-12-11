using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Decompiler
{
    public class Stack
    {
        List<StackValue> _stack;
        public bool IsAggregate { get; private set; } // Stateless stack information.

        public DataType TopType
        {
            get
            {
                if (_stack.Count == 0)
                    return DataType.Unk;
                return _stack[_stack.Count - 1].Datatype;
            }
        }

        public Stack(bool isAggregate = false)
        {
            _stack = new List<StackValue>();
            IsAggregate = isAggregate;
        }

        public void Dispose()
        {
            _stack.Clear();
        }

        public void Push(string value, DataType Datatype = DataType.Unk)
        {
            _stack.Add(new StackValue(StackValue.Type.Literal, value, Datatype));
        }

        public void PushGlobal(string value)
        {
            _stack.Add(StackValue.Global(StackValue.Type.Literal, value));
        }

        public void PushPGlobal(string value)
        {
            _stack.Add(StackValue.Global(StackValue.Type.Pointer, value));
        }

        private void PushCond(string value)
        {
            _stack.Add(new StackValue(StackValue.Type.Literal, value, DataType.Bool));
        }

        private void Push(StackValue item)
        {
            _stack.Add(item);
        }

        public void PushString(string value)
        {
            _stack.Add(new StackValue(StackValue.Type.Literal, value.ToString(), DataType.StringPtr));
        }

        public void Push(params int[] values)
        {
            foreach (int value in values)
            {
                switch (Program.getIntType)
                {
                    case Program.IntType._int:
                    case Program.IntType._hex:
                        _stack.Add(new StackValue(StackValue.Type.Literal, Hashes.inttohex(value), DataType.Int));
                        break;
                    case Program.IntType._uint:
                        _stack.Add(new StackValue(StackValue.Type.Literal, unchecked((uint)value).ToString(), DataType.Int));
                        break;
                }
            }
        }

        public void PushHexInt(uint value)
        {
            _stack.Add(new StackValue(StackValue.Type.Literal, Utils.FormatHexHash(value), DataType.Int));
        }

        public void PushVar(string value, Vars_Info.Var Variable)
        {
            if (Variable.Immediatesize == 3)
            {
                value += ".x";
            }
            _stack.Add(new StackValue(StackValue.Type.Literal, value, Variable));
        }

        public void PushPVar(string value, Vars_Info.Var Variable)
        {
            _stack.Add(new StackValue(StackValue.Type.Pointer, value, Variable));
        }

        public void Push(float value)
        {
            _stack.Add(new StackValue(StackValue.Type.Literal, value.ToString(CultureInfo.InvariantCulture) + "f", DataType.Float));
        }

        public void PushPointer(string value)
        {
            _stack.Add(new StackValue(StackValue.Type.Pointer, value));
        }

        private void PushStruct(string value, int size)
        {
            _stack.Add(new StackValue(value, size));
        }

        private void PushVector(string value)
        {
            _stack.Add(new StackValue(value, 3, true));
        }

        private void PushString(string value, int size)
        {
            _stack.Add(new StackValue(size, value));
        }

        private StackValue Pop()
        {
            int index = _stack.Count - 1;
            if (index < 0)
                return new StackValue(StackValue.Type.Literal, "StackVal");
            StackValue val = _stack[index];
            _stack.RemoveAt(index);
            return val;
        }

        public object Drop()
        {
            StackValue val = Pop();
            if (val.Value.Contains("(") && val.Value.EndsWith(")"))
                if (val.Value.IndexOf("(") > 4)
                    return val.Value.ToString() + ";";
            return null;
        }

        private StackValue[] PopList(int size)
        {
            int count = 0;
            List<StackValue> items = new List<StackValue>();
            while (count < size)
            {
                StackValue top = Pop();
                switch (top.ItemType)
                {
                    case StackValue.Type.Literal:
                    {
                        items.Add(top);
                        count++;
                        break;
                    }
                    case StackValue.Type.Pointer:
                    {
                        if (top.isNotVar)
                            items.Add(new StackValue(StackValue.Type.Literal, "&(" + top.Value + ")"));
                        else
                            items.Add(new StackValue(StackValue.Type.Literal, "&" + top.Value));
                        count++;
                        break;
                    }
                    case StackValue.Type.Struct:
                    {
                        if (count + top.StructSize > size)
                            throw new Exception("Struct size too large");
                        count += top.StructSize;
                        items.Add(new StackValue(StackValue.Type.Literal, top.Value));
                        break;
                    }
                    default:
                        throw new Exception("Unexpected Stack Type: " + top.ItemType.ToString());
                }
            }

            items.Reverse();
            return items.ToArray();
        }

        private StackValue[] PopTest(int size)
        {
            int count = 0;
            List<StackValue> items = new List<StackValue>();
            while (count < size)
            {
                StackValue top = Pop();
                switch (top.ItemType)
                {
                    case StackValue.Type.Literal:
                    {
                        items.Add(top);
                        count++;
                        break;
                    }
                    case StackValue.Type.Pointer:
                    {
                        if (top.isNotVar)
                            items.Add(new StackValue(StackValue.Type.Literal, "&(" + top.Value + ")"));
                        else
                            items.Add(new StackValue(StackValue.Type.Literal, "&" + top.Value));
                        count++;
                        break;
                    }
                    case StackValue.Type.Struct:
                    {
                        if (count + top.StructSize > size)
                            throw new Exception("Struct size too large");
                        count += top.StructSize;
                        items.Add(new StackValue(top.Value, top.StructSize));
                        break;
                    }
                    default:
                        throw new Exception("Unexpected Stack Type: " + top.ItemType.ToString());
                }
            }

            items.Reverse();
            return items.ToArray();
        }

        private string PopVector()
        {
            StackValue[] data = PopList(3);
            switch (data.Length)
            {
                case 1:
                    return data[0].Value;
                case 3:
                    return "Vector(" + data[2].Value + ", " + data[1].Value + ", " + data[0].Value + ")";
                case 2:
                    return "Vector(" + data[1].Value + ", " + data[0].Value + ")";
            }
            throw new Exception("Unexpected data length");
        }

        private StackValue Peek()
        {
            return _stack[_stack.Count - 1];
        }

        public void Dup()
        {
            StackValue top = Peek();
            if (top.Value.Contains("(") && top.Value.Contains(")"))
                Push("Stack.Peek()");
            else
                Push(top);
        }

        public string PopLit()
        {
            StackValue val = Pop();
            if (val.ItemType != StackValue.Type.Literal)
            {
                if (val.ItemType == StackValue.Type.Pointer)
                {
                    return "&" + val.Value;
                }
                else
                    throw new Exception("Not a literal item recieved");
            }
            return val.Value;
        }

        private string PeekLit()
        {
            StackValue val = Peek();
            if (val.ItemType != StackValue.Type.Literal)
            {
                if (val.ItemType == StackValue.Type.Pointer)
                {
                    return "&" + val.Value;
                }
                else
                    throw new Exception("Not a literal item recieved");
            }
            return val.Value;
        }

        private string PeekPointerRef()
        {
            StackValue val = Peek();
            if (val.ItemType == StackValue.Type.Pointer)
                return val.Value;
            else if (val.ItemType == StackValue.Type.Literal)
                return "*(" + val.Value + ")";
            throw new Exception("Not a pointer item recieved");
        }

        private string PopPointer()
        {
            StackValue val = Pop();
            if (val.ItemType == StackValue.Type.Pointer)
            {
                if (val.isNotVar)
                    return "&(" + val.Value + ")";
                else
                    return "&" + val.Value;
            }
            else if (val.ItemType == StackValue.Type.Literal)
                return val.Value;
            throw new Exception("Not a pointer item recieved");
        }

        private string PopPointerRef()
        {
            StackValue val = Pop();
            if (val.ItemType == StackValue.Type.Pointer)
                return val.Value;
            else if (val.ItemType == StackValue.Type.Literal)
                return "*" + (val.Value.Contains(" ") ? "(" + val.Value + ")" : val.Value);
            throw new Exception("Not a pointer item recieved");
        }

        public string PopListForCall(int size)
        {
            if (size == 0)
                return "";
            string items = "";
            foreach (StackValue val in PopList(size))
            {
                switch (val.ItemType)
                {
                    case StackValue.Type.Literal:
                        items += val.Value + ", ";
                        break;
                    case StackValue.Type.Pointer:
                    {
                        if (val.isNotVar)
                            items += "&(" + val.Value + "), ";
                        else
                            items += "&" + val.Value + ", ";
                        break;
                    }
                    case StackValue.Type.Struct:
                        items += val.Value + ", ";
                        break;
                    default:
                        throw new Exception("Unexpeced Stack Type\n" + val.ItemType.ToString());
                }
            }
            return items.Remove(items.Length - 2);
        }

        private string[] EmptyStack()
        {
            List<string> stack = new List<string>();
            foreach (StackValue val in _stack)
            {
                switch (val.ItemType)
                {
                    case StackValue.Type.Literal:
                        stack.Add(val.Value);
                        break;
                    case StackValue.Type.Pointer:
                    {
                        if (val.isNotVar)
                            stack.Add("&(" + val.Value + ")");
                        else
                            stack.Add("&" + val.Value);
                        break;
                    }
                    case StackValue.Type.Struct:
                        stack.Add(val.Value);
                        break;
                    default:
                        throw new Exception("Unexpeced Stack Type\n" + val.ItemType.ToString());
                }
            }
            _stack.Clear();
            return stack.ToArray();
        }

        public string FunctionCall(string name, int pcount, int rcount)
        {
            string functionline = (IsAggregate ? "func_" : name) + "(" + PopListForCall(pcount) + ")";
            if (rcount == 0)
                return functionline + ";";
            else if (rcount == 1)
                Push(functionline);
            else if (rcount > 1)
                PushStruct(functionline, rcount);
            else
                throw new Exception("Error in return items count");
            return "";
        }

        public string FunctionCall(Function function)
        {
            string functionline = function.Name + "(" + PopListForCall(function.Pcount) + ")";
            if (IsAggregate) functionline = "func_()"; // Burn the PopList call.
            if (function.Rcount == 0)
                return functionline + ";";
            else if (function.Rcount == 1)
                Push(new StackValue(StackValue.Type.Literal, functionline, function));
            else if (function.Rcount > 1)
                PushStruct(functionline, function.Rcount);
            else
                throw new Exception("Error in return items count");
            return "";
        }

        public string NativeCallTest(ulong hash, string name, int pcount, int rcount)
        {
            Native native;
            if (!Program.X64npi.FetchNativeCall(hash, name, pcount, rcount, out native))
                throw new Exception("Unknown Exception for Hash: " + hash.ToString("X"));

            string functionline = name + "(";
            List<DataType> _params = new List<DataType>();
            int count = 0;
            foreach (StackValue val in PopTest(pcount))
            {
                switch (val.ItemType)
                {
                    case StackValue.Type.Literal:
                    {
                        if (val.Variable != null)
                        {
                            if (val.Variable.DataType.Precedence() < native.Params[count].StackType.Precedence())
                            {
                                val.Variable.DataType = native.Params[count].StackType;
                            }
                            else if (val.Variable.DataType.Precedence() > native.Params[count].StackType.Precedence())
                            {
                                Program.X64npi.UpdateParam(hash, val.Variable.DataType, count);
                            }
                        }
                        if (val.Datatype == DataType.Bool || native.Params[count].StackType == DataType.Bool)
                        {
                            bool temp;
                            if (bool.TryParse(val.Value, out temp))
                                functionline += temp ? "true, " : "false, ";
                            else if (val.Value == "0")
                                functionline += "false, ";
                            else if (val.Value == "1")
                                functionline += "true, ";
                            else
                                functionline += val.Value + ", ";
                        }
                        else if (val.Datatype == DataType.Int && native.Params[count].StackType == DataType.Float)
                        {
                            switch (Program.getIntType)
                            {
                                case Program.IntType._int:
                                {
                                    int temp;
                                    if (int.TryParse(val.Value, out temp))
                                    {
                                        temp = Utils.SwapEndian(temp);
                                        float floatval = Utils.SwapEndian(BitConverter.ToSingle(BitConverter.GetBytes(temp), 0));
                                        functionline += floatval.ToString(CultureInfo.InvariantCulture) + "f, ";
                                    }
                                    else
                                        functionline += val.Value + ", ";
                                    break;
                                }
                                case Program.IntType._uint:
                                {
                                    uint tempu;
                                    if (uint.TryParse(val.Value, out tempu))
                                    {
                                        tempu = Utils.SwapEndian(tempu);
                                        float floatval = Utils.SwapEndian(BitConverter.ToSingle(BitConverter.GetBytes(tempu), 0));
                                        functionline += floatval.ToString(CultureInfo.InvariantCulture) + "f, ";
                                    }
                                    else
                                        functionline += val.Value + ", ";
                                    break;
                                }
                                case Program.IntType._hex:
                                {
                                    int temp;
                                    string temps = val.Value;
                                    if (temps.StartsWith("0x"))
                                        temps = temps.Substring(2);
                                    if (int.TryParse(temps, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out temp))
                                    {
                                        temp = Utils.SwapEndian(temp);
                                        float floatval = Utils.SwapEndian(BitConverter.ToSingle(BitConverter.GetBytes(temp), 0));
                                        functionline += floatval.ToString(CultureInfo.InvariantCulture) + "f, ";
                                    }
                                    else
                                        functionline += val.Value + ", ";
                                    break;
                                }
                                default:
                                    throw new System.ArgumentException("Invalid IntType");
                            }
                        }
                        else
                            functionline += val.Value + ", ";
                        _params.Add(val.Datatype);
                        count++;
                        break;
                    }
                    case StackValue.Type.Pointer:
                    {
                        functionline += val.isNotVar ? ("&(" + val.Value + "), ") : ("&" + val.Value + ", ");
                        if (val.Datatype.PointerType() != Stack.DataType.Unk)
                            _params.Add(val.Datatype.PointerType());
                        else
                            _params.Add(val.Datatype);
                        count++;
                        break;
                    }
                    case StackValue.Type.Struct:
                    {
                        functionline += val.Value + ", ";
                        if (val.StructSize == 3 && val.Datatype == DataType.Vector3)
                        {
                            _params.AddRange(new DataType[] { DataType.Float, DataType.Float, DataType.Float });
                            count += 3;
                        }
                        else
                        {
                            for (int i = 0; i < val.StructSize; i++)
                            {
                                _params.Add(DataType.Unk);
                                count++;
                            }
                        }
                        break;
                    }
                    default:
                        throw new Exception("Unexpeced Stack Type\n" + val.ItemType.ToString());
                }
            }
            if (pcount > 0)
                functionline = functionline.Remove(functionline.Length - 2) + ")";
            else
                functionline += ")";
            if (rcount == 0)
            {
                Program.X64npi.UpdateNative(hash, DataType.None, _params.ToArray());
                return functionline + ";";
            }
            else if (rcount == 1)
            {
                Program.X64npi.UpdateNative(hash, Program.X64npi.GetReturnType(hash), _params.ToArray());
                PushNative(functionline, hash, Program.X64npi.GetReturnType(hash));
            }
            else if (rcount > 1)
            {
                if (rcount == 2)
                    Program.X64npi.UpdateNative(hash, DataType.Unk, _params.ToArray());
                else if (rcount == 3)
                    Program.X64npi.UpdateNative(hash, DataType.Vector3, _params.ToArray());
                else
                    throw new Exception("Error in return items count");
                PushStructNative(functionline, hash, rcount, Program.X64npi.GetReturnType(hash));
            }
            else
                throw new Exception("Error in return items count");
            return "";
        }

        #region Opcodes

        public void Op_Add()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop();
            if (s1.ItemType == StackValue.Type.Literal && s2.ItemType == StackValue.Type.Literal)
            {
                Push("(" + s2.Value + " + " + s1.Value + ")", DataType.Int);
                return;
            }
            if (s2.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Literal)
            {
                Push("(&" + s2.Value + " + " + s1.Value + ")", DataType.Unk);
                return;
            }
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Literal)
            {
                Push("(&" + s1.Value + " + " + s2.Value + ")", DataType.Unk);
                return;
            }
            throw new Exception("Unexpected stack value");
        }

        public void Op_Addf()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " + " + s1 + ")", DataType.Float);
        }

        public void Op_Sub()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop();
            if (s1.ItemType == StackValue.Type.Literal && s2.ItemType == StackValue.Type.Literal)
            {
                Push("(" + s2.Value + " - " + s1.Value + ")", DataType.Int);
                return;
            }
            if (s2.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Literal)
            {
                Push("(&" + s2.Value + " - " + s1.Value + ")", DataType.Unk);
                return;
            }
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Literal)
            {
                Push("(&" + s1.Value + " - " + s2.Value + ")", DataType.Unk);
                return;
            }
            throw new Exception("Unexpected stack value");
        }

        public void Op_Subf()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " - " + s1 + ")", DataType.Float);
        }

        public void Op_Mult()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " * " + s1 + ")", DataType.Int);
        }

        public void Op_Multf()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " * " + s1 + ")", DataType.Float);
        }

        public void Op_Div()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " / " + s1 + ")", DataType.Int);
        }

        public void Op_Divf()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " / " + s1 + ")", DataType.Float);
        }

        public void Op_Mod()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " % " + s1 + ")", DataType.Int);
        }

        public void Op_Modf()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push("(" + s2 + " % " + s1 + ")", DataType.Float);
        }

        public void Op_Not()
        {
            string s1;
            s1 = PopLit();
            if (s1.StartsWith("!(") && s1.EndsWith(")"))
                PushCond(s1.Remove(s1.Length - 1).Substring(2));
            else if (s1.StartsWith("(") && s1.EndsWith(")"))
                PushCond("!" + s1);
            else if (!(s1.Contains("&&") && s1.Contains("||") && s1.Contains("^")))
            {
                if (s1.StartsWith("!"))
                    PushCond(s1.Substring(1));
                else
                    PushCond("!" + s1);
            }
            else
                PushCond("!(" + s1 + ")");
        }

        public void Op_Neg()
        {
            string s1;
            s1 = PopLit();
            Push("-" + s1, DataType.Int);
        }

        public void Op_Negf()
        {
            string s1;
            s1 = PopLit();
            Push("-" + s1, DataType.Float);
        }

        public void Op_CmpEQ()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " == " + s1);

        }

        public void Op_CmpNE()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " != " + s1);
        }

        public void Op_CmpGE()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " >= " + s1);
        }

        public void Op_CmpGT()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " > " + s1);
        }

        public void Op_CmpLE()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " <= " + s1);
        }

        public void Op_CmpLT()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            PushCond(s2 + " < " + s1);
        }

        public void Op_Vadd()
        {
            string s1, s2;
            s1 = PopVector();
            s2 = PopVector();
            PushVector(s2 + " + " + s1);
        }

        public void Op_VSub()
        {
            string s1, s2;
            s1 = PopVector();
            s2 = PopVector();
            PushVector(s2 + " - " + s1);
        }

        public void Op_VMult()
        {
            string s1, s2;
            s1 = PopVector();
            s2 = PopVector();
            PushVector(s2 + " * " + s1);
        }

        public void Op_VDiv()
        {
            string s1, s2;
            s1 = PopVector();
            s2 = PopVector();
            PushVector(s2 + " / " + s1);
        }

        public void Op_VNeg()
        {
            string s1;
            s1 = PopVector();
            PushVector("-" + s1);
        }

        public void Op_FtoV()
        {
            StackValue top = Pop();
            if (top.Value.Contains("(") && top.Value.Contains(")"))
                PushVector("FtoV(" + top.Value + ")");
            else
            {
                Push(top.Value, DataType.Float);
                Push(top.Value, DataType.Float);
                Push(top.Value, DataType.Float);
            }

        }

        public void Op_Itof()
        {
            Push("IntToFloat(" + PopLit() + ")", DataType.Float);
        }

        public void Op_FtoI()
        {
            Push("FloatToInt(" + PopLit() + ")", DataType.Int);
        }

        public void Op_And()
        {
            StackValue s1 = Pop();
            StackValue s2 = Pop();
            int temp;
            if (s1.ItemType != StackValue.Type.Literal && s2.ItemType != StackValue.Type.Literal)
                throw new Exception("Not a literal item recieved");
            if (s1.Datatype == DataType.Bool || s2.Datatype == DataType.Bool)
                PushCond("(" + s2.Value + " && " + s1.Value + ")");
            else if (Utils.IntParse(s1.Value, out temp) || Utils.IntParse(s2.Value, out temp))
                Push(s2.Value + " & " + s1.Value, DataType.Int);
            else
                Push("(" + s2.Value + " && " + s1.Value + ")");
        }

        public void Op_Or()
        {
            StackValue s1 = Pop();
            StackValue s2 = Pop();
            int temp;
            if (s1.ItemType != StackValue.Type.Literal && s2.ItemType != StackValue.Type.Literal)
                throw new Exception("Not a literal item recieved");
            if (s1.Datatype == DataType.Bool || s2.Datatype == DataType.Bool)
                PushCond("(" + s2.Value + " || " + s1.Value + ")");
            else if (Utils.IntParse(s1.Value, out temp) || Utils.IntParse(s2.Value, out temp))
                Push(s2.Value + " | " + s1.Value, DataType.Int);
            else
                Push("(" + s2.Value + " || " + s1.Value + ")");
        }

        public void Op_Xor()
        {
            string s1, s2;
            s1 = PopLit();
            s2 = PopLit();
            Push(s2 + " ^ " + s1, DataType.Int);
        }

        string PopStructAccess()
        {
            StackValue val = Pop();
            if (val.ItemType == StackValue.Type.Pointer)
                return val.Value + ".";
            else if (val.ItemType == StackValue.Type.Literal)
                return (val.Value.Contains(" ") ? "(" + val.Value + ")" : val.Value) + "->";
            throw new Exception("Not a pointer item recieved");
        }

        public void Op_GetImm(uint immediate)
        {
            if (PeekVar(0)?.Immediatesize == 3)
            {
                switch (immediate)
                {
                    case 1:
                    {
                        string saccess = PopStructAccess();
                        if (IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                            Push(new StackValue(StackValue.Type.Literal, saccess));
                        else
                            Push(new StackValue(StackValue.Type.Literal, saccess + "y"));
                        return;
                    }
                    case 2:
                    {
                        string saccess = PopStructAccess();
                        if (IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                            Push(new StackValue(StackValue.Type.Literal, saccess));
                        else
                            Push(new StackValue(StackValue.Type.Literal, saccess + "z"));
                        return;
                    }
                }
            }

            string structAss = PopStructAccess();
            if (IsAggregate)
            {
                if (Agg.Instance.CanAggregateLiteral(structAss))
                    Push(new StackValue(StackValue.Type.Literal, structAss + "f_"));
                else
                    Push(new StackValue(StackValue.Type.Literal, structAss + "f_" + (Program.Hex_Index ? immediate.ToString("X") : immediate.ToString())));
            }
            else
            {
                Push(new StackValue(StackValue.Type.Literal, structAss + "f_" + (Program.Hex_Index ? immediate.ToString("X") : immediate.ToString())));
            }
        }

        public string Op_SetImm(uint immediate)
        {
            string pointer = PopStructAccess();
            string value = PopLit();

            string imm = "";
            if (IsAggregate && Agg.Instance.CanAggregateLiteral(value))
                imm = "f_";
            else
            {
                imm = "f_" + (Program.Hex_Index ? immediate.ToString("X") : immediate.ToString());
                if (PeekVar(0) != null)
                {
                    if (PeekVar(0).Immediatesize == 3)
                    {
                        switch (immediate)
                        {
                            case 1: imm = "y"; break;
                            case 2: imm = "z"; break;
                        }
                    }
                }
            }
            return setcheck(pointer + imm, value);
        }

        public void Op_GetImmP(uint immediate)
        {
            string saccess = PopStructAccess();
            if (IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                Push(new StackValue(StackValue.Type.Pointer, saccess + "f_"));
            else
                Push(new StackValue(StackValue.Type.Pointer, saccess + "f_" + (Program.Hex_Index ? immediate.ToString("X") : immediate.ToString())));
        }

        public void Op_GetImmP()
        {
            string immediate = PopLit();
            string saccess = PopStructAccess();

            int temp;
            if (IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
            {
                if (Utils.IntParse(immediate, out temp))
                    Push(new StackValue(StackValue.Type.Pointer, saccess + "f_"));
                else
                    Push(new StackValue(StackValue.Type.Pointer, saccess + "f_[]"));
            }
            else
            {
                if (Utils.IntParse(immediate, out temp))
                    Push(new StackValue(StackValue.Type.Pointer, saccess + "f_" + (Program.Hex_Index ? temp.ToString("X") : temp.ToString())));
                else
                    Push(new StackValue(StackValue.Type.Pointer, saccess + "f_[" + immediate + "]"));
            }
        }

        /// <summary>
        /// returns a string saying the size of an array if its > 1
        /// </summary>
        /// <param name="immediate"></param>
        /// <returns></returns>
        private string getarray(uint immediate)
        {
            if (!Program.Show_Array_Size)
                return "";
            if (immediate == 1)
                return "";
            if (IsAggregate)
                return "";
            return " /*" + immediate.ToString() + "*/";
        }

        public string PopArrayAccess()
        {
            StackValue val = Pop();
            if (val.ItemType == StackValue.Type.Pointer)
                return val.Value;
            else if (val.ItemType == StackValue.Type.Literal)
                return $"(*{val.Value})";
            throw new Exception("Not a pointer item recieved");
        }

        public void Op_ArrayGet(uint immediate)
        {
            string arrayloc = PopArrayAccess();
            string index = PopLit();
            Push(new StackValue(StackValue.Type.Literal, arrayloc + "[" + index + getarray(immediate) + "]"));
        }

        public string Op_ArraySet(uint immediate)
        {
            string arrayloc = PopArrayAccess();
            string index = PopLit();
            string value = PopLit();
            return setcheck(arrayloc + "[" + index + getarray(immediate) + "]", value);
        }

        public void Op_ArrayGetP(uint immediate)
        {
            string arrayloc;
            string index;
            if (Peek().ItemType == StackValue.Type.Pointer)
            {
                arrayloc = PopArrayAccess();
                index = PopLit();
                Push(new StackValue(StackValue.Type.Pointer, arrayloc + "[" + index + getarray(immediate) + "]"));
            }
            else if (Peek().ItemType == StackValue.Type.Literal)
            {
                arrayloc = PopLit();
                index = PopLit();
                Push(new StackValue(StackValue.Type.Literal, arrayloc + "[" + index + getarray(immediate) + "]"));
            }
            else throw new Exception("Unexpected Stack Value :" + Peek().ItemType.ToString());

        }

        public void Op_RefGet()
        {
            Push(new StackValue(StackValue.Type.Literal, PopPointerRef()));
        }

        public void Op_ToStack()
        {
            string pointer, count;
            int amount;
            if (TopType == DataType.StringPtr || TopType == DataType.String)
            {
                pointer = PopPointerRef();
                count = PopLit();

                if (!Utils.IntParse(count, out amount))
                    throw new Exception("Expecting the amount to push");
                PushString(pointer, amount);
            }
            else
            {
                pointer = PopPointerRef();
                count = PopLit();

                if (!Utils.IntParse(count, out amount))
                    throw new Exception("Expecting the amount to push");
                PushStruct(pointer, amount);
            }
        }

        int GetIndex(int index)
        {
            int actindex = 0;
            if (_stack.Count == 0)
            {
                return -1;
            }
            for (int i = 0; i < index; i++)
            {
                int stackIndex = _stack.Count - i - 1;
                if (stackIndex < 0)
                    return -1;
                if (_stack[stackIndex].ItemType == StackValue.Type.Struct)
                {
                    index -= _stack[stackIndex].StructSize - 1;
                }
                if (i < index)
                    actindex++;
            }
            return actindex < _stack.Count ? actindex : -1;
        }

        public string PeekItem(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return "";
            }
            StackValue val = _stack[_stack.Count - newIndex - 1];
            if (val.ItemType != StackValue.Type.Literal)
            {
                if (val.ItemType == StackValue.Type.Pointer)
                {
                    return "&" + val.Value;
                }
                else
                    throw new Exception("Not a literal item recieved");
            }
            return val.Value;
        }

        public Vars_Info.Var PeekVar(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return null;
            }
            return _stack[_stack.Count - newIndex - 1].Variable;
        }

        public Function PeekFunc(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return null;
            }
            return _stack[_stack.Count - newIndex - 1].Function;
        }

        public ulong PeekNat64(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return 0;
            }
            return _stack[_stack.Count - newIndex - 1].X64NatHash;
        }

        public bool isnat(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return false;
            }
            return _stack[_stack.Count - newIndex - 1].isNative;
        }

        public bool isPointer(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return false;
            }
            return _stack[_stack.Count - newIndex - 1].ItemType == StackValue.Type.Pointer;
        }

        public bool isLiteral(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return false;
            }
            return _stack[_stack.Count - newIndex - 1].ItemType == StackValue.Type.Literal;
        }

        public void PushNative(string value, uint hash, DataType type)
        {
            Push(new StackValue(value, hash, type));
        }

        public void PushNative(string value, ulong hash, DataType type)
        {
            Push(new StackValue(value, hash, type));
        }

        public void PushStructNative(string value, uint hash, int structsize, DataType dt = DataType.Unk)
        {
            Push(new StackValue(value, structsize, hash, dt));
        }

        public void PushStructNative(string value, ulong hash, int structsize, DataType dt = DataType.Unk)
        {
            Push(new StackValue(value, structsize, hash, dt));
        }

        public DataType ItemType(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return DataType.Unk;
            }
            return _stack[_stack.Count - newIndex - 1].Datatype;
        }

        public string Op_FromStack()
        {
            string pointer, count;
            pointer = PopPointerRef();
            count = PopLit();
            int amount;
            if (!Utils.IntParse(count, out amount))
                throw new Exception("Expecting the amount to push");
            string res = pointer + " = { ";
            foreach (StackValue val in PopList(amount))
                res += val.Value + ", ";
            return res.Remove(res.Length - 2) + " };";
        }

        public void Op_AmmImm(int immediate)
        {
            if (immediate < 0)
            {
                Push(PopLit() + " - " + (-immediate).ToString());
            }
            else if (immediate == 0)
            { }
            else
                Push(PopLit() + " + " + immediate.ToString());
        }

        public void Op_MultImm(int immediate)
        {
            Push(PopLit() + " * " + immediate.ToString());
        }

        public string Op_RefSet()
        {
            string pointer, value;
            pointer = PopPointerRef();
            value = PopLit();
            return setcheck(pointer, value);
        }

        public string Op_PeekSet()
        {
            string pointer, value;

            value = PopLit();
            pointer = PeekPointerRef();
            return setcheck(pointer, value);
        }

        public string Op_Set(string location)
        {
            return setcheck(location, PopLit());
        }

        public string Op_Set(string location, Vars_Info.Var Variable)
        {
            if (Variable.Immediatesize == 3)
            {
                location += ".x";
            }
            return Op_Set(location);
        }

        public void Op_Hash()
        {
            Push("Hash(" + PopLit() + ")", DataType.Int);
        }

        public string op_strcopy(int size)
        {
            string pointer = PopPointer();
            string pointer2 = PopPointer();
            return "StringCopy(" + pointer + ", " + pointer2 + ", " + size.ToString() + ");";
        }

        public string op_stradd(int size)
        {
            string pointer = PopPointer();
            string pointer2 = PopPointer();
            return "StringConCat(" + pointer + ", " + pointer2 + ", " + size.ToString() + ");";
        }

        public string op_straddi(int size)
        {
            string pointer = PopPointer();
            string inttoadd = PopLit();
            return "StringIntConCat(" + pointer + ", " + inttoadd + ", " + size.ToString() + ");";
        }

        public string op_itos(int size)
        {
            string pointer = PopPointer();
            string intval = PopLit();
            return "IntToString(" + pointer + ", " + intval + ", " + size.ToString() + ");";
        }

        public string op_sncopy()
        {
            string pointer = PopPointer();
            string value = PopLit();
            string count = PopLit();
            int amount;
            if (!Utils.IntParse(count, out amount))
                throw new Exception("Int Stack value expected");
            return "MemCopy(" + pointer + ", " + "{" + PopListForCall(amount) + "}, " + value + ");";
        }

        public string[] pcall()
        {
            List<string> temp = new List<string>();
            string loc = PopLit();
            foreach (string s in EmptyStack())
            {
                temp.Add("Stack.Push(" + s + ");");
            }
            temp.Add("Call_Loc(" + loc + ");");
            return temp.ToArray();
        }

        /// <summary>
        /// Detects if you can use var++, var *= num etc
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string setcheck(string loc, string value)
        {
            if (!value.StartsWith(loc + " "))
                return loc + " = " + value + ";";

            string temp = value.Substring(loc.Length + 1);
            string op = temp.Remove(temp.IndexOf(' '));
            string newval = temp.Substring(temp.IndexOf(' ') + 1);
            if (newval == "1" || newval == "1f")
            {
                if (op == "+")
                    return loc + "++;";
                if (op == "-")
                    return loc + "--;";
            }
            return loc + " " + op + "= " + newval + ";";
        }

        #endregion

        #region subclasses

        public enum DataType
        {
            Int,
            IntPtr,
            Float,
            FloatPtr,
            String,
            StringPtr,
            Bool,
            BoolPtr,
            Unk,
            UnkPtr,
            Unsure,
            None, //For Empty returns
            Vector3,
            Vector3Ptr,
        }

        private class StackValue
        {
            public enum Type
            {
                Literal,
                Pointer,
                Struct
            }

            string _value;
            Type _type;
            int _structSize;
            DataType _datatype;
            Vars_Info.Var _var = null;
            ulong _xhash = 0;
            bool global = false;
            Function _function = null;

            public StackValue(Type type, string value)
            {
                _type = type;
                _value = value;
                _structSize = 0;
                _datatype = DataType.Unk;
            }

            public StackValue(Type type, string name, Vars_Info.Var var)
            {
                _type = type;
                _value = name;
                _structSize = 0;
                _datatype = var.DataType;
                _var = var;
            }

            public StackValue(Type type, string name, Function function)
            {
                _type = type;
                _value = name;
                _structSize = 0;
                _datatype = function.ReturnType;
                _function = function;
            }

            public StackValue(Type type, string value, DataType datatype)
            {
                _type = type;
                _value = value;
                _structSize = 0;
                _datatype = datatype;
            }

            public StackValue(string value, ulong hash, DataType datatype)
            {
                _type = Type.Literal;
                _value = value;
                _structSize = 0;
                _xhash = hash;
                _datatype = datatype;
            }

            public StackValue(string value, int structsize, ulong hash, DataType datatype = DataType.Unk)
            {
                _type = Type.Struct;
                _value = value;
                _structSize = structsize;
                _xhash = hash;
                _datatype = datatype;
            }

            public StackValue(string value, int structsize, bool Vector = false)
            {
                _type = Type.Struct;
                _value = value;
                _structSize = structsize;
                _datatype = (Vector && structsize == 3) ? DataType.Vector3 : DataType.Unk;
            }

            public StackValue(int stringsize, string value)
            {
                _type = Type.Struct;
                _value = value;
                _structSize = stringsize;
                _datatype = DataType.String;
            }

            public static StackValue Global(Type type, string name)
            {
                StackValue G = new StackValue(type, name);
                G.global = true;
                return G;
            }

            public string Value { get { return _value; } }

            public Type ItemType { get { return _type; } }

            public int StructSize { get { return _structSize; } }

            public DataType Datatype { get { return _datatype; } }

            public Vars_Info.Var Variable { get { return _var; } }

            public Function Function { get { return _function; } }

            public bool isNative { get { return _xhash != 0; } }

            public ulong X64NatHash { get { return _xhash; } }

            public bool isNotVar { get { return Variable == null && !global; } }

        }

        [Serializable]
        private class StackEmptyException : Exception
        {
            public StackEmptyException() : base() { }

            public StackEmptyException(string message) : base(message) { }

            public StackEmptyException(string message, Exception innerexception) : base(message, innerexception) { }
        }

        #endregion
    }

    public static class DataTypeExtensions
    {

        public static bool IsUnknown(this Stack.DataType c) { return (c == Stack.DataType.Unk || c == Stack.DataType.UnkPtr); }
        public static string ReturnType(this Stack.DataType c) { return LongName(c) + " "; }
        public static string VarArrayDeclaration(this Stack.DataType c) { return LongName(c) + "[] " + ShortName(c); }
        public static string VarDeclaration(this Stack.DataType c) { return LongName(c) + " " + ShortName(c); }

        /// <summary>
        ///
        /// </summary>
        public static Stack.DataType PointerType(this Stack.DataType c)
        {
            switch (c)
            {
                case Stack.DataType.Int: return Stack.DataType.IntPtr;
                case Stack.DataType.Unk: return Stack.DataType.UnkPtr;
                case Stack.DataType.Float: return Stack.DataType.FloatPtr;
                case Stack.DataType.Bool: return Stack.DataType.BoolPtr;
                case Stack.DataType.Vector3: return Stack.DataType.Vector3Ptr;
                default: return Stack.DataType.Unk;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static Stack.DataType BaseType(this Stack.DataType c)
        {
            switch (c)
            {
                case Stack.DataType.IntPtr: return Stack.DataType.Int;
                case Stack.DataType.UnkPtr: return Stack.DataType.Unk;
                case Stack.DataType.FloatPtr: return Stack.DataType.Float;
                case Stack.DataType.BoolPtr: return Stack.DataType.Bool;
                case Stack.DataType.Vector3Ptr: return Stack.DataType.Vector3;
                default: return Stack.DataType.Unk;
            }
        }

        /// <summary>
        /// Conversion of stack datatypes to precedence integers.
        /// </summary>
        public static int Precedence(this Stack.DataType c)
        {
            switch (c)
            {
                case Stack.DataType.Unsure:
                case Stack.DataType.UnkPtr:
                    return 1;
                case Stack.DataType.Vector3:
                    return 2;
                case Stack.DataType.BoolPtr:
                case Stack.DataType.Float:
                case Stack.DataType.Int:
                case Stack.DataType.String:
                case Stack.DataType.StringPtr:
                case Stack.DataType.IntPtr:
                case Stack.DataType.FloatPtr:
                case Stack.DataType.Vector3Ptr:
                    return 3;
                case Stack.DataType.Bool:
                case Stack.DataType.None:
                    return 4;
                case Stack.DataType.Unk:
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Conversion of stack datatypes to string/type labels.
        /// </summary>
        public static string LongName(this Stack.DataType c)
        {
            switch (c)
            {
                case Stack.DataType.Bool: return "bool";
                case Stack.DataType.BoolPtr: return "bool*";
                case Stack.DataType.Float: return "float";
                case Stack.DataType.FloatPtr: return "float*";
                case Stack.DataType.Int: return "int";
                case Stack.DataType.IntPtr: return "int*";
                case Stack.DataType.String: return "char[]";
                case Stack.DataType.StringPtr: return "char*";
                case Stack.DataType.Vector3: return "Vector3";
                case Stack.DataType.Vector3Ptr: return "Vector3*";
                case Stack.DataType.None: return "void";
                case Stack.DataType.Unk: return "var";
                case Stack.DataType.UnkPtr: return "var*";
                case Stack.DataType.Unsure:
                default:
                    return "var";
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static string ShortName(this Stack.DataType c)
        {
            switch (c)
            {
                case Stack.DataType.Bool: return "b";
                case Stack.DataType.BoolPtr: return "b";
                case Stack.DataType.Float: return "f";
                case Stack.DataType.FloatPtr: return "f";
                case Stack.DataType.Int: return "i";
                case Stack.DataType.IntPtr: return "i";
                case Stack.DataType.String: return "c";
                case Stack.DataType.StringPtr: return "s";
                case Stack.DataType.Vector3: return "v";
                case Stack.DataType.Vector3Ptr: return "v";
                case Stack.DataType.None: return "f";
                case Stack.DataType.Unk: return "u";
                case Stack.DataType.UnkPtr: return "u";
                case Stack.DataType.Unsure: return "u";
                default: return "u";
            }
        }
    }
}
