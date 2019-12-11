using System;
using System.Collections.Generic;
using System.IO;

namespace Decompiler
{
    public class X64NativeTable
    {
        List<string> _natives;
        List<ulong> _nativehash;
        public X64NativeTable(Stream scriptFile, int position, int length, int codeSize)
        {
            IO.Reader reader = new IO.Reader(scriptFile);
            int count = 0;
            ulong nat;
            reader.BaseStream.Position = position;
            _natives = new List<string>();
            _nativehash = new List<ulong>();
            while (count < length)
            {
                //GTA V PC natives arent stored sequentially in the table. Each native needs a bitwise rotate depending on its position and codetable size
                //Then the natives needs to go back through translation tables to get to their hash as defined in the vanilla game version
                //or the earliest game version that native was introduced in.
                //Just some of the steps Rockstar take to make reverse engineering harder
                nat = Utils.RotateLeft(reader.ReadUInt64(), (codeSize + count) & 0x3F);
                _nativehash.Add(nat);
                if (Program.X64npi.ContainsKey(nat))
                {
                    _natives.Add(Program.X64npi[nat].Display);
                }
                else
                {
                    string temps = nat.ToString("X");
                    while (temps.Length < 16)
                        temps = "0" + temps;
                    _natives.Add("unk_0x" + temps);
                }
                count++;
            }

        }
        public string[] GetNativeTable()
        {
            List<string> table = new List<string>();
            int i = 0;
            foreach (string native in _natives)
            {
                table.Add(i++.ToString("X2") + ": " + native);
            }
            return table.ToArray();
        }

        public string[] GetNativeHeader()
        {
            List<string> NativesHeader = new List<string>();
            foreach (ulong hash in _nativehash)
            {
                NativesHeader.Add(Program.X64npi.GetNativeInfo(hash));
            }

            return NativesHeader.ToArray();
        }

        public string GetNativeFromIndex(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("Index must be a positive integer");
            if (index >= _natives.Count)
                throw new ArgumentOutOfRangeException("Index is greater than native table size");
            return _natives[index];
        }

        public ulong GetNativeHashFromIndex(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("Index must be a positive integer");
            if (index >= _nativehash.Count)
                throw new ArgumentOutOfRangeException("Index is greater than native table size");
            return _nativehash[index];
        }

        public void dispose()
        {
            _natives.Clear();
        }
    }
}
