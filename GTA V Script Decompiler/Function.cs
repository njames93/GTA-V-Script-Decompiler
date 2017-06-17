using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;			   
using System.Threading;

namespace Decompiler
{
	[Serializable]
	public class DecompilingException : Exception
	{
		public DecompilingException(string Message) : base(Message)
		{
		}
	}

	public class Function
	{
		public string Name { get; private set; }
		public int Pcount { get; private set; }
		public int Vcount { get; private set; }
		public int Rcount { get; private set; }
		public int Location { get; private set; }
		public int MaxLocation { get; private set; }
		StringBuilder sb = null;
		List<HLInstruction> Instructions;
		Dictionary<int, int> InstructionMap;
		Stack Stack;
		int Offset = 0;
		string tabs = "";
		CodePath Outerpath;
		SwitchStatement OuterSwitch;
		ScriptFile Scriptfile;
		bool writeelse = false;
		//ReturnTypes RetType = ReturnTypes.Unkn;
		public Types.DataTypes ReturnType { get; private set; }
		FunctionName fnName;
		internal bool Decoded { get; private set; }
		internal bool DecodeStarted = false;
		internal bool predecoded = false;
		internal bool predecodeStarted = false;
		public Vars_Info Vars { get; private set; }
		public Vars_Info Params { get; private set; }
		public int LineCount = 0;
		private readonly bool _consoleVer;


		public Function(ScriptFile Owner, string name, int pcount, int vcount, int rcount, int location, int locmax = -1)
		{
			this.Scriptfile = Owner;
			_consoleVer = Owner.ConsoleVer;
			Name = name;
			Pcount = pcount;
			Vcount = vcount;
			Rcount = rcount;
			Location = location;
			if (locmax != -1)
				MaxLocation = locmax;
			else
				MaxLocation = Location;
			fnName = new FunctionName(Name, Pcount, Rcount, Location, MaxLocation);
			this.Scriptfile.FunctionLoc.Add(location, fnName);
			Decoded = false;
			Vars = new Vars_Info(Vars_Info.ListType.Vars, vcount - 2);
			Params = new Vars_Info(Vars_Info.ListType.Params, pcount);
		}

		/// <summary>
		/// Disposes of the function and returns the function text
		/// </summary>
		/// <returns>The whole function high level code</returns>
		public override string ToString()
		{
			InstructionMap.Clear();
			Instructions.Clear();
			CodeBlock.Clear();
			Stack.Dispose();
			try
			{
					return FirstLine() + "\r\n" + sb.ToString();

				/*if (RetType == ReturnTypes.Bool || RetType == ReturnTypes.BoolUnk)
					return FirstLine + "\r\n" + sb.ToString().Replace("return 0;", "return false;").Replace("return 1;", "return true;");
				else
					return FirstLine + "\r\n" + sb.ToString();*/
			}
			finally
			{
				sb.Clear();
				LineCount += 2;
			}
		}

		/// <summary>
		/// Gets the first line of the function Declaration
		/// return type + name + params
		/// </summary>
		public string FirstLine()
		{
			string name;
			string working = "";

			//extract return type of function
			if (Rcount == 0)
				working = "void ";
			else if (Rcount == 1)
			{
				working = ReturnType.returntype;
				/*switch (RetType)
				{
					case ReturnTypes.Bool:
					case ReturnTypes.BoolUnk:
						working = "bool "; break;
					case ReturnTypes.Float:
						working = "float "; break;
					case ReturnTypes.Int: working = "int "; break;
					case ReturnTypes.StringPtr: working = "*string "; break;
					default: working = "var "; break;
				}*/
			}
			else if (Rcount == 3)
				working = "Vector3 ";
			else if (Rcount > 1)
			{
				if (ReturnType.type == Stack.DataType.String)
				{
					working = "char[" + (Rcount*4).ToString() + "] ";
				}
				else
				{
					working = "struct<" + Rcount.ToString() + "> ";
				}
				/*if (RetType == ReturnTypes.String)
				{
					working = "char[" + (Rcount * 4).ToString() + "] ";
				}
				else
				{
					working = "struct<" + Rcount.ToString() + "> ";
				}*/
			}
			else throw new DecompilingException("Unexpected return count");

			name = working + Name;
			working = "(" + Params.GetPDec() + ")";

			return name + working + (Program.IncFuncPos ? ("//Position - 0x" + Location.ToString("X")) : "");
		}

		/// <summary>
		/// Determines if a frame variable is a parameter or a variable and returns its name
		/// </summary>
		/// <param name="index">the frame variable index</param>
		/// <returns>the name of the variable</returns>
		public string GetFrameVarName(uint index)
		{
			if (index < Pcount)
				return Params.GetVarName(index);
			else if (index < Pcount + 2)
				throw new Exception("Unexpecteed fvar");
			return Vars.GetVarName((uint) (index - 2 - Pcount));
		}

		/// <summary>
		/// Determines if a frame variable is a parameter or a variable and returns its index
		/// </summary>
		/// <param name="index">the frame variable index</param>
		/// <returns>The variable</returns>
		public Vars_Info.Var GetFrameVar(uint index)
		{
			if (index < Pcount)
				return Params.GetVarAtIndex(index);
			else if (index < Pcount + 2)
				throw new Exception("Unexpecteed fvar");
			return Vars.GetVarAtIndex((uint) (index - 2 - Pcount));
		}

		/// <summary>
		/// The block of code that the function takes up
		/// </summary>
		public List<byte> CodeBlock { get; set; }

		/// <summary>
		/// Gets the function info given the offset where its called from
		/// </summary>
		/// <param name="offset">the offset that is being called</param>
		/// <returns>basic information about the function at that offset</returns>
		public FunctionName GetFunctionNameFromOffset(int offset)
		{
			if (Scriptfile.FunctionLoc.ContainsKey(offset))
				return Scriptfile.FunctionLoc[offset];
			throw new Exception("Function Not Found");
		}

		/// <summary>
		/// Gets the function info given the offset where its called from
		/// </summary>
		/// <param name="offset">the offset that is being called</param>
		/// <returns>basic information about the function at that offset</returns>
		public Function GetFunctionFromOffset(int offset)
		{
			foreach (Function f in Scriptfile.Functions)
			{
				if (f.Location <= offset && offset <= f.MaxLocation)
					return f;
			}
			throw new Exception("Function Not Found");
		}

		public void ScruffDissasemble()
		{
			//getinstructions(false);

		}

		/// <summary>
		/// Indents everything below by 1 tab space
		/// </summary>
		/// <param name="write">if true(or default) it will write the open curly bracket, {</param>
		void opentab(bool write = true)
		{
			if (write)
				writeline("{");
			tabs += "\t";
		}

		/// <summary>
		/// Removes 1 tab space from indentation of everything below it
		/// </summary>
		/// <param name="write">if true(or default) it will write the close curly bracket, }</param>
		void closetab(bool write = true)
		{
			if (tabs.Length > 0)
			{
				tabs = tabs.Remove(tabs.Length - 1);
			}
			if (write)
				writeline("}");
		}

		/// <summary>
		/// Step done before decoding, getting the variables types
		/// Aswell as getting the list of instructions
		/// Needs to PreDecode all functions before decoding any as this step
		/// Builds The Static Variable types aswell
		/// </summary>
		public void PreDecode()
		{
			if (predecoded || predecodeStarted) return;
			predecodeStarted = true;
			getinstructions();
			decodeinsructionsforvarinfo();
			predecoded = true;
		}

