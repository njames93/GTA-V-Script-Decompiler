using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Decompiler.IO
{
    public class Reader : BinaryReader
    {
        public Reader(Stream stream) : base(stream) { }

        public void Advance(int size = 4) { base.BaseStream.Position += size; }

        public UInt32 CReadUInt32() { return Program.SwapEndian ? SReadUInt32() : ReadUInt32(); }
        public Int32 CReadInt32() { return Program.SwapEndian ? SReadInt32() : ReadInt32(); }
        public UInt32 SReadUInt32() { return Utils.SwapEndian(ReadUInt32()); }
        public Int32 SReadInt32() { return Utils.SwapEndian(ReadInt32()); }

        public UInt64 CReadUInt64() { return Program.SwapEndian ? SReadUInt64() : ReadUInt64(); }
        public Int64 CReadInt64() { return Program.SwapEndian ? SReadInt64() : ReadInt64(); }
        public UInt64 SReadUInt64() { return Utils.SwapEndian(ReadUInt64()); }
        public Int64 SReadInt64() { return Utils.SwapEndian(ReadInt64()); }

        public UInt16 CReadUInt16() { return Program.SwapEndian ? SReadUInt16() : ReadUInt16(); }
        public Int16 CReadInt16() { return Program.SwapEndian ? SReadInt16() : ReadInt16(); }
        public UInt16 SReadUInt16() { return Utils.SwapEndian(ReadUInt16()); }
        public Int16 SReadInt16() { return Utils.SwapEndian(ReadInt16()); }

        public Int32 CReadPointer() { return Program.SwapEndian ? SReadPointer() : ReadPointer(); }
        public Int32 ReadPointer() { return (ReadInt32() & 0xFFFFFF); }
        public Int32 SReadPointer() { return (SReadInt32() & 0xFFFFFF); }

        public override string ReadString()
        {
            string temp = "";
            byte next = ReadByte();
            while (next != 0)
            {
                temp += (char)next;
                next = ReadByte();
            }
            return temp;
        }
    }

    public class Writer : BinaryWriter
    {
        public Writer(Stream stream) : base(stream) { }

        public void SWrite(UInt16 num) { Write(Utils.SwapEndian(num)); }
        public void SWrite(UInt32 num) { Write(Utils.SwapEndian(num)); }
        public void SWrite(UInt64 num) { Write(Utils.SwapEndian(num)); }

        public void SWrite(Int16 num) { Write(Utils.SwapEndian(num)); }
        public void SWrite(Int32 num) { Write(Utils.SwapEndian(num)); }
        public void SWrite(Int64 num) { Write(Utils.SwapEndian(num)); }

        public void WritePointer(Int32 pointer)
        {
            if (pointer == 0)
            {
                Write((int)0);
                return;
            }
            Write((pointer & 0xFFFFFF) | 0x50000000);
        }

        public void SWritePointer(Int32 pointer)
        {
            if (pointer == 0)
            {
                Write((int)0);
                return;
            }
            Write(Utils.SwapEndian((pointer & 0xFFFFFF) | 0x50000000));
        }

        public override void Write(string str)
        {
            Write(System.Text.Encoding.ASCII.GetBytes(str + "\0"));
        }
    }
}
