using System;
using System.IO;

namespace Decompiler
{
	public class ScriptHeader
	{
		//Header Start
		public Int32 Magic { get; set; }
		public Int32 SubHeader { get; set; } //wtf?
		public Int32 CodeBlocksOffset { get; set; }
		public Int32 GlobalsVersion { get; set; } //Not sure if this is the globals version
		public Int32 CodeLength { get; set; } //Total length of code
		public Int32 ParameterCount { get; set; } //Count of paremeters to the script
		public Int32 StaticsCount { get; set; }
		public Int32 GlobalsCount { get; set; }
		public Int32 NativesCount { get; set; } //Native count * 4 = total block length
		public Int32 StaticsOffset { get; set; }
		public Int32 GlobalsOffset { get; set; }
		public Int32 NativesOffset { get; set; }
		public Int32 Null1 { get; set; } //unknown
		public Int32 Null2 { get; set; } //Unknown
		public Int32 NameHash { get; set; } //Hash of the script name at ScriptNameOffset
		public Int32 Null3 { get; set; }
		public Int32 ScriptNameOffset { get; set; }
		public Int32 StringsOffset { get; set; } //Offset of the string table
		public Int32 StringsSize { get; set; } //Total length of the string block
		public Int32 Null4 { get; set; }
		//Header End

		//Other Vars
		public Int32 RSC7Offset;
		public Int32[] StringTableOffsets { get; set; }
		public Int32[] CodeTableOffsets { get; set; }
		public Int32 StringBlocks { get; set; }
		public Int32 CodeBlocks { get; set; }
		public string ScriptName { get; set; }
		public bool isRSC7 { get; private set; }

		static ScriptHeader GenerateConsoleHeader(Stream scriptStream)
		{
			ScriptHeader header = new ScriptHeader();
			IO.Reader reader = new IO.Reader(scriptStream, true);
			scriptStream.Seek(0, SeekOrigin.Begin);
			header.RSC7Offset = (reader.SReadUInt32() == 0x52534337) ? 0x10 : 0x0;
			scriptStream.Seek(header.RSC7Offset, SeekOrigin.Begin);
			header.Magic = reader.SReadInt32(); //0x0
			header.SubHeader = reader.SReadPointer(); //0x4
			header.CodeBlocksOffset = reader.SReadPointer(); //0x8
			header.GlobalsVersion = reader.SReadInt32(); //0x C
			header.CodeLength = reader.SReadInt32(); //0x10
			header.ParameterCount = reader.SReadInt32(); //0x14
			header.StaticsCount = reader.SReadInt32(); //0x18
			header.GlobalsCount = reader.SReadInt32(); //0x1C
			header.NativesCount = reader.SReadInt32(); //0x20
			header.StaticsOffset = reader.SReadPointer(); //0x24
			header.GlobalsOffset = reader.SReadPointer(); //0x28
			header.NativesOffset = reader.SReadPointer(); //0x2C
			header.Null1 = reader.SReadInt32(); //0x30
			header.Null2 = reader.SReadInt32(); //0x34
			header.NameHash = reader.SReadInt32();
			header.Null3 = reader.SReadInt32(); //0x38
			header.ScriptNameOffset = reader.SReadPointer(); //0x40
			header.StringsOffset = reader.SReadPointer(); //0x44
			header.StringsSize = reader.SReadInt32(); //0x48
			header.Null4 = reader.ReadInt32(); //0x4C

			header.StringBlocks = (header.StringsSize + 0x3FFF) >> 14;
			header.CodeBlocks = (header.CodeLength + 0x3FFF) >> 14;

			header.StringTableOffsets = new Int32[header.StringBlocks];
			scriptStream.Seek(header.StringsOffset + header.RSC7Offset, SeekOrigin.Begin);
			for (int i = 0; i < header.StringBlocks; i++)
				header.StringTableOffsets[i] = reader.SReadPointer() + header.RSC7Offset;


			header.CodeTableOffsets = new Int32[header.CodeBlocks];
			scriptStream.Seek(header.CodeBlocksOffset + header.RSC7Offset, SeekOrigin.Begin);
			for (int i = 0; i < header.CodeBlocks; i++)
				header.CodeTableOffsets[i] = reader.SReadPointer() + header.RSC7Offset;
			scriptStream.Position = header.ScriptNameOffset + header.RSC7Offset;
			int data = scriptStream.ReadByte();
			header.ScriptName = "";
			while (data != 0 && data != -1)
			{
				header.ScriptName += (char)data;
				data = scriptStream.ReadByte();
			}
			return header;
		}

