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
        public static OpcodeSet Codeset;
        public static x64NativeFile X64npi;
        internal static Ini.IniFile Config;
        public static Object ThreadLock;
        public static int ThreadCount;

        class Options
        {
            [Option('n', "natives", Required = false, HelpText = "native json file")]
            public string NativeFile { get; set; }

            [Option('i', "in", Default = null, Required = true, HelpText = "Input Directory/File Path.")]
            public string InputPath { get; set; }

            [Option('o', "out", Default = null, Required = false, HelpText = "Output Directory/File Path")]
            public string OutputPath { get; set; }

            [Option("gzin", Default = false, Required = false, HelpText = "Compressed Input (GZIP)")]
            public bool CompressedInput { get; set; }

            [Option("gzout", Default = false, Required = false, HelpText = "Compress Output (GZIP)")]
            public bool CompressOutput { get; set; }

            [Option('c', "opcode", Default = "v", Required = true, HelpText = "Opcode Set (v|vconsole|rdr|rdrconsole)")]
            public string Opcode { get; set; }

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
            _bit32 = _endian = _rdrOpcodes = _rdrPCCipher = false;
            switch (o.Opcode.ToLower())
            {
                case "v":
                    Program.Codeset = new OpcodeSet();
                    break;
                case "vconsole":
                    _bit32 = _endian = true;
                    Program.Codeset = new OpcodeSet();
                    break;
                case "rdr":
                    _rdrOpcodes = _rdrPCCipher = true;
                    Program.Codeset = new RDOpcodeSet();
                    break;
                case "rdrconsole":
                    _rdrOpcodes = true;
                    Program.Codeset = new RDRConsoleOpcodeSet();
                    break;
                default:
                    throw new System.ArgumentException("Invalid Opcode Set: " + o.Opcode);
            }

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

            Program._compressin = o.CompressedInput;
            Program._compressout = o.CompressOutput;
            Program._AggregateFunctions = o.Aggregate;
            if (o.AggMinHits > 0) Program._agg_min_hits = o.AggMinHits;
            if (o.AggMinLines > 0) Program._agg_min_lines = o.AggMinLines;
        }

        private static void InitializeNativeTable(string nativeFile)
        {
            Stream nativeJson;
            if (nativeFile != null && File.Exists(nativeFile))
                nativeJson = File.OpenRead(nativeFile);
            else if (Program.RDROpcodes)
                nativeJson = new MemoryStream(Properties.Resources.RDNatives);
            else
                nativeJson = new MemoryStream(Properties.Resources.Natives);
            X64npi = new x64NativeFile(nativeJson);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        private static ScriptFile ProcessScriptfile(string inputPath, string outputPath)
        {
            /* A ScriptFile tends to skip around the offset table */
            MemoryStream buffer = new MemoryStream();
            using (Stream fs = File.OpenRead(inputPath))
            {
                (Program.CompressedInput ? new GZipStream(fs, CompressionMode.Decompress) : fs).CopyTo(buffer);
            }

            ScriptFile scriptFile = new ScriptFile(buffer, Program.Codeset);
            if (outputPath != null)
            {
                using (Stream stream = File.Create(outputPath))
                    scriptFile.Save(Program.CompressedOutput ? new GZipStream(stream, CompressionMode.Compress) : stream, true);
            }
            else
                scriptFile.Save(Console.OpenStandardOutput(), false);

            buffer.Close();
            return scriptFile;
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
                string inputPath = Utils.GetAbsolutePath(o.InputPath);
                string outputPath = o.OutputPath != null ? Utils.GetAbsolutePath(o.OutputPath) : null;
                string nativeFile = o.NativeFile != null ? Utils.GetAbsolutePath(o.NativeFile) : null;
                if (File.Exists(inputPath)) // Decompile a single file if given the option.
                {
                    if (outputPath != null && File.Exists(outputPath) && !o.Force) { Console.WriteLine("Cannot overwrite file, use -f to force."); return; }

                    InitializeINIFields(o); Program._AggregateFunctions = false;
                    InitializeNativeTable(nativeFile);
                    ProcessScriptfile(inputPath, outputPath).Close();
                }
                else if (Directory.Exists(inputPath)) // Decompile directory
                {
                    if (outputPath == null || !Directory.Exists(outputPath)) { Console.WriteLine("Invalid Output Directory"); return; }

                    InitializeINIFields(o);
                    InitializeNativeTable(nativeFile);
                    foreach (string file in Directory.GetFiles(inputPath, "*.ysc*"))
                        CompileList.Enqueue(file);
                    foreach (string file in Directory.GetFiles(inputPath, "*.ysc.full*"))
                        CompileList.Enqueue(file);

                    SaveDirectory = outputPath;
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
                        Agg.Instance.SaveAggregate(outputPath);
                        Agg.Instance.SaveFrequency(outputPath);
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
                    string suffix = ".c" + (Program.CompressedOutput ? ".gz" : "");
                    string output = Path.Combine(SaveDirectory, Path.GetFileNameWithoutExtension(scriptToDecode) + suffix);
                    Console.WriteLine("Decompiling: " + scriptToDecode + " > " + output);

                    ScriptFile scriptFile = ProcessScriptfile(scriptToDecode, output);
                    if (Program.AggregateFunctions) /* Compile aggregation statistics for each function. */
                        scriptFile.CompileAggregate();

                    scriptFile.Close();
                    if ((_gcCount.Value++) % 25 == 0)
                        GC.Collect();
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
        public static IntType getIntType { get => _getINTType; }

        public static bool Find_Show_Array_Size()
        {
            return _Show_Array_Size = Program.Config.IniReadBool("Base", "Show_Array_Size", true);
        }

        private static bool _Show_Array_Size = false;
        public static bool Show_Array_Size { get => _Show_Array_Size; }

        public static bool Find_Reverse_Hashes()
        {
            return _Reverse_Hashes = Program.Config.IniReadBool("Base", "Reverse_Hashes", true);
        }

        private static bool _Reverse_Hashes = false;
        public static bool Reverse_Hashes { get => _Reverse_Hashes; }

        public static bool Find_Declare_Variables()
        {
            return _Declare_Variables = Program.Config.IniReadBool("Base", "Declare_Variables", true);
        }

        private static bool _Declare_Variables = false;
        public static bool Declare_Variables { get => _Declare_Variables; }

        public static bool Find_Shift_Variables()
        {
            return _Shift_Variables = Program.Config.IniReadBool("Base", "Shift_Variables", true);
        }

        private static bool _Shift_Variables = false;
        public static bool Shift_Variables { get => _Shift_Variables; }

        public static bool Find_Use_MultiThreading()
        {
            return _Use_MultiThreading = Program.Config.IniReadBool("Base", "Use_MultiThreading", false);
        }

        private static bool _Use_MultiThreading = false;
        public static bool Use_MultiThreading { get => _Use_MultiThreading; }

        public static bool Find_IncFuncPos()
        {
            return _IncFuncPos = Program.Config.IniReadBool("Base", "Include_Function_Position", false);
        }

        private static bool _IncFuncPos = false;
        public static bool IncFuncPos { get => _IncFuncPos; }

        public static bool Find_Show_Func_Pointer()
        {
            return _Show_Func_Pointer = Program.Config.IniReadBool("Base", "Show_Func_Pointer", false);
        }

        private static bool _Show_Func_Pointer = false;
        public static bool Show_Func_Pointer { get => _Show_Func_Pointer; }

        public static bool Find_Nat_Namespace()
        {
            return _Show_Nat_Namespace = Program.Config.IniReadBool("Base", "Show_Nat_Namespace", false);
        }

        private static bool _Show_Nat_Namespace = false;
        public static bool Show_Nat_Namespace { get => _Show_Nat_Namespace; }

        public static bool Find_Hex_Index()
        {
            return _Hex_Index = Program.Config.IniReadBool("Base", "Hex_Index", false);
        }

        private static bool _Hex_Index = false;
        public static bool Hex_Index { get => _Hex_Index; }

        public static bool Find_Upper_Natives()
        {
            return _upper_Natives = Program.Config.IniReadBool("Base", "Uppercase_Natives", false);
        }

        private static bool _upper_Natives = false;
        public static bool Upper_Natives { get => _upper_Natives; }
        public static string NativeName(string s) => Program.Upper_Natives ? s.ToUpper() : s.ToLower();

        private static bool _AggregateFunctions = false;
        public static bool AggregateFunctions { get => _AggregateFunctions; }

        private static int _agg_min_lines = 7;
        public static int AggregateMinLines { get => _agg_min_lines; }

        private static int _agg_min_hits = 3;
        public static int AggregateMinHits { get => _agg_min_hits; }

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

        private static bool _compressin = false;
        public static bool CompressedInput { get => _compressin; }

        private static bool _compressout = false;
        public static bool CompressedOutput { get => _compressout; }

        private static bool _bit32 = false;
        public static bool IsBit32 { get => _bit32; }

        private static bool _endian = false;
        public static bool SwapEndian { get => _endian; }

        private static bool _rdrOpcodes = false;
        public static bool RDROpcodes { get => _rdrOpcodes; }

        private static bool _rdrPCCipher = false;
        public static bool RDRNativeCipher { get => _rdrPCCipher; }
    }
}
