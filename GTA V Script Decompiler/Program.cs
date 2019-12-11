using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.CodeDom.Compiler;
using CommandLine;

namespace Decompiler
{
    static class Program
    {
        public static x64NativeFile x64nativefile;
        internal static Ini.IniFile Config;
        public static Object ThreadLock;
        public static int ThreadCount;

        class Options
        {
            [Option('x', "x64", Required = true, HelpText = "x64native file")]
            public string NativeFile { get; set; }

            [Option('y', "ysc", Default = null, Required = true, HelpText = "YSC Path")]
            public string YSCPath { get; set; }

            [Option('o', "out", Default = null, Required = false, HelpText = "Output Directory/File Path")]
            public string OutputPath { get; set; }

            [Option('t', "translation", Default = null, Required = false, HelpText = "Native translation table")]
            public string Translation { get; set; }

            [Option('f', "force", Default = false, Required = false, HelpText = "Allow output file overriding.")]
            public bool Force { get; set; }
        }

        private static void InitializeINIFields()
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
            Program.Find_Decomplied();
        }

        private static void InitializeNativeTable(string nativeFile, string translationFile)
        {
            Stream translation;
            if (translationFile != null && File.Exists(translationFile))
                translation = File.OpenRead(translationFile);
            else
                translation = new MemoryStream(Properties.Resources.native_translation);
            x64nativefile = new x64NativeFile(File.OpenRead(nativeFile), translation);
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

                    InitializeINIFields();
                    InitializeNativeTable(o.NativeFile, o.Translation);
                    using (Stream fs = File.OpenRead(o.YSCPath)) {
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

                    InitializeINIFields();
                    InitializeNativeTable(o.NativeFile, o.Translation);
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
                }
                else
                {
                    Console.WriteLine("Invalid YSC Path");
                }
            });
        }

        static string SaveDirectory = "";
        static Queue<string> CompileList = new Queue<string>();
        private static void Decompile()
        {
            ulong count = 0;
            while (CompileList.Count > 0)
            {
                string scriptToDecode;
                lock (Program.ThreadLock)
                {
                    scriptToDecode = CompileList.Dequeue();
                }
                try
                {
                    using (Stream fs = File.OpenRead(scriptToDecode)) {
                        MemoryStream buffer = new MemoryStream();
                        fs.CopyTo(buffer);

                        Console.WriteLine("Decompiling: " + scriptToDecode);
                        ScriptFile scriptFile = new ScriptFile(buffer);
                        scriptFile.Save(Path.Combine(SaveDirectory, Path.GetFileNameWithoutExtension(scriptToDecode) + ".c"));
                        scriptFile.Close();

                        count++;
                        if (count % 25 == 0)
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

        public static bool Find_Decomplied()
        {
            X64NativeTable.Translate = Program.Config.IniReadBool("Base", "Decomplied_With_Translation", false);
            return Program.Config.IniReadBool("Base", "Decomplied_With_Translation", false);
        }
    }
}
