using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.CodeDom.Compiler;
using CommandLine;
using System.Threading;

namespace Decompiler
{
    static class Program
    {
        public static x64NativeFile X64npi;
        internal static Ini.IniFile Config;
        public static Object ThreadLock;
        public static int ThreadCount;

        class Options
        {
            [Option('n', "natives", Required = true, HelpText = "native json file")]
            public string NativeFile { get; set; }

            [Option('y', "ysc", Default = null, Required = true, HelpText = "YSC Path")]
            public string YSCPath { get; set; }

            [Option('o', "out", Default = null, Required = false, HelpText = "Output Directory/File Path")]
            public string OutputPath { get; set; }

            [Option('f', "force", Default = false, Required = false, HelpText = "Allow output file overriding.")]
            public bool Force { get; set; }

            [Option('a', "aggregate", Default = false, Required = false, HelpText = "Compute aggregation statistics of bulk dataset.")]
            public bool Aggregate { get; set; }

            [Option("minlines", Default = -1, Required = false, HelpText = "Minimum function line count for aggregation")]
            public int AggMinLines { get; set; }

            [Option("minhits", Default = -1, Required = false, HelpText = "Minimum number of occurrences for aggregation.")]
            public int AggMinHits { get; set; }
        }

        private static void InitializeINIFields(Options o)
        {
            Program.Find_getINTType();
            Program.Find_Show_Array_Size();
            Program.Find_Reverse_Hashes();
            Program.Find_Declare_Variables();
            Program.Find_Shift_Variables();
            Program.Find_Show_Func_Pointer();
            Program.Find_Use_MultiThreading();
            Program.Find_IncFuncPos();
            Program.Find_Nat_Namespace();
            Program.Find_Hex_Index();
            Program.Find_Upper_Natives();
            Program.Find_Aggregate_MinHits();
            Program.Find_Aggregate_MinLines();

            Program.SaveDirectory = o.OutputPath;
            Program._AggregateFunctions = o.Aggregate;
            if (o.AggMinHits > 0) Program._agg_min_hits = o.AggMinHits;
            if (o.AggMinLines > 0) Program._agg_min_lines = o.AggMinLines;
        }