		/// <summary>
		/// The method that actually decodes the function into high level
		/// </summary>
		public void Decode()
		{
			lock (Program.ThreadLock)
			{
				DecodeStarted = true;
				if (Decoded) return;
			}
			//Set up a stack
			Stack = new Stack();

			//Get The Instructions in the function along with their operands
			//getinstructions();

			//Set up the codepaths to a null item
			Outerpath = new CodePath(CodePathType.Main, CodeBlock.Count, -1);
			OuterSwitch = new SwitchStatement(null, -1);

			sb = new StringBuilder();
			opentab();
			Offset = 0;

			//write all the function variables declared by the function
			if (Program.Declare_Variables)
			{
				bool temp = false;
				foreach (string s in Vars.GetDeclaration(_consoleVer))
				{
					writeline(s);
					temp = true;
				}
				if (temp) writeline("");
			}
			while (Offset < Instructions.Count)
				decodeinstruction();
			//Fix for switches that end at the end of a function
			while (OuterSwitch.Parent != null)
			{
				closetab(false);
				closetab();
				OuterSwitch = OuterSwitch.Parent;
			}
			closetab();
			//fnName.RetType = RetType;
			fnName.retType = ReturnType;
			Decoded = true;
		}

		/// <summary>
		/// Writes a line to the function text as well as any tab chars needed before it
		/// </summary>
		/// <param name="line">the line to write</param>
		void writeline(string line)
		{
			if (writeelse)
			{
				writeelse = false;
				writeline("else");
				opentab(true);
			}
			AppendLine(tabs + line);
		}

		public void AppendLine(string line)
		{
			sb.AppendLine(line);
			LineCount++;
		}

		/// <summary>
		/// Check if a jump is jumping out of the function
		/// if not, then add it to the list of instructions
		/// </summary>
		void checkjumpcodepath()
		{
			int cur = Offset;
			HLInstruction temp = new HLInstruction(CodeBlock[Offset], GetArray(2), cur, _consoleVer);
			if (temp.GetJumpOffset > 0)
			{
				if (temp.GetJumpOffset < CodeBlock.Count)
				{
					AddInstruction(cur, temp);
					return;
				}
			}

			//if the jump is out the function then its useless
			//So nop this jump
			AddInstruction(cur, new HLInstruction((byte) 0, cur, _consoleVer));
			AddInstruction(cur + 1, new HLInstruction((byte) 0, cur + 1, _consoleVer));
			AddInstruction(cur + 2, new HLInstruction((byte) 0, cur + 2, _consoleVer));
		}

		/// <summary>
		/// See if a dup is being used for an AND or OR
		/// if it is, dont add it (Rockstars way of doing and/or conditionals)
		/// </summary>
		void checkdupforinstruction()
		{
			//May need refining, but works fine for rockstars code
			int off = 0;
			Start:
			off += 1;
			if (CodeBlock[Offset + off] == 0)
				goto Start;
			if (CodeBlock[Offset + off] == 86)
			{
				Offset = Offset + off + 2;
				return;
			}
			if (CodeBlock[Offset + off] == 6)
			{
				goto Start;
			}
			Instructions.Add(new HLInstruction(CodeBlock[Offset], Offset, _consoleVer));
			return;
		}

		/// <summary>
		/// Gets the given amount of bytes from the codeblock at its offset
		/// while advancing its position by how ever many items it uses
		/// </summary>
		/// <param name="items">how many bytes to grab</param>
		/// <returns>the operands for the instruction</returns>
		IEnumerable<byte> GetArray(int items)
		{
			int temp = Offset + 1;
			Offset += items;

			return CodeBlock.GetRange(temp, items);
		}

		/// <summary>
		/// When we hit a jump, decide how to handle it
		/// </summary>
		void handlejumpcheck()
		{
			//Check the jump location against each switch statement, to see if it is recognised as a break
			SwitchStatement tempsw = OuterSwitch;
			startsw:
			if (Instructions[Offset].GetJumpOffset == tempsw.breakoffset)
			{
				writeline("break;");
				return;
			}
			else
			{
				if (tempsw.Parent != null)
				{
					tempsw = tempsw.Parent;
					goto startsw;
				}
			}
			int tempoff = 0;
			if (Instructions[Offset + 1].Offset == Outerpath.EndOffset)
			{

				if (Instructions[Offset].GetJumpOffset != Instructions[Offset + 1].Offset)
				{

					if (!Instructions[Offset].IsWhileJump)
					{
						//The jump is detected as being an else statement
						//finish the current if code path and add an else code path
						CodePath temp = Outerpath;
						Outerpath = Outerpath.Parent;
						Outerpath.ChildPaths.Remove(temp);
						Outerpath = Outerpath.CreateCodePath(CodePathType.Else, Instructions[Offset].GetJumpOffset, -1);
						closetab();
						writeelse = true;
						return;
					}
					throw new Exception("Shouldnt find a while loop here");
				}
				return;
			}
			start:
			//Check to see if the jump is just jumping past nops(end of code table)
			//should be the only case for finding another jump now
			if (Instructions[Offset].GetJumpOffset != Instructions[Offset + 1 + tempoff].Offset)
			{
				if (Instructions[Offset + 1 + tempoff].Instruction == Instruction.Nop)
				{
					tempoff++;
					goto start;
				}
				else if (Instructions[Offset + 1 + tempoff].Instruction == Instruction.Jump)
				{
					if (Instructions[Offset + 1 + tempoff].GetOperandsAsInt == 0)
					{
						tempoff++;
						goto start;
					}

				}
				//These seem to be cause from continue statements in for loops
				//But given the current implementation of codepaths, it is not really faesible
				//to add in support for for loops. And to save rewriting the entire codepath handling
				//I'll just ignore this case, only occurs in 2 scripts in the whole script_rel.rpf
				//If I was to fix this, it would involve rewriting the codepath(probably as a tree
				//structure like it really should've been done in the first place
				if (Instructions[Offset].GetOperandsAsInt != 0)
				{
					writeline("Jump @" + Instructions[Offset].GetJumpOffset.ToString() + $"; //curOff = {Instructions[Offset].Offset}");
					//int JustOffset = InstructionMap[Instructions[Offset].GetJumpOffset];
					//HLInstruction instruction = Instructions[JustOffset];
					System.Diagnostics.Debug.WriteLine(this.Scriptfile.name);
				}
			}
		}

