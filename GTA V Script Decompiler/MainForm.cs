using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;
using System.Threading;

namespace Decompiler
{

    public partial class MainForm : Form
    {
        bool loadingfile = false;
        string filename = "";
        private bool scriptopen = false;
        ScriptFile fileopen;
        Style highlight;
        Queue<Tuple<string, bool>> CompileList;
		List<Tuple<uint, string>> FoundStrings;  
		uint[] HashToFind;
        string SaveDirectory;

        public bool ScriptOpen
        {
            get { return scriptopen; }
            set { extractToolStripMenuItem.Visible = extractToolStripMenuItem.Enabled = scriptopen = value; }
        }


        public MainForm()
        {
            InitializeComponent();
			ScriptFile.npi = new NativeParamInfo(); 

			//ScriptFile.hashbank = temp;
			// ScriptFile.hashbank = new Hashes();
			panel1.Size = new Size(0, panel1.Height);
           Program.Config = new Ini.IniFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));
           if (!File.Exists(Program.Config.path))
           {
               Program.Config.IniWriteValue("Base", "IntStyle", "int");
               Program.Config.IniWriteBool("Base", "Show_Array_Size", true);
               Program.Config.IniWriteBool("Base", "Reverse_Hashes", true);
               Program.Config.IniWriteBool("Base", "Declare_Variables", true);  
               Program.Config.IniWriteBool("Base", "Shift_Variables", true);
               Program.Config.IniWriteBool("Base", "Show_Func_Pointer", false);
               Program.Config.IniWriteBool("Base", "Use_MultiThreading", false);
               Program.Config.IniWriteBool("View", "Line_Numbers", true);
           }
           showArraySizeToolStripMenuItem.Checked = Program.Find_Show_Array_Size();
           reverseHashesToolStripMenuItem.Checked = Program.Find_Reverse_Hashes();
           declareVariablesToolStripMenuItem.Checked = Program.Find_Declare_Variables();
           shiftVariablesToolStripMenuItem.Checked = Program.Find_Shift_Variables();
           showFuncPointerToolStripMenuItem.Checked = Program.Find_Show_Func_Pointer();
           useMultiThreadingToolStripMenuItem.Checked = Program.Find_Use_MultiThreading();
           showLineNumbersToolStripMenuItem.Checked = fctb1.ShowLineNumbers = Program.Config.IniReadBool("View", "Line_Numbers");
           ToolStripMenuItem t = null;
           switch (Program.Find_getINTType())
           {
               case Program.IntType._int: t = intToolStripMenuItem; break;
               case Program.IntType._uint: t = uintToolStripMenuItem; break;
               case Program.IntType._hex: t = hexToolStripMenuItem; break;
           }
           t.Checked = true;
           t.Enabled = false;
           highlight = (Style)new TextStyle(Brushes.Black, Brushes.Orange, fctb1.DefaultStyle.FontStyle);
           
        }

        void updatestatus(string text)
        {
            toolStripStatusLabel1.Text = text;
            Application.DoEvents();
        }
        void ready()
        {
            updatestatus("Ready");
            loadingfile = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "GTA V Script Files|*.xsc;*.csc;*.ysc;*.ysc.full";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DateTime Start = DateTime.Now;
                filename = Path.GetFileNameWithoutExtension(ofd.FileName);
                loadingfile = true;
                fctb1.Clear();
                listView1.Items.Clear();
                updatestatus("Opening Script File...");
				string ext = Path.GetExtension(ofd.FileName);
				if (ext == ".full")						 //handle openIV exporting pc scripts as *.ysc.full
				{
					ext = Path.GetExtension(Path.GetFileNameWithoutExtension(ofd.FileName));
				}
				fileopen = new ScriptFile(ofd.OpenFile(), ext != ".ysc");
                updatestatus("Decompiled Script File, Time taken: " + (DateTime.Now - Start).ToString());
                MemoryStream ms = new MemoryStream();
                fileopen.Save(ms, false);
                
                foreach (KeyValuePair<string, int> locations in fileopen.Function_loc)
                {
                    listView1.Items.Add(new ListViewItem(new string[] { locations.Key, locations.Value.ToString() }));
                }
                fileopen.Close();
                StreamReader sr = new StreamReader(ms);
                ms.Position = 0;
                updatestatus("Loading Text in Viewer...");
                fctb1.Text = sr.ReadToEnd();
                SetFileName(filename);
                ScriptOpen = true;
                updatestatus("Ready, Time taken: " + (DateTime.Now - Start).ToString());
                if (ext != ".ysc")
                    ScriptFile.npi.savefile();
                else
                    ScriptFile.X64npi.savefile();

            }
        }

        private void directoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompileList = new Queue<Tuple<string, bool>>();
            Program.ThreadCount = 0;
            FolderSelectDialog fsd = new FolderSelectDialog();
            if (fsd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DateTime Start = DateTime.Now;
                SaveDirectory = Path.Combine(fsd.SelectedPath, "exported");
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);
                this.Hide();
                bool console = false, pc = false;
                
                    foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.xsc"))
                    {
                        console = true;
                        CompileList.Enqueue(new Tuple<string, bool>(file, true));
                    }
                    foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.csc"))
                    {
                        console = true;
                        CompileList.Enqueue(new Tuple<string, bool>(file, true));
                    }
                    foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.ysc"))
                    {
                        pc = true;
                        CompileList.Enqueue(new Tuple<string, bool>(file, false));
                    }
					foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.ysc.full"))
					{
						pc = true;
						CompileList.Enqueue(new Tuple<string, bool>(file, false));
					}
				if (Program.Use_MultiThreading)
				{
					for (int i = 0; i < Environment.ProcessorCount - 1; i++)
					{
						Program.ThreadCount++;
						new System.Threading.Thread(Decompile).Start();
						//System.Threading.Thread.Sleep(0);
					}
					Program.ThreadCount++;
					Decompile();
					while (Program.ThreadCount > 0)
					{
						System.Threading.Thread.Sleep(10);
					}
				}
				else
				{
					Program.ThreadCount++;
					Decompile();
				}

                updatestatus("Directory Extracted, Time taken: " + (DateTime.Now - Start).ToString());
                if (console)
                    ScriptFile.npi.savefile();
                if (pc)
                    ScriptFile.X64npi.savefile();
            }
            this.Show();
        }

        private void Decompile()
        {
            while( CompileList.Count > 0)
            {
                Tuple<string, bool> scriptToDecode;
                lock (Program.ThreadLock)
                {
                   scriptToDecode = CompileList.Dequeue();
                }
                ScriptFile scriptFile = new ScriptFile((Stream)File.OpenRead(scriptToDecode.Item1), scriptToDecode.Item2);
                scriptFile.Save(Path.Combine(SaveDirectory, Path.GetFileNameWithoutExtension(scriptToDecode.Item1) + ".c"));
                scriptFile.Close();
            }
            Program.ThreadCount--;
        }

        private void fileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "GTA V Script Files|*.xsc;*.csc;*.ysc";
            
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DateTime Start = DateTime.Now;
                ScriptFile file = new ScriptFile(ofd.OpenFile(), (Path.GetExtension(ofd.FileName) != ".ysc"));
                file.Save(Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName) + ".c"));
                file.Close();
                if ((Path.GetExtension(ofd.FileName) != ".ysc"))
                    ScriptFile.npi.savefile();
                else
                    ScriptFile.X64npi.savefile();
                updatestatus("File Saved, Time taken: " + (DateTime.Now - Start).ToString());
            }
        }

        #region Config Options

        private void intstylechanged(object sender, EventArgs e)
        {
            ToolStripMenuItem clicked = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem t in intStyleToolStripMenuItem.DropDownItems)
            {
                t.Enabled = true;
                t.Checked = false;
            }
            clicked.Checked = true;
            clicked.Enabled = false;
            Program.Config.IniWriteValue("Base", "IntStyle", clicked.Text.ToLower());
            Program.Find_getINTType();
        }

        private void showArraySizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showArraySizeToolStripMenuItem.Checked = !showArraySizeToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Show_Array_Size", showArraySizeToolStripMenuItem.Checked);
            Program.Find_Show_Array_Size();
        }

        private void reverseHashesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reverseHashesToolStripMenuItem.Checked = !reverseHashesToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Reverse_Hashes", reverseHashesToolStripMenuItem.Checked);
            Program.Find_Reverse_Hashes();
        }

        private void declareVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            declareVariablesToolStripMenuItem.Checked = !declareVariablesToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Declare_Variables", declareVariablesToolStripMenuItem.Checked);
            Program.Find_Declare_Variables();
        }

        private void shiftVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shiftVariablesToolStripMenuItem.Checked = !shiftVariablesToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Shift_Variables", shiftVariablesToolStripMenuItem.Checked);
            Program.Find_Shift_Variables();
        }

        private void showLineNumbersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showLineNumbersToolStripMenuItem.Checked = !showLineNumbersToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("View", "Line_Numbers", showLineNumbersToolStripMenuItem.Checked);
            fctb1.ShowLineNumbers = showLineNumbersToolStripMenuItem.Checked;
        }

        private void showFuncPointerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showFuncPointerToolStripMenuItem.Checked = !showFuncPointerToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Show_Func_Pointer", showFuncPointerToolStripMenuItem.Checked);
            Program.Find_Show_Func_Pointer();
        }

        private void useMultiThreadingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*if (!useMultiThreadingToolStripMenuItem.Checked)
            {
                if (MessageBox.Show(this, "Using multithreading can cause stability issues and program crashes while decompiling single scripts\nAre you sure you want to carry on", "Use Multithreading", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                    return;
            }*/
            useMultiThreadingToolStripMenuItem.Checked = !useMultiThreadingToolStripMenuItem.Checked;
            Program.Config.IniWriteBool("Base", "Use_MultiThreading", useMultiThreadingToolStripMenuItem.Checked);
            Program.Find_Use_MultiThreading();
        }

        #endregion

        #region Function Location
        bool opening = false;
        bool forceclose = false;
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                int num = Convert.ToInt32(listView1.SelectedItems[0].SubItems[1].Text);
                fctb1.Selection = new FastColoredTextBoxNS.Range(fctb1, 0, num, 0, num);
                fctb1.DoSelectionVisible();
            }
        }

        private void listView1_MouseLeave(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (panel1.ClientRectangle.Contains(panel1.PointToClient(Control.MousePosition)))
            {
                return;
            }
            opening = false;
            timer2.Start();
            timer1.Stop();
        }


        private void listView1_MouseEnter(object sender, EventArgs e)
        {
            if (forceclose)
                return;
            timer1.Stop();
            opening = true;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (opening)
            {
                if (panel1.Size.Width < 165) panel1.Size = new Size(panel1.Size.Width + 6, panel1.Size.Height);
                else
                {
                    panel1.Size = new Size(170, panel1.Size.Height);
                    timer2.Stop();
                    forceclose = false;
                }
                    
            }
            if (!opening)
            {
                if (panel1.Size.Width > 2) panel1.Size = new Size(panel1.Size.Width - 2, panel1.Size.Height);
                else
                {
                    panel1.Size = new Size(0, panel1.Size.Height);
                    timer2.Stop();
                    forceclose = false;
                }
                
            }

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            opening = !opening;
            if (!opening)
                forceclose = true;
            timer2.Start();
            columnHeader1.Width = 80;
            columnHeader2.Width = 76;
        }
        #endregion

        private void entitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptFile.hashbank.Export_Entities();
        }

        private void nativesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "natives_exp.dat");
            FileStream fs = File.Create(path);
            new MemoryStream(Properties.Resources.natives).CopyTo(fs);
            fs.Close();
            System.Diagnostics.Process.Start(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)),"natives_exp.dat");
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void expandAllBlocksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updatestatus("Expanding all blocks...");
            fctb1.ExpandAllFoldingBlocks();
            ready();
        }

        private void collaspeAllBlocksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updatestatus("Collasping all blocks...");
            fctb1.CollapseAllFoldingBlocks();
            ready();
        }

        private void fctb1_MouseClick(object sender, MouseEventArgs e)
        {
            opening = false;
            forceclose = true;
            timer2.Start();
            timer1.Stop();
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (fctb1.SelectionLength == 0)
                {
                    fctb1.SelectionStart = fctb1.PointToPosition(fctb1.PointToClient(Cursor.Position));
                }
                cmsText.Show();
            }
        }

        public string getfunctionfromline(int line)
        {
            if (listView1.Items.Count == 0)
                return "";
            
            int temp;
            if (int.TryParse(listView1.Items[0].SubItems[1].Text, out temp))
            {
                if (line < temp - 1)
                    return "Local Vars";
            }
            else return "";
            int max = listView1.Items.Count - 1;
            for (int i = 0; i < max; i++)
            {
                if (!int.TryParse(listView1.Items[i].SubItems[1].Text, out temp))
                    continue;
                if (line >= temp)
                {
                    if (!int.TryParse(listView1.Items[i+1].SubItems[1].Text, out temp))
                    continue;
                    if (line < temp-1)
                    {
                        return listView1.Items[i].SubItems[0].Text;
                    }
                }
            }
            if (int.TryParse(listView1.Items[max].SubItems[1].Text, out temp))
            {
                if (line >= temp)
                    return listView1.Items[max].SubItems[0].Text;
            }
            return "";
        }

        private void fctb1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel3.Text = getfunctionfromline(fctb1.Selection.Start.iLine + 1);
                fctb1.Range.ClearStyle(highlight);
                if (fctb1.SelectionLength > 0)
                {
                    if (!fctb1.SelectedText.Contains('\n') && !fctb1.SelectedText.Contains('\n'))
                        fctb1.Range.SetStyle(highlight, "\\b" + fctb1.Selection.Text + "\\b", RegexOptions.IgnoreCase);
                }
                getcontextitems();
            }
            catch { }
        }

        public void fill_function_table()
        {
            try
            {
                loadingfile = true;
                Dictionary<string, int> functionloc = new Dictionary<string, int>();
                for (int i = 0; i < fctb1.LinesCount; i++)
                {
                    if (fctb1.Lines[i].Length == 0)
                        continue;
                    if (fctb1.Lines[i].Contains(' '))
                    {
                        if (!fctb1.Lines[i].Contains('('))
                            continue;
                        string type = fctb1.Lines[i].Remove(fctb1.Lines[i].IndexOf(' '));
                        switch (type.ToLower())
                        {
                            case "void":
                            case "var":
                            case "float":
                            case "bool":
                            case "int":
                            case "vector3":
                            case "*string": string name = fctb1.Lines[i].Remove(fctb1.Lines[i].IndexOf('(')).Substring(fctb1.Lines[i].IndexOf(' ') + 1);
                                functionloc.Add(name, i + 1);
                                continue;
                            default: if (type.ToLower().StartsWith("struct<"))
                                    goto case "var";
                                break;

                        }
                    }
                }
                listView1.Items.Clear();
                foreach (KeyValuePair<string, int> locations in functionloc)
                {
                    listView1.Items.Add(new ListViewItem(new string[] { locations.Key, locations.Value.ToString() }));
                }
                loadingfile = false;
            }
            catch
            {
                loadingfile = false;
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Stop();
            fill_function_table();
        }

        private void fctb1_LineInserted(object sender, FastColoredTextBoxNS.LineInsertedEventArgs e)
        {
            if (!loadingfile)
            timer3.Start();
        }

        private void fctb1_LineRemoved(object sender, FastColoredTextBoxNS.LineRemovedEventArgs e)
        {
            if (!loadingfile)
            timer3.Start();
        }

        private void openCFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "C Source files *.c|*.c";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                loadingfile = true;
                filename = Path.GetFileNameWithoutExtension(ofd.FileName);
                fctb1.Clear();
                listView1.Items.Clear();
                updatestatus("Loading Text in Viewer...");
                fctb1.OpenFile(ofd.FileName);
                updatestatus("Loading Functions...");
                fill_function_table();
                SetFileName(filename);
                ready();
                ScriptOpen = false;
            }
        }
        private void SetFileName(string name)
        {
            if (name == null)
                this.Text = "GTA V High Level Decompiler";
            if (name.Length == 0)
                this.Text = "GTA V High Level Decompiler";
            else 
            { if (name.Contains('.'))
                name = name.Remove(name.IndexOf('.'));
            this.Text = "GTA V High Level Decompiler - " + name;
            }
        }

        private void saveCFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "C Source files *.c|*.c";
            sfd.FileName = filename + ".c";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fctb1.SaveToFile(sfd.FileName, System.Text.Encoding.Default);
                MessageBox.Show("File Saved");
            }
        }

        private void cmsText_Opening(object sender, CancelEventArgs e)
        {
            getcontextitems();
            if (cmsText.Items.Count == 0) e.Cancel = true;
        }

        bool islegalchar(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            return c == '_';

        }
        string getwordatcursor()
        {
            string line = fctb1.Lines[fctb1.Selection.Start.iLine];
            if (line.Length == 0)
                return "";
            int pos = fctb1.Selection.Start.iChar;
            if (pos == line.Length)
                return "";
            int min = pos, max = pos;
            while (min > 0)
            {
                if (islegalchar(line[min-1]))
                    min--;
                else
                    break;
            }
            int len = line.Length;
            while (max < len)
            {
                if (islegalchar(line[max]))
                    max++;
                else
                    break;
            }
            return line.Substring(min, max - min);
        }
        private void getcontextitems()
        {
            string word = getwordatcursor();
            cmsText.Items.Clear();
            foreach (ListViewItem lvi in listView1.Items)
            {
                if (lvi.Text == word)
                {
                    cmsText.Items.Add(new ToolStripMenuItem("Goto Declaration(" + lvi.Text + ")", null, new EventHandler(delegate(object o, EventArgs a)
                    {
                        int num = Convert.ToInt32(lvi.SubItems[1].Text);
                        fctb1.Selection = new FastColoredTextBoxNS.Range(fctb1, 0, num, 0, num);
                        fctb1.DoSelectionVisible();
                    }), Keys.F12));
                }
            }

        }

        private void fullNativeInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptFile.npi.exportnativeinfo();
        }
        private void fullPCNativeInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScriptFile.X64npi.exportnativeinfo();
        }


        private void stringsTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ScriptOpen)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Select location to save string table";
                sfd.Filter = "Text files|*.txt|All Files|*.*";
                sfd.FileName = ((filename.Contains('.')) ? filename.Remove(filename.IndexOf('.')) : filename) + "(Strings).txt";
                if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                StreamWriter sw = File.CreateText(sfd.FileName);
                foreach (string line in fileopen.GetStringTable())
                {
                    sw.WriteLine(line);
                }
                sw.Close();
                MessageBox.Show("File Saved");
            }
            else
            {
                MessageBox.Show("No script file is open");
            }
                
        }

        private void nativeTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ScriptOpen)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Select location to save native table";
                sfd.Filter = "Text files|*.txt|All Files|*.*";
                sfd.FileName = ((filename.Contains('.')) ? filename.Remove(filename.IndexOf('.')) : filename) + "(natives).txt";
                if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                StreamWriter sw = File.CreateText(sfd.FileName);
                foreach (string line in fileopen.GetNativeTable())
                {
                    sw.WriteLine(line);
                }
                sw.Close();
                MessageBox.Show("File Saved");
            }
            else
            {
                MessageBox.Show("No script file is open");
            }
        }

		/// <summary>
		/// Generates a c like header for all the natives. made it back when i was first trying to recompile the files back to *.*sc
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void nativehFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ScriptOpen)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Select location to save native table";
                sfd.Filter = "C header files|*.h|All Files|*.*";
                sfd.FileName = ((filename.Contains('.')) ? filename.Remove(filename.IndexOf('.')) : filename) + ".h";
                if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                StreamWriter sw = File.CreateText(sfd.FileName);
                sw.WriteLine("/*************************************************************");
                sw.WriteLine("******* Header file generated for " + filename + " *******");
                sw.WriteLine("*************************************************************/\n");
                sw.WriteLine("#region Vectors\ntypedef struct Vector3{\n\tfloat x;\n\tfloat y;\n\tfloat z;\n} Vector3, *PVector3;\n\n");
                sw.WriteLine("extern Vector3 VectorAdd(Vector3 v0, Vector3 v1);");
                sw.WriteLine("extern Vector3 VectorSub(Vector3 v0, Vector3 v1);");
                sw.WriteLine("extern Vector3 VectorMult(Vector3 v0, Vector3 v1);");
                sw.WriteLine("extern Vector3 VectorDiv(Vector3 v0, Vector3 v1);");
                sw.WriteLine("extern Vector3 VectorNeg(Vector3 v0);\n#endregion\n\n");
                sw.WriteLine("#define TRUE 1\n#define FALSE 0\n#define true 1\n#define false 0\n");
                sw.WriteLine("typedef unsigned int uint;");
                sw.WriteLine("typedef uint bool;");
                sw.WriteLine("typedef uint var;");
                sw.WriteLine("");
                foreach (string line in fileopen.GetNativeHeader())
                {
                    sw.WriteLine("extern " + line);
                }
                sw.Close();
                MessageBox.Show("File Saved");
            }
            else
            {
                MessageBox.Show("No script file is open");
            }
        }

        private void navigateForwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!fctb1.NavigateForward())
            {
                MessageBox.Show("Error, cannont navigate forwards anymore");
            }
        }

        private void navigateBackwardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!fctb1.NavigateBackward())
            {
                MessageBox.Show("Error, cannont navigate backwards anymore");
            }
        }


		/// <summary>
		/// The games language files store items as hashes. This function will grab all strings in a all scripts in a directory
		/// and hash each string and them compare with a list of hashes supplied in the input box. Any matches get saved to a file STRINGS.txt in the directory
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void findHashFromStringsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InputBox IB = new InputBox();
			if (!IB.ShowList("Input Hash", "Input hash to find", this))
				return;
			uint hash;
			List<uint> Hashes = new List<uint>();
			foreach (string result in IB.ListValue)
			{

				if (result.StartsWith("0x"))
				{
					if (uint.TryParse(result.Substring(2), System.Globalization.NumberStyles.HexNumber, new System.Globalization.CultureInfo("en-gb"), out hash))
					{
						Hashes.Add(hash);
					}
					else
					{
						MessageBox.Show($"Error converting {result} to hash value");
					}
				}
				else
				{
					if (uint.TryParse(result, out hash))
					{
						Hashes.Add(hash);
					}
					else
					{
						MessageBox.Show($"Error converting {result} to hash value");
					}
				}
			}
			if (Hashes.Count == 0)
			{
				MessageBox.Show($"Error, no hashes inputted, please try again");
				return;
			}
			HashToFind = Hashes.ToArray();
			CompileList = new Queue<Tuple<string, bool>>();
			FoundStrings = new List<Tuple<uint, string>>();
			Program.ThreadCount = 0;
			FolderSelectDialog fsd = new FolderSelectDialog();
			if (fsd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				DateTime Start = DateTime.Now;
				this.Hide();

				foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.xsc"))
				{
					CompileList.Enqueue(new Tuple<string, bool>(file, true));
				}
				foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.csc"))
				{
					CompileList.Enqueue(new Tuple<string, bool>(file, true));
				}
				foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.ysc"))
				{
					CompileList.Enqueue(new Tuple<string, bool>(file, false));
				}
				foreach (string file in Directory.GetFiles(fsd.SelectedPath, "*.ysc.full"))
				{
					CompileList.Enqueue(new Tuple<string, bool>(file, false));
				}
				if (Program.Use_MultiThreading)
				{
					for (int i = 0; i < Environment.ProcessorCount - 1; i++)
					{
						Program.ThreadCount++;
						new System.Threading.Thread(FindString).Start();
						System.Threading.Thread.Sleep(0);
					}
					Program.ThreadCount++;
					FindString();
					while (Program.ThreadCount > 0)
					{
						System.Threading.Thread.Sleep(10);
					}
				}
				else
				{
					Program.ThreadCount++;
					FindString();
				}

				if (FoundStrings.Count == 0)
					updatestatus($"No Strings Found, Time taken: {DateTime.Now - Start}");
				else
				{
					updatestatus($"Founs {FoundStrings.Count} strings, Time taken: {DateTime.Now - Start}");
					FoundStrings.Sort((x, y) => x.Item1.CompareTo(y.Item1));
					using (StreamWriter oFile = File.CreateText(Path.Combine(fsd.SelectedPath, "STRINGS.txt")))
					{
						foreach (Tuple<uint, string> Item in FoundStrings)
						{
							oFile.WriteLine($"0x{Utils.formathexhash(Item.Item1)} : \"{Item.Item2}\"");
						}
					}
				}
			}
			this.Show();
		}
		/// <summary>
		/// This does the actual searching of the hashes from the above function. Designed to run on multiple threads
		/// </summary>
		private void FindString()
		{
			while (CompileList.Count > 0)
			{
				Tuple<string, bool> scriptToSearch;
				lock (Program.ThreadLock)
				{
					scriptToSearch = CompileList.Dequeue();
				}
				using (Stream ScriptFile = File.OpenRead(scriptToSearch.Item1))
				{
					ScriptHeader header = ScriptHeader.Generate(ScriptFile, scriptToSearch.Item2);
					StringTable table = new StringTable(ScriptFile, header.StringTableOffsets, header.StringBlocks, header.StringsSize);
					foreach (string str in table.Values)
					{
						if (HashToFind.Contains(Utils.jenkins_one_at_a_time_hash(str)))
						{
							if (islower(str))
								continue;
							lock (Program.ThreadLock)
							{
								if (!FoundStrings.Any(item => item.Item2 == str))
								{
									FoundStrings.Add(new Tuple<uint, string>(Utils.jenkins_one_at_a_time_hash(str), str));
								}
							}
						}
					}
				}
			}
			Program.ThreadCount--;
		}
		private static bool islower(string s)
		{
			foreach (char c in s)
				if (char.IsLower(c))
				{
					return true;
				}
			return false;
		}
	}
}
