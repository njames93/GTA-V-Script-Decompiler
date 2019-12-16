using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Decompiler
{
    public class Stack
    {
        public Function Function { get; private set; }
        List<StackValue> _stack = new List<StackValue>();

        public bool DecodeVarInfo { get; private set; }
        public DataType TopType => (_stack.Count == 0) ? DataType.Unk : _stack[_stack.Count - 1].Datatype;

        public Stack(Function parent, bool decodeVar = false)
        {
            Function = parent;
            DecodeVarInfo = decodeVar;
        }

        public void Dispose()
        {
            _stack.Clear();
        }

        public void Push(string value, DataType Datatype = DataType.Unk)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, value, Datatype));
        }

        public void PushGlobal(string value)
        {
            _stack.Add(StackValue.Global(this, StackValue.Type.Literal, value));
        }

        public void PushPGlobal(string value)
        {
            _stack.Add(StackValue.Global(this, StackValue.Type.Pointer, value));
        }

        public void PushCond(string value)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, value, DataType.Bool));
        }

        private void Push(StackValue item)
        {
            _stack.Add(item);
        }

        public void PushString(string value)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, value.ToString(), DataType.StringPtr));
        }

        public void Push(params int[] values)
        {
            foreach (int value in values)
            {
                switch (Program.IntStyle)
                {
                    case Program.IntType._int:
                    case Program.IntType._hex:
                        _stack.Add(new StackValue(this, StackValue.Type.Literal, Hashes.inttohex(value), DataType.Int));
                        break;
                    case Program.IntType._uint:
                        _stack.Add(new StackValue(this, StackValue.Type.Literal, unchecked((uint)value).ToString(), DataType.Int));
                        break;
                }
            }
        }

        public void PushHexInt(uint value)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, Utils.FormatHexHash(value), DataType.Int));
        }

        public void PushVar(Vars_Info.Var Variable)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, Variable, (Variable.Immediatesize == 3) ? ".x" : ""));
        }

        public void PushPVar(Vars_Info.Var Variable, string suffix = "")
        {
            _stack.Add(new StackValue(this, StackValue.Type.Pointer, Variable, suffix));
        }

        public void Push(float value)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Literal, value.ToString(CultureInfo.InvariantCulture) + "f", DataType.Float));
        }

        public void PushPointer(string value)
        {
            _stack.Add(new StackValue(this, StackValue.Type.Pointer, value));
        }

        private void PushStruct(string value, int size)
        {
            _stack.Add(new StackValue(this, value, size));
        }

        private void PushVector(string value)
        {
            _stack.Add(new StackValue(this, value, 3, true));
        }

        private void PushString(string value, int size)
        {
            _stack.Add(new StackValue(this, size, value));
        }

        public StackValue Pop()
        {
            int index = _stack.Count - 1;
            if (index < 0)
                return new StackValue(this, StackValue.Type.Literal, "StackVal");
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
                        items.Add(new StackValue(this, StackValue.Type.Literal, top.AsPointer));
                        count++;
                        break;
                    }
                    case StackValue.Type.Struct:
                    {
                        if (count + top.StructSize > size)
                            throw new Exception("Struct size too large");
                        count += top.StructSize;
                        items.Add(new StackValue(this, StackValue.Type.Literal, top.Value));
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
                        items.Add(new StackValue(this, StackValue.Type.Literal, top.AsPointer));
                        count++;
                        break;
                    }
                    case StackValue.Type.Struct:
                    {
                        if (count + top.StructSize > size)
                            throw new Exception("Struct size too large");
                        count += top.StructSize;
                        items.Add(new StackValue(this, top.Value, top.StructSize));
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
            return StackValue.AsVector(PopList(3));
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

        public string PopListForCall(int size)
        {
            if (size == 0) return "";
            string items = StackValue.AsCall(PopList(size));
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
                        stack.Add(val.AsPointer);
                        break;
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
            string functionline = (Function.IsAggregate ? "func_" : name) + "(" + PopListForCall(pcount) + ")";
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

        public string FunctionCall(Function func)
        {
            string popList = "";
            if (DecodeVarInfo)
            {
                if (func.Pcount != 0)
                {
                    StackValue[] items = PopList(func.Pcount);
                    for (int i = 0; i < items.Length; ++i)
                    {
                        if (func.Params.GetTypeAtIndex((uint)i).Precedence() < items[i].Datatype.Precedence())
                        {
                            if (func != Function)
                                func.UpdateFuncParamType((uint)i, items[i].Datatype);
                        }
                        else if (func.Params.GetTypeAtIndex((uint)i) != items[i].Datatype)
                            items[i].Datatype = func.Params.GetTypeAtIndex((uint)i);
                    }
                    popList = StackValue.AsCall(items);
                    popList = popList.Remove(popList.Length - 2);
                }
            }
            else
                popList = (func.Pcount > 0) ? PopListForCall(func.Pcount) : "";

            string functionline = func.Name + "(" + popList + ")";
            if (Function.IsAggregate) functionline = "func_()"; // Burn the PopList call.
            if (func.Rcount == 0)
                return functionline + ";";
            else if (func.Rcount == 1)
                Push(new StackValue(this, StackValue.Type.Literal, functionline, func));
            else if (func.Rcount > 1)
                PushStruct(functionline, func.Rcount);
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
                        if (val.Variable != null && DecodeVarInfo)
                        {
                            if (val.Variable.DataType.Precedence() < native.GetParam(count).StackType.Precedence())
                                val.Variable.DataType = native.GetParam(count).StackType;
                            else if (val.Variable.DataType.Precedence() > native.GetParam(count).StackType.Precedence())
                                Function.UpdateNativeParameter(hash, val.Variable.DataType, count);
                        }

                        if (val.Datatype == DataType.Bool || native.GetParam(count).StackType == DataType.Bool)
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
                        else if (val.Datatype == DataType.Int && native.GetParam(count).StackType == DataType.Float)
                        {
                            switch (Program.IntStyle)
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
                        functionline += val.AsPointer + " ";
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
                PushNative(functionline, Program.X64npi.UpdateNative(hash, Program.X64npi.GetReturnType(hash), _params.ToArray()));
            }
            else if (rcount > 1)
            {
                Native n = null;
                if (rcount == 2)
                    n = Program.X64npi.UpdateNative(hash, DataType.Unk, _params.ToArray());
                else if (rcount == 3)
                    n = Program.X64npi.UpdateNative(hash, DataType.Vector3, _params.ToArray());
                else
                    throw new Exception("Error in return items count");
                PushStructNative(functionline, n, rcount);
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
                Push("(" + s2.AsType(DataType.Int).Value + " + " + s1.AsType(DataType.Int).Value + ")", DataType.Int);
            else if (s2.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Literal)
                Push("(&" + s2.UnifyType(s1).Value + " + " + s1.UnifyType(s2).Value + ")", DataType.Unk);
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Literal)
                Push("(&" + s1.UnifyType(s2).Value + " + " + s2.UnifyType(s1).Value + ")", DataType.Unk);
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Pointer)
                Push("(" + s1.UnifyType(s2).Value + " + " + s2.UnifyType(s1).Value + ") // PointerArith", DataType.Unk);
            else
                throw new Exception("Unexpected stack value");
        }

        public void Op_Addf()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop();
            Push("(" + s2.AsType(DataType.Float).AsLiteral + " + " + s1.AsType(DataType.Float).AsLiteral + ")", DataType.Float);
        }

        public void Op_Sub()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop();
            if (s1.ItemType == StackValue.Type.Literal && s2.ItemType == StackValue.Type.Literal)
                Push("(" + s2.AsType(DataType.Int).Value + " - " + s1.AsType(DataType.Int).Value + ")", DataType.Int);
            else if (s2.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Literal)
                Push("(&" + s2.UnifyType(s1).Value + " - " + s1.UnifyType(s2).Value + ")", DataType.Unk);
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Literal)
                Push("(&" + s1.UnifyType(s2).Value + " - " + s2.UnifyType(s1).Value + ")", DataType.Unk);
            else if (s1.ItemType == StackValue.Type.Pointer && s2.ItemType == StackValue.Type.Pointer)
                Push("(" + s1.UnifyType(s2).Value + " - " + s2.UnifyType(s1).Value + ") // PointerArith", DataType.Unk);
            else
                throw new Exception("Unexpected stack value");
        }

        public void Op_Subf()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Float);
            s2 = Pop().AsType(DataType.Float);
            Push("(" + s2.AsLiteral + " - " + s1.AsLiteral + ")", DataType.Float);
        }

        public void Op_Mult()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Int);
            s2 = Pop().AsType(DataType.Int);
            Push("(" + s2.AsLiteral + " * " + s1.AsLiteral + ")", DataType.Int);
        }

        public void Op_Multf()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Float);
            s2 = Pop().AsType(DataType.Float);
            Push("(" + s2.AsLiteral + " * " + s1.AsLiteral + ")", DataType.Float);
        }

        public void Op_Div()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Int);
            s2 = Pop().AsType(DataType.Int);
            Push("(" + s2.AsLiteral + " / " + s1.AsLiteral + ")", DataType.Int);
        }

        public void Op_Divf()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Float);
            s2 = Pop().AsType(DataType.Float);
            Push("(" + s2.AsLiteral + " / " + s1.AsLiteral + ")", DataType.Float);
        }

        public void Op_Mod()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Int);
            s2 = Pop().AsType(DataType.Int);
            Push("(" + s2.AsLiteral + " % " + s1.AsLiteral + ")", DataType.Int);
        }

        public void Op_Modf()
        {
            StackValue s1, s2;
            s1 = Pop().AsType(DataType.Float);
            s2 = Pop().AsType(DataType.Float);
            Push("(" + s2.AsLiteral + " % " + s1.AsLiteral + ")", DataType.Float);
        }

        public void Op_Not()
        {
            StackValue s1 = Pop().AsType(DataType.Bool);
            string s1v = s1.AsLiteral;
            if (s1v.StartsWith("!(") && s1v.EndsWith(")"))
                PushCond(s1v.Remove(s1v.Length - 1).Substring(2));
            else if (s1v.StartsWith("(") && s1v.EndsWith(")"))
                PushCond("!" + s1v);
            else if (!(s1v.Contains("&&") && s1v.Contains("||") && s1v.Contains("^")))
            {
                if (s1v.StartsWith("!"))
                    PushCond(s1v.Substring(1));
                else
                    PushCond("!" + s1v);
            }
            else
                PushCond("!(" + s1v + ")");
        }

        public void Op_Neg()
        {
            StackValue s1;
            s1 = Pop().AsType(DataType.Int);
            Push("-" + s1.AsLiteral, DataType.Int);
        }

        public void Op_Negf()
        {
            StackValue s1;
            s1 = Pop().AsType(DataType.Float);
            Push("-" + s1.AsLiteral, DataType.Float);
        }

        public void Op_CmpEQ()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " == " + s1.AsLiteral);
        }

        public void Op_CmpNE()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " != " + s1.AsLiteral);
        }

        public void Op_CmpGE()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " >= " + s1.AsLiteral);
        }

        public void Op_CmpGT()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " > " + s1.AsLiteral);
        }

        public void Op_CmpLE()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " <= " + s1.AsLiteral);
        }

        public void Op_CmpLT()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop().UnifyType(s1); s1.UnifyType(s2);
            PushCond(s2.AsLiteral + " < " + s1.AsLiteral);
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
            Push("IntToFloat(" + Pop().AsType(DataType.Int).AsLiteral + ")", DataType.Float);
        }

        public void Op_FtoI()
        {
            Push("FloatToInt(" + Pop().AsType(DataType.Float).AsLiteral + ")", DataType.Int);
        }

        public void Op_And()
        {
            StackValue s1 = Pop();
            StackValue s2 = Pop();
            int temp;
            if (s1.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Pointer)
                Push("(" + s2.UnifyType(s1).Value + " && " + s1.UnifyType(s1).Value + ") // PointerArith");
            else if (s1.ItemType != StackValue.Type.Literal && s2.ItemType != StackValue.Type.Literal)
                throw new Exception("Not a literal item recieved: " + s1.ItemType + " " + s2.ItemType);
            else if (s1.Datatype == DataType.Bool || s2.Datatype == DataType.Bool)
                PushCond("(" + s2.AsType(DataType.Bool).Value + " && " + s1.AsType(DataType.Bool).Value + ")");
            else if (Utils.IntParse(s1.Value, out temp) || Utils.IntParse(s2.Value, out temp))
                Push(s2.AsType(DataType.Int).Value + " & " + s1.AsType(DataType.Int).Value, DataType.Int);
            else
                Push("(" + s2.UnifyType(s1).Value + " && " + s1.UnifyType(s2).Value + ")");
        }

        public void Op_Or()
        {
            StackValue s1 = Pop();
            StackValue s2 = Pop();
            int temp;
            if (s1.ItemType == StackValue.Type.Pointer && s1.ItemType == StackValue.Type.Pointer)
                Push("(" + s2.Value + " || " + s1.Value + ") // PointerArith");
            else if (s1.ItemType != StackValue.Type.Literal && s2.ItemType != StackValue.Type.Literal)
                throw new Exception("Not a literal item recieved: " + s1.ItemType + " " + s2.ItemType);
            else if (s1.Datatype == DataType.Bool || s2.Datatype == DataType.Bool)
                PushCond("(" + s2.AsType(DataType.Bool).Value + " || " + s1.AsType(DataType.Bool).Value + ")");
            else if (Utils.IntParse(s1.Value, out temp) || Utils.IntParse(s2.Value, out temp))
                Push(s2.AsType(DataType.Int).Value + " | " + s1.AsType(DataType.Int).Value, DataType.Int);
            else
                Push("(" + s2.UnifyType(s1).Value + " || " + s1.UnifyType(s2).Value + ")");
        }

        public void Op_Xor()
        {
            StackValue s1, s2;
            s1 = Pop();
            s2 = Pop();
            Push(s2.AsType(DataType.Int).AsLiteral + " ^ " + s1.AsType(DataType.Int).AsLiteral, DataType.Int);
        }

        public void Op_GetImm(uint immediate)
        {
            if (PeekVar(0)?.Immediatesize == 3)
            {
                switch (immediate)
                {
                    case 1:
                    {
                        string saccess = Pop().AsStructAccess;
                        if (Function.IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                            Push(new StackValue(this, StackValue.Type.Literal, saccess));
                        else
                            Push(new StackValue(this, StackValue.Type.Literal, saccess + "y"));
                        return;
                    }
                    case 2:
                    {
                        string saccess = Pop().AsStructAccess;
                        if (Function.IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                            Push(new StackValue(this, StackValue.Type.Literal, saccess));
                        else
                            Push(new StackValue(this, StackValue.Type.Literal, saccess + "z"));
                        return;
                    }
                }
            }

            string structAss = Pop().AsStructAccess;
            if (Function.IsAggregate)
            {
                if (Agg.Instance.CanAggregateLiteral(structAss))
                    Push(new StackValue(this, StackValue.Type.Literal, structAss + "f_"));
                else
                    Push(new StackValue(this, StackValue.Type.Literal, structAss + "f_" + (Program.HexIndex ? immediate.ToString("X") : immediate.ToString())));
            }
            else
            {
                Push(new StackValue(this, StackValue.Type.Literal, structAss + "f_" + (Program.HexIndex ? immediate.ToString("X") : immediate.ToString())));
            }
        }

        public string Op_SetImm(uint immediate)
        {
            StackValue pointer, value;
            pointer = Pop();
            value = Pop();

            string imm = "";
            if (Function.IsAggregate && Agg.Instance.CanAggregateLiteral(value.AsLiteral))
                imm = "f_";
            else
            {
                imm = "f_" + (Program.HexIndex ? immediate.ToString("X") : immediate.ToString());
                if (PeekVar(0)?.DataType == DataType.Vector3)
                {
                    switch (immediate)
                    {
                        case 0: imm = "x"; break;
                        case 1: imm = "y"; break;
                        case 2: imm = "z"; break;
                    }
                }
            }
            return setcheck(pointer.AsStructAccess + imm, value.AsLiteral, value.LiteralComment);
        }

        public void Op_GetImmP(uint immediate)
        {
            string saccess = Pop().AsStructAccess;
            if (Function.IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
                Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_"));
            else
                Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_" + (Program.HexIndex ? immediate.ToString("X") : immediate.ToString())));
        }

        public void Op_GetImmP()
        {
            string immediate = Pop().AsLiteral;
            string saccess = Pop().AsStructAccess;

            int temp;
            if (Function.IsAggregate && Agg.Instance.CanAggregateLiteral(saccess))
            {
                if (Utils.IntParse(immediate, out temp))
                    Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_"));
                else
                    Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_[]"));
            }
            else
            {
                if (Utils.IntParse(immediate, out temp))
                    Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_" + (Program.HexIndex ? temp.ToString("X") : temp.ToString())));
                else
                    Push(new StackValue(this, StackValue.Type.Pointer, saccess + "f_[" + immediate + "]"));
            }
        }

        /// <summary>
        /// returns a string saying the size of an array if its > 1
        /// </summary>
        /// <param name="immediate"></param>
        /// <returns></returns>
        private string getarray(uint immediate)
        {
            if (!Program.ShowArraySize)
                return "";
            if (immediate == 1)
                return "";
            if (Function.IsAggregate)
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
            string index = Pop().AsLiteral;
            Push(new StackValue(this, StackValue.Type.Literal, arrayloc + "[" + index + getarray(immediate) + "]"));
        }

        public string Op_ArraySet(uint immediate)
        {
            StackValue index, value;
            string arrayloc = PopArrayAccess();
            index = Pop();
            value = Pop();
            return setcheck(arrayloc + "[" + index.AsLiteral + getarray(immediate) + "]", value.AsLiteral, value.LiteralComment);
        }

        public void Op_ArrayGetP(uint immediate)
        {
            string arrayloc;
            string index;
            if (Peek().ItemType == StackValue.Type.Pointer)
            {
                arrayloc = PopArrayAccess();
                index = Pop().AsLiteral;
                Push(new StackValue(this, StackValue.Type.Pointer, arrayloc + "[" + index + getarray(immediate) + "]"));
            }
            else if (Peek().ItemType == StackValue.Type.Literal)
            {
                arrayloc = Pop().AsLiteral;
                index = Pop().AsLiteral;
                Push(new StackValue(this, StackValue.Type.Literal, arrayloc + "[" + index + getarray(immediate) + "]"));
            }
            else throw new Exception("Unexpected Stack Value :" + Peek().ItemType.ToString());
        }

        public void Op_RefGet()
        {
            Push(new StackValue(this, StackValue.Type.Literal, Pop().AsPointerRef));
        }

        public void Op_ToStack()
        {
            string pointer, count;
            int amount;
            if (TopType == DataType.StringPtr || TopType == DataType.String)
            {
                pointer = Pop().AsPointerRef;
                count = Pop().AsLiteral;

                if (!Utils.IntParse(count, out amount))
                    throw new Exception("Expecting the amount to push");
                PushString(pointer, amount);
            }
            else
            {
                pointer = Pop().AsPointerRef;
                count = Pop().AsLiteral;

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

                if (_stack[stackIndex].ItemType == StackValue.Type.Struct && _stack[stackIndex].Datatype != DataType.Vector3)
                    index -= _stack[stackIndex].StructSize - 1;
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

        public Native PeekNat64(int index)
        {
            int newIndex = GetIndex(index);
            if (newIndex == -1)
            {
                return null;
            }
            return _stack[_stack.Count - newIndex - 1].Native;
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

        public void PushNative(string value, Native native)
        {
            Push(new StackValue(this, value, native));
        }

        public void PushStructNative(string value, Native native, int structsize)
        {
            Push(new StackValue(this, value, structsize, native));
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
            pointer = Pop().AsPointerRef;
            count = Pop().AsLiteral;
            int amount;
            if (!Utils.IntParse(count, out amount))
                throw new Exception("Expecting the amount to push");
            return StackValue.AsList(pointer, PopList(amount));
        }

        public void Op_AmmImm(int immediate)
        {
            if (immediate < 0)
                Push(Pop().AsLiteral + " - " + (-immediate).ToString());
            else if (immediate > 0)
                Push(Pop().AsLiteral + " + " + immediate.ToString());
            //else if (immediate == 0) { }
        }

        public void Op_MultImm(int immediate)
        {
            Push(Pop().AsLiteral + " * " + immediate.ToString());
        }

        public string Op_RefSet()
        {
            StackValue pointer, value;
            pointer = Pop();
            value = Pop();
            return setcheck(pointer.AsPointerRef, value.AsLiteral, value.LiteralComment);
        }

        public string Op_PeekSet()
        {
            string pointer, value;
            value = Pop().AsLiteral;
            pointer = Peek().AsPointerRef;
            return setcheck(pointer, value);
        }

        public string Op_Set(string location)
        {
            StackValue set = Pop();
            return setcheck(location, set.AsLiteral, set.LiteralComment);
        }

        public string Op_Set(string location, Vars_Info.Var Variable)
        {
            return Op_Set(location + (Variable.Immediatesize == 3 ? ".x" : ""));
        }

        public void Op_Hash()
        {
            Push("Hash(" + Pop().AsLiteral + ")", DataType.Int);
        }

        public string Op_StrCpy(int size)
        {
            StackValue pointer = Pop().AsType(DataType.StringPtr);
            StackValue pointer2 = Pop().AsType(DataType.StringPtr);
            return "StringCopy(" + pointer.AsPointer + ", " + pointer2.AsPointer + ", " + size.ToString() + ");";
        }

        public string Op_StrAdd(int size)
        {
            StackValue pointer = Pop().AsType(DataType.StringPtr);
            StackValue pointer2 = Pop().AsType(DataType.StringPtr);
            return "StringConCat(" + pointer.AsPointer + ", " + pointer2.AsPointer + ", " + size.ToString() + ");";
        }

        public string Op_StrAddI(int size)
        {
            string pointer = Pop().AsType(DataType.StringPtr).AsPointer;
            string inttoadd = Pop().AsType(DataType.Int).AsLiteral;
            return "StringIntConCat(" + pointer + ", " + inttoadd + ", " + size.ToString() + ");";
        }

        public string Op_ItoS(int size)
        {
            string pointer = Pop().AsPointer;
            string intval = Pop().AsLiteral;
            return "IntToString(" + pointer + ", " + intval + ", " + size.ToString() + ");";
        }

        public string Op_SnCopy()
        {
            string pointer = Pop().AsPointer;
            string value = Pop().AsLiteral;
            string count = Pop().AsLiteral;
            int amount;
            if (!Utils.IntParse(count, out amount))
                throw new Exception("Int Stack value expected");
            return "MemCopy(" + pointer + ", " + "{" + PopListForCall(amount) + "}, " + value + ");";
        }

        public string[] pcall()
        {
            List<string> temp = new List<string>();
            string loc = Pop().AsLiteral;
            foreach (string s in EmptyStack())
                temp.Add("Stack.Push(" + s + ");");
            temp.Add("Call_Loc(" + loc + ");");
            return temp.ToArray();
        }

        /// <summary>
        /// Detects if you can use var++, var *= num etc
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string setcheck(string loc, string value, string suffix = "")
        {
            if (!value.StartsWith(loc + " ")) return loc + " = " + value + ";" + suffix;

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
            return loc + " " + op + "= " + newval + ";" + suffix;
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

        public class StackValue
        {
            public enum Type
            {
                Literal,
                Pointer,
                Struct
            }

            Stack _parent;
            string _value;
            Type _type;
            DataType _datatype;
            int _structSize = 0;
            Vars_Info.Var _var = null;
            Native _native = null;
            Function _function = null;
            bool global = false;

            public StackValue(Stack parent, Type type, string value, DataType datatype = DataType.Unk)
            {
                _parent = parent;
                _type = type;
                _value = value;
                _datatype = datatype;
            }

            public StackValue(Stack parent, Type type, Vars_Info.Var var, string suffix = "") : this(parent, type, var.Name + suffix, var.DataType)
            {
                _var = var;
            }

            public StackValue(Stack parent, Type type, string name, Function function) : this(parent, type, name, function.ReturnType)
            {
                _function = function;
            }

            public StackValue(Stack parent, string value, Native native) : this(parent, Type.Literal, value, native.ReturnParam.StackType)
            {
                _native = native;
            }

            public StackValue(Stack parent, string value, int structsize, Native native) : this(parent, Type.Struct, value, native.ReturnParam.StackType)
            {
                _native = native;
                _structSize = structsize;
            }

            public StackValue(Stack parent, string value, int structsize, bool Vector = false) : this(parent, Type.Struct, value, (Vector && structsize == 3) ? DataType.Vector3 : DataType.Unk)
            {
                _structSize = structsize;
            }

            public StackValue(Stack parent, int stringsize, string value) : this(parent, Type.Struct, value, DataType.String)
            {
                _structSize = stringsize;
            }

            public static StackValue Global(Stack parent, Type type, string name)
            {
                StackValue G = new StackValue(parent, type, name);
                G.global = true;
                return G;
            }

            public string Value => _value;
            public Type ItemType => _type;
            private bool isLiteral => ItemType == Type.Literal;
            public int StructSize => _structSize;

            public Vars_Info.Var Variable => _var;
            public Function Function => _function;
            public Native Native => _native;
            public bool isNative => Native != null;
            public bool isNotVar => (Variable == null && !global);

            private static DataType PrecendenceSet(DataType a, DataType b) => (a.Precedence() < b.Precedence() ? b : a);
            public DataType Datatype
            {
                get
                {
                    if (_parent.DecodeVarInfo)
                    {
                        if (Native != null && isLiteral) { return Native.ReturnParam.StackType; }
                        if (Variable != null && isLiteral) { return Variable.DataType; }
                        if (Function != null && isLiteral) { return Function.ReturnType; }
                    }
                    return _datatype;
                }

                set
                {
                    if (_parent.DecodeVarInfo)
                    {
                        _datatype = PrecendenceSet(_datatype, value);
                        if (Native != null && isLiteral) { _parent.Function.UpdateNativeReturnType(Native.Hash, PrecendenceSet(Native.ReturnParam.StackType, value)); }
                        if (Variable != null && isLiteral) Variable.DataType = PrecendenceSet(Variable.DataType, value);
                        if (Function != null && isLiteral) Function.ReturnType = PrecendenceSet(Function.ReturnType, value);
                    }
                    else
                        _datatype = value;
                }
            }

            public StackValue AsType(DataType t)
            {
                if (_parent.DecodeVarInfo) Datatype = t;
                return this;
            }

            public StackValue UnifyType(StackValue other)
            {
                if (_parent.DecodeVarInfo)
                {
                    if (Datatype != DataType.Unk && Datatype != DataType.UnkPtr && Datatype != DataType.Unsure)
                        Datatype = other.Datatype;
                }
                return this;
            }

            public object AsDrop
            {
                get
                {
                    if (Value != null && Value.Contains("(") && Value.EndsWith(")"))
                    {
                        if (Value.IndexOf("(") > 4)
                            return Value.ToString() + ";";
                    }
                    return null;
                }
            }

            public string AsLiteralStatement { get { return AsLiteral + LiteralComment; } }

            public string AsLiteral
            {
                get
                {
                    if (ItemType != StackValue.Type.Literal)
                    {
                        if (ItemType == StackValue.Type.Pointer)
                            return "&" + Value;
                        else
                            throw new Exception("Not a literal item recieved");
                    }
                    return Value;
                }
            }

            public string LiteralComment
            {
                get
                {
                    int temp;
                    if (ItemType == StackValue.Type.Literal && Datatype == DataType.Int && int.TryParse(Value, out temp))
                    {
                        return Program.gxtbank.GetEntry(temp, true);
                    }
                    return "";
                }
            }

            public string AsPointer
            {
                get
                {
                    if (ItemType == StackValue.Type.Pointer)
                    {
                        if (isNotVar)
                            return "&(" + Value + ")";
                        else
                            return "&" + Value;
                    }
                    else if (ItemType == StackValue.Type.Literal)
                        return Value;
                    throw new Exception("Not a pointer item recieved");
                }
            }

            public string AsPointerRef
            {
                get
                {
                    if (ItemType == StackValue.Type.Pointer)
                        return Value;
                    else if (ItemType == StackValue.Type.Literal)
                        return "*" + (Value.Contains(" ") ? "(" + Value + ")" : Value);
                    throw new Exception("Not a pointer item recieved");
                }
            }

            public string AsStructAccess
            {
                get
                {
                    if (ItemType == StackValue.Type.Pointer)
                        return Value + ".";
                    else if (ItemType == StackValue.Type.Literal)
                        return (Value.Contains(" ") ? "(" + Value + ")" : Value) + "->";
                    throw new Exception("Not a pointer item recieved");
                }
            }

            public static string AsVector(StackValue[] data)
            {
                switch (data.Length)
                {
                    case 1:
                        data[0]._datatype = DataType.Vector3;
                        return data[0].AsLiteral;
                    case 3:
                        return "Vector(" + data[2].AsType(DataType.Float).AsLiteral + ", " + data[1].AsType(DataType.Float).AsLiteral + ", " + data[0].AsType(DataType.Float).AsLiteral + ")";
                    case 2:
                        return "Vector(" + data[1].AsType(DataType.Float).AsLiteral + ", " + data[0].AsType(DataType.Float).AsLiteral + ")";
                }
                throw new Exception("Unexpected data length");
            }

            public static string AsList(string prefix, StackValue[] data)
            {
                string res = prefix + " = { ";
                foreach (StackValue val in data)
                    res += val.AsLiteralStatement + ", ";
                return res.Remove(res.Length - 2) + " };";
            }

            public static string AsCall(StackValue[] data)
            {
                if (data.Length == 0) return "";

                string items = "";
                foreach (StackValue val in data)
                {
                    switch (val.ItemType)
                    {
                        case StackValue.Type.Literal:
                            items += val.AsLiteralStatement + ", ";
                            break;
                        case StackValue.Type.Pointer:
                            items += val.AsPointer + ", ";
                            break;
                        case StackValue.Type.Struct:
                            items += val.Value + ", ";
                            break;
                        default:
                            throw new Exception("Unexpeced Stack Type\n" + val.ItemType.ToString());
                    }
                }
                return items;
            }

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
                    return 1;
                case Stack.DataType.Vector3:
                    return 2;
                case Stack.DataType.BoolPtr:
                case Stack.DataType.Int:
                case Stack.DataType.IntPtr:
                case Stack.DataType.String:
                case Stack.DataType.StringPtr:
                case Stack.DataType.Vector3Ptr:
                case Stack.DataType.Float:
                case Stack.DataType.FloatPtr:
                    return 3;
                case Stack.DataType.Bool:
                case Stack.DataType.None:
                    return 4;

                case Stack.DataType.UnkPtr:
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