		static ScriptHeader GeneratePcHeader(Stream scriptStream)
		{
			ScriptHeader header = new ScriptHeader();
			IO.Reader reader = new IO.Reader(scriptStream, false);
			scriptStream.Seek(0, SeekOrigin.Begin);
			header.RSC7Offset = (reader.SReadUInt32() == 0x52534337) ? 0x10 : 0x0;
			scriptStream.Seek(header.RSC7Offset, SeekOrigin.Begin);
			header.Magic = reader.ReadInt32(); //0x0
			reader.Advance();
			header.SubHeader = reader.ReadPointer(); //0x8
			reader.Advance();
			header.CodeBlocksOffset = reader.ReadPointer(); //0x10
			reader.Advance();
			header.GlobalsVersion = reader.ReadInt32(); //0x18
			header.CodeLength = reader.ReadInt32(); //0x1C
			header.ParameterCount = reader.ReadInt32(); //0x20
			header.StaticsCount = reader.ReadInt32(); //0x24
			header.GlobalsCount = reader.ReadInt32(); //0x28
			header.NativesCount = reader.ReadInt32(); //0x2C
			header.StaticsOffset = reader.ReadPointer(); //0x30
			reader.Advance();
			header.GlobalsOffset = reader.ReadPointer(); //0x38
			reader.Advance();
			header.NativesOffset = reader.ReadPointer(); //0x40
			reader.Advance();
			header.Null1 = reader.ReadInt32(); //0x48
			reader.Advance();
			header.Null2 = reader.ReadInt32(); //0x50
			reader.Advance();
			header.NameHash = reader.ReadInt32(); //0x58
			header.Null3 = reader.ReadInt32(); //0x5C
			header.ScriptNameOffset = reader.ReadPointer(); //0x60
			reader.Advance();
			header.StringsOffset = reader.ReadPointer(); //0x68
			reader.Advance();
			header.StringsSize = reader.ReadInt32(); //0x70
			reader.Advance();
			header.Null4 = reader.ReadInt32(); //0x78
			reader.Advance();

			header.StringBlocks = (header.StringsSize + 0x3FFF) >> 14;
			header.CodeBlocks = (header.CodeLength + 0x3FFF) >> 14;

			header.StringTableOffsets = new Int32[header.StringBlocks];
			scriptStream.Seek(header.StringsOffset + header.RSC7Offset, SeekOrigin.Begin);
			for (int i = 0; i < header.StringBlocks; i++)
			{
				header.StringTableOffsets[i] = reader.ReadPointer() + header.RSC7Offset;
				reader.Advance();
			}


			header.CodeTableOffsets = new Int32[header.CodeBlocks];
			scriptStream.Seek(header.CodeBlocksOffset + header.RSC7Offset, SeekOrigin.Begin);
			for (int i = 0; i < header.CodeBlocks; i++)
			{
				header.CodeTableOffsets[i] = reader.ReadPointer() + header.RSC7Offset;
				reader.Advance();
			}
			scriptStream.Position = header.ScriptNameOffset + header.RSC7Offset;
			int data = scriptStream.ReadByte();
			header.ScriptName = "";
			while (data != 0 && data != -1)
			{
				header.ScriptName += (char)data;
				data = scriptStream.ReadByte();
			}
			return header;
		}

		public static ScriptHeader Generate(Stream scriptStream, bool consoleVersion)
		{
			return consoleVersion ? GenerateConsoleHeader(scriptStream) : GeneratePcHeader(scriptStream);
		}

		private ScriptHeader()
		{ }
	}

}
