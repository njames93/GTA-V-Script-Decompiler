using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decompiler
{
	//Not a fan of this code, should really have been handled using a tree but too far into project to change this now
	internal class CodePath
	{
		public CodePath Parent;
		public int EndOffset;
		public int BreakOffset;
		public CodePathType Type;
		public List<CodePath> ChildPaths;

		public CodePath(CodePathType Type, int EndOffset, int BreakOffset)
		{
			Parent = null;
			this.Type = Type;
			this.EndOffset = EndOffset;
			this.BreakOffset = BreakOffset;
			ChildPaths = new List<CodePath>();
		}

		public CodePath(CodePath Parent, CodePathType Type, int EndOffset, int BreakOffset)
		{
			this.Parent = Parent;
			this.Type = Type;
			this.EndOffset = EndOffset;
			this.BreakOffset = BreakOffset;
			ChildPaths = new List<CodePath>();
		}

		public CodePath CreateCodePath(CodePathType Type, int EndOffset, int BreakOffset)
		{
			CodePath C = new CodePath(this, Type, EndOffset, BreakOffset);
			this.ChildPaths.Add(C);
			return C;
		}

	}

	internal enum CodePathType
	{
		While,
		If,
		Else,
		Main
	}

	internal class SwitchStatement
	{
		public Dictionary<int, List<string>> Cases;
		public List<int> Offsets;
		public int breakoffset;
		public SwitchStatement Parent;
		public List<SwitchStatement> ChildSwitches;

		public SwitchStatement(Dictionary<int, List<string>> Cases, int BreakOffset)
		{
			Parent = null;
			this.Cases = Cases;
			this.breakoffset = BreakOffset;
			ChildSwitches = new List<SwitchStatement>();
			Offsets = Cases == null ? new List<int>() : Cases.Keys.ToList();
			Offsets.Add(BreakOffset);
		}

		public SwitchStatement(SwitchStatement Parent, Dictionary<int, List<string>> Cases, int BreakOffset)
		{
			this.Parent = Parent;
			this.Cases = Cases;
			this.breakoffset = BreakOffset;
			ChildSwitches = new List<SwitchStatement>();
			Offsets = Cases == null ? new List<int>() : Cases.Keys.ToList();
			Offsets.Add(BreakOffset);
		}

		public SwitchStatement CreateSwitchStatement(Dictionary<int, List<string>> Cases, int BreakOffset)
		{
			SwitchStatement S = new SwitchStatement(this, Cases, BreakOffset);
			this.ChildSwitches.Add(S);
			return S;
		}
	}

}