        private static void InitializeNativeTable(string nativeFile)
        {
            Stream nativeJson;
            if (nativeFile != null && File.Exists(nativeFile))
                nativeJson = File.OpenRead(nativeFile);
            else
                nativeJson = new MemoryStream(Properties.Resources.Natives);
            X64npi = new x64NativeFile(nativeJson);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ThreadLock = new object();
            Config = new Ini.IniFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (!File.Exists(o.NativeFile)) { Console.WriteLine("Invalid Native File"); return; }
                if (File.Exists(o.YSCPath)) // Decompile a single file if given the option.
                {
                    if (o.OutputPath != null && File.Exists(o.OutputPath) && !o.Force) { Console.WriteLine("Cannot overwrite file, use -f to force."); return; }

                    InitializeINIFields(o); Program._AggregateFunctions = false;
                    InitializeNativeTable(o.NativeFile);
                    using (Stream fs = File.OpenRead(o.YSCPath))
                    {
                        MemoryStream buffer = new MemoryStream(); fs.CopyTo(buffer);
                        ScriptFile scriptFile = new ScriptFile(buffer);

                        if (o.OutputPath != null)
                            scriptFile.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, o.OutputPath));
                        else
                            scriptFile.Save(Console.OpenStandardOutput(), false);
                        scriptFile.Close();
                    }
                }
                else if (Directory.Exists(o.YSCPath)) // Decompile directory
                {
                    if (o.OutputPath == null || !Directory.Exists(o.OutputPath)) { Console.WriteLine("Invalid Output Directory"); return; }

                    InitializeINIFields(o);
                    InitializeNativeTable(o.NativeFile);
                    foreach (string file in Directory.GetFiles(o.YSCPath, "*.ysc"))
                        CompileList.Enqueue(file);
                    foreach (string file in Directory.GetFiles(o.YSCPath, "*.ysc.full"))
                        CompileList.Enqueue(file);

                    if (Program.Use_MultiThreading)
                    {
                        for (int i = 0; i < Environment.ProcessorCount - 1; i++)
                        {
                            Program.ThreadCount++;
                            new System.Threading.Thread(Decompile).Start();
                        }

                        Program.ThreadCount++;
                        Decompile();
                        while (Program.ThreadCount > 0)
                            System.Threading.Thread.Sleep(10);
                    }
                    else
                    {
                        Program.ThreadCount++;
                        Decompile();
                    }

                    if (Program.AggregateFunctions)
                    {
                        Agg.Instance.SaveAggregate(SaveDirectory);
                        Agg.Instance.SaveFrequency(SaveDirectory);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid YSC Path");
                }
            });
        }

        static string SaveDirectory = "";
        static Queue<string> CompileList = new Queue<string>();
        public static ThreadLocal<int> _gcCount = new ThreadLocal<int>(() => { return 0; });
        private static void Decompile()
        {
            while (CompileList.Count > 0)
            {
                string scriptToDecode;
                lock (Program.ThreadLock)
                {
                    scriptToDecode = CompileList.Dequeue();
                }
                try
                {
                    string output = Path.Combine(SaveDirectory, Path.GetFileNameWithoutExtension(scriptToDecode) + ".c");
                    using (Stream fs = File.OpenRead(scriptToDecode)) {
                        MemoryStream buffer = new MemoryStream(); fs.CopyTo(buffer);

                        Console.WriteLine("Decompiling: " + scriptToDecode + " > " + output);
                        ScriptFile scriptFile = new ScriptFile(buffer);
                        scriptFile.Save(output);
                        if (Program.AggregateFunctions) scriptFile.TryAgg();
                        scriptFile.Close();
                        buffer.Close();

                        if ((_gcCount.Value++) % 25 == 0)
                            GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    throw new SystemException("Error decompiling script " + Path.GetFileNameWithoutExtension(scriptToDecode) + " - " + ex.Message);
                }
            }
            Program.ThreadCount--;
        }

        public enum IntType
        {
            _int,
            _uint,
            _hex
        }

        public static IntType Find_getINTType()
        {
            string s = Program.Config.IniReadValue("Base", "IntStyle").ToLower();
            if (s.StartsWith("int")) return _getINTType = IntType._int;
            else if (s.StartsWith("uint")) return _getINTType = IntType._uint;
            else if (s.StartsWith("hex")) return _getINTType = IntType._hex;
            else
            {
                Program.Config.IniWriteValue("Base", "IntStyle", "int");
                return _getINTType = IntType._int;
            }
        }

        private static IntType _getINTType = IntType._int;

        public static IntType getIntType
        {
            get { return _getINTType; }
        }

        public static bool Find_Show_Array_Size()
        {
            return _Show_Array_Size = Program.Config.IniReadBool("Base", "Show_Array_Size", true);
        }

        private static bool _Show_Array_Size = false;

        public static bool Find_Reverse_Hashes()
        {
            return _Reverse_Hashes = Program.Config.IniReadBool("Base", "Reverse_Hashes", true);
        }

        private static bool _Reverse_Hashes = false;

        public static bool Reverse_Hashes
        {
            get { return _Reverse_Hashes; }
        }

        public static bool Show_Array_Size
        {
            get { return _Show_Array_Size; }
        }

        public static bool Find_Declare_Variables()
        {
            return _Declare_Variables = Program.Config.IniReadBool("Base", "Declare_Variables", true);
        }

        private static bool _Declare_Variables = false;

        public static bool Declare_Variables
        {
            get { return _Declare_Variables; }
        }

        public static bool Find_Shift_Variables()
        {
            return _Shift_Variables = Program.Config.IniReadBool("Base", "Shift_Variables", true);
        }

        private static bool _Shift_Variables = false;

        public static bool Shift_Variables
        {
            get { return _Shift_Variables; }
        }

        public static bool Find_Use_MultiThreading()
        {
            return _Use_MultiThreading = Program.Config.IniReadBool("Base", "Use_MultiThreading", false);
        }

        private static bool _Use_MultiThreading = false;

        public static bool Use_MultiThreading
        {
            get { return _Use_MultiThreading; }
        }

        public static bool Find_IncFuncPos()
        {
            return _IncFuncPos = Program.Config.IniReadBool("Base", "Include_Function_Position", false);
        }

        private static bool _IncFuncPos = false;

        public static bool IncFuncPos
        {
            get { return _IncFuncPos; }
        }

        public static bool Find_Show_Func_Pointer()
        {
            return _Show_Func_Pointer = Program.Config.IniReadBool("Base", "Show_Func_Pointer", false);
        }

        private static bool _Show_Func_Pointer = false;

        public static bool Show_Func_Pointer
        {
            get { return _Show_Func_Pointer; }
        }

        public static bool Find_Nat_Namespace()
        {
            return _Show_Nat_Namespace = Program.Config.IniReadBool("Base", "Show_Nat_Namespace", false);
        }

        private static bool _Show_Nat_Namespace = false;

        public static bool Show_Nat_Namespace
        {
            get { return _Show_Nat_Namespace; }
        }

        public static bool Find_Hex_Index()
        {
            return _Hex_Index = Program.Config.IniReadBool("Base", "Hex_Index", false);
        }

        private static bool _Hex_Index = false;

        public static bool Hex_Index
        {
            get { return _Hex_Index; }
        }

        public static bool Find_Upper_Natives()
        {
            return _upper_Natives = Program.Config.IniReadBool("Base", "Uppercase_Natives", false);
        }

        private static bool _upper_Natives = false;
        public static bool Upper_Natives
        {
            get { return _upper_Natives; }
        }

        private static bool _AggregateFunctions = false;
        public static bool AggregateFunctions { get { return _AggregateFunctions; } }

        private static int _agg_min_lines = 7;
        public static int AggregateMinLines { get { return _agg_min_lines; } }

        private static int _agg_min_hits = 3;
        public static int AggregateMinHits { get { return _agg_min_hits; } }

        public static int Find_Aggregate_MinLines()
        {
            int tmp;
            if (int.TryParse(Program.Config.IniReadValue("Base", "Aggregate_MinimumLines").ToLower(), out tmp))
                _agg_min_lines = tmp;
            return _agg_min_lines;
        }

        public static int Find_Aggregate_MinHits()
        {
            int tmp;
            if (int.TryParse(Program.Config.IniReadValue("Base", "Aggregate_MinimumHits").ToLower(), out tmp))
                _agg_min_hits = tmp;
            return _agg_min_hits;
        }
    }
}
