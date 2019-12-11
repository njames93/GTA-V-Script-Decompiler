using System;
using System.Collections.Generic;
using System.Linq;

namespace Decompiler
{
    internal class HLInstruction
    {
        public static readonly int INST_COUNT = 126;

        int offset;
        Instruction instruction;
        byte[] operands;

        public HLInstruction(Instruction Instruction, IEnumerable<byte> Operands, int Offset)
        {
            instruction = Instruction;
            operands = Operands.ToArray();
            offset = Offset;
        }

        public HLInstruction(byte Instruction, IEnumerable<byte> Operands, int Offset)
        {
            instruction = (Instruction)Instruction;
            operands = Operands.ToArray();
            offset = Offset;
        }

        public HLInstruction(Instruction Instruction, int Offset)
        {
            instruction = Instruction;
            operands = new byte[0];
            offset = Offset;
        }

        public HLInstruction(byte Instruction, int Offset)
        {
            instruction = (Instruction)Instruction;
            operands = new byte[0];
            offset = Offset;
        }

        public Instruction Instruction
        {
            get { return instruction; }
        }

        public void NopInstruction()
        {
            instruction = Instruction.Nop;
        }

        public int Offset
        {
            get { return offset; }
        }

        public int InstructionLength
        {
            get { return 1 + operands.Count(); }
        }

