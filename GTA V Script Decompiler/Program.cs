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
        public static Hashes hashbank;
        public static GXTEntries gxtbank;
        static string SaveDirectory = "";

        public static Object ReadLock;
        public static Object WriteLock;
        public static int ThreadCount;
        static Queue<string> CompileList = new Queue<string>();
        static Queue<Tuple<ScriptFile, string>> DecodeList = new Queue<Tuple<ScriptFile, string>>();
        public static ThreadLocal<int> _gcCount = new ThreadLocal<int>(() => { return 0; });

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

            [Option('f', "force", Default = false, Required = false, HelpText = "Allow output file overriding")]
            public bool Force { get; set; }

            [Option('a', "aggregate", Default = false, Required = false, HelpText = "Compute aggregation statistics of bulk dataset")]
            public bool Aggregate { get; set; }

            [Option("minlines", Default = -1, Required = false, HelpText = "Minimum function line count for aggregation")]
            public int AggregateMinHits { get; set; }

            [Option("minhits", Default = -1, Required = false, HelpText = "Minimum number of occurrences for aggregation")]
            public int AggregateMinLines { get; set; }

            /* Previous INI Configuration */

            [Option("default", Default = false, Required = false, HelpText = "Use default configuration")]
            public bool Default { get; set; }

            [Option("uppercase", Default = false, Required = false, HelpText = "Use uppercase native names")]
            public bool UppercaseNatives { get; set; }

            [Option("namespace", Default = false, Required = false, HelpText = "Concatenate Namespace to Native definition")]
            public bool ShowNamespace { get; set; }

            [Option("int", Default = "int", Required = false, HelpText = "Integer Formatting Method (int, uint, hex)")]
            public string IntStyle { get; set; }

            [Option("hash", Default = false, Required = false, HelpText = "Use hash (Entity.dat) lookup table when formatting integers")]
            public bool ReverseHashes { get; set; }

            [Option("arraysize", Default = false, Required = false, HelpText = "Show array sizes in definitions")]
            public bool ShowArraySize { get; set; }

            [Option("declare", Default = false, Required = false, HelpText = "Declare all variables at the beginning of function/script definitions")]
            public bool DeclareVariables { get; set; }

            [Option("shift", Default = false, Required = false, HelpText = "Shift variable names, i.e., take into consideration the immediate size of stack values")]
            public bool ShiftVariables { get; set; }

            [Option("thread", Default = false, Required = false, HelpText = "Multithread bulk decompilation")]
            public bool UseMultiThreading { get; set; }

            [Option("position", Default = false, Required = false, HelpText = "Show function location in definition")]
            public bool ShowFuncPosition { get; set; }

            [Option("comment", Default = false, Required = false, HelpText = "Show inlined GXT entries and other comments.")]
            public bool ShowEntryComments { get; set; }

            [Option("HexIndex", Default = false, Required = false, HelpText = "")]
            public bool HexIndex { get; set; }
        }

        private static void InitializeINIFields(Options o)
        {
            IsBit32 = SwapEndian = RDROpcodes = RDRNativeCipher = false;
            switch (o.Opcode.ToLower())
            {
                case "v":
                    Program.Codeset = new OpcodeSet();
                    break;
                case "vconsole":
                    IsBit32 = SwapEndian = true;
                    Program.Codeset = new OpcodeSet();
                    break;
                case "rdr":
                    RDROpcodes = RDRNativeCipher = true;
                    Program.Codeset = new RDROpcodeSet();
                    break;
                case "rdr1311":
                    RDROpcodes = RDRNativeCipher = true;
                    Program.Codeset = new RDR1311OpcodeSet();
                    break;
                case "rdr1355":
                    RDROpcodes = RDRNativeCipher = true;
                    Program.Codeset = new RDR1355OpcodeSet();
                    break;
                case "rdrconsole":
                    RDROpcodes = true;
                    Program.Codeset = new RDRConsoleOpcodeSet();
                    break;
                default:
                    throw new System.ArgumentException("Invalid Opcode Set: " + o.Opcode);
            }

            Program.hashbank = new Hashes();
            Program.gxtbank = new GXTEntries();
            Program.UseMultiThreading = o.UseMultiThreading;
            Program.CompressedInput = o.CompressedInput;
            Program.CompressedOutput = o.CompressOutput;
            Program.AggregateFunctions = o.Aggregate;
            if (o.AggregateMinLines > 0) Program.AggregateMinLines = o.AggregateMinLines;
            if (o.AggregateMinHits > 0) Program.AggregateMinHits = o.AggregateMinHits;

            if (!o.Default)
            {
                Program.UppercaseNatives = o.UppercaseNatives;
                Program.ShowNamespace = o.ShowNamespace;
                Program.DeclareVariables = o.DeclareVariables;
                Program.ShiftVariables = o.ShiftVariables;
                Program.ReverseHashes = o.ReverseHashes;
                Program.ShowArraySize = o.ShowArraySize;
                Program.ShowEntryComments = o.ShowEntryComments;
                Program.HexIndex = o.HexIndex;
                Program.ShowFuncPosition = o.ShowFuncPosition;
                switch (o.IntStyle.ToLower())
                {
                    case "uint": Program.IntStyle = IntType._uint; break;
                    case "hex": Program.IntStyle = IntType._hex; break;
                    case "int":
                    default:
                        Program.IntStyle = IntType._int; break;
                }
            }
        }

        private static void InitializeNativeTable(string nativeFile)
        {
            if (nativeFile != null && !File.Exists(nativeFile))
                throw new Exception("Could not find provided native file: " + nativeFile);

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
        /// Open & Load a Scriptfile from the specified path.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <returns></returns>
        private static ScriptFile LoadScriptFile(string inputPath)
        {
            /* A ScriptFile tends to skip around the offset table */
            MemoryStream buffer = new MemoryStream();
            using (Stream fs = File.OpenRead(inputPath))
            {
                (Program.CompressedInput ? new GZipStream(fs, CompressionMode.Decompress) : fs).CopyTo(buffer);
            }
            return new ScriptFile(buffer, Program.Codeset);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        private static ScriptFile SaveScriptFile(ScriptFile scriptFile, string outputPath)
        {
            if (outputPath != null)
            {
                using (Stream stream = File.Create(outputPath))
                    scriptFile.Save(Program.CompressedOutput ? new GZipStream(stream, CompressionMode.Compress) : stream, true);
            }
            else
                scriptFile.Save(Console.OpenStandardOutput(), false);
            return scriptFile;
        }

        /// <summary>
        /// Open, Decode, Save a script file.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        private static ScriptFile ProcessScriptfile(string inputPath, string outputPath)
        {
            ScriptFile scriptFile = LoadScriptFile(inputPath).Predecode().BuildAggregation().Decode();
            return SaveScriptFile(scriptFile, outputPath);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ReadLock = new object();
            WriteLock = new object();
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                string inputPath = Utils.GetAbsolutePath(o.InputPath);
                string outputPath = o.OutputPath != null ? Utils.GetAbsolutePath(o.OutputPath) : null;
                string nativeFile = o.NativeFile != null ? Utils.GetAbsolutePath(o.NativeFile) : null;
                if (File.Exists(inputPath)) // Decompile a single file if given the option.
                {
                    if (outputPath != null && File.Exists(outputPath) && !o.Force) { Console.WriteLine("Cannot overwrite file, use -f to force."); return; }

                    InitializeINIFields(o); Program.AggregateFunctions = false;
                    InitializeNativeTable(nativeFile);
                    ProcessScriptfile(inputPath, outputPath).Close();
                }
                else if (Directory.Exists(inputPath)) // Decompile directory
                {
                    if (outputPath == null || !Directory.Exists(outputPath)) { Console.WriteLine("Invalid Output Directory"); return; }
                    SaveDirectory = outputPath;

                    InitializeINIFields(o);
                    InitializeNativeTable(nativeFile);
                    foreach (string file in Directory.GetFiles(inputPath, "*.ysc*"))
                        CompileList.Enqueue(file);
                    foreach (string file in Directory.GetFiles(inputPath, "*.ysc.full*"))
                        CompileList.Enqueue(file);

                    // Predecode all script files.
                    ThreadPool(PredecodeScripts);

                    // In a single thread, ensure all scripts have no dirty
                    // functions, i.e., native & global parameter changes
                    // affecting the state to other functions.
                    Console.WriteLine("Cleaning up dirty predecodes");

                    int count = 0;
                    bool dirty = true;
                    while (dirty)
                    {
                        count++;
                        if (count > 10000)
                            throw new Exception("Program.BulkDecode: infinite loop");

                        dirty = false;
                        foreach (Tuple<ScriptFile, string> scriptTuple in DecodeList)
                            dirty = scriptTuple.Item1.PredecodeFunctions() || dirty;
                    }

                    // Finally, decode and save all script files.
                    ThreadPool(DecodeScripts);

                    // Finally compute aggregation functions.
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

        private static void ThreadPool(ThreadStart method)
        {
            if (Program.UseMultiThreading)
            {
                for (int i = 0; i < Environment.ProcessorCount - 1; i++)
                {
                    Program.ThreadCount++; new System.Threading.Thread(method).Start();
                }

                Program.ThreadCount++; method();
                while (Program.ThreadCount > 0)
                    System.Threading.Thread.Sleep(10);
            }
            else
            {
                Program.ThreadCount++; method();
            }
        }

        private static void PredecodeScripts()
        {
            string scriptToDecode;
            while (CompileList.Count > 0)
            {
                lock (Program.ReadLock)
                    scriptToDecode = CompileList.Dequeue();
                try
                {
                    string suffix = ".c" + (Program.CompressedOutput ? ".gz" : "");
                    string outname = Path.GetFileNameWithoutExtension(scriptToDecode);
                    if (Path.GetExtension(scriptToDecode) == ".gz") // Ensure the extension without compression is removed.
                        outname = Path.GetFileNameWithoutExtension(outname);

                    string output = Path.Combine(SaveDirectory, outname + suffix);

                    Console.WriteLine("Predecoding: " + scriptToDecode);
                    ScriptFile scriptFile = LoadScriptFile(scriptToDecode).Predecode();
                    lock (Program.WriteLock)
                        DecodeList.Enqueue(new Tuple<ScriptFile, string>(scriptFile, output));

                    if ((_gcCount.Value++) % 50 == 0)
                        GC.Collect();
                }
                catch (Exception ex)
                {
                    throw new SystemException("Error decompiling script " + Path.GetFileNameWithoutExtension(scriptToDecode) + " - " + ex.Message);
                }
            }
            Program.ThreadCount--;
        }

        private static void DecodeScripts()
        {
            Tuple<ScriptFile, string> scriptTuple;

            while (DecodeList.Count > 0)
            {
                lock (Program.ReadLock)
                    scriptTuple = DecodeList.Dequeue();

                try
                {
                    Console.WriteLine("Decoding: " + scriptTuple.Item1.Header.ScriptName + " > " + scriptTuple.Item2);
                    ScriptFile scriptFile = scriptTuple.Item1.BuildAggregation().Decode();
                    SaveScriptFile(scriptFile, scriptTuple.Item2).Close();

                    if (AggregateFunctions)
                        scriptFile.CompileAggregate();
                }
                catch (Exception ex)
                {
                    throw new SystemException("Error decoding script " + scriptTuple.Item1.Header.ScriptName + " - " + ex.Message);
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

        public static bool UseMultiThreading { get; private set; } = false;

        public static bool UppercaseNatives { get; private set; } = true;
        public static string NativeName(string s) => Program.UppercaseNatives ? s.ToUpper() : s.ToLower();
        public static bool ShowNamespace { get; private set; } = true;

        public static bool DeclareVariables { get; private set; } = true;
        public static bool ShiftVariables { get; private set; } = false;
        public static bool ReverseHashes { get; private set; } = true;
        public static IntType IntStyle { get; private set; } = IntType._int;
        public static bool ShowArraySize { get; private set; } = true;
        public static bool ShowEntryComments { get; private set; } = true;

        public static bool HexIndex { get; private set; } = false;
        public static bool ShowFuncPosition { get; private set; } = false;

        public static bool AggregateFunctions { get; private set; } = false;
        public static int AggregateMinLines { get; private set; } = 7;
        public static int AggregateMinHits { get; private set; } = 3;

        public static bool CompressedInput { get; private set; } = false;
        public static bool CompressedOutput { get; private set; } = false;
        public static bool IsBit32 { get; private set; } = false;
        public static bool SwapEndian { get; private set; } = false;
        public static bool RDROpcodes { get; private set; } = false;
        public static bool RDRNativeCipher { get; private set; } = false;

    }
}