		//Needs Merging with method below
		bool isnewcodepath()
		{
			if (Outerpath.Parent != null)
			{
				if (InstructionMap[Outerpath.EndOffset] == Offset)
				{
					return true;
				}
			}
			if (OuterSwitch.Offsets.Count > 0)
			{
				if (Instructions[Offset].Offset == OuterSwitch.Offsets[0])
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Checks if the current offset is a new code path, then decides how to handle it
		/// </summary>
		void handlenewpath()
		{
			start:
			if (Instructions[Offset].Offset == Outerpath.EndOffset)
			{
				//Offset recognised as the exit instruction of the outermost code path
				//remove outermost code path
				CodePath temp = Outerpath;
				Outerpath = Outerpath.Parent;
				Outerpath.ChildPaths.Remove(temp);
				closetab();
				//check next codepath to see if it belongs there aswell
				goto start;
			}
			if (OuterSwitch.Offsets.Count > 0)
			{
				if (Instructions[Offset].Offset == OuterSwitch.Offsets[0])
				{
					closetab(false);

					if (OuterSwitch.Offsets.Count == 1)
					{
						//end of switch statement detected
						//remove child class 
						SwitchStatement temp = OuterSwitch;
						OuterSwitch = OuterSwitch.Parent;
						OuterSwitch.ChildSwitches.Remove(temp);
						closetab();
						//go check if its the next switch exit instruction
						//probably isnt and the goto can probably be removed
						goto start;
					}
					else
					{
						//more cases left in switch
						//so write the next switch case
						writeline("");
						for (int i = 0; i < (OuterSwitch.Cases[OuterSwitch.Offsets[0]]).Count; i++)
						{
							string temp = OuterSwitch.Cases[OuterSwitch.Offsets[0]][i];
							if (temp == "default")
								writeline("default:");
							else
								writeline("case " + temp + ":");
						}
						opentab(false);

						//remove last switch case from class, so it wont attemp to jump there again
						OuterSwitch.Offsets.RemoveAt(0);

						//as before, probably not needed, so should always skip past here
						goto start;
					}
				}
			}
		}

		/// <summary>
		/// Create a switch statement, then set up the rest of the decompiler to handle the rest of the switch statement
		/// </summary>
		void handleswitch()
		{
			Dictionary<int, List<string>> cases = new Dictionary<int, List<string>>();
			string case_val;
			int offset;
			int defaultloc;
			int breakloc;
			bool usedefault;
			HLInstruction temp;

			for (int i = 0; i < Instructions[Offset].GetOperand(0); i++)
			{
				//Check if the case is a known hash
				case_val = Instructions[Offset].GetSwitchStringCase(i);

				//Get the offset to jump to
				offset = Instructions[Offset].GetSwitchOffset(i);


				if (!cases.ContainsKey(offset))
				{
					//unknown offset
					cases.Add(offset, new List<string>(new string[] {case_val}));
				}
				else
				{
					//This offset is known, multiple cases are jumping to this path
					cases[offset].Add(case_val);
				}
			}

			//Not sure how necessary this step is, but just incase R* compiler doesnt order jump offsets, do it anyway
			List<int> sorted = cases.Keys.ToList();
			sorted.Sort();

			//Hanldle(skip past) any Nops immediately after switch statement
			int tempoff = 0;
			while (Instructions[Offset + 1 + tempoff].Instruction == Instruction.Nop)
				tempoff++;

			//Extract the location to jump to if no cases match
			defaultloc = Instructions[Offset + 1 + tempoff].GetJumpOffset;

			//We have found the jump location, so that instruction is no longer needed and can be nopped
			Instructions[Offset + 1 + tempoff].NopInstruction();

			//Temporary stage
			breakloc = defaultloc;
			usedefault = true;
			bool allreturns = true;

			//check if case last instruction is a jump to default location, if so default location is a break;
			//if not break location is where last instrcution jumps to
			for (int i = 0; i <= sorted.Count; i++)
			{
				int index = 0;
				if (i == sorted.Count)
					index = InstructionMap[defaultloc] - 1;
				else
					index = InstructionMap[sorted[i]] - 1;
				if (index - 1 == Offset)
				{
					continue;
				}
				temp = Instructions[index];
				if (temp.Instruction != Instruction.Jump)
				{
					if (temp.Instruction != Instruction.Return)
						allreturns = false;
					continue;
				}
				if (temp.GetJumpOffset == defaultloc)
				{
					usedefault = false;
					breakloc = defaultloc;
					break;
				}
				breakloc = temp.GetJumpOffset;
			}
			if (usedefault)
			{
				//this seems to be causing some errors
				// if (allreturns)
				// usedefault = false;
			}

			if (usedefault)
			{
				//Default location found, best add it in
				if (cases.ContainsKey(defaultloc))
				{
					//Default location shares code path with other known case
					cases[defaultloc].Add("default");
				}
				else
				{
					//Default location is a new code path
					cases.Add(defaultloc, new List<string>(new string[] {"default"}));
					sorted = cases.Keys.ToList();
					sorted.Sort();
				}
			}

			//Found all information about switch, write the first case, the rest will be handled when we get to them
			writeline("switch (" + Stack.PopLit() + ")");
			opentab();
			for (int i = 0; i < cases[sorted[0]].Count; i++)
			{
				writeline("case " + cases[sorted[0]][i] + ":");
			}
			opentab(false);
			cases.Remove(sorted[0]);

			//Create the class the rest of the decompiler needs to handle the rest of the switch
			OuterSwitch = OuterSwitch.CreateSwitchStatement(cases, breakloc);
		}

		/// <summary>
		/// If we have a conditional statement determine whether its for an if/while statement
		/// Then handle it accordingly
		/// </summary>
		void CheckConditional()
		{
			string tempstring = Stack.PopLit();
			if (!(tempstring.StartsWith("(") && tempstring.EndsWith(")")))
				tempstring = "(" + tempstring + ")";

			int offset = Instructions[Offset].GetJumpOffset;
			CodePath tempcp = Outerpath;
			start:

			if (tempcp.Type == CodePathType.While)
			{
				if (offset == tempcp.EndOffset)
				{
					writeline("if " + tempstring);
					opentab(false);
					writeline("break;");
					closetab(false);
					return;
				}
			}

			if (tempcp.Parent != null)
			{
				tempcp = tempcp.Parent;
				goto start;
			}
			HLInstruction jumploc = Instructions[InstructionMap[offset] - 1];

			if (jumploc.IsWhileJump && jumploc.GetJumpOffset < Instructions[Offset].Offset)
			{
				jumploc.NopInstruction();
				if (tempstring == "(1)")
					tempstring = "(true)";
				writeline("while " + tempstring);
				Outerpath = Outerpath.CreateCodePath(CodePathType.While, Instructions[Offset].GetJumpOffset, -1);
				opentab();
			}
			else
			{
				bool written = false;
				if (writeelse)
				{
					if (Outerpath.EndOffset == Instructions[Offset].GetJumpOffset)
					{
						writeelse = false;
						CodePath temp = Outerpath;
						Outerpath = Outerpath.Parent;
						Outerpath.ChildPaths.Remove(temp);
						Outerpath = Outerpath.CreateCodePath(CodePathType.If, Instructions[Offset].GetJumpOffset, -1);
						writeline("else if " + tempstring);
						opentab();
						written = true;
					}
					else if (Instructions[InstructionMap[Instructions[Offset].GetJumpOffset] - 1].Instruction == Instruction.Jump)
					{
						if (Outerpath.EndOffset == Instructions[InstructionMap[Instructions[Offset].GetJumpOffset] - 1].GetJumpOffset)
						{
							writeelse = false;
							CodePath temp = Outerpath;
							Outerpath = Outerpath.Parent;
							Outerpath.ChildPaths.Remove(temp);
							Outerpath = Outerpath.CreateCodePath(CodePathType.If, Instructions[Offset].GetJumpOffset, -1);
							writeline("else if " + tempstring);
							opentab();
							written = true;
						}
					}
				}
				if (!written)
				{
					writeline("if " + tempstring);
					Outerpath = Outerpath.CreateCodePath(CodePathType.If, Instructions[Offset].GetJumpOffset, -1);
					opentab();
				}
			}
		}

		/// <summary>
		/// Turns the raw code into a list of instructions
		/// </summary>
		public void getinstructions()
		{
			Offset = CodeBlock[4] + 5;
			Instructions = new List<HLInstruction>();
			InstructionMap = new Dictionary<int, int>();
			int curoff;
			while (Offset < CodeBlock.Count)
			{
				while (Offset < CodeBlock.Count)
				{
					curoff = Offset;
					switch (CodeBlock[Offset])
					{
						//		case 0: if (addnop) AddInstruction(curoff, new HLInstruction((byte)0, curoff)); break;
						case 37:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(1), curoff, _consoleVer));
							break;
						case 38:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(2), curoff, _consoleVer));
							break;
						case 39:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(3), curoff, _consoleVer));
							break;
						case 40:
						case 41:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(4), curoff, _consoleVer));
							break;
						case 42: //Because of how rockstar codes and/or conditionals, its neater to detect dups
							//and only add them if they are not used for conditionals
							checkdupforinstruction();
							break;
						case 44:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(3), curoff, _consoleVer));
							break;
						case 45:
							throw new Exception("Function not exptected");
						case 46:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(2), curoff, _consoleVer));
							break;
						case 52:
						case 53:
						case 54:
						case 55:
						case 56:
						case 57:
						case 58:
						case 59:
						case 60:
						case 61:
						case 62:
						case 64:
						case 65:
						case 66:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(1), curoff, _consoleVer));
							break;
						case 67:
						case 68:
						case 69:
						case 70:
						case 71:
						case 72:
						case 73:
						case 74:
						case 75:
						case 76:
						case 77:
						case 78:
						case 79:
						case 80:
						case 81:
						case 82:
						case 83:
						case 84:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(2), curoff, _consoleVer));
							break;
						case 85:
							checkjumpcodepath();
							break;
						case 86:
						case 87:
						case 88:
						case 89:
						case 90:
						case 91:
						case 92:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(2), curoff, _consoleVer));
							break;
						case 93:
						case 94:
						case 95:
						case 96:
						case 97:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(3), curoff, _consoleVer));
							break;
						case 98:
							int temp = CodeBlock[Offset + 1];
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(temp*6 + 1), curoff, _consoleVer));
							break;
						case 101:
						case 102:
						case 103:
						case 104:
							AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], GetArray(1), curoff, _consoleVer));
							break;
						default:
							if (CodeBlock[Offset] <= 126) AddInstruction(curoff, new HLInstruction(CodeBlock[Offset], curoff, _consoleVer));
							else throw new Exception("Unexpected Opcode");
							break;
					}
					Offset++;
				}
			}
		}


		/// <summary>
		/// Adds an instruction to the list of instructions
		/// then adds the offset to a dictionary
		/// </summary>
		/// <param name="offset">the offset in the code</param>
		/// <param name="instruction">the instruction</param>
		void AddInstruction(int offset, HLInstruction instruction)
		{
			Instructions.Add(instruction);
			InstructionMap.Add(offset, Instructions.Count - 1);
		}

		/// <summary>
		/// Decodes the instruction at the current offset
		/// </summary>
		public void decodeinstruction()
		{
			object temp;
			int tempint;
			string tempstring;
			if (isnewcodepath()) handlenewpath();
			switch (Instructions[Offset].Instruction)
			{
				case Instruction.Nop:
					break;
				case Instruction.iAdd:
					Stack.Op_Add();
					break;
				case Instruction.fAdd:
					Stack.Op_Addf();
					break;
				case Instruction.iSub:
					Stack.Op_Sub();
					break;
				case Instruction.fSub:
					Stack.Op_Subf();
					break;
				case Instruction.iMult:
					Stack.Op_Mult();
					break;
				case Instruction.fMult:
					Stack.Op_Multf();
					break;
				case Instruction.iDiv:
					Stack.Op_Div();
					break;
				case Instruction.fDiv:
					Stack.Op_Divf();
					break;
				case Instruction.iMod:
					Stack.Op_Mod();
					break;
				case Instruction.fMod:
					Stack.Op_Modf();
					break;
				case Instruction.iNot:
					Stack.Op_Not();
					break;
				case Instruction.iNeg:
					Stack.Op_Neg();
					break;
				case Instruction.fNeg:
					Stack.Op_Negf();
					break;
				case Instruction.iCmpEq:
				case Instruction.fCmpEq:
					Stack.Op_CmpEQ();
					break;
				case Instruction.iCmpNe:
				case Instruction.fCmpNe:
					Stack.Op_CmpNE();
					break;
				case Instruction.iCmpGt:
				case Instruction.fCmpGt:
					Stack.Op_CmpGT();
					break;
				case Instruction.iCmpGe:
				case Instruction.fCmpGe:
					Stack.Op_CmpGE();
					break;
				case Instruction.iCmpLt:
				case Instruction.fCmpLt:
					Stack.Op_CmpLT();
					break;
				case Instruction.iCmpLe:
				case Instruction.fCmpLe:
					Stack.Op_CmpLE();
					break;
				case Instruction.vAdd:
					Stack.Op_Vadd();
					break;
				case Instruction.vSub:
					Stack.Op_VSub();
					break;
				case Instruction.vMult:
					Stack.Op_VMult();
					break;
				case Instruction.vDiv:
					Stack.Op_VDiv();
					break;
				case Instruction.vNeg:
					Stack.Op_VNeg();
					break;
				case Instruction.And:
					Stack.Op_And();
					break;
				case Instruction.Or:
					Stack.Op_Or();
					break;
				case Instruction.Xor:
					Stack.Op_Xor();
					break;
				case Instruction.ItoF:
					Stack.Op_Itof();
					break;
				case Instruction.FtoI:
					Stack.Op_FtoI();
					break;
				case Instruction.FtoV:
					Stack.Op_FtoV();
					break;
				case Instruction.iPushByte1:
					Stack.Push(Instructions[Offset].GetOperand(0));
					break;
				case Instruction.iPushByte2:
					Stack.Push(Instructions[Offset].GetOperand(0), Instructions[Offset].GetOperand(1));
					break;
				case Instruction.iPushByte3:
					Stack.Push(Instructions[Offset].GetOperand(0), Instructions[Offset].GetOperand(1),
						Instructions[Offset].GetOperand(2));
					break;
				case Instruction.iPushInt:
				case Instruction.iPushI24:
					tempstring = "";
					if (Program.Show_Func_Pointer)
					{
						tempint = Instructions[Offset].GetOperandsAsInt;
						if (tempint > 10)
							//sanity check, though should never really be an issue as any push values < 10 wont be 3 or 4 byte pushes
						{
							if (Scriptfile.FunctionLoc.ContainsKey(tempint))
							{
								tempstring = "/*" + Scriptfile.FunctionLoc[tempint].Name + "*/";
							}
						}
					}
					Stack.DataType type = Stack.DataType.Int;
					if (Program.getIntType == Program.IntType._uint)
						Stack.Push(ScriptFile.hashbank.GetHash(Instructions[Offset].GetOperandsAsUInt, tempstring), type);
					else
						Stack.Push(ScriptFile.hashbank.GetHash(Instructions[Offset].GetOperandsAsInt, tempstring), type);
					break;
				case Instruction.iPushShort:
					Stack.Push(Instructions[Offset].GetOperandsAsInt);
					break;
				case Instruction.fPush:
					Stack.Push(Instructions[Offset].GetFloat);
					break;
				case Instruction.dup:
					Stack.Dup();
					break;
				case Instruction.pop:
					temp = Stack.Drop();
					if (temp is string)
						writeline(temp as string);
					break;
				case Instruction.Native:
					if (_consoleVer)
						tempstring =
							Stack.NativeCallTest(this.Scriptfile.NativeTable.GetNativeHashFromIndex(Instructions[Offset].GetNativeIndex),
								this.Scriptfile.NativeTable.GetNativeFromIndex(Instructions[Offset].GetNativeIndex),
								Instructions[Offset].GetNativeParams, Instructions[Offset].GetNativeReturns);
					else
						tempstring =
							Stack.NativeCallTest(this.Scriptfile.X64NativeTable.GetNativeHashFromIndex(Instructions[Offset].GetNativeIndex),
								this.Scriptfile.X64NativeTable.GetNativeFromIndex(Instructions[Offset].GetNativeIndex),
								Instructions[Offset].GetNativeParams, Instructions[Offset].GetNativeReturns);
					//tempstring = Stack.FunctionCall(this.Scriptfile.NativeTable.GetNativeFromIndex(Instructions[Offset].GetNativeIndex), Instructions[Offset].GetNativeParams, Instructions[Offset].GetNativeReturns);
					if (tempstring != "")
					{
						writeline(tempstring);
					}
					break;
				case Instruction.Enter:
					throw new Exception("Unexpected Function Definition");
				case Instruction.Return:
					Stack.DataType DataType = Instructions[Offset].GetOperand(1) == 1 ? Stack.TopType : Stack.DataType.Unk;
					tempstring = Stack.PopListForCall(Instructions[Offset].GetOperand(1));
					switch (Instructions[Offset].GetOperand(1))
					{
						case 0:
							if (Offset < Instructions.Count - 1)
								writeline("return;");
							break;
						case 1:
							switch (DataType)
							{
								case Stack.DataType.Bool:
								case Stack.DataType.Float:
								case Stack.DataType.StringPtr:
								case Stack.DataType.Int:
									ReturnType = Types.gettype(DataType);
									break;
								default:
									returncheck(tempstring);
									break;
							}
							writeline("return " + tempstring + ";");
							break;
						default:
							if (Stack.TopType == Stack.DataType.String)
							{
								//RetType = ReturnTypes.String;
								ReturnType = Types.gettype(Stack.DataType.String);
							}
							writeline("return " + tempstring + ";");
							break;
					}
					break;
				case Instruction.pGet:
					Stack.Op_RefGet();
					break;
				case Instruction.pSet:
					if (Stack.PeekVar(1) == null)
					{
						writeline(Stack.Op_RefSet());
					}
					else if (Stack.PeekVar(1).Is_Array)
					{
						Stack.Op_RefSet();
					}
					else
						writeline(Stack.Op_RefSet());
					break;
				case Instruction.pPeekSet:
					if (Stack.PeekVar(1) == null)
					{
						writeline(Stack.Op_PeekSet());
					}
					else if (Stack.PeekVar(1).Is_Array)
					{
						Stack.Op_PeekSet();
					}
					else
						writeline(Stack.Op_PeekSet());
					break;
				case Instruction.ToStack:
					Stack.Op_ToStack();
					break;
				case Instruction.FromStack:
					writeline(Stack.Op_FromStack());
					break;
				case Instruction.pArray1:
				case Instruction.pArray2:
					Stack.Op_ArrayGetP(Instructions[Offset].GetOperandsAsUInt);
					break;
				case Instruction.ArrayGet1:
				case Instruction.ArrayGet2:
					Stack.Op_ArrayGet(Instructions[Offset].GetOperandsAsUInt);
					break;
				case Instruction.ArraySet1:
				case Instruction.ArraySet2:
					writeline(Stack.Op_ArraySet(Instructions[Offset].GetOperandsAsUInt));
					break;
				case Instruction.pFrame1:
				case Instruction.pFrame2:
					Stack.PushPVar(GetFrameVarName(Instructions[Offset].GetOperandsAsUInt),
						GetFrameVar(Instructions[Offset].GetOperandsAsUInt));
					break;
				case Instruction.GetFrame1:
				case Instruction.GetFrame2:
					Stack.PushVar(GetFrameVarName(Instructions[Offset].GetOperandsAsUInt),
						GetFrameVar(Instructions[Offset].GetOperandsAsUInt));
					break;
				case Instruction.SetFrame1:
				case Instruction.SetFrame2:
					tempstring = Stack.Op_Set(GetFrameVarName(Instructions[Offset].GetOperandsAsUInt),
						GetFrameVar(Instructions[Offset].GetOperandsAsUInt));
					if (GetFrameVar(Instructions[Offset].GetOperandsAsUInt).DataType == Stack.DataType.Bool)
						tempstring = tempstring.Replace("= 0;", "= false;").Replace("= 1;", "= true;");
					if (!GetFrameVar(Instructions[Offset].GetOperandsAsUInt).Is_Array)
						writeline(tempstring);
					break;
				case Instruction.pStatic1:
				case Instruction.pStatic2:
					Stack.PushPVar(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt),
						Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt));
					break;
				// Stack.PushPointer(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt)); break;
				case Instruction.StaticGet1:
				case Instruction.StaticGet2:
					Stack.PushVar(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt),
						Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt));
					break;
				//Stack.Push(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt), Scriptfile.Statics.GetTypeAtIndex(Instructions[Offset].GetOperandsAsUInt)); break;
				case Instruction.StaticSet1:
				case Instruction.StaticSet2:
					tempstring = Stack.Op_Set(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt),
						Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt));
					if (Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt).DataType == Stack.DataType.Bool)
						tempstring = tempstring.Replace("= 0;", "= false;").Replace("= 1;", "= true;");
					if (!Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt).Is_Array)
						writeline(tempstring);
					break;
				case Instruction.Add1:
				case Instruction.Add2:
					Stack.Op_AmmImm(Instructions[Offset].GetOperandsAsInt);
					break;
				case Instruction.Mult1:
				case Instruction.Mult2:
					Stack.Op_MultImm(Instructions[Offset].GetOperandsAsInt);
					break;
				case Instruction.pStructStack:
					Stack.Op_GetImmP();
					break;
				case Instruction.pStruct1:
				case Instruction.pStruct2:
					Stack.Op_GetImmP(Instructions[Offset].GetOperandsAsUInt);
					break;
				case Instruction.GetStruct1:
				case Instruction.GetStruct2:
					Stack.Op_GetImm(Instructions[Offset].GetOperandsAsUInt);
					break;
				case Instruction.SetStruct1:
				case Instruction.SetStruct2:
					writeline(Stack.Op_SetImm(Instructions[Offset].GetOperandsAsUInt));
					break;
				case Instruction.pGlobal2:
				case Instruction.pGlobal3:
					Stack.PushPGlobal(Instructions[Offset].GetGlobalString);
					break;
				case Instruction.GlobalGet2:
				case Instruction.GlobalGet3:
					Stack.PushGlobal(Instructions[Offset].GetGlobalString);
					break;
				case Instruction.GlobalSet2:
				case Instruction.GlobalSet3:
					writeline(Stack.Op_Set(Instructions[Offset].GetGlobalString));
					break;
				case Instruction.Jump:
					handlejumpcheck();
					break;
				case Instruction.JumpFalse:
					goto HandleJump;
				case Instruction.JumpNe:
					Stack.Op_CmpEQ();
					goto HandleJump;
				case Instruction.JumpEq:
					Stack.Op_CmpNE();
					goto HandleJump;
				case Instruction.JumpLe:
					Stack.Op_CmpGE();
					goto HandleJump;
				case Instruction.JumpLt:
					Stack.Op_CmpGT();
					goto HandleJump;
				case Instruction.JumpGe:
					Stack.Op_CmpLE();
					goto HandleJump;
				case Instruction.JumpGt:
					Stack.Op_CmpLT();
					goto HandleJump;
				case Instruction.Call:
					FunctionName tempf = GetFunctionNameFromOffset(Instructions[Offset].GetOperandsAsInt);
					tempstring = Stack.FunctionCall(tempf.Name, tempf.Pcount, tempf.Rcount);
					if (tempstring != "")
					{
						writeline(tempstring);
					}
					break;
				case Instruction.Switch:
					handleswitch();
					break;
				case Instruction.PushString:
					tempstring = Stack.PopLit();
					if (!Utils.IntParse(tempstring, out tempint))
						Stack.Push("StringTable(" + tempstring + ")", Stack.DataType.StringPtr);
					else if (!this.Scriptfile.StringTable.StringExists(tempint))
						Stack.Push("StringTable(" + tempstring + ")", Stack.DataType.StringPtr);
					else
						Stack.Push("\"" + this.Scriptfile.StringTable[tempint] + "\"", Stack.DataType.StringPtr);
					break;
				case Instruction.GetHash:
					Stack.Op_Hash();
					break;
				case Instruction.StrCopy:
					writeline(Stack.op_strcopy(Instructions[Offset].GetOperandsAsInt));
					break;
				case Instruction.ItoS:
					writeline(Stack.op_itos(Instructions[Offset].GetOperandsAsInt));
					break;
				case Instruction.StrConCat:
					writeline(Stack.op_stradd(Instructions[Offset].GetOperandsAsInt));
					break;
				case Instruction.StrConCatInt:
					writeline(Stack.op_straddi(Instructions[Offset].GetOperandsAsInt));
					break;
				case Instruction.MemCopy:
					writeline(Stack.op_sncopy());
					break;
				case Instruction.Catch:
					throw new Exception(); // writeline("catch;"); break;
				case Instruction.Throw:
					throw new Exception(); // writeline("throw;"); break;
				case Instruction.pCall:
					foreach (string s in Stack.pcall())
					{
						writeline(s);
					}
					break;
				case Instruction.iPush_n1:
				case Instruction.iPush_0:
				case Instruction.iPush_1:
				case Instruction.iPush_2:
				case Instruction.iPush_3:
				case Instruction.iPush_4:
				case Instruction.iPush_5:
				case Instruction.iPush_6:
				case Instruction.iPush_7:
					Stack.Push(Instructions[Offset].GetImmBytePush);
					break;
				case Instruction.fPush_n1:
				case Instruction.fPush_0:
				case Instruction.fPush_1:
				case Instruction.fPush_2:
				case Instruction.fPush_3:
				case Instruction.fPush_4:
				case Instruction.fPush_5:
				case Instruction.fPush_6:
				case Instruction.fPush_7:
					Stack.Push(Instructions[Offset].GetImmFloatPush);
					break;
				default:
					throw new Exception("Unexpected Instruction");
					HandleJump:
					CheckConditional();
					break;

			}
			Offset++;
		}

		//Bunch of methods that extracts what data type a static/frame variable is

		#region GetDataType

		public void CheckInstruction(int index, Stack.DataType type, int count = 1)
		{
			if (type == Stack.DataType.Unk)
				return;
			for (int i = 0; i < count; i++)
			{
				Vars_Info.Var Var = Stack.PeekVar(index + i);
				if (Var != null && (Stack.isLiteral(index + i) || Stack.isPointer(index + i)))
				{
					if (Types.gettype(type).precedence < Types.gettype(Var.DataType).precedence)
						continue;
					if (type == Stack.DataType.StringPtr && Stack.isPointer(index + 1))
						Var.DataType = Stack.DataType.String;
					else
						Var.DataType = type;
					if (Stack.isPointer(index + i))
					{
					}
					continue;
				}
				Function func = Stack.PeekFunc(index + i);
				if(func != null)
				{
					if(Types.gettype(type).precedence < func.ReturnType.precedence)
						continue;
					if(type == Stack.DataType.StringPtr && Stack.isPointer(index + 1))
						func.ReturnType = Types.gettype(Stack.DataType.String);
					else
						func.ReturnType = Types.gettype(type);
					if(Stack.isPointer(index + i))
					{
					}
					continue;
				}
				if (Stack.isnat(index + i))
				{
					if (_consoleVer)
						ScriptFile.npi.updaterettype(Stack.PeekNat(index + i), type);
					else
						ScriptFile.X64npi.updaterettype(Stack.PeekNat64(index + i), type);
				}

			}
		}

		public void CheckInstructionString(int index, int strsize, int count = 1)
		{
			for (int i = 0; i < count; i++)
			{
				Vars_Info.Var Var = Stack.PeekVar(index + i);
				if (Var != null && (Stack.isLiteral(index + i) || Stack.isPointer(index + i)))
				{
					if (Stack.isPointer(index + i))
					{
						if (Var.Immediatesize == 1 || Var.Immediatesize == strsize / 4)
						{
							Var.DataType = Stack.DataType.String;
							Var.Immediatesize = strsize / (_consoleVer? 4 : 8);
						}
					}
					else
						Var.DataType = Stack.DataType.StringPtr;
					continue;
				}
				if (Stack.isnat(index + i))
				{
					if (_consoleVer)
						ScriptFile.npi.updaterettype(Stack.PeekNat(index + i), Stack.DataType.StringPtr);
					else
						ScriptFile.X64npi.updaterettype(Stack.PeekNat64(index + i), Stack.DataType.StringPtr);
				}
			}
		}

	/*	public void CheckInstructionString(int strsize, int index = 0)
		{
			Vars_Info.Var Var = Stack.PeekVar(index);
			if (Var != null && Stack.isPointer(index))
			{
				if (Var.Immediatesize == 1 || Var.Immediatesize == strsize/4)
				{
					Var.DataType = Stack.DataType.String;
					Var.Immediatesize = strsize/4;
				}
			}

		}*/

		public void SetImmediate(int size)
		{
			if (size == 15)
			{
			}
			Vars_Info.Var Var = Stack.PeekVar(0);
			if (Var != null && Stack.isPointer(0))
			{
				if (Var.DataType == Stack.DataType.String)
				{
					if (Var.Immediatesize != size)
					{
						Var.Immediatesize = size;
						Var.makestruct();
					}
				}
				else
				{
					Var.Immediatesize = size;
					Var.makestruct();
				}
			}
		}

		public void CheckImmediate(int size)
		{
			Vars_Info.Var Var = Stack.PeekVar(0);
			if (Var != null && Stack.isPointer(0))
			{
				if (Var.Immediatesize < size)
					Var.Immediatesize = size;
				Var.makestruct();
			}
		}

		public void CheckArray(uint width, int size = -1)
		{
			Vars_Info.Var Var = Stack.PeekVar(0);
			if (Var != null && Stack.isPointer(0))
			{
				if (Var.Value < size)
					Var.Value = size;
				Var.Immediatesize = (int) width;
				Var.makearray();
			}
			CheckInstruction(1, Stack.DataType.Int);

		}

		public void SetArray(Stack.DataType type)
		{
			if (type == Stack.DataType.Unk)
				return;
			Vars_Info.Var Var = Stack.PeekVar(0);
			if (Var != null && Stack.isPointer(0)) Var.DataType = type;
		}

		public void returncheck(string temp)
		{
			if (Rcount != 1)
				return;
			if (ReturnType.type == Stack.DataType.Float)
				return;
			if (ReturnType.type == Stack.DataType.Int)
				return;
			if (ReturnType.type == Stack.DataType.Bool)
				return;
			if (temp.EndsWith("f"))
				ReturnType = Types.gettype(Stack.DataType.Float);
			int tempint;
			if (Utils.IntParse(temp, out tempint))
			{
				ReturnType = Types.gettype(Stack.DataType.Int);
				return;
			}
			if (temp.StartsWith("joaat("))
			{
				ReturnType = Types.gettype(Stack.DataType.Int);
				return;
			}
			if (temp.StartsWith("func_"))
			{
				string loc = temp.Remove(temp.IndexOf("(")).Substring(5);
				if (int.TryParse(loc, out tempint))
				{
					if (Scriptfile.Functions[tempint] == this)
					{
						return;
					}
					if (!Scriptfile.Functions[tempint].Decoded)
					{
						if (!Scriptfile.Functions[tempint].DecodeStarted)
							Scriptfile.Functions[tempint].Decode();
						else
						{
							while (!Scriptfile.Functions[tempint].Decoded)
							{
								Thread.Sleep(1);
							}
						}
					}
					switch (Scriptfile.Functions[tempint].ReturnType.type)
					{
						case Stack.DataType.Float:
						case Stack.DataType.Bool:
						case Stack.DataType.Int:
							ReturnType = Scriptfile.Functions[tempint].ReturnType;
							break;
					}
					return;
				}
			}
			if (temp.EndsWith(")") && !temp.StartsWith("("))
			{
				ReturnType = Types.gettype(Stack.DataType.Unsure);
				return;
			}
			ReturnType = Types.gettype(Stack.DataType.Unsure);

		}

		public void decodeinsructionsforvarinfo()
		{
			Stack = new Stack();
			ReturnType = Types.gettype(Stack.DataType.Unk);
			int tempint;
			string tempstring;
			for (int i = 0; i < Instructions.Count; i++)
			{
				HLInstruction ins = Instructions[i];

				switch (ins.Instruction)
				{
					case Instruction.Nop:
						break;
					case Instruction.iAdd:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Add();
						break;
					case Instruction.fAdd:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_Addf();
						break;
					case Instruction.iSub:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Sub();
						break;
					case Instruction.fSub:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_Subf();
						break;
					case Instruction.iMult:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Mult();
						break;
					case Instruction.fMult:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_Multf();
						break;
					case Instruction.iDiv:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Div();
						break;
					case Instruction.fDiv:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_Divf();
						break;
					case Instruction.iMod:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Mod();
						break;
					case Instruction.fMod:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_Modf();
						break;
					case Instruction.iNot:
						CheckInstruction(0, Stack.DataType.Bool);
						Stack.Op_Not();
						break;
					case Instruction.iNeg:
						CheckInstruction(0, Stack.DataType.Int);
						Stack.Op_Neg();
						break;
					case Instruction.fNeg:
						CheckInstruction(0, Stack.DataType.Float);
						Stack.Op_Negf();
						break;
					case Instruction.iCmpEq:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpEq:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.iCmpNe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpNe:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.iCmpGt:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpGt:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.iCmpGe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpGe:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.iCmpLt:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpLt:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.iCmpLe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.fCmpLe:
						CheckInstruction(0, Stack.DataType.Float, 2);
						Stack.Op_CmpEQ();
						break;
					case Instruction.vAdd:
						Stack.Op_Vadd();
						break;
					case Instruction.vSub:
						Stack.Op_VSub();
						break;
					case Instruction.vMult:
						Stack.Op_VMult();
						break;
					case Instruction.vDiv:
						Stack.Op_VDiv();
						break;
					case Instruction.vNeg:
						Stack.Op_VNeg();
						break;
					case Instruction.And:
						Stack.Op_And();
						break;
					case Instruction.Or:
						Stack.Op_Or();
						break;
					case Instruction.Xor:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Op_Xor();
						break;
					case Instruction.ItoF:
						CheckInstruction(0, Stack.DataType.Int);
						Stack.Op_Itof();
						break;
					case Instruction.FtoI:
						CheckInstruction(0, Stack.DataType.Float);
						Stack.Op_FtoI();
						break;
					case Instruction.FtoV:
						CheckInstruction(0, Stack.DataType.Float);
						Stack.Op_FtoV();
						break;
					case Instruction.iPushByte1:
						Stack.Push(ins.GetOperand(0));
						break;
					case Instruction.iPushByte2:
						Stack.Push(ins.GetOperand(0), ins.GetOperand(1));
						break;
					case Instruction.iPushByte3:
						Stack.Push(ins.GetOperand(0), ins.GetOperand(1), ins.GetOperand(2));
						break;
					case Instruction.iPushInt:
						Stack.Push(ins.GetOperandsAsInt.ToString(), Stack.DataType.Int);
						break;
					case Instruction.iPushI24:
					case Instruction.iPushShort:
						Stack.Push(ins.GetOperandsAsInt.ToString(), Stack.DataType.Int);
						break;
					case Instruction.fPush:
						Stack.Push(ins.GetFloat);
						break;
					case Instruction.dup:
						Stack.Dup();
						break;
					case Instruction.pop:
						Stack.Drop();
						break;
					case Instruction.Native:
						if (_consoleVer)
							tempstring = Stack.NativeCallTest(this.Scriptfile.NativeTable.GetNativeHashFromIndex(ins.GetNativeIndex),
								this.Scriptfile.NativeTable.GetNativeFromIndex(ins.GetNativeIndex), ins.GetNativeParams, ins.GetNativeReturns);
						else
							tempstring = Stack.NativeCallTest(this.Scriptfile.X64NativeTable.GetNativeHashFromIndex(ins.GetNativeIndex),
								this.Scriptfile.X64NativeTable.GetNativeFromIndex(ins.GetNativeIndex), ins.GetNativeParams, ins.GetNativeReturns);
						break;
					case Instruction.Enter:
						throw new Exception("Unexpected Function Definition");
					case Instruction.Return:
						Stack.PopListForCall(ins.GetOperand(1));
						break;


					case Instruction.pGet:
						Stack.Op_RefGet();
						break;
					case Instruction.pSet:
						if (Stack.PeekVar(1) == null)
						{
							Stack.Drop();
							Stack.Drop();
							break;
						}
						if (Stack.TopType == Stack.DataType.Int)
						{
							tempstring = Stack.PopLit();
							if (Utils.IntParse(tempstring, out tempint))
							{
								Stack.PeekVar(0).Value = tempint;
							}
							break;
						}
						Stack.Drop();
						break;
					case Instruction.pPeekSet:
						if (Stack.PeekVar(1) == null)
						{
							Stack.Drop();
							break;
						}
						if (Stack.TopType == Stack.DataType.Int)
						{
							tempstring = Stack.PopLit();
							if (Utils.IntParse(tempstring, out tempint))
							{
								Stack.PeekVar(0).Value = tempint;
							}
						}
						break;

					case Instruction.ToStack:
						if (Program.getIntType == Program.IntType._hex)
							tempint = int.Parse(Stack.PeekItem(1).Substring(2), System.Globalization.NumberStyles.HexNumber);
						else
							tempint = int.Parse(Stack.PeekItem(1));
						SetImmediate(tempint);
						Stack.Op_ToStack();
						break;
					case Instruction.FromStack:
						if (Program.getIntType == Program.IntType._hex)
							tempint = int.Parse(Stack.PeekItem(1).Substring(2), System.Globalization.NumberStyles.HexNumber);
						else
							tempint = int.Parse(Stack.PeekItem(1));
						SetImmediate(tempint);
						Stack.Op_FromStack();
						break;



					case Instruction.pArray1:
					case Instruction.pArray2:

						if (!Utils.IntParse(Stack.PeekItem(1), out tempint))
						{
							tempint = -1;
						}

						CheckArray(ins.GetOperandsAsUInt, tempint);
						Stack.Op_ArrayGetP(ins.GetOperandsAsUInt);
						break;
					case Instruction.ArrayGet1:
					case Instruction.ArrayGet2:
						if (!Utils.IntParse(Stack.PeekItem(1), out tempint))
						{
							tempint = -1;
						}
						CheckArray(ins.GetOperandsAsUInt, tempint);
						Stack.Op_ArrayGet(ins.GetOperandsAsUInt);
						break;
					case Instruction.ArraySet1:
					case Instruction.ArraySet2:
						if (!Utils.IntParse(Stack.PeekItem(1), out tempint))
						{
							tempint = -1;
						}
						CheckArray(ins.GetOperandsAsUInt, tempint);
						SetArray(Stack.ItemType(2));
						Vars_Info.Var Var = Stack.PeekVar(0);
						if (Var != null && Stack.isPointer(0))
						{
							CheckInstruction(2, Var.DataType);
						}
						Stack.Op_ArraySet(ins.GetOperandsAsUInt);
						break;



					case Instruction.pFrame1:
					case Instruction.pFrame2:
						Stack.PushPVar("FrameVar", GetFrameVar(ins.GetOperandsAsUInt));
						GetFrameVar(ins.GetOperandsAsUInt).call();
						break;
					case Instruction.GetFrame1:
					case Instruction.GetFrame2:
						Stack.PushVar("FrameVar", GetFrameVar(ins.GetOperandsAsUInt));
						GetFrameVar(ins.GetOperandsAsUInt).call();
						break;
					case Instruction.SetFrame1:
					case Instruction.SetFrame2:
						if (Stack.TopType != Stack.DataType.Unk)
						{
							if (Types.gettype(Stack.TopType).precedence >
							    Types.gettype(GetFrameVar(ins.GetOperandsAsUInt).DataType).precedence)
							{
								GetFrameVar(ins.GetOperandsAsUInt).DataType = Stack.TopType;
							}
						}
						else
						{
							CheckInstruction(0, GetFrameVar(ins.GetOperandsAsUInt).DataType);
						}
						tempstring = Stack.PopLit();
						if (Stack.TopType == Stack.DataType.Int)
						{
							tempstring = Stack.PopLit();
							if (ins.GetOperandsAsUInt > Pcount)
								if (Utils.IntParse(tempstring, out tempint))
								{
									GetFrameVar(ins.GetOperandsAsUInt).Value = tempint;
								}
						}
						else
						{
							Stack.Drop();
						}
						GetFrameVar(ins.GetOperandsAsUInt).call();
						break;
					case Instruction.pStatic1:
					case Instruction.pStatic2:
						Stack.PushPVar("Static", Scriptfile.Statics.GetVarAtIndex(ins.GetOperandsAsUInt));
						break;
					case Instruction.StaticGet1:
					case Instruction.StaticGet2:
						Stack.PushVar("Static", Scriptfile.Statics.GetVarAtIndex(ins.GetOperandsAsUInt));
						break;
					case Instruction.StaticSet1:
					case Instruction.StaticSet2:
						if (Stack.TopType != Stack.DataType.Unk)
						{
							Scriptfile.Statics.SetTypeAtIndex(ins.GetOperandsAsUInt, Stack.TopType);
						}
						else
						{
							CheckInstruction(0, Scriptfile.Statics.GetTypeAtIndex(ins.GetOperandsAsUInt));
						}
						Stack.Drop();
						break;



					case Instruction.Add1:
					case Instruction.Add2:
					case Instruction.Mult1:
					case Instruction.Mult2:
						CheckInstruction(0, Stack.DataType.Int);
						Stack.Op_AmmImm(ins.GetOperandsAsInt);
						break;


					case Instruction.pStructStack:
						Stack.Op_GetImmP();
						break;
					case Instruction.pStruct1:
					case Instruction.pStruct2:
						CheckImmediate((int) ins.GetOperandsAsUInt + 1);
						Stack.Op_GetImmP(ins.GetOperandsAsUInt);
						break;
					case Instruction.GetStruct1:
					case Instruction.GetStruct2:
						CheckImmediate((int) ins.GetOperandsAsUInt + 1);
						Stack.Op_GetImm(ins.GetOperandsAsUInt);
						break;
					case Instruction.SetStruct1:
					case Instruction.SetStruct2:
						CheckImmediate((int) ins.GetOperandsAsUInt + 1);
						Stack.Op_SetImm(ins.GetOperandsAsUInt);
						break;


					case Instruction.pGlobal2:
					case Instruction.pGlobal3:
						Stack.PushPointer("Global_" + ins.GetOperandsAsUInt.ToString());
						break;
					case Instruction.GlobalGet2:
					case Instruction.GlobalGet3:
						Stack.Push("Global_" + ins.GetOperandsAsUInt.ToString());
						break;
					case Instruction.GlobalSet2:
					case Instruction.GlobalSet3:
						Stack.Op_Set("Global_" + ins.GetOperandsAsUInt.ToString());
						break;


					case Instruction.Jump:
						break;
					case Instruction.JumpFalse:
						CheckInstruction(0, Stack.DataType.Bool);
						Stack.Drop();
						break;
					case Instruction.JumpNe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;
					case Instruction.JumpEq:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;
					case Instruction.JumpLe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;
					case Instruction.JumpLt:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;
					case Instruction.JumpGe:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;
					case Instruction.JumpGt:
						CheckInstruction(0, Stack.DataType.Int, 2);
						Stack.Drop();
						Stack.Drop();
						break;


					case Instruction.Call:			
						Function func = GetFunctionFromOffset(ins.GetOperandsAsInt);
						if (!func.predecodeStarted)
						{
							func.PreDecode();
						}
						if (func.predecoded)
						{
							for (int j = 0; j < func.Pcount; j++)
							{
								//CheckInstruction(func.Pcount - j - 1, func.Params.GetTypeAtIndex((uint)j));
								if (Stack.ItemType(func.Pcount - j - 1) != Stack.DataType.Unk)
								{
									if (Types.gettype(Stack.ItemType(func.Pcount - j - 1)).precedence >
									    Types.gettype(func.Params.GetTypeAtIndex((uint) j)).precedence)
									{
										func.Params.SetTypeAtIndex((uint) j, Stack.ItemType(func.Pcount - j - 1));
									}
								}
								CheckInstruction(func.Pcount - j - 1, func.Params.GetTypeAtIndex((uint) j));
							}
						}
						Stack.FunctionCall(func);
						break;


					case Instruction.Switch:
						CheckInstruction(0, Stack.DataType.Int);
						break;

					case Instruction.PushString:
						tempstring = Stack.PopLit();
						Stack.PushString("");
						break;
					case Instruction.GetHash:
						CheckInstruction(0, Stack.DataType.StringPtr);
						Stack.Op_Hash();
						break;

					case Instruction.StrCopy:
						CheckInstructionString(0, ins.GetOperandsAsInt, 2);
						Stack.op_strcopy(ins.GetOperandsAsInt);
						break;
					case Instruction.ItoS:
						CheckInstructionString(0, ins.GetOperandsAsInt);
						CheckInstruction(1, Stack.DataType.Int);
						Stack.op_itos(ins.GetOperandsAsInt);
						break;
					case Instruction.StrConCat:
						CheckInstructionString(0, ins.GetOperandsAsInt, 2);
						Stack.op_stradd(ins.GetOperandsAsInt);
						break;
					case Instruction.StrConCatInt:
						CheckInstructionString(0, ins.GetOperandsAsInt);
						CheckInstruction(1, Stack.DataType.Int);
						Stack.op_straddi(ins.GetOperandsAsInt);
						break;


					case Instruction.MemCopy:
						Stack.op_sncopy();
						break;
					case Instruction.Catch:
						break;
					case Instruction.Throw:
						break;
					case Instruction.pCall:
						Stack.pcall();
						break;
					case Instruction.iPush_n1:
					case Instruction.iPush_0:
					case Instruction.iPush_1:
					case Instruction.iPush_2:
					case Instruction.iPush_3:
					case Instruction.iPush_4:
					case Instruction.iPush_5:
					case Instruction.iPush_6:
					case Instruction.iPush_7:
						Stack.Push(ins.GetImmBytePush);
						break;
					case Instruction.fPush_n1:
					case Instruction.fPush_0:
					case Instruction.fPush_1:
					case Instruction.fPush_2:
					case Instruction.fPush_3:
					case Instruction.fPush_4:
					case Instruction.fPush_5:
					case Instruction.fPush_6:
					case Instruction.fPush_7:
						Stack.Push(ins.GetImmFloatPush);
						break;
					default:
						throw new Exception("Unexpected Instruction");

				}
			}
			Vars.checkvars();
			Params.checkvars();
		}

		#endregion

	}

	public class FunctionName
	{
		string name;
		int pcount, rcount, minloc; //, maxloc;
		//ReturnTypes _retType;
		Types.DataTypes _RetType;

		internal FunctionName(string Name, int Pcount, int Rcount, int MinLoc, int MaxLoc)
		{
			pcount = Pcount;
			rcount = Rcount;
			minloc = MinLoc;
			//maxloc = MaxLoc;
			name = Name;
			//_retType = ReturnTypes.Unkn;
			_RetType = Types.gettype(Stack.DataType.Unk);
		}

		public string Name
		{
			get { return name; }
		}

		public int Pcount
		{
			get { return pcount; }
		}

		public int Rcount
		{
			get { return rcount; }
		}

		internal int MinLoc
		{
			get { return minloc; }
		}

		//internal int MaxLoc { get { return maxloc; } }
		//public ReturnTypes RetType { get { return _retType; } set { _retType = value; } }
		public Types.DataTypes retType
		{
			get { return _RetType; }
			set { _RetType = value; }
		}
	}

	public enum ReturnTypes
	{
		Unkn,
		Unsure,
		Int,
		Float,
		Bool,
		BoolUnk,
		String,
		StringPtr,
		Ambiguous
	}
}
