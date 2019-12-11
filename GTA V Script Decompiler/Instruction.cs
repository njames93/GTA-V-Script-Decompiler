using System;
using System.Collections.Generic;
using System.Linq;

namespace Decompiler
{
    internal class HLInstruction
    {
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
            instruction = Instruction.RAGE_NOP;
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

        public UInt16 GetOperandsAsUInt16
        {
            get
            {
                return BitConverter.ToUInt16(operands, 0);
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
                if (instruction == Instruction.RAGE_NATIVE)
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
                if (instruction == Instruction.RAGE_NATIVE)
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
                if (instruction == Instruction.RAGE_NATIVE)
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
            if (instruction != Instruction.RAGE_SWITCH)
                throw new Exception("Not A Switch Statement");

            int cases;
            if (Program.RDROpcodes)
            {
                if ((cases = BitConverter.ToUInt16(operands, 0)) <= index)
					throw new Exception("Out Or Range Script Case");
				return Program.getIntType == Program.IntType._uint
					? ScriptFile.hashbank.GetHash(BitConverter.ToUInt32(operands, 2 + index * 6))
					: ScriptFile.hashbank.GetHash(BitConverter.ToInt32(operands, 2 + index * 6));
            }
            else
            {
                if ((cases = GetOperand(0)) <= index)
                    throw new Exception("Out Or Range Script Case");
                return Program.getIntType == Program.IntType._uint
                    ? ScriptFile.hashbank.GetHash(Program.Bit32 ? Utils.SwapEndian(BitConverter.ToUInt32(operands, 1 + index * 6)) : BitConverter.ToUInt32(operands, 1 + index * 6))
                    : ScriptFile.hashbank.GetHash(Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt32(operands, 1 + index * 6)) : BitConverter.ToInt32(operands, 1 + index * 6));
            }
        }

        public int GetSwitchOffset(int index)
        {
            if (instruction != Instruction.RAGE_SWITCH)
                throw new Exception("Not A Switch Statement");

            int cases;
            if (Program.RDROpcodes)
            {
                if ((cases = BitConverter.ToUInt16(operands, 0)) <= index)
					throw new Exception("Out of range script case");
				return (offset + 8 + 1) + index * 6 + BitConverter.ToInt16(operands, 6 + index * 6);
            }
            else
            {
                if ((cases = GetOperand(0)) <= index)
                    throw new Exception("Out Or Range Script Case");
                return offset + 8 + index * 6 + (Program.Bit32 ? Utils.SwapEndian(BitConverter.ToInt16(operands, 5 + index * 6)) : BitConverter.ToInt16(operands, 5 + index * 6));
            }
        }

