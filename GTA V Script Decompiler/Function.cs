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
        public static readonly string FunctionName = "func_";

        public string Name { get; protected set; }
        public int Pcount { get; protected set; }
        public int Vcount { get; protected set; }
        public int Rcount { get; protected set; }
        public int Location { get; protected set; }
        public int MaxLocation { get; protected set; }
        public int Offset { get; set; } = 0;
        protected bool _dirty = false;

        public Stack.DataType ReturnType { get; set; } = Stack.DataType.Unk;
        public Vars_Info Vars { get; protected set; }
        public Vars_Info Params { get; protected set; }
        public HashSet<Function> ParentFunctions = new HashSet<Function>();
        public HashSet<Function> ChildFunctions = new HashSet<Function>();

        public ScriptFile Scriptfile { get; protected set; }
        public List<byte> CodeBlock { get; set; }
        public int NativeCount { get; protected set; } // Number of decoded native calls.
        public bool IsAggregate { get; private set; } // Stateless function.
        public Function BaseFunction { get; set; } // For aggregate functions.

        StringBuilder sb = null;
        List<HLInstruction> Instructions;
        Dictionary<int, int> InstructionMap;
        Stack stack;

        string tabs = "";
        CodePath Outerpath;
        bool writeelse = false;
        public int LineCount = 0;

        internal bool Decoded { get; private set; }
        internal bool DecodeStarted = false;
        internal bool predecoded = false;
        internal bool predecodeStarted = false;

        private Function(ScriptFile owner, string name)
        {
            Scriptfile = owner;
            Name = name;
            NativeCount = 0;
            IsAggregate = false;
            BaseFunction = null;
        }

        public Function(ScriptFile owner, string name, int pcount, int vcount, int rcount, int location, int locmax = -1) : this(owner, name)
        {
            Pcount = pcount;
            Vcount = vcount;
            Rcount = rcount;
            Location = location;
            MaxLocation = (locmax != -1) ? locmax : Location;
            Decoded = false;

            Vars = new Vars_Info(Vars_Info.ListType.Vars, false, vcount - 2);
            Params = new Vars_Info(Vars_Info.ListType.Params, false, pcount);
            Scriptfile.FunctionLoc.Add(location, this);
        }

        public override int GetHashCode() => Scriptfile.GetHashCode() ^ Name.GetHashCode();

        public Function CreateAggregate()
        {
            Function f = new Function(Scriptfile, Name);
            f.IsAggregate = true;
            f.Pcount = Pcount;
            f.Vcount = Vcount;
            f.Rcount = Rcount;
            f.Location = Location;
            f.MaxLocation = MaxLocation;

            f._dirty = false;
            f.CodeBlock = CodeBlock;
            f.NativeCount = NativeCount;
            f.BaseFunction = this;
            f.Instructions = new List<HLInstruction>();
            f.InstructionMap = (InstructionMap == null) ? null : new Dictionary<int, int>(InstructionMap);
            foreach (HLInstruction h in Instructions) f.Instructions.Add(h == null ? null : h.Clone());

            f.Offset = Offset;
            f.Decoded = false;
            f.ReturnType = ReturnType;
            f.predecoded = predecoded;
            f.predecodeStarted = predecodeStarted;
            f.Vars = Vars.Clone(true);
            f.Params = Params.Clone(true);

            Scriptfile.AggregateLoc.Add(f.Location, f);
            return f;
        }

        /// <summary>
        /// Compute the hash of the current string buffer (function signature for aggregate functions).
        /// </summary>
        /// <returns></returns>
        public string ToHash() => Utils.SHA256(sb.ToString());

        public void UpdateNativeReturnType(ulong hash, Stack.DataType datatype)
        {
            if (!IsAggregate)
                Scriptfile.UpdateNativeReturnType(hash, datatype);
        }

        public void UpdateNativeParameter(ulong hash, Stack.DataType dataType, int index)
        {
            if (!IsAggregate)
                Scriptfile.UpdateNativeParameter(hash, dataType, index);
        }

        public void UpdateFuncParamType(uint index, Stack.DataType dataType)
        {
            if (!IsAggregate && Params.SetTypeAtIndex(index, dataType))
                Dirty = true;
        }

        public bool Dirty
        {
            get => _dirty;
            set
            {
                if (IsAggregate) return;

                bool forward = !_dirty && value;
                _dirty = value;
                if (forward) // Dirty all associate functions.
                {
                    foreach (Function f in ChildFunctions)
                        f.Dirty = true;
                }
            }
        }

        public static void CreateFunctionPath(Function parent, Function child)
        {
            if (parent == child || parent.IsAggregate || child.IsAggregate) return;
            parent.ChildFunctions.Add(child);
            child.ParentFunctions.Add(parent);
        }

        /// <summary>
        /// Invalidate function aggregate cache
        /// </summary>
        public void Invalidate()
        {
            strCache = null; strFirstLineCache = null;
            if (InstructionMap != null)
            {
                InstructionMap.Clear(); InstructionMap = null;
                Instructions.Clear(); Instructions = null;
                CodeBlock.Clear(); CodeBlock = null;
                stack.Dispose(); stack = null;
                sb.Clear(); sb = null;
            }
        }

        /// <summary>
        /// Disposes of the function and returns the function text
        /// </summary>
        /// <returns>The whole function high level code</returns>
        private string strCache = null;
        public override string ToString()
        {
            if (strCache == null)
            {
                InstructionMap.Clear(); InstructionMap = null;
                Instructions.Clear(); Instructions = null;
                CodeBlock.Clear(); CodeBlock = null;
                stack.Dispose(); stack = null;

                try
                {
                    if (ReturnType == Stack.DataType.Bool)
                        strCache = FirstLine() + "\r\n" + sb.ToString().Replace("return 0;", "return false;").Replace("return 1;", "return true;");
                    else
                        strCache = FirstLine() + "\r\n" + sb.ToString();
                }
                finally
                {
                    sb.Clear(); sb = null;
                    LineCount += 2;
                }
            }
            return strCache;
        }

        /// <summary>
        /// Gets the first line of the function Declaration
        /// return type + name + params
        /// </summary>
        private string strFirstLineCache = null;
        public virtual string FirstLine()
        {
            if (strFirstLineCache == null)
            {
                string name, working = "";
                if (Rcount == 0) // extract return type of function
                    working = "void ";
                else if (Rcount == 1)
                    working = ReturnType.ReturnType();
                else if (Rcount == 3)
                    working = "Vector3 ";
                else if (Rcount > 1)
                {
                    if (ReturnType == Stack.DataType.String)
                        working = "char[" + (Rcount * 4).ToString() + "] ";
                    else
                        working = "struct<" + Rcount.ToString() + "> ";
                }
                else throw new DecompilingException("Unexpected return count");

                name = IsAggregate ? (working + Function.FunctionName) : (working + Name);
                working = "(" + Params.GetPDec() + ")";
                strFirstLineCache = name + working + (Program.ShowFuncPosition ? ("//Position - 0x" + Location.ToString("X")) : "");
            }
            return strFirstLineCache;
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
            return Vars.GetVarAtIndex((uint)(index - 2 - Pcount));
        }

        /// <summary>
        /// The block of code that the function takes up
        /// </summary>
        private Instruction Map(byte b) => Scriptfile.CodeSet.Map(b);
        private Instruction MapOffset(int offset) => Map(CodeBlock[offset]);

        /// <summary>
        /// Gets the function info given the offset where its called from
        /// </summary>
        /// <param name="offset">the offset that is being called</param>
        /// <returns>basic information about the function at that offset</returns>
        public Function GetFunctionAtOffset(int offset)
        {
            Dictionary<int, Function> lookup = IsAggregate ? Scriptfile.AggregateLoc : Scriptfile.FunctionLoc;
            if (lookup.ContainsKey(offset))
                return lookup[offset];
            throw new Exception("Function Not Found");
        }

        /// <summary>
        /// Gets the function info given the offset where its called from
        /// </summary>
        /// <param name="offset">the offset that is being called</param>
        /// <returns>basic information about the function at that offset</returns>
        public Function GetFunctionWithinOffset(int offset)
        {
            foreach (Function f in (IsAggregate ? Scriptfile.AggFunctions : Scriptfile.Functions))
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
            Dirty = false;
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
            lock (Program.ReadLock)
            {
                DecodeStarted = true;
                if (Decoded) return;
            }

            //Set up a stack
            stack = new Stack(this, false);

            //Set up the codepaths to a null item
            Outerpath = new CodePath(CodePathType.Main, CodeBlock.Count, -1);

            sb = new StringBuilder();
            opentab();
            Offset = 0;

            //write all the function variables declared by the function
            if (Program.DeclareVariables)
            {
                bool temp = false;
                foreach (string s in Vars.GetDeclaration())
                {
                    writeline(s);
                    temp = true;
                }
                if (temp) writeline("");
            }
            while (Offset < Instructions.Count)
                decodeinstruction();
            //Fix for switches that end at the end of a function
            while (Outerpath.Parent != null)
            {
                if (Outerpath.IsSwitch)
                {
                    SwitchPath outerSwitch = (SwitchPath)Outerpath;
                    if (outerSwitch.HasDefaulted)
                        closetab(false);
                    else
                    {
                        outerSwitch.HasDefaulted = true;
                        writeline("default:");
                        opentab(false);
                        writeline("break;");
                        closetab(false);
                        closetab(false);
                    }
                }
                closetab();
                Outerpath = Outerpath.Parent;
            }
            closetab(true);
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
            sb.AppendLine(line.TrimEnd());
            LineCount++;
        }

        /// <summary>
        /// Check if a jump is jumping out of the function
        /// if not, then add it to the list of instructions
        /// </summary>
        void checkjumpcodepath()
        {
            int cur = Offset;
            HLInstruction temp = new HLInstruction(MapOffset(Offset), GetArray(2), cur);
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
            AddInstruction(cur, new HLInstruction(Instruction.RAGE_NOP, cur));
            AddInstruction(cur + 1, new HLInstruction(Instruction.RAGE_NOP, cur + 1));
            AddInstruction(cur + 2, new HLInstruction(Instruction.RAGE_NOP, cur + 2));
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
            if (MapOffset(Offset + off) == Instruction.RAGE_NOP)
                goto Start;
            if (MapOffset(Offset + off) == Instruction.RAGE_JZ)
            {
                Offset = Offset + off + 2;
                return;
            }
            if (MapOffset(Offset + off) == Instruction.RAGE_INOT)
            {
                goto Start;
            }
            Instructions.Add(new HLInstruction(MapOffset(Offset), Offset));
            return;
        }

        /// <summary>
        /// Gets the given amount of bytes from the codeblock at its offset
        /// while advancing its position by how ever many items it uses
        /// </summary>
        /// <param name="items">how many bytes to grab</param>
        /// <returns>the operands for the instruction</returns>
        //IEnumerable<byte> GetArray(int items)
        //{
        //    int temp = Offset + 1;
        //    Offset += items;
        //    return CodeBlock.GetRange(temp, items);
        //}
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
        startsw:
            if (Outerpath.IsSwitch && Instructions[Offset].GetJumpOffset == Outerpath.BreakOffset)
            {
                SwitchPath outerSwitch = (SwitchPath)Outerpath;
                int switchOffset = outerSwitch.ActiveOffset;
                if (!outerSwitch.EscapedCases[switchOffset])
                {
                    writeline("break;");
                    outerSwitch.HasDefaulted = false;
                }

                return;
            }
            else if (Outerpath.IsSwitch)
            {
                if (Outerpath.Parent != null)
                {
                    Outerpath = Outerpath.Parent;
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
                if (Instructions[Offset + 1 + tempoff].Instruction == Instruction.RAGE_NOP)
                {
                    tempoff++;
                    goto start;
                }
                else if (Instructions[Offset + 1 + tempoff].Instruction == Instruction.RAGE_J)
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
                    //System.Diagnostics.Debug.WriteLine(this.Scriptfile.name);
                }
            }
        }

        //Needs Merging with method below
        bool isnewcodepath()
        {
            if (!Outerpath.IsSwitch && Outerpath.Parent != null)
            {
                if (InstructionMap[Outerpath.EndOffset] == Offset)
                {
                    return true;
                }
            }
            if (Outerpath.IsSwitch && ((SwitchPath)Outerpath).Offsets.Count > 0)
            {
                if (Instructions[Offset].Offset == ((SwitchPath)Outerpath).Offsets[0])
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
            if (!Outerpath.IsSwitch && Instructions[Offset].Offset == Outerpath.EndOffset)
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

            if (Outerpath.IsSwitch && ((SwitchPath)Outerpath).Offsets.Count > 0)
            {
                SwitchPath OuterSwitch = ((SwitchPath)Outerpath);
                if (Instructions[Offset].Offset == OuterSwitch.Offsets[0])
                {
                    if (OuterSwitch.Offsets.Count == 1)
                    {
                        if (OuterSwitch.HasDefaulted && !OuterSwitch.EscapedCases[OuterSwitch.ActiveOffset])
                        {
                            writeline("break;");
                            OuterSwitch.HasDefaulted = false;
                        }
                        closetab(false);
                        OuterSwitch.ActiveOffset = -1;

                        //end of switch statement detected
                        //remove child class
                        CodePath temp = OuterSwitch;
                        Outerpath = Outerpath.Parent;
                        Outerpath.ChildPaths.Remove(temp);
                        closetab();
                        //go check if its the next switch exit instruction
                        //probably isnt and the goto can probably be removed
                        goto start;
                    }
                    else
                    {
                        closetab(false);
                        OuterSwitch.ActiveOffset = OuterSwitch.Offsets[0];

                        //more cases left in switch
                        //so write the next switch case
                        for (int i = 0; i < (OuterSwitch.Cases[OuterSwitch.Offsets[0]]).Count; i++)
                        {
                            string temp = OuterSwitch.Cases[OuterSwitch.Offsets[0]][i];
                            if (temp == "default")
                            {
                                OuterSwitch.HasDefaulted = true;
                                writeline("default:");
                            }
                            else
                                writeline("case " + temp + ":" + Program.gxtbank.GetEntry(temp, false));
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
            int defaultloc;
            int breakloc;
            bool usedefault;
            HLInstruction temp;

            //Hanldle(skip past) any Nops immediately after switch statement
            int tempoff = 0;
            while (Instructions[Offset + 1 + tempoff].Instruction == Instruction.RAGE_NOP)
                tempoff++;

            //Extract the location to jump to if no cases match
            defaultloc = Instructions[Offset + 1 + tempoff].GetJumpOffset;

            UInt16 switchCount = Program.RDROpcodes ? Instructions[Offset].GetOperandsAsUInt16 : Instructions[Offset].GetOperand(0);
            for (int i = 0; i < switchCount; i++)
            {
                string case_val = Instructions[Offset].GetSwitchStringCase(i);
                int offset = Instructions[Offset].GetSwitchOffset(i); // Get the offset to jump to
                if (!cases.ContainsKey(offset)) // Check if the case is a known hash
                    cases.Add(offset, new List<string>(new string[] { case_val }));
                else // This offset is known, multiple cases are jumping to this path
                    cases[offset].Add(case_val);
            }

            //Not sure how necessary this step is, but just incase R* compiler doesnt order jump offsets, do it anyway
            List<int> sorted = cases.Keys.ToList();
            sorted.Sort();

            //We have found the jump location, so that instruction is no longer needed and can be nopped
            Instructions[Offset + 1 + tempoff].NopInstruction();

            //Temporary stage
            breakloc = defaultloc;
            usedefault = true;

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
                if (temp.Instruction != Instruction.RAGE_J)
                {
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
                //Default location found, best add it in
                if (cases.ContainsKey(defaultloc))
                {
                    //Default location shares code path with other known case
                    cases[defaultloc].Add("default");
                }
                else
                {
                    //Default location is a new code path
                    sorted = cases.Keys.ToList();
                    sorted.Sort();
                    sorted.Add(defaultloc);  // Ensure default is last offset
                    cases.Add(defaultloc, new List<string>(new string[] { "default" }));
                }
            }

            // Create the class the rest of the decompiler needs to handle the rest of the switch
            int sortedOffset = sorted[0];
            Outerpath = new SwitchPath(Outerpath, cases, -1, breakloc);

            // Found all information about switch, write the first case, the rest will be handled when we get to them
            writeline("switch (" + stack.Pop().AsLiteral + ")");
            opentab();
            for (int i = 0; i < cases[sortedOffset].Count; i++)
            {
                string caseStr = cases[sortedOffset][i];
                writeline("case " + caseStr + ":" + Program.gxtbank.GetEntry(caseStr, false));
            }

            opentab(false);

            // Need to build the escape paths prior to removing the offsets.
            cases.Remove(sortedOffset);
            ((SwitchPath)Outerpath).ActiveOffset = sortedOffset;
            ((SwitchPath)Outerpath).Cases.Remove(sortedOffset);
            ((SwitchPath)Outerpath).Offsets.Remove(sortedOffset);
        }

        /// <summary>
        /// If we have a conditional statement determine whether its for an if/while statement
        /// Then handle it accordingly
        /// </summary>
        void CheckConditional()
        {
            string tempstring = stack.Pop().AsLiteral;
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
                    else if (Instructions[InstructionMap[Instructions[Offset].GetJumpOffset] - 1].Instruction == Instruction.RAGE_J)
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
                    Instruction instruct = MapOffset(Offset);
                    switch (MapOffset(Offset))
                    {
                        //case Instruction.RAGE_NOP: if (addnop) AddInstruction(curoff, new HLInstruction(instruct, curoff)); break;
                        case Instruction.RAGE_PUSH_CONST_U8:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(1), curoff));
                            break;
                        case Instruction.RAGE_PUSH_CONST_U8_U8:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(2), curoff));
                            break;
                        case Instruction.RAGE_PUSH_CONST_U8_U8_U8:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(3), curoff));
                            break;
                        case Instruction.RAGE_PUSH_CONST_U32:
                        case Instruction.RAGE_PUSH_CONST_F:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(4), curoff));
                            break;
                        case Instruction.RAGE_DUP:
                            // Because of how rockstar codes and/or conditionals, its neater to detect dups
                            // and only add them if they are not used for conditionals
                            checkdupforinstruction();
                            break;
                        case Instruction.RAGE_NATIVE:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(3), curoff));
                            break;
                        case Instruction.RAGE_ENTER:
                            throw new Exception("Function not exptected");
                        case Instruction.RAGE_LEAVE:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(2), curoff));
                            break;
                        case Instruction.RAGE_ARRAY_U8:
                        case Instruction.RAGE_ARRAY_U8_LOAD:
                        case Instruction.RAGE_ARRAY_U8_STORE:
                        case Instruction.RAGE_LOCAL_U8:
                        case Instruction.RAGE_LOCAL_U8_LOAD:
                        case Instruction.RAGE_LOCAL_U8_STORE:
                        case Instruction.RAGE_STATIC_U8:
                        case Instruction.RAGE_STATIC_U8_LOAD:
                        case Instruction.RAGE_STATIC_U8_STORE:
                        case Instruction.RAGE_IADD_U8:
                        case Instruction.RAGE_IMUL_U8:
                        case Instruction.RAGE_IOFFSET_U8:
                        case Instruction.RAGE_IOFFSET_U8_LOAD:
                        case Instruction.RAGE_IOFFSET_U8_STORE:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(1), curoff));
                            break;
                        case Instruction.RAGE_PUSH_CONST_S16:
                        case Instruction.RAGE_IADD_S16:
                        case Instruction.RAGE_IMUL_S16:
                        case Instruction.RAGE_IOFFSET_S16:
                        case Instruction.RAGE_IOFFSET_S16_LOAD:
                        case Instruction.RAGE_IOFFSET_S16_STORE:
                        case Instruction.RAGE_ARRAY_U16:
                        case Instruction.RAGE_ARRAY_U16_LOAD:
                        case Instruction.RAGE_ARRAY_U16_STORE:
                        case Instruction.RAGE_LOCAL_U16:
                        case Instruction.RAGE_LOCAL_U16_LOAD:
                        case Instruction.RAGE_LOCAL_U16_STORE:
                        case Instruction.RAGE_STATIC_U16:
                        case Instruction.RAGE_STATIC_U16_LOAD:
                        case Instruction.RAGE_STATIC_U16_STORE:
                        case Instruction.RAGE_GLOBAL_U16:
                        case Instruction.RAGE_GLOBAL_U16_LOAD:
                        case Instruction.RAGE_GLOBAL_U16_STORE:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(2), curoff));
                            break;
                        case Instruction.RAGE_J:
                            checkjumpcodepath();
                            break;
                        case Instruction.RAGE_JZ:
                        case Instruction.RAGE_IEQ_JZ:
                        case Instruction.RAGE_INE_JZ:
                        case Instruction.RAGE_IGT_JZ:
                        case Instruction.RAGE_IGE_JZ:
                        case Instruction.RAGE_ILT_JZ:
                        case Instruction.RAGE_ILE_JZ:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(2), curoff));
                            break;
                        case Instruction.RAGE_CALL:
                        case Instruction.RAGE_GLOBAL_U24:
                        case Instruction.RAGE_GLOBAL_U24_LOAD:
                        case Instruction.RAGE_GLOBAL_U24_STORE:
                        case Instruction.RAGE_PUSH_CONST_U24:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(3), curoff));
                            break;
                        case Instruction.RAGE_SWITCH:
                        {
                            if (Program.RDROpcodes)
                            {
                                int length = (CodeBlock[Offset + 2] << 8) | CodeBlock[Offset + 1];
                                AddInstruction(curoff, new HLInstruction(instruct, GetArray(length * 6 + 2), curoff));
                            }
                            else
                            {
                                int temp = CodeBlock[Offset + 1];
                                AddInstruction(curoff, new HLInstruction(instruct, GetArray(temp * 6 + 1), curoff));
                            }
                            break;
                        }
                        case Instruction.RAGE_TEXT_LABEL_ASSIGN_STRING:
                        case Instruction.RAGE_TEXT_LABEL_ASSIGN_INT:
                        case Instruction.RAGE_TEXT_LABEL_APPEND_STRING:
                        case Instruction.RAGE_TEXT_LABEL_APPEND_INT:
                            AddInstruction(curoff, new HLInstruction(instruct, GetArray(1), curoff));
                            break;
                        default:
                            if (instruct != Instruction.RAGE_last)
                                AddInstruction(curoff, new HLInstruction(instruct, curoff));
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
            if (isnewcodepath()) handlenewpath();
            switch (Instructions[Offset].Instruction)
            {
                case Instruction.RAGE_NOP:
                    break;
                case Instruction.RAGE_IADD:
                    stack.Op_Add();
                    break;
                case Instruction.RAGE_FADD:
                    stack.Op_Addf();
                    break;
                case Instruction.RAGE_ISUB:
                    stack.Op_Sub();
                    break;
                case Instruction.RAGE_FSUB:
                    stack.Op_Subf();
                    break;
                case Instruction.RAGE_IMUL:
                    stack.Op_Mult();
                    break;
                case Instruction.RAGE_FMUL:
                    stack.Op_Multf();
                    break;
                case Instruction.RAGE_IDIV:
                    stack.Op_Div();
                    break;
                case Instruction.RAGE_FDIV:
                    stack.Op_Divf();
                    break;
                case Instruction.RAGE_IMOD:
                    stack.Op_Mod();
                    break;
                case Instruction.RAGE_FMOD:
                    stack.Op_Modf();
                    break;
                case Instruction.RAGE_INOT:
                    stack.Op_Not();
                    break;
                case Instruction.RAGE_INEG:
                    stack.Op_Neg();
                    break;
                case Instruction.RAGE_FNEG:
                    stack.Op_Negf();
                    break;
                case Instruction.RAGE_IEQ:
                case Instruction.RAGE_FEQ:
                    stack.Op_CmpEQ();
                    break;
                case Instruction.RAGE_INE:
                case Instruction.RAGE_FNE:
                    stack.Op_CmpNE();
                    break;
                case Instruction.RAGE_IGT:
                case Instruction.RAGE_FGT:
                    stack.Op_CmpGT();
                    break;
                case Instruction.RAGE_IGE:
                case Instruction.RAGE_FGE:
                    stack.Op_CmpGE();
                    break;
                case Instruction.RAGE_ILT:
                case Instruction.RAGE_FLT:
                    stack.Op_CmpLT();
                    break;
                case Instruction.RAGE_ILE:
                case Instruction.RAGE_FLE:
                    stack.Op_CmpLE();
                    break;
                case Instruction.RAGE_VADD:
                    stack.Op_Vadd();
                    break;
                case Instruction.RAGE_VSUB:
                    stack.Op_VSub();
                    break;
                case Instruction.RAGE_VMUL:
                    stack.Op_VMult();
                    break;
                case Instruction.RAGE_VDIV:
                    stack.Op_VDiv();
                    break;
                case Instruction.RAGE_VNEG:
                    stack.Op_VNeg();
                    break;
                case Instruction.RAGE_IAND:
                    stack.Op_And();
                    break;
                case Instruction.RAGE_IOR:
                    stack.Op_Or();
                    break;
                case Instruction.RAGE_IXOR:
                    stack.Op_Xor();
                    break;
                case Instruction.RAGE_I2F:
                    stack.Op_Itof();
                    break;
                case Instruction.RAGE_F2I:
                    stack.Op_FtoI();
                    break;
                case Instruction.RAGE_F2V:
                    stack.Op_FtoV();
                    break;
                case Instruction.RAGE_PUSH_CONST_U8:
                    stack.Push(Instructions[Offset].GetOperand(0));
                    break;
                case Instruction.RAGE_PUSH_CONST_U8_U8:
                    stack.Push(Instructions[Offset].GetOperand(0), Instructions[Offset].GetOperand(1));
                    break;
                case Instruction.RAGE_PUSH_CONST_U8_U8_U8:
                    stack.Push(Instructions[Offset].GetOperand(0), Instructions[Offset].GetOperand(1),
                        Instructions[Offset].GetOperand(2));
                    break;
                case Instruction.RAGE_PUSH_CONST_U32:
                case Instruction.RAGE_PUSH_CONST_U24:
                {
                    Stack.DataType type = Stack.DataType.Int;
                    if (Program.IntStyle == Program.IntType._uint)
                        stack.Push(Program.hashbank.GetHash(Instructions[Offset].GetOperandsAsUInt), type);
                    else
                        stack.Push(Program.hashbank.GetHash(Instructions[Offset].GetOperandsAsInt), type);
                    break;
                }
                case Instruction.RAGE_PUSH_CONST_S16:
                    stack.Push(Instructions[Offset].GetOperandsAsInt);
                    break;
                case Instruction.RAGE_PUSH_CONST_F:
                    stack.Push(Instructions[Offset].GetFloat);
                    break;
                case Instruction.RAGE_DUP:
                    stack.Dup();
                    break;
                case Instruction.RAGE_DROP:
                {
                    object temp = stack.Drop();
                    if (temp is string)
                        writeline(temp as string);
                    break;
                }
                case Instruction.RAGE_NATIVE:
                {
                    ulong natHash = this.Scriptfile.X64NativeTable.GetNativeHashFromIndex(Instructions[Offset].GetNativeIndex);
                    string natStr = this.Scriptfile.X64NativeTable.GetNativeFromIndex(Instructions[Offset].GetNativeIndex);
                    NativeCount++;
                    if (!IsAggregate) Agg.Instance.Count(natStr);

                    string tempstring = stack.NativeCallTest(natHash, natStr, Instructions[Offset].GetNativeParams, Instructions[Offset].GetNativeReturns);
                    if (tempstring != "")
                        writeline(tempstring);
                    break;
                }
                case Instruction.RAGE_ENTER:
                    throw new Exception("Unexpected Function Definition");
                case Instruction.RAGE_LEAVE:
                {
                    if (Outerpath.IsSwitch)
                    {
                        SwitchPath switchPath = (SwitchPath)Outerpath;
                        switchPath.EscapedCases[switchPath.ActiveOffset] = true;
                    }

                    string tempstring = stack.PopListForCall(Instructions[Offset].GetOperand(1));
                    switch (Instructions[Offset].GetOperand(1))
                    {
                        case 0:
                        {
                            if (Offset < Instructions.Count - 1)
                                writeline("return;");
                            break;
                        }
                        default:
                        {
                            writeline("return " + tempstring + ";");
                            break;
                        }
                    }
                    break;
                }
                case Instruction.RAGE_LOAD:
                    stack.Op_RefGet();
                    break;
                case Instruction.RAGE_STORE:
                    if (stack.PeekVar(1) == null)
                        writeline(stack.Op_RefSet());
                    else if (stack.PeekVar(1).Is_Array)
                        stack.Op_RefSet();
                    else
                        writeline(stack.Op_RefSet());
                    break;
                case Instruction.RAGE_STORE_REV:
                    if (stack.PeekVar(1) == null)
                        writeline(stack.Op_PeekSet());
                    else if (stack.PeekVar(1).Is_Array)
                        stack.Op_PeekSet();
                    else
                        writeline(stack.Op_PeekSet());
                    break;
                case Instruction.RAGE_LOAD_N:
                    stack.Op_ToStack();
                    break;
                case Instruction.RAGE_STORE_N:
                    writeline(stack.Op_FromStack());
                    break;
                case Instruction.RAGE_ARRAY_U8:
                case Instruction.RAGE_ARRAY_U16:
                    stack.Op_ArrayGetP(Instructions[Offset].GetOperandsAsUInt);
                    break;
                case Instruction.RAGE_ARRAY_U8_LOAD:
                case Instruction.RAGE_ARRAY_U16_LOAD:
                    stack.Op_ArrayGet(Instructions[Offset].GetOperandsAsUInt);
                    break;
                case Instruction.RAGE_ARRAY_U8_STORE:
                case Instruction.RAGE_ARRAY_U16_STORE:
                    writeline(stack.Op_ArraySet(Instructions[Offset].GetOperandsAsUInt));
                    break;
                case Instruction.RAGE_LOCAL_U8:
                case Instruction.RAGE_LOCAL_U16:
                    stack.PushPVar(GetFrameVar(Instructions[Offset].GetOperandsAsUInt));
                    break;
                case Instruction.RAGE_LOCAL_U8_LOAD:
                case Instruction.RAGE_LOCAL_U16_LOAD:
                    stack.PushVar(GetFrameVar(Instructions[Offset].GetOperandsAsUInt));
                    break;
                case Instruction.RAGE_LOCAL_U8_STORE:
                case Instruction.RAGE_LOCAL_U16_STORE:
                {
                    Vars_Info.Var var = GetFrameVar(Instructions[Offset].GetOperandsAsUInt);
                    string tempstring = stack.Op_Set(var.Name, var);
                    if (var.DataType == Stack.DataType.Bool)
                        tempstring = tempstring.Replace("= 0;", "= false;").Replace("= 1;", "= true;");
                    if (!var.Is_Array)
                        writeline(tempstring);
                    break;
                }
                case Instruction.RAGE_STATIC_U8:
                case Instruction.RAGE_STATIC_U16:
                    stack.PushPVar(Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt).Fixed());
                    break;
                case Instruction.RAGE_STATIC_U8_LOAD:
                case Instruction.RAGE_STATIC_U16_LOAD:
                    stack.PushVar(Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt).Fixed());
                    break;
                case Instruction.RAGE_STATIC_U8_STORE:
                case Instruction.RAGE_STATIC_U16_STORE:
                {
                    Vars_Info.Var var = Scriptfile.Statics.GetVarAtIndex(Instructions[Offset].GetOperandsAsUInt).Fixed();
                    string tempstring = stack.Op_Set(Scriptfile.Statics.GetVarName(Instructions[Offset].GetOperandsAsUInt), var);
                    if (var.DataType == Stack.DataType.Bool)
                        tempstring = tempstring.Replace("= 0;", "= false;").Replace("= 1;", "= true;");
                    if (!var.Is_Array)
                        writeline(tempstring);
                    break;
                }
                case Instruction.RAGE_IADD_U8:
                case Instruction.RAGE_IADD_S16:
                    stack.Op_AmmImm(Instructions[Offset].GetOperandsAsInt);
                    break;
                case Instruction.RAGE_IMUL_U8:
                case Instruction.RAGE_IMUL_S16:
                    stack.Op_MultImm(Instructions[Offset].GetOperandsAsInt);
                    break;
                case Instruction.RAGE_IOFFSET:
                    stack.Op_GetImmP();
                    break;
                case Instruction.RAGE_IOFFSET_U8:
                case Instruction.RAGE_IOFFSET_S16:
                    stack.Op_GetImmP(Instructions[Offset].GetOperandsAsUInt);
                    break;
                case Instruction.RAGE_IOFFSET_U8_LOAD:
                case Instruction.RAGE_IOFFSET_S16_LOAD:
                    stack.Op_GetImm(Instructions[Offset].GetOperandsAsUInt);
                    break;
                case Instruction.RAGE_IOFFSET_U8_STORE:
                case Instruction.RAGE_IOFFSET_S16_STORE:
                    writeline(stack.Op_SetImm(Instructions[Offset].GetOperandsAsUInt));
                    break;
                case Instruction.RAGE_GLOBAL_U16:
                case Instruction.RAGE_GLOBAL_U24:
                    stack.PushPGlobal(Instructions[Offset].GetGlobalString());
                    break;
                case Instruction.RAGE_GLOBAL_U16_LOAD:
                case Instruction.RAGE_GLOBAL_U24_LOAD:
                    stack.PushGlobal(Instructions[Offset].GetGlobalString());
                    break;
                case Instruction.RAGE_GLOBAL_U16_STORE:
                case Instruction.RAGE_GLOBAL_U24_STORE:
                    writeline(stack.Op_Set(Instructions[Offset].GetGlobalString()));
                    break;
                case Instruction.RAGE_J:
                    handlejumpcheck();
                    break;
                case Instruction.RAGE_JZ:
                    goto HandleJump;
                case Instruction.RAGE_IEQ_JZ:
                    stack.Op_CmpEQ();
                    goto HandleJump;
                case Instruction.RAGE_INE_JZ:
                    stack.Op_CmpNE();
                    goto HandleJump;
                case Instruction.RAGE_IGT_JZ:
                    stack.Op_CmpGT();
                    goto HandleJump;
                case Instruction.RAGE_IGE_JZ:
                    stack.Op_CmpGE();
                    goto HandleJump;
                case Instruction.RAGE_ILT_JZ:
                    stack.Op_CmpLT();
                    goto HandleJump;
                case Instruction.RAGE_ILE_JZ:
                    stack.Op_CmpLE();
                    goto HandleJump;
                case Instruction.RAGE_CALL:
                {
                    string tempstring = stack.FunctionCall(GetFunctionAtOffset(Instructions[Offset].GetOperandsAsInt));
                    if (tempstring != "")
                        writeline(tempstring);
                    break;
                }
                case Instruction.RAGE_SWITCH:
                    handleswitch();
                    break;
                case Instruction.RAGE_STRING:
                {
                    int tempint;
                    string tempstring = stack.Pop().AsLiteral;
                    if (!Utils.IntParse(tempstring, out tempint))
                        stack.Push("StringTable(" + tempstring + ")", Stack.DataType.StringPtr);
                    else if (!this.Scriptfile.StringTable.StringExists(tempint))
                        stack.Push("StringTable(" + tempstring + ")", Stack.DataType.StringPtr);
                    else
                        stack.Push("\"" + this.Scriptfile.StringTable[tempint] + "\"", Stack.DataType.StringPtr);
                    break;
                }
                case Instruction.RAGE_STRINGHASH:
                    stack.Op_Hash();
                    break;
                case Instruction.RAGE_TEXT_LABEL_ASSIGN_STRING:
                    writeline(stack.Op_StrCpy(Instructions[Offset].GetOperandsAsInt));
                    break;
                case Instruction.RAGE_TEXT_LABEL_ASSIGN_INT:
                    writeline(stack.Op_ItoS(Instructions[Offset].GetOperandsAsInt));
                    break;
                case Instruction.RAGE_TEXT_LABEL_APPEND_STRING:
                    writeline(stack.Op_StrAdd(Instructions[Offset].GetOperandsAsInt));
                    break;
                case Instruction.RAGE_TEXT_LABEL_APPEND_INT:
                    writeline(stack.Op_StrAddI(Instructions[Offset].GetOperandsAsInt));
                    break;
                case Instruction.RAGE_TEXT_LABEL_COPY:
                    writeline(stack.Op_SnCopy());
                    break;
                case Instruction.RAGE_CATCH:
                    throw new Exception(); // writeline("catch;"); break;
                case Instruction.RAGE_THROW:
                    throw new Exception(); // writeline("throw;"); break;
                case Instruction.RAGE_CALLINDIRECT:
                    foreach (string s in stack.pcall())
                        writeline(s);
                    break;
                case Instruction.RAGE_PUSH_CONST_M1:
                case Instruction.RAGE_PUSH_CONST_0:
                case Instruction.RAGE_PUSH_CONST_1:
                case Instruction.RAGE_PUSH_CONST_2:
                case Instruction.RAGE_PUSH_CONST_3:
                case Instruction.RAGE_PUSH_CONST_4:
                case Instruction.RAGE_PUSH_CONST_5:
                case Instruction.RAGE_PUSH_CONST_6:
                case Instruction.RAGE_PUSH_CONST_7:
                    stack.Push(Instructions[Offset].GetImmBytePush);
                    break;
                case Instruction.RAGE_PUSH_CONST_FM1:
                case Instruction.RAGE_PUSH_CONST_F0:
                case Instruction.RAGE_PUSH_CONST_F1:
                case Instruction.RAGE_PUSH_CONST_F2:
                case Instruction.RAGE_PUSH_CONST_F3:
                case Instruction.RAGE_PUSH_CONST_F4:
                case Instruction.RAGE_PUSH_CONST_F5:
                case Instruction.RAGE_PUSH_CONST_F6:
                case Instruction.RAGE_PUSH_CONST_F7:
                    stack.Push(Instructions[Offset].GetImmFloatPush);
                    break;

                // RDR Extended Instruction Set.
                case Instruction.RAGE_LOCAL_LOAD_S:
                case Instruction.RAGE_LOCAL_STORE_S:
                case Instruction.RAGE_LOCAL_STORE_SR:
                case Instruction.RAGE_STATIC_LOAD_S:
                case Instruction.RAGE_STATIC_STORE_S:
                case Instruction.RAGE_STATIC_STORE_SR:
                case Instruction.RAGE_LOAD_N_S:
                case Instruction.RAGE_STORE_N_S:
                case Instruction.RAGE_STORE_N_SR:
                case Instruction.RAGE_GLOBAL_LOAD_S:
                case Instruction.RAGE_GLOBAL_STORE_S:
                case Instruction.RAGE_GLOBAL_STORE_SR:
                    if (Scriptfile.CodeSet.Count <= 127) throw new Exception("Unexpected Instruction");
                    stack.PushGlobal("RDR_" + Instructions[Offset].Instruction);
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

        public void CheckInstruction(int index, Stack.DataType type, int count = 1, bool functionPars = false)
        {
            if (type == Stack.DataType.Unk)
                return;
            for (int i = 0; i < count; i++)
            {
                Vars_Info.Var Var = stack.PeekVar(index + i);
                if (Var != null && (stack.isLiteral(index + i) || stack.isPointer(index + i)))
                {
                    if (type.LessThan(Var.DataType))
                        continue;
                    if (type == Stack.DataType.StringPtr && stack.isPointer(index + 1))
                        Var.DataType = Stack.DataType.String;
                    else if (functionPars && stack.isPointer(index + i) && type.BaseType() != Stack.DataType.Unk)
                        Var.DataType = type.BaseType();
                    else if (!functionPars)
                        Var.DataType = type;
                    continue;
                }
                Function func = stack.PeekFunc(index + i);
                if (func != null)
                {
                    if (type.LessThan(func.ReturnType))
                        continue;
                    if (type == Stack.DataType.StringPtr && stack.isPointer(index + 1))
                        func.ReturnType = Stack.DataType.String;
                    else
                        func.ReturnType = type;
                    continue;
                }
                if (stack.isnat(index + i))
                {
                    UpdateNativeReturnType(stack.PeekNat64(index + i).Hash, type);
                }
            }
        }

        public void CheckInstructionString(int index, int strsize, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Vars_Info.Var Var = stack.PeekVar(index + i);
                if (Var != null && (stack.isLiteral(index + i) || stack.isPointer(index + i)))
                {
                    if (stack.isPointer(index + i))
                    {
                        if (Var.Immediatesize == 1 || Var.Immediatesize == strsize / 4)
                        {
                            Var.DataType = Stack.DataType.String;
                            Var.Immediatesize = strsize / 8;
                        }
                    }
                    else
                        Var.DataType = Stack.DataType.StringPtr;
                    continue;
                }
                if (stack.isnat(index + i))
                {
                    UpdateNativeReturnType(stack.PeekNat64(index + i).Hash, Stack.DataType.StringPtr);
                }
            }
        }

        public void SetImmediate(int size)
        {
            Vars_Info.Var Var = stack.PeekVar(0);
            if (Var != null && stack.isPointer(0))
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
            Vars_Info.Var Var = stack.PeekVar(0);
            if (Var != null && stack.isPointer(0))
            {
                if (Var.Immediatesize < size)
                    Var.Immediatesize = size;
                Var.makestruct();
            }
        }

        public void CheckArray(uint width, int size = -1)
        {
            Vars_Info.Var Var = stack.PeekVar(0);
            if (Var != null && stack.isPointer(0))
            {
                if (Var.Value < size)
                    Var.Value = size;
                Var.Immediatesize = (int)width;
                Var.makearray();
            }
            CheckInstruction(1, Stack.DataType.Int);
        }

        public void SetArray(Stack.DataType type)
        {
            if (type == Stack.DataType.Unk)
                return;
            Vars_Info.Var Var = stack.PeekVar(0);
            if (Var != null && stack.isPointer(0)) Var.DataType = type;
        }

        public Stack.DataType returncheck(string temp)
        {
            List<Function> functions = IsAggregate ? Scriptfile.AggFunctions : Scriptfile.Functions;
            int tempint;
            if (Rcount != 1) return ReturnType;
            if (temp.StartsWith("joaat(")) return Stack.DataType.Int;
            if (temp.StartsWith(Function.FunctionName))
            {
                string loc = temp.Remove(temp.IndexOf("(")).Substring(5);
                if (int.TryParse(loc, out tempint))
                {
                    if (functions[tempint] == this) return ReturnType;

                    // Ensure the function is predecoded.
                    if (!functions[tempint].predecoded)
                    {
                        if (!functions[tempint].predecodeStarted)
                            functions[tempint].PreDecode();
                        else
                        {
                            while (!functions[tempint].predecoded)
                            {
                                Thread.Sleep(1);
                            }
                        }
                    }
                    return functions[tempint].ReturnType;
                }
            }

            if (ReturnType == Stack.DataType.Float) return ReturnType;
            if (ReturnType == Stack.DataType.Int) return ReturnType;
            if (ReturnType == Stack.DataType.Bool) return ReturnType;
            if (Utils.IntParse(temp, out tempint)) return Stack.DataType.Int;
            return ReturnType;
            //return (temp.EndsWith(")") && !temp.StartsWith("(")) ? Stack.DataType.Unsure : Stack.DataType.Unsure;
        }

        public void decodeinsructionsforvarinfo()
        {
            stack = new Stack(this, true);
            //ReturnType = Stack.DataType.Unk;
            for (int i = 0; i < Instructions.Count; i++)
            {
                HLInstruction ins = Instructions[i];
                switch (ins.Instruction)
                {
                    case Instruction.RAGE_NOP:
                        break;
                    case Instruction.RAGE_IADD:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Add();
                        break;
                    case Instruction.RAGE_FADD:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_Addf();
                        break;
                    case Instruction.RAGE_ISUB:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Sub();
                        break;
                    case Instruction.RAGE_FSUB:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_Subf();
                        break;
                    case Instruction.RAGE_IMUL:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Mult();
                        break;
                    case Instruction.RAGE_FMUL:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_Multf();
                        break;
                    case Instruction.RAGE_IDIV:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Div();
                        break;
                    case Instruction.RAGE_FDIV:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_Divf();
                        break;
                    case Instruction.RAGE_IMOD:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Mod();
                        break;
                    case Instruction.RAGE_FMOD:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_Modf();
                        break;
                    case Instruction.RAGE_INOT:
                        CheckInstruction(0, Stack.DataType.Bool);
                        stack.Op_Not();
                        break;
                    case Instruction.RAGE_INEG:
                        CheckInstruction(0, Stack.DataType.Int);
                        stack.Op_Neg();
                        break;
                    case Instruction.RAGE_FNEG:
                        CheckInstruction(0, Stack.DataType.Float);
                        stack.Op_Negf();
                        break;
                    case Instruction.RAGE_IEQ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FEQ:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_INE:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FNE:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_IGT:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FGT:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_IGE:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FGE:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_ILT:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FLT:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_ILE:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_FLE:
                        CheckInstruction(0, Stack.DataType.Float, 2);
                        stack.Op_CmpEQ();
                        break;
                    case Instruction.RAGE_VADD:
                        stack.Op_Vadd();
                        break;
                    case Instruction.RAGE_VSUB:
                        stack.Op_VSub();
                        break;
                    case Instruction.RAGE_VMUL:
                        stack.Op_VMult();
                        break;
                    case Instruction.RAGE_VDIV:
                        stack.Op_VDiv();
                        break;
                    case Instruction.RAGE_VNEG:
                        stack.Op_VNeg();
                        break;
                    case Instruction.RAGE_IAND:
                        stack.Op_And();
                        break;
                    case Instruction.RAGE_IOR:
                        stack.Op_Or();
                        break;
                    case Instruction.RAGE_IXOR:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Op_Xor();
                        break;
                    case Instruction.RAGE_I2F:
                        CheckInstruction(0, Stack.DataType.Int);
                        stack.Op_Itof();
                        break;
                    case Instruction.RAGE_F2I:
                        CheckInstruction(0, Stack.DataType.Float);
                        stack.Op_FtoI();
                        break;
                    case Instruction.RAGE_F2V:
                        CheckInstruction(0, Stack.DataType.Float);
                        stack.Op_FtoV();
                        break;
                    case Instruction.RAGE_PUSH_CONST_U8:
                        stack.Push(ins.GetOperand(0));
                        break;
                    case Instruction.RAGE_PUSH_CONST_U8_U8:
                        stack.Push(ins.GetOperand(0), ins.GetOperand(1));
                        break;
                    case Instruction.RAGE_PUSH_CONST_U8_U8_U8:
                        stack.Push(ins.GetOperand(0), ins.GetOperand(1), ins.GetOperand(2));
                        break;
                    case Instruction.RAGE_PUSH_CONST_U32:
                        stack.Push(ins.GetOperandsAsInt.ToString(), Stack.DataType.Int);
                        break;
                    case Instruction.RAGE_PUSH_CONST_U24:
                    case Instruction.RAGE_PUSH_CONST_S16:
                        stack.Push(ins.GetOperandsAsInt.ToString(), Stack.DataType.Int);
                        break;
                    case Instruction.RAGE_PUSH_CONST_F:
                        stack.Push(ins.GetFloat);
                        break;
                    case Instruction.RAGE_DUP:
                        stack.Dup();
                        break;
                    case Instruction.RAGE_DROP:
                        stack.Drop();
                        break;
                    case Instruction.RAGE_NATIVE:
                    {
                        ulong hash = Scriptfile.X64NativeTable.GetNativeHashFromIndex(ins.GetNativeIndex);
                        Scriptfile.CrossReferenceNative(hash, this);
                        stack.NativeCallTest(hash, this.Scriptfile.X64NativeTable.GetNativeFromIndex(ins.GetNativeIndex), ins.GetNativeParams, ins.GetNativeReturns);
                        break;
                    }
                    case Instruction.RAGE_ENTER:
                        throw new Exception("Unexpected Function Definition");
                    case Instruction.RAGE_LEAVE:
                    {
                        Stack.DataType type = ins.GetOperand(1) == 1 ? stack.TopType : Stack.DataType.Unk;
                        string tempstring = stack.PopListForCall(ins.GetOperand(1));
                        switch (ins.GetOperand(1))
                        {
                            case 0:
                            {
                                ReturnType = Stack.DataType.None;
                                break;
                            }
                            case 1:
                            {
                                Stack.DataType leftType = returncheck(tempstring);
                                ReturnType = type.LessThan(leftType) ? leftType : type;
                                break;
                            }
                            default:
                            {
                                if (stack.TopType == Stack.DataType.String)
                                    ReturnType = Stack.DataType.String;
                                break;
                            }
                        }
                        break;
                    }
                    case Instruction.RAGE_LOAD:
                        stack.Op_RefGet();
                        break;
                    case Instruction.RAGE_STORE:
                    {
                        if (stack.PeekVar(1) == null)
                        {
                            stack.Drop();
                            stack.Drop();
                            break;
                        }
                        if (stack.TopType == Stack.DataType.Int)
                        {
                            int tempint;
                            string tempstring = stack.Pop().AsLiteral;
                            if (Utils.IntParse(tempstring, out tempint))
                                stack.PeekVar(0).Value = tempint;
                            break;
                        }
                        stack.Drop();
                        break;
                    }
                    case Instruction.RAGE_STORE_REV:
                    {
                        if (stack.PeekVar(1) == null)
                        {
                            stack.Drop();
                            break;
                        }
                        if (stack.TopType == Stack.DataType.Int)
                        {
                            int tempint;
                            string tempstring = stack.Pop().AsLiteral;
                            if (Utils.IntParse(tempstring, out tempint))
                                stack.PeekVar(0).Value = tempint;
                        }
                        break;
                    }
                    case Instruction.RAGE_LOAD_N:
                    {
                        int tempint;
                        if (Program.IntStyle == Program.IntType._hex)
                            tempint = int.Parse(stack.PeekItem(1).Substring(2), System.Globalization.NumberStyles.HexNumber);
                        else
                            tempint = int.Parse(stack.PeekItem(1));
                        SetImmediate(tempint);
                        stack.Op_ToStack();
                        break;
                    }
                    case Instruction.RAGE_STORE_N:
                    {
                        int tempint;
                        if (Program.IntStyle == Program.IntType._hex)
                            tempint = int.Parse(stack.PeekItem(1).Substring(2), System.Globalization.NumberStyles.HexNumber);
                        else
                            tempint = int.Parse(stack.PeekItem(1));
                        SetImmediate(tempint);
                        stack.Op_FromStack();
                        break;
                    }
                    case Instruction.RAGE_ARRAY_U8:
                    case Instruction.RAGE_ARRAY_U16:
                    {
                        int tempint;
                        if (!Utils.IntParse(stack.PeekItem(1), out tempint))
                            tempint = -1;
                        CheckArray(ins.GetOperandsAsUInt, tempint);
                        stack.Op_ArrayGetP(ins.GetOperandsAsUInt);
                        break;
                    }
                    case Instruction.RAGE_ARRAY_U8_LOAD:
                    case Instruction.RAGE_ARRAY_U16_LOAD:
                    {
                        int tempint;
                        if (!Utils.IntParse(stack.PeekItem(1), out tempint))
                            tempint = -1;
                        CheckArray(ins.GetOperandsAsUInt, tempint);
                        stack.Op_ArrayGet(ins.GetOperandsAsUInt);
                        break;
                    }
                    case Instruction.RAGE_ARRAY_U8_STORE:
                    case Instruction.RAGE_ARRAY_U16_STORE:
                    {
                        int tempint;
                        if (!Utils.IntParse(stack.PeekItem(1), out tempint))
                            tempint = -1;
                        CheckArray(ins.GetOperandsAsUInt, tempint);
                        SetArray(stack.ItemType(2));
                        Vars_Info.Var Var = stack.PeekVar(0);
                        if (Var != null && stack.isPointer(0))
                            CheckInstruction(2, Var.DataType);
                        stack.Op_ArraySet(ins.GetOperandsAsUInt);
                        break;
                    }
                    case Instruction.RAGE_LOCAL_U8:
                    case Instruction.RAGE_LOCAL_U16:
                        stack.PushPVar(GetFrameVar(ins.GetOperandsAsUInt));
                        GetFrameVar(ins.GetOperandsAsUInt).call();
                        break;
                    case Instruction.RAGE_LOCAL_U8_LOAD:
                    case Instruction.RAGE_LOCAL_U16_LOAD:
                        stack.PushVar(GetFrameVar(ins.GetOperandsAsUInt));
                        GetFrameVar(ins.GetOperandsAsUInt).call();
                        break;
                    case Instruction.RAGE_LOCAL_U8_STORE:
                    case Instruction.RAGE_LOCAL_U16_STORE:
                    {
                        if (stack.TopType != Stack.DataType.Unk)
                        {
                            if (GetFrameVar(ins.GetOperandsAsUInt).DataType.LessThan(stack.TopType))
                                GetFrameVar(ins.GetOperandsAsUInt).DataType = stack.TopType;
                        }
                        else
                        {
                            CheckInstruction(0, GetFrameVar(ins.GetOperandsAsUInt).DataType);
                        }

                        string tempstring = stack.Pop().AsLiteral;
                        if (stack.TopType == Stack.DataType.Int)
                        {
                            tempstring = stack.Pop().AsLiteral;
                            if (ins.GetOperandsAsUInt > Pcount)
                            {
                                int tempint;
                                if (Utils.IntParse(tempstring, out tempint))
                                    GetFrameVar(ins.GetOperandsAsUInt).Value = tempint;
                            }
                        }
                        else
                        {
                            stack.Drop();
                        }
                        GetFrameVar(ins.GetOperandsAsUInt).call();
                        break;
                    }
                    case Instruction.RAGE_STATIC_U8:
                    case Instruction.RAGE_STATIC_U16:
                        stack.PushPVar(Scriptfile.Statics.GetVarAtIndex(ins.GetOperandsAsUInt).Fixed());
                        break;
                    case Instruction.RAGE_STATIC_U8_LOAD:
                    case Instruction.RAGE_STATIC_U16_LOAD:
                        stack.PushVar(Scriptfile.Statics.GetVarAtIndex(ins.GetOperandsAsUInt).Fixed());
                        break;
                    case Instruction.RAGE_STATIC_U8_STORE:
                    case Instruction.RAGE_STATIC_U16_STORE:
                        if (stack.TopType != Stack.DataType.Unk)
                            Scriptfile.UpdateStaticType(ins.GetOperandsAsUInt, stack.TopType);
                        else
                            CheckInstruction(0, Scriptfile.Statics.GetTypeAtIndex(ins.GetOperandsAsUInt));
                        stack.Drop();
                        break;
                    case Instruction.RAGE_IADD_U8:
                    case Instruction.RAGE_IADD_S16:
                    case Instruction.RAGE_IMUL_U8:
                    case Instruction.RAGE_IMUL_S16:
                        CheckInstruction(0, Stack.DataType.Int);
                        stack.Op_AmmImm(ins.GetOperandsAsInt);
                        break;
                    case Instruction.RAGE_IOFFSET:
                        stack.Op_GetImmP();
                        break;
                    case Instruction.RAGE_IOFFSET_U8:
                    case Instruction.RAGE_IOFFSET_S16:
                        CheckImmediate((int)ins.GetOperandsAsUInt + 1);
                        stack.Op_GetImmP(ins.GetOperandsAsUInt);
                        break;
                    case Instruction.RAGE_IOFFSET_U8_LOAD:
                    case Instruction.RAGE_IOFFSET_S16_LOAD:
                        CheckImmediate((int)ins.GetOperandsAsUInt + 1);
                        stack.Op_GetImm(ins.GetOperandsAsUInt);
                        break;
                    case Instruction.RAGE_IOFFSET_U8_STORE:
                    case Instruction.RAGE_IOFFSET_S16_STORE:
                        CheckImmediate((int)ins.GetOperandsAsUInt + 1);
                        stack.Op_SetImm(ins.GetOperandsAsUInt);
                        break;
                    case Instruction.RAGE_GLOBAL_U16:
                    case Instruction.RAGE_GLOBAL_U24:
                        stack.PushPointer(Vars_Info.GlobalName + "_" + ins.GetOperandsAsUInt.ToString());
                        break;
                    case Instruction.RAGE_GLOBAL_U16_LOAD:
                    case Instruction.RAGE_GLOBAL_U24_LOAD:
                        stack.Push(Vars_Info.GlobalName + "_" + ins.GetOperandsAsUInt.ToString());
                        break;
                    case Instruction.RAGE_GLOBAL_U16_STORE:
                    case Instruction.RAGE_GLOBAL_U24_STORE:
                        stack.Op_Set(Vars_Info.GlobalName + "_" + ins.GetOperandsAsUInt.ToString());
                        break;
                    case Instruction.RAGE_J:
                        break;
                    case Instruction.RAGE_JZ:
                        CheckInstruction(0, Stack.DataType.Bool);
                        stack.Drop();
                        break;
                    case Instruction.RAGE_IEQ_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_INE_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_IGT_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_IGE_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_ILT_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_ILE_JZ:
                        CheckInstruction(0, Stack.DataType.Int, 2);
                        stack.Drop();
                        stack.Drop();
                        break;
                    case Instruction.RAGE_CALL:
                    {
                        Function func = GetFunctionWithinOffset(ins.GetOperandsAsInt);
                        if (!func.predecodeStarted)
                            func.PreDecode();
                        if (func.predecoded)
                        {
                            for (int j = 0; j < func.Pcount; j++)
                            {
                                if (stack.ItemType(func.Pcount - j - 1) != Stack.DataType.Unk)
                                {
                                    if (func.Params.GetTypeAtIndex((uint)j).LessThan(stack.ItemType(func.Pcount - j - 1)))
                                    {
                                        if (func != this)
                                            func.UpdateFuncParamType((uint)j, stack.ItemType(func.Pcount - j - 1));
                                    }
                                }
                                CheckInstruction(func.Pcount - j - 1, func.Params.GetTypeAtIndex((uint)j), 1, true);
                            }
                        }

                        CreateFunctionPath(this, func);
                        stack.FunctionCall(func);
                        break;
                    }
                    case Instruction.RAGE_SWITCH:
                        CheckInstruction(0, Stack.DataType.Int, Program.RDROpcodes ? 2 : 1);
                        break;
                    case Instruction.RAGE_STRING:
                    {
                        string tempstring = stack.Pop().AsLiteral;
                        stack.PushString("");
                        break;
                    }
                    case Instruction.RAGE_STRINGHASH:
                        CheckInstruction(0, Stack.DataType.StringPtr);
                        stack.Op_Hash();
                        break;
                    case Instruction.RAGE_TEXT_LABEL_ASSIGN_STRING:
                        CheckInstructionString(0, ins.GetOperandsAsInt, 2);
                        stack.Op_StrCpy(ins.GetOperandsAsInt);
                        break;
                    case Instruction.RAGE_TEXT_LABEL_ASSIGN_INT:
                        CheckInstructionString(0, ins.GetOperandsAsInt);
                        CheckInstruction(1, Stack.DataType.Int);
                        stack.Op_ItoS(ins.GetOperandsAsInt);
                        break;
                    case Instruction.RAGE_TEXT_LABEL_APPEND_STRING:
                        CheckInstructionString(0, ins.GetOperandsAsInt, 2);
                        stack.Op_StrAdd(ins.GetOperandsAsInt);
                        break;
                    case Instruction.RAGE_TEXT_LABEL_APPEND_INT:
                        CheckInstructionString(0, ins.GetOperandsAsInt);
                        CheckInstruction(1, Stack.DataType.Int);
                        stack.Op_StrAddI(ins.GetOperandsAsInt);
                        break;
                    case Instruction.RAGE_TEXT_LABEL_COPY:
                        stack.Op_SnCopy();
                        break;
                    case Instruction.RAGE_CATCH:
                        break;
                    case Instruction.RAGE_THROW:
                        break;
                    case Instruction.RAGE_CALLINDIRECT:
                        stack.pcall();
                        break;
                    case Instruction.RAGE_PUSH_CONST_M1:
                    case Instruction.RAGE_PUSH_CONST_0:
                    case Instruction.RAGE_PUSH_CONST_1:
                    case Instruction.RAGE_PUSH_CONST_2:
                    case Instruction.RAGE_PUSH_CONST_3:
                    case Instruction.RAGE_PUSH_CONST_4:
                    case Instruction.RAGE_PUSH_CONST_5:
                    case Instruction.RAGE_PUSH_CONST_6:
                    case Instruction.RAGE_PUSH_CONST_7:
                        stack.Push(ins.GetImmBytePush);
                        break;
                    case Instruction.RAGE_PUSH_CONST_FM1:
                    case Instruction.RAGE_PUSH_CONST_F0:
                    case Instruction.RAGE_PUSH_CONST_F1:
                    case Instruction.RAGE_PUSH_CONST_F2:
                    case Instruction.RAGE_PUSH_CONST_F3:
                    case Instruction.RAGE_PUSH_CONST_F4:
                    case Instruction.RAGE_PUSH_CONST_F5:
                    case Instruction.RAGE_PUSH_CONST_F6:
                    case Instruction.RAGE_PUSH_CONST_F7:
                        stack.Push(ins.GetImmFloatPush);
                        break;

                    // RDR Extended Instruction Set.
                    case Instruction.RAGE_LOCAL_LOAD_S:
                    case Instruction.RAGE_LOCAL_STORE_S:
                    case Instruction.RAGE_LOCAL_STORE_SR:
                    case Instruction.RAGE_STATIC_LOAD_S:
                    case Instruction.RAGE_STATIC_STORE_S:
                    case Instruction.RAGE_STATIC_STORE_SR:
                    case Instruction.RAGE_LOAD_N_S:
                    case Instruction.RAGE_STORE_N_S:
                    case Instruction.RAGE_STORE_N_SR:
                    case Instruction.RAGE_GLOBAL_LOAD_S:
                    case Instruction.RAGE_GLOBAL_STORE_S:
                    case Instruction.RAGE_GLOBAL_STORE_SR:
                        if (Scriptfile.CodeSet.Count <= 127) throw new Exception("Unexpected Instruction");
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
}