        public int GetOperandsAsInt
        {
            get
            {
                switch (operands.Count())
                {
                    case 1:
                        return operands[0];
                    case 2:
                        return Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt16(operands, 0)) : BitConverter.ToInt16(operands, 0);
                    case 3:
                        return Program.Bit32 ? (operands[0] << 16 | operands[1] << 8 | operands[2]) : (operands[2] << 16 | operands[1] << 8 | operands[0]);
                    case 4:
                        return Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt32(operands, 0)) : BitConverter.ToInt32(operands, 0);
                    default:
                        throw new Exception("Invalid amount of operands (" + operands.Count().ToString() + ")");
                }
            }
        }

        public float GetFloat
        {
            get
            {
                if (operands.Count() != 4)
                    throw new Exception("Not a Float");
                return Program.Bit32 ? Utils.SwapEndian(BitConverter.ToSingle(operands, 0)) : BitConverter.ToSingle(operands, 0);
            }
        }

        public byte GetOperand(int index)
        {
            return operands[index];
        }

        public uint GetOperandsAsUInt
        {
            get
            {
                switch (operands.Count())
                {
                    case 1:
                        return operands[0];
                    case 2:
                        return Program.Bit32 ? (uint) Utils.SwapEndian(BitConverter.ToInt16(operands, 0)) : BitConverter.ToUInt16(operands, 0);
                    case 3:
                        return Program.Bit32 ? (uint)(operands[2] << 16 | operands[1] << 8 | operands[0]) : (uint)(operands[2] << 16 | operands[1] << 8 | operands[0]);
                    case 4:
                        return Program.Bit32 ? BitConverter.ToUInt32(operands, 0) : BitConverter.ToUInt32(operands, 0);
                    default:
                        throw new Exception("Invalid amount of operands (" + operands.Count().ToString() + ")");
                }
            }
        }

        public int GetJumpOffset
        {
            get
            {
                if (IsJumpInstruction)
                    return Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt16(operands, 0)) + offset + 3 : BitConverter.ToInt16(operands, 0) + offset + 3;
                throw new Exception("Not A jump");
            }
        }

        public byte GetNativeParams
        {
            get
            {
                if (instruction == Instruction.Native)
                {
                    return (byte)(operands[0] >> 2);
                }
                throw new Exception("Not A Native");
            }
        }

        public byte GetNativeReturns
        {
            get
            {
                if (instruction == Instruction.Native)
                {
                    return (byte)(operands[0] & 0x3);
                }
                throw new Exception("Not A Native");
            }
        }

        public ushort GetNativeIndex
        {
            get
            {
                if (instruction == Instruction.Native)
                {
                    return Utils.SwapEndian(BitConverter.ToUInt16(operands, 1));
                }
                throw new Exception("Not A Native");
            }
        }

        /*public int GetSwitchCase(int index)
		{
			if (instruction == Instruction.Switch)
			{
				int cases = GetOperand(0);
				if (index >= cases)
					throw new Exception("Out Or Range Script Case");
				return Utils.SwapEndian(BitConverter.ToInt32(operands, 1 + index * 6));
			}
			throw new Exception("Not A Switch Statement");
		}*/

        public string GetSwitchStringCase(int index)
        {
            if (instruction == Instruction.Switch)
            {
                int cases = GetOperand(0);
                if (index >= cases)
                    throw new Exception("Out Or Range Script Case");
                return Program.getIntType == Program.IntType._uint
                    ? ScriptFile.hashbank.GetHash(Program.Bit32 ? Utils.SwapEndian(BitConverter.ToUInt32(operands, 1 + index * 6)) : BitConverter.ToUInt32(operands, 1 + index * 6))
                    : ScriptFile.hashbank.GetHash(Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt32(operands, 1 + index * 6)) : BitConverter.ToInt32(operands, 1 + index * 6));
            }
            throw new Exception("Not A Switch Statement");
        }

        public int GetSwitchOffset(int index)
        {
            if (instruction == Instruction.Switch)
            {
                int cases = GetOperand(0);
                if (index >= cases)
                    throw new Exception("Out of range script case");
                return offset + 8 + index * 6 + (Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt16(operands, 5 + index * 6)) : BitConverter.ToInt16(operands, 5 + index * 6));
            }
            throw new Exception("Not A Switch Statement");
        }

        public int GetImmBytePush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int) Instruction.iPush_n1 && _instruction <= (int) Instruction.iPush_7)
                    return _instruction - (int) Instruction.iPush_0;
                throw new Exception("Not An Immediate Int Push");
            }
        }

        public float GetImmFloatPush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int) Instruction.fPush_n1 && _instruction <= (int) Instruction.fPush_7)
                    return (float)(_instruction - (int) Instruction.fPush_0);
                throw new Exception("Not An Immediate Float Push");
            }
        }

        public bool IsJumpInstruction
        {
            get { return (int)instruction > (int) Instruction.GlobalSet2 && (int)instruction < (int) Instruction.Call; }
        }

        public bool IsConditionJump
        {
            get { return (int)instruction > (int) Instruction.Jump && (int)instruction < (int) Instruction.Call; }
        }

        public bool IsWhileJump
        {
            get
            {
                if (instruction == Instruction.Jump)
                {
                    if (GetJumpOffset <= 0) return false;
                    return (GetOperandsAsInt < 0);
                }
                return false;
            }
        }

        public string GetGlobalString(bool aggregateName)
        {
            switch (instruction)
            {
                case Instruction.pGlobal2:
                case Instruction.GlobalGet2:
                case Instruction.GlobalSet2:
                    if (aggregateName) return "Global";
                    return "Global_" + (Program.Hex_Index ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
                case Instruction.pGlobal3:
                case Instruction.GlobalGet3:
                case Instruction.GlobalSet3:
                    if (aggregateName) return "Global";
                    return "Global_" + (Program.Hex_Index ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
            }
            throw new Exception("Not a global variable");
        }
    }

    internal enum Instruction //opcodes reversed from gta v default.xex
    {
        Nop = 0,
        iAdd, //1
        iSub, //2
        iMult, //3
        iDiv, //4
        iMod, //5
        iNot, //6
        iNeg, //7
        iCmpEq, //8
        iCmpNe, //9
        iCmpGt, //10
        iCmpGe, //11
        iCmpLt, //12
        iCmpLe, //13
        fAdd, //14
        fSub, //15
        fMult, //16
        fDiv, //17
        fMod, //18
        fNeg, //19
        fCmpEq, //20
        fCmpNe, //21
        fCmpGt, //22
        fCmpGe, //23
        fCmpLt, //24
        fCmpLe, //25
        vAdd, //26
        vSub, //27
        vMult, //28
        vDiv, //29
        vNeg, //30
        And, //31
        Or, //32
        Xor, //33
        ItoF, //34
        FtoI, //35
        FtoV, //36
        iPushByte1, //37
        iPushByte2, //38
        iPushByte3, //39
        iPushInt, //40
        fPush, //41
        dup, //42
        pop, //43
        Native, //44
        Enter, //45
        Return, //46
        pGet, //47
        pSet, //48
        pPeekSet, //49
        ToStack, //50
        FromStack, //51
        pArray1, //52
        ArrayGet1, //53
        ArraySet1, //54
        pFrame1, //55
        GetFrame1, //56
        SetFrame1, //57
        pStatic1, //58
        StaticGet1, //59
        StaticSet1, //60
        Add1, //61
        Mult1, //62
        pStructStack, //63
        pStruct1, //64
        GetStruct1, //65
        SetStruct1, //66
        iPushShort, //67
        Add2, //68
        Mult2, //69
        pStruct2, //70
        GetStruct2, //71
        SetStruct2, //72
        pArray2, //73
        ArrayGet2, //74
        ArraySet2, //75
        pFrame2, //76
        GetFrame2, //77
        SetFrame2, //78
        pStatic2, //79
        StaticGet2, //80
        StaticSet2, //81
        pGlobal2, //82
        GlobalGet2, //83
        GlobalSet2, //84
        Jump, //85
        JumpFalse, //86
        JumpNe, //87
        JumpEq, //88
        JumpLe, //89
        JumpLt, //90
        JumpGe, //91
        JumpGt, //92
        Call, //93
        pGlobal3, //94
        GlobalGet3, //95
        GlobalSet3, //96
        iPushI24, //97
        Switch, //98
        PushString, //99
        GetHash, //100
        StrCopy, //101
        ItoS, //102
        StrConCat, //103
        StrConCatInt, //104
        MemCopy, //105
        Catch, //106	 //No handling of these as Im unsure exactly how they work
        Throw, //107 //No script files in the game use these opcodes
        pCall, //108
        iPush_n1, //109
        iPush_0, //110
        iPush_1, //111
        iPush_2, //112
        iPush_3, //113
        iPush_4, //114
        iPush_5, //115
        iPush_6, //116
        iPush_7, //117
        fPush_n1, //118
        fPush_0, //119
        fPush_1, //120
        fPush_2, //121
        fPush_3, //122
        fPush_4, //123
        fPush_5, //124
        fPush_6, //125
        fPush_7 //126
    }
}