        public int GetImmBytePush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int) Instruction.RAGE_PUSH_CONST_M1 && _instruction <= (int) Instruction.RAGE_PUSH_CONST_7)
                    return _instruction - (int) Instruction.RAGE_PUSH_CONST_0;
                throw new Exception("Not An Immediate Int Push");
            }
        }

        public float GetImmFloatPush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int) Instruction.RAGE_PUSH_CONST_FM1 && _instruction <= (int) Instruction.RAGE_PUSH_CONST_F7)
                    return (float)(_instruction - (int) Instruction.RAGE_PUSH_CONST_F0);
                throw new Exception("Not An Immediate Float Push");
            }
        }

        public bool IsJumpInstruction
        {
            get { return (int)instruction > (int) Instruction.RAGE_GLOBAL_U16_STORE && (int)instruction < (int) Instruction.RAGE_CALL; }
        }

        public bool IsConditionJump
        {
            get { return (int)instruction > (int) Instruction.RAGE_J && (int)instruction < (int) Instruction.RAGE_CALL; }
        }

        public bool IsWhileJump
        {
            get
            {
                if (instruction == Instruction.RAGE_J)
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
                case Instruction.RAGE_GLOBAL_U16:
                case Instruction.RAGE_GLOBAL_U16_LOAD:
                case Instruction.RAGE_GLOBAL_U16_STORE:
                    if (aggregateName) return "Global";
                    return "Global_" + (Program.Hex_Index ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
                case Instruction.RAGE_GLOBAL_U24:
                case Instruction.RAGE_GLOBAL_U24_LOAD:
                case Instruction.RAGE_GLOBAL_U24_STORE:
                    if (aggregateName) return "Global";
                    return "Global_" + (Program.Hex_Index ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
            }
            throw new Exception("Not a global variable");
        }
    }

    public enum Instruction //opcodes reversed from gta v default.xex
    {
        RAGE_NOP = 0,
        RAGE_IADD, //1
        RAGE_ISUB, //2
        RAGE_IMUL, //3
        RAGE_IDIV, //4
        RAGE_IMOD, //5
        RAGE_INOT, //6
        RAGE_INEG, //7
        RAGE_IEQ, //8
        RAGE_INE, //9
        RAGE_IGT, //10
        RAGE_IGE, //11
        RAGE_ILT, //12
        RAGE_ILE, //13
        RAGE_FADD, //14
        RAGE_FSUB, //15
        RAGE_FMUL, //16
        RAGE_FDIV, //17
        RAGE_FMOD, //18
        RAGE_FNEG, //19
        RAGE_FEQ, //20
        RAGE_FNE, //21
        RAGE_FGT, //22
        RAGE_FGE, //23
        RAGE_FLT, //24
        RAGE_FLE, //25
        RAGE_VADD, //26
        RAGE_VSUB, //27
        RAGE_VMUL, //28
        RAGE_VDIV, //29
        RAGE_VNEG, //30
        RAGE_IAND, //31
        RAGE_IOR, //32
        RAGE_IXOR, //33
        RAGE_I2F, //34
        RAGE_F2I, //35
        RAGE_F2V, //36
        RAGE_PUSH_CONST_U8, //37
        RAGE_PUSH_CONST_U8_U8, //38
        RAGE_PUSH_CONST_U8_U8_U8, //39
        RAGE_PUSH_CONST_U32, //40
        RAGE_PUSH_CONST_F, //41
        RAGE_DUP, //42
        RAGE_DROP, //43
        RAGE_NATIVE, //44
        RAGE_ENTER, //45
        RAGE_LEAVE, //46
        RAGE_LOAD, //47
        RAGE_STORE, //48
        RAGE_STORE_REV, //49
        RAGE_LOAD_N, //50
        RAGE_STORE_N, //51
        RAGE_ARRAY_U8, //52
        RAGE_ARRAY_U8_LOAD, //53
        RAGE_ARRAY_U8_STORE, //54
        RAGE_LOCAL_U8, //55
        RAGE_LOCAL_U8_LOAD, //56
        RAGE_LOCAL_U8_STORE, //57
        RAGE_STATIC_U8, //58
        RAGE_STATIC_U8_LOAD, //59
        RAGE_STATIC_U8_STORE, //60
        RAGE_IADD_U8, //61
        RAGE_IMUL_U8, //62
        RAGE_IOFFSET, //63
        RAGE_IOFFSET_U8, //64
        RAGE_IOFFSET_U8_LOAD, //65
        RAGE_IOFFSET_U8_STORE, //66
        RAGE_PUSH_CONST_S16, //67
        RAGE_IADD_S16, //68
        RAGE_IMUL_S16, //69
        RAGE_IOFFSET_S16, //70
        RAGE_IOFFSET_S16_LOAD, //71
        RAGE_IOFFSET_S16_STORE, //72
        RAGE_ARRAY_U16, //73
        RAGE_ARRAY_U16_LOAD, //74
        RAGE_ARRAY_U16_STORE, //75
        RAGE_LOCAL_U16, //76
        RAGE_LOCAL_U16_LOAD, //77
        RAGE_LOCAL_U16_STORE, //78
        RAGE_STATIC_U16, //79
        RAGE_STATIC_U16_LOAD, //80
        RAGE_STATIC_U16_STORE, //81
        RAGE_GLOBAL_U16, //82
        RAGE_GLOBAL_U16_LOAD, //83
        RAGE_GLOBAL_U16_STORE, //84
        RAGE_J, //85
        RAGE_JZ, //86
        RAGE_IEQ_JZ, //87
        RAGE_INE_JZ, //88
        RAGE_IGT_JZ, //89
        RAGE_IGE_JZ, //90
        RAGE_ILT_JZ, //91
        RAGE_ILE_JZ, //92
        RAGE_CALL, //93
        RAGE_GLOBAL_U24, //94
        RAGE_GLOBAL_U24_LOAD, //95
        RAGE_GLOBAL_U24_STORE, //96
        RAGE_PUSH_CONST_U24, //97
        RAGE_SWITCH, //98
        RAGE_STRING, //99
        RAGE_STRINGHASH, //100
        RAGE_TEXT_LABEL_ASSIGN_STRING, //101
        RAGE_TEXT_LABEL_ASSIGN_INT, //102
        RAGE_TEXT_LABEL_APPEND_STRING, //103
        RAGE_TEXT_LABEL_APPEND_INT, //104
        RAGE_TEXT_LABEL_COPY, //105
        RAGE_CATCH, //106, No handling of these as Im unsure exactly how they work
        RAGE_THROW, //107, No script files in the game use these opcodes
        RAGE_CALLINDIRECT, //108
        RAGE_PUSH_CONST_M1, //109
        RAGE_PUSH_CONST_0, //110
        RAGE_PUSH_CONST_1, //111
        RAGE_PUSH_CONST_2, //112
        RAGE_PUSH_CONST_3, //113
        RAGE_PUSH_CONST_4, //114
        RAGE_PUSH_CONST_5, //115
        RAGE_PUSH_CONST_6, //116
        RAGE_PUSH_CONST_7, //117
        RAGE_PUSH_CONST_FM1, //118
        RAGE_PUSH_CONST_F0, //119
        RAGE_PUSH_CONST_F1, //120
        RAGE_PUSH_CONST_F2, //121
        RAGE_PUSH_CONST_F3, //122
        RAGE_PUSH_CONST_F4, //123
        RAGE_PUSH_CONST_F5, //124
        RAGE_PUSH_CONST_F6, //125
        RAGE_PUSH_CONST_F7, //126

        // Extended RDR Instructions
        RAGE_LOCAL_LOAD_S, //127
        RAGE_LOCAL_STORE_S, //128
        RAGE_LOCAL_STORE_SR, //129
        RAGE_STATIC_LOAD_S, //130
        RAGE_STATIC_STORE_S, //131
        RAGE_STATIC_STORE_SR, //132
        RAGE_LOAD_N_S, //133
        RAGE_STORE_N_S, //134
        RAGE_STORE_N_SR, //135
        RAGE_GLOBAL_LOAD_S, //136
        RAGE_GLOBAL_STORE_S, //137
        RAGE_GLOBAL_STORE_SR, //138
        RAGE_last, //139
    }

    /// <summary>
    /// Wrapped used for converting opcodes.
    /// </summary>
    public class OpcodeSet
    {
        /// <summary>
        /// Index of RAGE_last
        /// </summary>
        public virtual int Count => 127;

        /// <summary>
        /// Convert a codeblock byte to Instruction.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public virtual Instruction Map(byte v) { return v < Count ? (Instruction) v : Instruction.RAGE_last; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public virtual List<int> ConvertCodeblock(List<byte> list)
        {
            List<int> cCodeBlock = new List<int>();
            for (int j = 0; j < list.Count; ++j) cCodeBlock.Add((int) Map(list[j]));
            return cCodeBlock;
        }
    }
}
