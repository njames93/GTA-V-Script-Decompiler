using System;
using System.Collections.Generic;
using System.Linq;

namespace Decompiler
{
    public class HLInstruction
    {
        int offset;
        Instruction instruction;
        byte[] operands;

        private HLInstruction() { }

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

        public HLInstruction Clone()
        {
            HLInstruction h = new HLInstruction();
            h.offset = offset;
            h.instruction = instruction;
            h.operands = (byte[])operands.Clone();
            return h;
        }

        public Instruction Instruction
        {
            get { return instruction; }
        }

        public void NopInstruction()
        {
            instruction = Instruction.NOP;
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
                        return Program.SwapEndian ? Utils.SwapEndian(BitConverter.ToInt16(operands, 0)) : BitConverter.ToInt16(operands, 0);
                    case 3:
                        return Program.SwapEndian ? (operands[0] << 16 | operands[1] << 8 | operands[2]) : (operands[2] << 16 | operands[1] << 8 | operands[0]);
                    case 4:
                        return Program.SwapEndian ? Utils.SwapEndian(BitConverter.ToInt32(operands, 0)) : BitConverter.ToInt32(operands, 0);
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
                return Program.SwapEndian ? Utils.SwapEndian(BitConverter.ToSingle(operands, 0)) : BitConverter.ToSingle(operands, 0);
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
                        return Program.SwapEndian ? (uint)Utils.SwapEndian(BitConverter.ToInt16(operands, 0)) : BitConverter.ToUInt16(operands, 0);
                    case 3:
                        return Program.SwapEndian ? (uint)(operands[2] << 16 | operands[1] << 8 | operands[0]) : (uint)(operands[2] << 16 | operands[1] << 8 | operands[0]);
                    case 4:
                        return Program.SwapEndian ? BitConverter.ToUInt32(operands, 0) : BitConverter.ToUInt32(operands, 0);
                    default:
                        throw new Exception("Invalid amount of operands (" + operands.Count().ToString() + ")");
                }
            }
        }

        public int GetJumpOffset
        {
            get
            {
                if (!IsJumpInstruction)
                    throw new Exception("Not A jump");
                Int16 length = BitConverter.ToInt16(operands, 0);
                return offset + 3 + (Program.SwapEndian ? Utils.SwapEndian(length) : length);
            }
        }

        public byte GetNativeParams
        {
            get
            {
                if (instruction == Instruction.NATIVE)
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
                if (instruction == Instruction.NATIVE)
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
                if (instruction == Instruction.NATIVE)
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
            if (instruction != Instruction.SWITCH)
                throw new Exception("Not A Switch Statement");

            int cases;
            if (Program.RDROpcodes)
            {
                if ((cases = BitConverter.ToUInt16(operands, 0)) <= index)
                    throw new Exception("Out Or Range Script Case");
                else if (Program.IntStyle == Program.IntType._uint)
                {
                    UInt32 hash = BitConverter.ToUInt32(operands, 2 + index * 6);
                    return Program.hashbank.GetHash(Program.SwapEndian ? Utils.SwapEndian(hash) : hash);
                }
                else
                {
                    Int32 hash = BitConverter.ToInt32(operands, 2 + index * 6);
                    return Program.hashbank.GetHash(Program.SwapEndian ? Utils.SwapEndian(hash) : hash);
                }
            }
            else
            {
                if ((cases = GetOperand(0)) <= index)
                    throw new Exception("Out Or Range Script Case");
                else if (Program.IntStyle == Program.IntType._uint)
                {
                    UInt32 hash = BitConverter.ToUInt32(operands, 1 + index * 6);
                    return Program.hashbank.GetHash(Program.SwapEndian ? Utils.SwapEndian(hash) : hash);
                }
                else
                {
                    Int32 hash = BitConverter.ToInt32(operands, 1 + index * 6);
                    return Program.hashbank.GetHash(Program.SwapEndian ? Utils.SwapEndian(hash) : hash);
                }
            }
        }

        public int GetSwitchOffset(int index)
        {
            if (instruction != Instruction.SWITCH)
                throw new Exception("Not A Switch Statement");

            int cases;
            if (Program.RDROpcodes)
            {
                if ((cases = BitConverter.ToUInt16(operands, 0)) <= index)
                    throw new Exception("Out of range script case");
                Int16 length = BitConverter.ToInt16(operands, 6 + index * 6);
                return (offset + 8 + 1) + index * 6 + (Program.SwapEndian ? Utils.SwapEndian(length) : length);
            }
            else
            {
                if ((cases = GetOperand(0)) <= index)
                    throw new Exception("Out Or Range Script Case");
                Int16 length = BitConverter.ToInt16(operands, 5 + index * 6);
                return offset + 8 + index * 6 + (Program.SwapEndian ? Utils.SwapEndian(length) : length);
            }
        }

        public int GetImmBytePush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int)Instruction.PUSH_CONST_M1 && _instruction <= (int)Instruction.PUSH_CONST_7)
                    return _instruction - (int)Instruction.PUSH_CONST_0;
                throw new Exception("Not An Immediate Int Push");
            }
        }

        public float GetImmFloatPush
        {
            get
            {
                int _instruction = (int)Instruction;
                if (_instruction >= (int)Instruction.PUSH_CONST_FM1 && _instruction <= (int)Instruction.PUSH_CONST_F7)
                    return (float)(_instruction - (int)Instruction.PUSH_CONST_F0);
                throw new Exception("Not An Immediate Float Push");
            }
        }

        public bool IsJumpInstruction
        {
            get { return (int)instruction > (int)Instruction.GLOBAL_U16_STORE && (int)instruction < (int)Instruction.CALL; }
        }

        public bool IsConditionJump
        {
            get { return (int)instruction > (int)Instruction.J && (int)instruction < (int)Instruction.CALL; }
        }

        public bool IsWhileJump
        {
            get
            {
                if (instruction == Instruction.J)
                {
                    if (GetJumpOffset <= 0) return false;
                    return (GetOperandsAsInt < 0);
                }
                return false;
            }
        }

        public string GetGlobalString()
        {
            switch (instruction)
            {
                case Instruction.GLOBAL_U16:
                case Instruction.GLOBAL_U16_LOAD:
                case Instruction.GLOBAL_U16_STORE:
                    return Vars_Info.GlobalName + "_" + (Program.HexIndex ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
                case Instruction.GLOBAL_U24:
                case Instruction.GLOBAL_U24_LOAD:
                case Instruction.GLOBAL_U24_STORE:
                    return Vars_Info.GlobalName + "_" + (Program.HexIndex ? GetOperandsAsUInt.ToString("X") : GetOperandsAsUInt.ToString());
            }
            throw new Exception("Not a global variable");
        }
    }

    public enum Instruction //opcodes reversed from gta v default.xex
    {
        NOP = 0,
        IADD, //1
        ISUB, //2
        IMUL, //3
        IDIV, //4
        IMOD, //5
        INOT, //6
        INEG, //7
        IEQ, //8
        INE, //9
        IGT, //10
        IGE, //11
        ILT, //12
        ILE, //13
        FADD, //14
        FSUB, //15
        FMUL, //16
        FDIV, //17
        FMOD, //18
        FNEG, //19
        FEQ, //20
        FNE, //21
        FGT, //22
        FGE, //23
        FLT, //24
        FLE, //25
        VADD, //26
        VSUB, //27
        VMUL, //28
        VDIV, //29
        VNEG, //30
        IAND, //31
        IOR, //32
        IXOR, //33
        I2F, //34
        F2I, //35
        F2V, //36
        PUSH_CONST_U8, //37
        PUSH_CONST_U8_U8, //38
        PUSH_CONST_U8_U8_U8, //39
        PUSH_CONST_U32, //40
        PUSH_CONST_F, //41
        DUP, //42
        DROP, //43
        NATIVE, //44
        ENTER, //45
        LEAVE, //46
        LOAD, //47
        STORE, //48
        STORE_REV, //49
        LOAD_N, //50
        STORE_N, //51
        ARRAY_U8, //52
        ARRAY_U8_LOAD, //53
        ARRAY_U8_STORE, //54
        LOCAL_U8, //55
        LOCAL_U8_LOAD, //56
        LOCAL_U8_STORE, //57
        STATIC_U8, //58
        STATIC_U8_LOAD, //59
        STATIC_U8_STORE, //60
        IADD_U8, //61
        IMUL_U8, //62
        IOFFSET, //63
        IOFFSET_U8, //64
        IOFFSET_U8_LOAD, //65
        IOFFSET_U8_STORE, //66
        PUSH_CONST_S16, //67
        IADD_S16, //68
        IMUL_S16, //69
        IOFFSET_S16, //70
        IOFFSET_S16_LOAD, //71
        IOFFSET_S16_STORE, //72
        ARRAY_U16, //73
        ARRAY_U16_LOAD, //74
        ARRAY_U16_STORE, //75
        LOCAL_U16, //76
        LOCAL_U16_LOAD, //77
        LOCAL_U16_STORE, //78
        STATIC_U16, //79
        STATIC_U16_LOAD, //80
        STATIC_U16_STORE, //81
        GLOBAL_U16, //82
        GLOBAL_U16_LOAD, //83
        GLOBAL_U16_STORE, //84
        J, //85
        JZ, //86
        IEQ_JZ, //87
        INE_JZ, //88
        IGT_JZ, //89
        IGE_JZ, //90
        ILT_JZ, //91
        ILE_JZ, //92
        CALL, //93
        GLOBAL_U24, //94
        GLOBAL_U24_LOAD, //95
        GLOBAL_U24_STORE, //96
        PUSH_CONST_U24, //97
        SWITCH, //98
        STRING, //99
        STRINGHASH, //100
        TEXT_LABEL_ASSIGN_STRING, //101
        TEXT_LABEL_ASSIGN_INT, //102
        TEXT_LABEL_APPEND_STRING, //103
        TEXT_LABEL_APPEND_INT, //104
        TEXT_LABEL_COPY, //105
        CATCH, //106, No handling of these as Im unsure exactly how they work
        THROW, //107, No script files in the game use these opcodes
        CALLINDIRECT, //108
        PUSH_CONST_M1, //109
        PUSH_CONST_0, //110
        PUSH_CONST_1, //111
        PUSH_CONST_2, //112
        PUSH_CONST_3, //113
        PUSH_CONST_4, //114
        PUSH_CONST_5, //115
        PUSH_CONST_6, //116
        PUSH_CONST_7, //117
        PUSH_CONST_FM1, //118
        PUSH_CONST_F0, //119
        PUSH_CONST_F1, //120
        PUSH_CONST_F2, //121
        PUSH_CONST_F3, //122
        PUSH_CONST_F4, //123
        PUSH_CONST_F5, //124
        PUSH_CONST_F6, //125
        PUSH_CONST_F7, //126

        // Extended RDR Instructions
        LOCAL_LOAD_S, //127
        LOCAL_STORE_S, //128
        LOCAL_STORE_SR, //129
        STATIC_LOAD_S, //130
        STATIC_STORE_S, //131
        STATIC_STORE_SR, //132
        LOAD_N_S, //133
        STORE_N_S, //134
        STORE_N_SR, //135
        GLOBAL_LOAD_S, //136
        GLOBAL_STORE_S, //137
        GLOBAL_STORE_SR, //138
        STATIC_U24, //139
        STATIC_U24_LOAD, //140
        STATIC_U24_STORE, //141
        last, //142
    }

    /// <summary>
    /// Wrapped used for converting opcodes.
    /// </summary>
    public class OpcodeSet
    {
        /// <summary>
        /// Index of last
        /// </summary>
        public virtual int Count => 127;

        /// <summary>
        /// Convert a codeblock byte to Instruction.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public virtual Instruction Map(byte v) { return v < Count ? (Instruction)v : Instruction.last; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public virtual List<int> ConvertCodeblock(List<byte> list)
        {
            List<int> cCodeBlock = new List<int>();
            for (int j = 0; j < list.Count; ++j) cCodeBlock.Add((int)Map(list[j]));
            return cCodeBlock;
        }
    }

    /// <summary>
    /// Unshuffled instruction sets used for console editions.
    /// </summary>
    public class RDRConsoleOpcodeSet : OpcodeSet
    {
        /// <summary>
        /// Index of last
        /// </summary>
        public override int Count => 139;
        public override Instruction Map(byte v) { return v < Count ? (Instruction)v : Instruction.last; }
    }

    /// <summary>
    ///
    /// </summary>
    public class RDROpcodeSet : OpcodeSet
    {
        public override int Count => 139;

        public override Instruction Map(byte v) { return v < Count ? RDROpcodeSet.Remap[v] : Instruction.last; }

        public static readonly Dictionary<Instruction, int> ShuffledInstructions = new Dictionary<Instruction, int> {
            { Instruction.NOP, 77 },
            { Instruction.IADD, 105 },
            { Instruction.ISUB, 79 },
            { Instruction.IMUL, 12 },
            { Instruction.IDIV, 27 },
            { Instruction.IMOD, 124 },
            { Instruction.INOT, 89 },
            { Instruction.INEG, 120 },
            { Instruction.IEQ, 138 },
            { Instruction.INE, 2 },
            { Instruction.IGT, 101 },
            { Instruction.IGE, 133 },
            { Instruction.ILT, 11 },
            { Instruction.ILE, 53 },
            { Instruction.FADD, 115 },
            { Instruction.FSUB, 84 },
            { Instruction.FMUL, 51 },
            { Instruction.FDIV, 70 },
            { Instruction.FMOD, 135 },
            { Instruction.FNEG, 37 },
            { Instruction.FEQ, 50 },
            { Instruction.FNE, 57 },
            { Instruction.FGT, 131 },
            { Instruction.FGE, 130 },
            { Instruction.FLT, 90 },
            { Instruction.FLE, 68 },
            { Instruction.VADD, 88 },
            { Instruction.VSUB, 5 },
            { Instruction.VMUL, 60 },
            { Instruction.VDIV, 55 },
            { Instruction.VNEG, 121 },
            { Instruction.IAND, 44 },
            { Instruction.IOR, 36 },
            { Instruction.IXOR, 75 },
            { Instruction.I2F, 35 },
            { Instruction.F2I, 104 },
            { Instruction.F2V, 18 },
            { Instruction.PUSH_CONST_U8, 128 },
            { Instruction.PUSH_CONST_U8_U8, 85 },
            { Instruction.PUSH_CONST_U8_U8_U8, 21 },
            { Instruction.PUSH_CONST_U32, 80 },
            { Instruction.PUSH_CONST_F, 103 },
            { Instruction.DUP, 16 },
            { Instruction.DROP, 92 },
            { Instruction.NATIVE, 95 },
            { Instruction.ENTER, 134 },
            { Instruction.LEAVE, 107 },
            { Instruction.LOAD, 10 },
            { Instruction.STORE, 33 },
            { Instruction.STORE_REV, 69 },
            { Instruction.LOAD_N, 137 },
            { Instruction.STORE_N, 67 },
            { Instruction.ARRAY_U8, 97 },
            { Instruction.ARRAY_U8_LOAD, 28 },
            { Instruction.ARRAY_U8_STORE, 71 },
            { Instruction.LOCAL_U8, 112 },
            { Instruction.LOCAL_U8_LOAD, 136 },
            { Instruction.LOCAL_U8_STORE, 127 },
            { Instruction.STATIC_U8, 119 },
            { Instruction.STATIC_U8_LOAD, 48 },
            { Instruction.STATIC_U8_STORE, 123 },
            { Instruction.IADD_U8, 9 },
            { Instruction.IMUL_U8, 106 },
            { Instruction.IOFFSET, 125 },
            { Instruction.IOFFSET_U8, 100 },
            { Instruction.IOFFSET_U8_LOAD, 110 },
            { Instruction.IOFFSET_U8_STORE, 3 },
            { Instruction.PUSH_CONST_S16, 26 },
            { Instruction.IADD_S16, 54 },
            { Instruction.IMUL_S16, 91 },
            { Instruction.IOFFSET_S16, 38 },
            { Instruction.IOFFSET_S16_LOAD, 6 },
            { Instruction.IOFFSET_S16_STORE, 93 },
            { Instruction.ARRAY_U16, 111 },
            { Instruction.ARRAY_U16_LOAD, 64 },
            { Instruction.ARRAY_U16_STORE, 25 },
            { Instruction.LOCAL_U16, 56 },
            { Instruction.LOCAL_U16_LOAD, 76 },
            { Instruction.LOCAL_U16_STORE, 4 },
            { Instruction.STATIC_U16, 114 },
            { Instruction.STATIC_U16_LOAD, 39 },
            { Instruction.STATIC_U16_STORE, 94 },
            { Instruction.GLOBAL_U16, 30 },
            { Instruction.GLOBAL_U16_LOAD, 126 },
            { Instruction.GLOBAL_U16_STORE, 23 },
            { Instruction.J, 96 },
            { Instruction.JZ, 66 },
            { Instruction.IEQ_JZ, 129 },
            { Instruction.INE_JZ, 31 },
            { Instruction.IGT_JZ, 1 },
            { Instruction.IGE_JZ, 99 },
            { Instruction.ILT_JZ, 29 },
            { Instruction.ILE_JZ, 118 },
            { Instruction.CALL, 34 },
            { Instruction.GLOBAL_U24, 86 },
            { Instruction.GLOBAL_U24_LOAD, 7 },
            { Instruction.GLOBAL_U24_STORE, 65 },
            { Instruction.PUSH_CONST_U24, 46 },
            { Instruction.SWITCH, 102 },
            { Instruction.STRING, 116 },
            { Instruction.STRINGHASH, 62 },
            { Instruction.TEXT_LABEL_ASSIGN_STRING, 78 },
            { Instruction.TEXT_LABEL_ASSIGN_INT, 32 },
            { Instruction.TEXT_LABEL_APPEND_STRING, 40 },
            { Instruction.TEXT_LABEL_APPEND_INT, 72 },
            { Instruction.TEXT_LABEL_COPY, 109 },
            { Instruction.CATCH, 117 },
            { Instruction.THROW, 47 },
            { Instruction.CALLINDIRECT, 22 },
            { Instruction.PUSH_CONST_M1, 24 },
            { Instruction.PUSH_CONST_0, 13 },
            { Instruction.PUSH_CONST_1, 98 },
            { Instruction.PUSH_CONST_2, 45 },
            { Instruction.PUSH_CONST_3, 0 },
            { Instruction.PUSH_CONST_4, 108 },
            { Instruction.PUSH_CONST_5, 83 },
            { Instruction.PUSH_CONST_6, 73 },
            { Instruction.PUSH_CONST_7, 15 },
            { Instruction.PUSH_CONST_FM1, 17 },
            { Instruction.PUSH_CONST_F0, 14 },
            { Instruction.PUSH_CONST_F1, 52 },
            { Instruction.PUSH_CONST_F2, 122 },
            { Instruction.PUSH_CONST_F3, 81 },
            { Instruction.PUSH_CONST_F4, 49 },
            { Instruction.PUSH_CONST_F5, 63 },
            { Instruction.PUSH_CONST_F6, 41 },
            { Instruction.PUSH_CONST_F7, 87 },

            // Temporary Mapping
            { Instruction.LOCAL_LOAD_S, 74 },
            { Instruction.LOCAL_STORE_S, 43 },
            { Instruction.LOCAL_STORE_SR, 59 },
            { Instruction.STATIC_LOAD_S, 132 },
            { Instruction.STATIC_STORE_S, 113 },
            { Instruction.STATIC_STORE_SR, 82 },
            { Instruction.LOAD_N_S, 20 },
            { Instruction.STORE_N_S, 19 },
            { Instruction.STORE_N_SR, 61 },
            { Instruction.GLOBAL_LOAD_S, 58 },
            { Instruction.GLOBAL_STORE_S, 8 },
            { Instruction.GLOBAL_STORE_SR, 42 },
        };

        public static readonly Dictionary<int, Instruction> Remap = ShuffledInstructions.ToDictionary((i) => i.Value, (i) => i.Key);
    }

    /// <summary>
    /// </summary>
    public class RDR1311OpcodeSet : OpcodeSet
    {
        public override int Count => 142;

        public override Instruction Map(byte v) { return v < Count ? RDR1311OpcodeSet.Remap[v] : Instruction.last; }

        public static readonly Dictionary<Instruction, int> ShuffledInstructions = new Dictionary<Instruction, int> {
            { Instruction.NOP, 61 },
            { Instruction.IADD, 50 },
            { Instruction.ISUB, 86 },
            { Instruction.IMUL, 53 },
            { Instruction.IDIV, 124 },
            { Instruction.IMOD, 140 },
            { Instruction.INOT, 75 },
            { Instruction.INEG, 92 },
            { Instruction.IEQ, 99 },
            { Instruction.INE, 82 },
            { Instruction.IGT, 139 },
            { Instruction.IGE, 44 },
            { Instruction.ILT, 131 },
            { Instruction.ILE, 38 },
            { Instruction.FADD, 123 },
            { Instruction.FSUB, 28 },
            { Instruction.FMUL, 34 },
            { Instruction.FDIV, 81 },
            { Instruction.FMOD, 78 },
            { Instruction.FNEG, 1 },
            { Instruction.FEQ, 60 },
            { Instruction.FNE, 129 },
            { Instruction.FGT, 106 },
            { Instruction.FGE, 5 }, //
            { Instruction.FLT, 21 },
            { Instruction.FLE, 79 },
            { Instruction.VADD, 113 },
            { Instruction.VSUB, 110 },
            { Instruction.VMUL, 135 },
            { Instruction.VDIV, 29 },
            { Instruction.VNEG, 117 },
            { Instruction.IAND, 13 },
            { Instruction.IOR, 22 },
            { Instruction.IXOR, 122 },
            { Instruction.I2F, 90 },
            { Instruction.F2I, 31 },
            { Instruction.F2V, 118 },
            { Instruction.PUSH_CONST_U8, 25 },
            { Instruction.PUSH_CONST_U8_U8, 105 },
            { Instruction.PUSH_CONST_U8_U8_U8, 95 },
            { Instruction.PUSH_CONST_U32, 9 },
            { Instruction.PUSH_CONST_F, 70 },
            { Instruction.DUP, 37 },
            { Instruction.DROP, 65 },
            { Instruction.NATIVE, 14 },
            { Instruction.ENTER, 87 },
            { Instruction.LEAVE, 42 },
            { Instruction.LOAD, 18 },
            { Instruction.STORE, 55 },
            { Instruction.STORE_REV, 73 },
            { Instruction.LOAD_N, 27 },
            { Instruction.STORE_N, 59 },
            { Instruction.ARRAY_U8, 134 },
            { Instruction.ARRAY_U8_LOAD, 41 },
            { Instruction.ARRAY_U8_STORE, 94 },
            { Instruction.LOCAL_U8, 46 },
            { Instruction.LOCAL_U8_LOAD, 19 },
            { Instruction.LOCAL_U8_STORE, 32 },
            { Instruction.STATIC_U8, 84 },
            { Instruction.STATIC_U8_LOAD, 121 },
            { Instruction.STATIC_U8_STORE, 83 },
            { Instruction.IADD_U8, 10 },
            { Instruction.IMUL_U8, 101 },
            { Instruction.IOFFSET, 56 },
            { Instruction.IOFFSET_U8, 36 },
            { Instruction.IOFFSET_U8_LOAD, 74 },
            { Instruction.IOFFSET_U8_STORE, 66 },
            { Instruction.PUSH_CONST_S16, 130 },
            { Instruction.IADD_S16, 91 },
            { Instruction.IMUL_S16, 132 },
            { Instruction.IOFFSET_S16, 85 },
            { Instruction.IOFFSET_S16_LOAD, 88 },
            { Instruction.IOFFSET_S16_STORE, 109 },
            { Instruction.ARRAY_U16, 40 },
            { Instruction.ARRAY_U16_LOAD, 11 },
            { Instruction.ARRAY_U16_STORE, 137 },
            { Instruction.LOCAL_U16, 128 },
            { Instruction.LOCAL_U16_LOAD, 80 },
            { Instruction.LOCAL_U16_STORE, 100 },
            { Instruction.STATIC_U16, 112 },
            { Instruction.STATIC_U16_LOAD, 120 },
            { Instruction.STATIC_U16_STORE, 8 },
            { Instruction.GLOBAL_U16, 97 },
            { Instruction.GLOBAL_U16_LOAD, 77 },
            { Instruction.GLOBAL_U16_STORE, 115 },
            { Instruction.J, 12 },
            { Instruction.JZ, 119 },
            { Instruction.IEQ_JZ, 107 },
            { Instruction.INE_JZ, 58 },
            { Instruction.IGT_JZ, 39 },
            { Instruction.IGE_JZ, 33 },
            { Instruction.ILT_JZ, 43 },
            { Instruction.ILE_JZ, 17 },
            { Instruction.CALL, 49 },
            { Instruction.GLOBAL_U24, 3 },
            { Instruction.GLOBAL_U24_LOAD, 127 },
            { Instruction.GLOBAL_U24_STORE, 48 },
            { Instruction.PUSH_CONST_U24, 136 },
            { Instruction.SWITCH, 52 },
            { Instruction.STRING, 64 },
            { Instruction.STRINGHASH, 111 },
            { Instruction.TEXT_LABEL_ASSIGN_STRING, 93 },
            { Instruction.TEXT_LABEL_ASSIGN_INT, 89 },
            { Instruction.TEXT_LABEL_APPEND_STRING, 4 },
            { Instruction.TEXT_LABEL_APPEND_INT, 57 },
            { Instruction.TEXT_LABEL_COPY, 71 },
            { Instruction.CATCH, 133 },
            { Instruction.THROW, 63 },
            { Instruction.CALLINDIRECT, 98 },
            { Instruction.PUSH_CONST_M1, 68 },
            { Instruction.PUSH_CONST_0, 96 },
            { Instruction.PUSH_CONST_1, 30 },
            { Instruction.PUSH_CONST_2, 126 },
            { Instruction.PUSH_CONST_3, 51 },
            { Instruction.PUSH_CONST_4, 16 },
            { Instruction.PUSH_CONST_5, 7 },
            { Instruction.PUSH_CONST_6, 125 },
            { Instruction.PUSH_CONST_7, 104 },
            { Instruction.PUSH_CONST_FM1, 114 },
            { Instruction.PUSH_CONST_F0, 2 },
            { Instruction.PUSH_CONST_F1, 69 },
            { Instruction.PUSH_CONST_F2, 45 },
            { Instruction.PUSH_CONST_F3, 76 },
            { Instruction.PUSH_CONST_F4, 138 },
            { Instruction.PUSH_CONST_F5, 62 },
            { Instruction.PUSH_CONST_F6, 72 },
            { Instruction.PUSH_CONST_F7, 47 },
            // RDR3 extended
            { Instruction.LOCAL_LOAD_S, 6 },
            { Instruction.LOCAL_STORE_S, 0 },
            { Instruction.LOCAL_STORE_SR, 67 },
            { Instruction.STATIC_LOAD_S, 141 },
            { Instruction.STATIC_STORE_S, 15 },
            { Instruction.STATIC_STORE_SR, 20 },
            { Instruction.GLOBAL_LOAD_S, 103 },
            { Instruction.GLOBAL_STORE_S, 102 },
            { Instruction.GLOBAL_STORE_SR, 26 },
            { Instruction.LOAD_N_S, 54 },
            { Instruction.STORE_N_S, 108 },
            { Instruction.STORE_N_SR, 23 },
            // 1311
            { Instruction.STATIC_U24, 24 },
            { Instruction.STATIC_U24_LOAD, 35 },
            { Instruction.STATIC_U24_STORE, 116 },
        };

        public static readonly Dictionary<int, Instruction> Remap = ShuffledInstructions.ToDictionary((i) => i.Value, (i) => i.Key);
    }

    /// <summary>
    /// </summary>
    public class RDR1355OpcodeSet : OpcodeSet
    {
        public override int Count => 142;

        public override Instruction Map(byte v) { return v < Count ? RDR1355OpcodeSet.Remap[v] : Instruction.last; }

        public static readonly Dictionary<Instruction, int> ShuffledInstructions = new Dictionary<Instruction, int> {
            { Instruction.NOP, 0 },
            { Instruction.IADD, 0 },
            { Instruction.ISUB, 0 },
            { Instruction.IMUL, 0 },
            { Instruction.IDIV, 0 },
            { Instruction.IMOD, 0 },
            { Instruction.INOT, 0 },
            { Instruction.INEG, 0 },
            { Instruction.IEQ, 0 },
            { Instruction.INE, 0 },
            { Instruction.IGT, 0 },
            { Instruction.IGE, 0 },
            { Instruction.ILT, 0 },
            { Instruction.ILE, 0 },
            { Instruction.FADD, 0 },
            { Instruction.FSUB, 0 },
            { Instruction.FMUL, 0 },
            { Instruction.FDIV, 0 },
            { Instruction.FMOD, 0 },
            { Instruction.FNEG, 0 },
            { Instruction.FEQ, 0 },
            { Instruction.FNE, 0 },
            { Instruction.FGT, 0 },
            { Instruction.FGE, 0 },
            { Instruction.FLT, 0 },
            { Instruction.FLE, 0 },
            { Instruction.VADD, 0 },
            { Instruction.VSUB, 0 },
            { Instruction.VMUL, 0 },
            { Instruction.VDIV, 0 },
            { Instruction.VNEG, 0 },
            { Instruction.IAND, 0 },
            { Instruction.IOR, 0 },
            { Instruction.IXOR, 0 },
            { Instruction.I2F, 0 },
            { Instruction.F2I, 0 },
            { Instruction.F2V, 0 },
            { Instruction.PUSH_CONST_U8, 0 },
            { Instruction.PUSH_CONST_U8_U8, 0 },
            { Instruction.PUSH_CONST_U8_U8_U8, 0 },
            { Instruction.PUSH_CONST_U32, 0 },
            { Instruction.PUSH_CONST_F, 0 },
            { Instruction.DUP, 0 },
            { Instruction.DROP, 0 },
            { Instruction.NATIVE, 0 },
            { Instruction.ENTER, 0 },
            { Instruction.LEAVE, 0 },
            { Instruction.LOAD, 0 },
            { Instruction.STORE, 0 },
            { Instruction.STORE_REV, 0 },
            { Instruction.LOAD_N, 0 },
            { Instruction.STORE_N, 0 },
            { Instruction.ARRAY_U8, 0 },
            { Instruction.ARRAY_U8_LOAD, 0 },
            { Instruction.ARRAY_U8_STORE, 0 },
            { Instruction.LOCAL_U8, 0 },
            { Instruction.LOCAL_U8_LOAD, 0 },
            { Instruction.LOCAL_U8_STORE, 0 },
            { Instruction.STATIC_U8, 0 },
            { Instruction.STATIC_U8_LOAD, 0 },
            { Instruction.STATIC_U8_STORE, 0 },
            { Instruction.IADD_U8, 0 },
            { Instruction.IMUL_U8, 0 },
            { Instruction.IOFFSET, 0 },
            { Instruction.IOFFSET_U8, 0 },
            { Instruction.IOFFSET_U8_LOAD, 0 },
            { Instruction.IOFFSET_U8_STORE, 0 },
            { Instruction.PUSH_CONST_S16, 0 },
            { Instruction.IADD_S16, 0 },
            { Instruction.IMUL_S16, 0 },
            { Instruction.IOFFSET_S16, 0 },
            { Instruction.IOFFSET_S16_LOAD, 0 },
            { Instruction.IOFFSET_S16_STORE, 0 },
            { Instruction.ARRAY_U16, 0 },
            { Instruction.ARRAY_U16_LOAD, 0 },
            { Instruction.ARRAY_U16_STORE, 0 },
            { Instruction.LOCAL_U16, 0 },
            { Instruction.LOCAL_U16_LOAD, 0 },
            { Instruction.LOCAL_U16_STORE, 0 },
            { Instruction.STATIC_U16, 0 },
            { Instruction.STATIC_U16_LOAD, 0 },
            { Instruction.STATIC_U16_STORE, 0 },
            { Instruction.GLOBAL_U16, 0 },
            { Instruction.GLOBAL_U16_LOAD, 0 },
            { Instruction.GLOBAL_U16_STORE, 0 },
            { Instruction.J, 0 },
            { Instruction.JZ, 0 },
            { Instruction.IEQ_JZ, 0 },
            { Instruction.INE_JZ, 0 },
            { Instruction.IGT_JZ, 0 },
            { Instruction.IGE_JZ, 0 },
            { Instruction.ILT_JZ, 0 },
            { Instruction.ILE_JZ, 0 },
            { Instruction.CALL, 0 },
            { Instruction.GLOBAL_U24, 0 },
            { Instruction.GLOBAL_U24_LOAD, 0 },
            { Instruction.GLOBAL_U24_STORE, 0 },
            { Instruction.PUSH_CONST_U24, 0 },
            { Instruction.SWITCH, 0 },
            { Instruction.STRING, 0 },
            { Instruction.STRINGHASH, 0 },
            { Instruction.TEXT_LABEL_ASSIGN_STRING, 0 },
            { Instruction.TEXT_LABEL_ASSIGN_INT, 0 },
            { Instruction.TEXT_LABEL_APPEND_STRING, 0 },
            { Instruction.TEXT_LABEL_APPEND_INT, 0 },
            { Instruction.TEXT_LABEL_COPY, 0 },
            { Instruction.CATCH, 0 },
            { Instruction.THROW, 0 },
            { Instruction.CALLINDIRECT, 0 },
            { Instruction.PUSH_CONST_M1, 0 },
            { Instruction.PUSH_CONST_0, 0 },
            { Instruction.PUSH_CONST_1, 0 },
            { Instruction.PUSH_CONST_2, 0 },
            { Instruction.PUSH_CONST_3, 0 },
            { Instruction.PUSH_CONST_4, 0 },
            { Instruction.PUSH_CONST_5, 0 },
            { Instruction.PUSH_CONST_6, 0 },
            { Instruction.PUSH_CONST_7, 0 },
            { Instruction.PUSH_CONST_FM1, 0 },
            { Instruction.PUSH_CONST_F0, 0 },
            { Instruction.PUSH_CONST_F1, 0 },
            { Instruction.PUSH_CONST_F2, 0 },
            { Instruction.PUSH_CONST_F3, 0 },
            { Instruction.PUSH_CONST_F4, 0 },
            { Instruction.PUSH_CONST_F5, 0 },
            { Instruction.PUSH_CONST_F6, 0 },
            { Instruction.PUSH_CONST_F7, 0 },
            // RDR3 extended
            { Instruction.LOCAL_LOAD_S, 0 },
            { Instruction.LOCAL_STORE_S, 0 },
            { Instruction.LOCAL_STORE_SR, 0 },
            { Instruction.STATIC_LOAD_S, 0 },
            { Instruction.STATIC_STORE_S, 0 },
            { Instruction.STATIC_STORE_SR, 0 },
            { Instruction.GLOBAL_LOAD_S, 0 },
            { Instruction.GLOBAL_STORE_S, 0 },
            { Instruction.GLOBAL_STORE_SR, 0 },
            { Instruction.LOAD_N_S, 0 },
            { Instruction.STORE_N_S, 0 },
            { Instruction.STORE_N_SR, 0 },
            // >= 1311
            { Instruction.STATIC_U24, 0 },
            { Instruction.STATIC_U24_LOAD, 0 },
            { Instruction.STATIC_U24_STORE, 0 },
        };

        public static readonly Dictionary<int, Instruction> Remap = ShuffledInstructions.ToDictionary((i) => i.Value, (i) => i.Key);
    }
}
