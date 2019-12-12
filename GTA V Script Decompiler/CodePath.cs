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
        public bool Escaped = false;

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

        public bool IsSwitch => (Type == CodePathType.Switch);

        public virtual bool AllEscaped()
        {
            bool escaped = true;
            foreach (CodePath p in ChildPaths)
                escaped &= p.Escaped;
            return escaped;
        }
    }

    internal enum CodePathType
    {
        While,
        If,
        Else,
        Main,
        Switch
    }

    internal class SwitchPath : CodePath
    {
        public int ActiveOffset = -1;
        public bool HasDefaulted = false;
        public Dictionary<int, bool> EscapedCases = new Dictionary<int, bool>();

        public Dictionary<int, List<string>> Cases;
        public List<int> Offsets;

        public SwitchPath(CodePathType Type, int EndOffset, int BreakOffset)
            : base(Type, EndOffset, BreakOffset)
        {
            Offsets = new List<int>();
            Cases = new Dictionary<int, List<string>>();
        }

        public SwitchPath(CodePath Parent, CodePathType Type, int EndOffset, int BreakOffset)
            : base(Parent, Type, EndOffset, BreakOffset)
        {
            Offsets = new List<int>();
            Cases = new Dictionary<int, List<string>>();
        }

        public SwitchPath(Dictionary<int, List<string>> Cases, int EndOffset, int BreakOffset)
            : base(CodePathType.Switch, EndOffset, BreakOffset)
        {
            this.Cases = Cases;
            Offsets = Cases == null ? new List<int>() : Cases.Keys.ToList();
            Offsets.Add(BreakOffset);
            if (Program.RDROpcodes) Offsets.Sort();
            foreach (int offset in Offsets)
                EscapedCases[offset] = false;
        }

        public SwitchPath(CodePath Parent, Dictionary<int, List<string>> Cases, int EndOffset, int BreakOffset)
            : base(Parent, CodePathType.Switch, EndOffset, BreakOffset)
        {
            this.Cases = Cases;
            Offsets = Cases == null ? new List<int>() : Cases.Keys.ToList();
            Offsets.Add(BreakOffset);
            if (Program.RDROpcodes) Offsets.Sort();
            foreach (int offset in Offsets)
                EscapedCases[offset] = false;
        }

        public override bool AllEscaped()
        {
            bool escaped = base.AllEscaped();
            foreach (KeyValuePair<int, bool> entry in EscapedCases)
                escaped &= entry.Value;
            return escaped;
        }
    }
}
