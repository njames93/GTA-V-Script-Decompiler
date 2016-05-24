namespace Decompiler
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			if (disposing && (highlight != null))
			{
				highlight.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.fctb1 = new FastColoredTextBoxNS.FastColoredTextBox();
			this.cmsText = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openCFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.saveCFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.directoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fileToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.intStyleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.intToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.uintToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.hexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showArraySizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reverseHashesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.declareVariablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.shiftVariablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showFuncPointerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.useMultiThreadingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportTablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.entitiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nativesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fullNativeInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.consoleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.findHashFromStringsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.expandAllBlocksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collaspeAllBlocksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.showLineNumbersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.navigateForwardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.navigateBackwardsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stringsTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nativeTableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.nativehFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panel1 = new System.Windows.Forms.Panel();
			this.listView1 = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.timer2 = new System.Windows.Forms.Timer(this.components);
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.timer3 = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.fctb1)).BeginInit();
			this.menuStrip1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// fctb1
			// 
			this.fctb1.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
			this.fctb1.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n^\\s*(case|default)\\s*[^:]" +
    "*(?<range>:)\\s*(?<range>[^;]+);\r\n";
			this.fctb1.AutoScrollMinSize = new System.Drawing.Size(27, 14);
			this.fctb1.BackBrush = null;
			this.fctb1.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
			this.fctb1.CharHeight = 14;
			this.fctb1.CharWidth = 8;
			this.fctb1.ContextMenuStrip = this.cmsText;
			this.fctb1.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.fctb1.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
			this.fctb1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.fctb1.Font = new System.Drawing.Font("Courier New", 9.75F);
			this.fctb1.IsReplaceMode = false;
			this.fctb1.Language = FastColoredTextBoxNS.Language.CSharp;
			this.fctb1.LeftBracket = '(';
			this.fctb1.LeftBracket2 = '{';
			this.fctb1.Location = new System.Drawing.Point(0, 24);
			this.fctb1.Name = "fctb1";
			this.fctb1.Paddings = new System.Windows.Forms.Padding(0);
			this.fctb1.RightBracket = ')';
			this.fctb1.RightBracket2 = '}';
			this.fctb1.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
			this.fctb1.Size = new System.Drawing.Size(622, 582);
			this.fctb1.TabIndex = 1;
			this.fctb1.Zoom = 100;
			this.fctb1.SelectionChanged += new System.EventHandler(this.fctb1_SelectionChanged);
			this.fctb1.LineInserted += new System.EventHandler<FastColoredTextBoxNS.LineInsertedEventArgs>(this.fctb1_LineInserted);
			this.fctb1.LineRemoved += new System.EventHandler<FastColoredTextBoxNS.LineRemovedEventArgs>(this.fctb1_LineRemoved);
			this.fctb1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.fctb1_MouseClick);
			// 
			// cmsText
			// 
			this.cmsText.Name = "cmsText";
			this.cmsText.Size = new System.Drawing.Size(61, 4);
			this.cmsText.Opening += new System.ComponentModel.CancelEventHandler(this.cmsText_Opening);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.extractToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(810, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.openCFileToolStripMenuItem,
            this.toolStripSeparator3,
            this.saveCFileToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.toolStripSeparator2,
            this.closeToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.openToolStripMenuItem.Text = "Open";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// openCFileToolStripMenuItem
			// 
			this.openCFileToolStripMenuItem.Name = "openCFileToolStripMenuItem";
			this.openCFileToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.openCFileToolStripMenuItem.Text = "Open C File";
			this.openCFileToolStripMenuItem.Click += new System.EventHandler(this.openCFileToolStripMenuItem_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(132, 6);
			// 
			// saveCFileToolStripMenuItem
			// 
			this.saveCFileToolStripMenuItem.Name = "saveCFileToolStripMenuItem";
			this.saveCFileToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.saveCFileToolStripMenuItem.Text = "Save C File";
			this.saveCFileToolStripMenuItem.Click += new System.EventHandler(this.saveCFileToolStripMenuItem_Click);
			// 
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.directoryToolStripMenuItem,
            this.fileToolStripMenuItem1});
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			this.exportToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.exportToolStripMenuItem.Text = "Export";
			// 
			// directoryToolStripMenuItem
			// 
			this.directoryToolStripMenuItem.Name = "directoryToolStripMenuItem";
			this.directoryToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
			this.directoryToolStripMenuItem.Text = "Directory";
			this.directoryToolStripMenuItem.Click += new System.EventHandler(this.directoryToolStripMenuItem_Click);
			// 
			// fileToolStripMenuItem1
			// 
			this.fileToolStripMenuItem1.Name = "fileToolStripMenuItem1";
			this.fileToolStripMenuItem1.Size = new System.Drawing.Size(122, 22);
			this.fileToolStripMenuItem1.Text = "File";
			this.fileToolStripMenuItem1.Click += new System.EventHandler(this.fileToolStripMenuItem1_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(132, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.closeToolStripMenuItem.Text = "Close";
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.intStyleToolStripMenuItem,
            this.showArraySizeToolStripMenuItem,
            this.reverseHashesToolStripMenuItem,
            this.declareVariablesToolStripMenuItem,
            this.shiftVariablesToolStripMenuItem,
            this.showFuncPointerToolStripMenuItem,
            this.useMultiThreadingToolStripMenuItem,
            this.exportTablesToolStripMenuItem,
            this.findHashFromStringsToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "Options";
			// 
			// intStyleToolStripMenuItem
			// 
			this.intStyleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.intToolStripMenuItem,
            this.uintToolStripMenuItem,
            this.hexToolStripMenuItem});
			this.intStyleToolStripMenuItem.Name = "intStyleToolStripMenuItem";
			this.intStyleToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.intStyleToolStripMenuItem.Text = "IntStyle";
			this.intStyleToolStripMenuItem.ToolTipText = "Choose how to display int32 data types";
			// 
			// intToolStripMenuItem
			// 
			this.intToolStripMenuItem.Name = "intToolStripMenuItem";
			this.intToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
			this.intToolStripMenuItem.Text = "Int";
			this.intToolStripMenuItem.Click += new System.EventHandler(this.intstylechanged);
			// 
			// uintToolStripMenuItem
			// 
			this.uintToolStripMenuItem.Name = "uintToolStripMenuItem";
			this.uintToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
			this.uintToolStripMenuItem.Text = "Uint";
			this.uintToolStripMenuItem.Click += new System.EventHandler(this.intstylechanged);
			// 
			// hexToolStripMenuItem
			// 
			this.hexToolStripMenuItem.Name = "hexToolStripMenuItem";
			this.hexToolStripMenuItem.Size = new System.Drawing.Size(96, 22);
			this.hexToolStripMenuItem.Text = "Hex";
			this.hexToolStripMenuItem.Click += new System.EventHandler(this.intstylechanged);
			// 
			// showArraySizeToolStripMenuItem
			// 
			this.showArraySizeToolStripMenuItem.Name = "showArraySizeToolStripMenuItem";
			this.showArraySizeToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.showArraySizeToolStripMenuItem.Text = "Show Array Size";
			this.showArraySizeToolStripMenuItem.ToolTipText = "Shows the size of the items in an array \r\nuLocal_5[index <item_size>]\r\nan array o" +
    "f vector3s would look like this\r\nvStatic_1[0 <3>];";
			this.showArraySizeToolStripMenuItem.Click += new System.EventHandler(this.showArraySizeToolStripMenuItem_Click);
			// 
			// reverseHashesToolStripMenuItem
			// 
			this.reverseHashesToolStripMenuItem.Name = "reverseHashesToolStripMenuItem";
			this.reverseHashesToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.reverseHashesToolStripMenuItem.Text = "Reverse Hashes";
			this.reverseHashesToolStripMenuItem.ToolTipText = "Reverse known hashes into their text equivalent\r\ne.g 0xB779A091 -> joaat(\"adder\")" +
    "";
			this.reverseHashesToolStripMenuItem.Click += new System.EventHandler(this.reverseHashesToolStripMenuItem_Click);
			// 
			// declareVariablesToolStripMenuItem
			// 
			this.declareVariablesToolStripMenuItem.Name = "declareVariablesToolStripMenuItem";
			this.declareVariablesToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.declareVariablesToolStripMenuItem.Text = "Declare Variables";
			this.declareVariablesToolStripMenuItem.ToolTipText = "Include Variable declarations at the start of file and functions";
			this.declareVariablesToolStripMenuItem.Click += new System.EventHandler(this.declareVariablesToolStripMenuItem_Click);
			// 
			// shiftVariablesToolStripMenuItem
			// 
			this.shiftVariablesToolStripMenuItem.Name = "shiftVariablesToolStripMenuItem";
			this.shiftVariablesToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.shiftVariablesToolStripMenuItem.Text = "Shift Variables";
			this.shiftVariablesToolStripMenuItem.ToolTipText = resources.GetString("shiftVariablesToolStripMenuItem.ToolTipText");
			this.shiftVariablesToolStripMenuItem.Click += new System.EventHandler(this.shiftVariablesToolStripMenuItem_Click);
			// 
			// showFuncPointerToolStripMenuItem
			// 
			this.showFuncPointerToolStripMenuItem.Name = "showFuncPointerToolStripMenuItem";
			this.showFuncPointerToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.showFuncPointerToolStripMenuItem.Text = "Show Func Pointer";
			this.showFuncPointerToolStripMenuItem.Click += new System.EventHandler(this.showFuncPointerToolStripMenuItem_Click);
			// 
			// useMultiThreadingToolStripMenuItem
			// 
			this.useMultiThreadingToolStripMenuItem.Name = "useMultiThreadingToolStripMenuItem";
			this.useMultiThreadingToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.useMultiThreadingToolStripMenuItem.Text = "Use MultiThreading";
			this.useMultiThreadingToolStripMenuItem.Click += new System.EventHandler(this.useMultiThreadingToolStripMenuItem_Click);
			// 
			// exportTablesToolStripMenuItem
			// 
			this.exportTablesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.entitiesToolStripMenuItem,
            this.nativesToolStripMenuItem,
            this.fullNativeInfoToolStripMenuItem});
			this.exportTablesToolStripMenuItem.Name = "exportTablesToolStripMenuItem";
			this.exportTablesToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.exportTablesToolStripMenuItem.Text = "Export Tables";
			// 
			// entitiesToolStripMenuItem
			// 
			this.entitiesToolStripMenuItem.Name = "entitiesToolStripMenuItem";
			this.entitiesToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.entitiesToolStripMenuItem.Text = "Entities";
			this.entitiesToolStripMenuItem.ToolTipText = "Export The entites file (entities_exp.dat) built into the program so you can edit" +
    " it.\r\nThe program will search for entities.dat in its directory and use that for" +
    " reversing hashes";
			this.entitiesToolStripMenuItem.Click += new System.EventHandler(this.entitiesToolStripMenuItem_Click);
			// 
			// nativesToolStripMenuItem
			// 
			this.nativesToolStripMenuItem.Name = "nativesToolStripMenuItem";
			this.nativesToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.nativesToolStripMenuItem.Text = "Natives";
			this.nativesToolStripMenuItem.ToolTipText = "Export The natives file (natives_exp.dat) built into the program so you can edit " +
    "it.\r\nThe program will search for natives.dat in its directory and use that for r" +
    "eversing natives";
			this.nativesToolStripMenuItem.Click += new System.EventHandler(this.nativesToolStripMenuItem_Click);
			// 
			// fullNativeInfoToolStripMenuItem
			// 
			this.fullNativeInfoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.consoleToolStripMenuItem,
            this.pCToolStripMenuItem});
			this.fullNativeInfoToolStripMenuItem.Name = "fullNativeInfoToolStripMenuItem";
			this.fullNativeInfoToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
			this.fullNativeInfoToolStripMenuItem.Text = "Full Native info";
			this.fullNativeInfoToolStripMenuItem.ToolTipText = "Export a file containing definitions of natives to a h file in programs directory" +
    "\r\n";
			// 
			// consoleToolStripMenuItem
			// 
			this.consoleToolStripMenuItem.Name = "consoleToolStripMenuItem";
			this.consoleToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.consoleToolStripMenuItem.Text = "Console";
			this.consoleToolStripMenuItem.Click += new System.EventHandler(this.fullNativeInfoToolStripMenuItem_Click);
			// 
			// pCToolStripMenuItem
			// 
			this.pCToolStripMenuItem.Name = "pCToolStripMenuItem";
			this.pCToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
			this.pCToolStripMenuItem.Text = "PC";
			this.pCToolStripMenuItem.Click += new System.EventHandler(this.fullPCNativeInfoToolStripMenuItem_Click);
			// 
			// findHashFromStringsToolStripMenuItem
			// 
			this.findHashFromStringsToolStripMenuItem.Name = "findHashFromStringsToolStripMenuItem";
			this.findHashFromStringsToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
			this.findHashFromStringsToolStripMenuItem.Text = "Reverse Hashes From Strings";
			this.findHashFromStringsToolStripMenuItem.Click += new System.EventHandler(this.findHashFromStringsToolStripMenuItem_Click);
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllBlocksToolStripMenuItem,
            this.collaspeAllBlocksToolStripMenuItem,
            this.toolStripSeparator1,
            this.showLineNumbersToolStripMenuItem,
            this.navigateForwardToolStripMenuItem,
            this.navigateBackwardsToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.viewToolStripMenuItem.Text = "View";
			// 
			// expandAllBlocksToolStripMenuItem
			// 
			this.expandAllBlocksToolStripMenuItem.Name = "expandAllBlocksToolStripMenuItem";
			this.expandAllBlocksToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
			this.expandAllBlocksToolStripMenuItem.Text = "Expand All Blocks";
			this.expandAllBlocksToolStripMenuItem.Click += new System.EventHandler(this.expandAllBlocksToolStripMenuItem_Click);
			// 
			// collaspeAllBlocksToolStripMenuItem
			// 
			this.collaspeAllBlocksToolStripMenuItem.Name = "collaspeAllBlocksToolStripMenuItem";
			this.collaspeAllBlocksToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
			this.collaspeAllBlocksToolStripMenuItem.Text = "Collaspe All Blocks";
			this.collaspeAllBlocksToolStripMenuItem.Click += new System.EventHandler(this.collaspeAllBlocksToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(270, 6);
			// 
			// showLineNumbersToolStripMenuItem
			// 
			this.showLineNumbersToolStripMenuItem.Name = "showLineNumbersToolStripMenuItem";
			this.showLineNumbersToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
			this.showLineNumbersToolStripMenuItem.Text = "Show Line Numbers";
			this.showLineNumbersToolStripMenuItem.Click += new System.EventHandler(this.showLineNumbersToolStripMenuItem_Click);
			// 
			// navigateForwardToolStripMenuItem
			// 
			this.navigateForwardToolStripMenuItem.Name = "navigateForwardToolStripMenuItem";
			this.navigateForwardToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemplus)));
			this.navigateForwardToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
			this.navigateForwardToolStripMenuItem.Text = "Navigate Forward";
			this.navigateForwardToolStripMenuItem.Click += new System.EventHandler(this.navigateForwardToolStripMenuItem_Click);
			// 
			// navigateBackwardsToolStripMenuItem
			// 
			this.navigateBackwardsToolStripMenuItem.Name = "navigateBackwardsToolStripMenuItem";
			this.navigateBackwardsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.OemMinus)));
			this.navigateBackwardsToolStripMenuItem.Size = new System.Drawing.Size(273, 22);
			this.navigateBackwardsToolStripMenuItem.Text = "Navigate Backwards";
			this.navigateBackwardsToolStripMenuItem.Click += new System.EventHandler(this.navigateBackwardsToolStripMenuItem_Click);
			// 
			// extractToolStripMenuItem
			// 
			this.extractToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stringsTableToolStripMenuItem,
            this.nativeTableToolStripMenuItem,
            this.nativehFileToolStripMenuItem});
			this.extractToolStripMenuItem.Enabled = false;
			this.extractToolStripMenuItem.Name = "extractToolStripMenuItem";
			this.extractToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.extractToolStripMenuItem.Text = "Extract";
			this.extractToolStripMenuItem.Visible = false;
			// 
			// stringsTableToolStripMenuItem
			// 
			this.stringsTableToolStripMenuItem.Name = "stringsTableToolStripMenuItem";
			this.stringsTableToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.stringsTableToolStripMenuItem.Text = "Strings table";
			this.stringsTableToolStripMenuItem.Click += new System.EventHandler(this.stringsTableToolStripMenuItem_Click);
			// 
			// nativeTableToolStripMenuItem
			// 
			this.nativeTableToolStripMenuItem.Name = "nativeTableToolStripMenuItem";
			this.nativeTableToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.nativeTableToolStripMenuItem.Text = "Native table";
			this.nativeTableToolStripMenuItem.Click += new System.EventHandler(this.nativeTableToolStripMenuItem_Click);
			// 
			// nativehFileToolStripMenuItem
			// 
			this.nativehFileToolStripMenuItem.Name = "nativehFileToolStripMenuItem";
			this.nativehFileToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
			this.nativehFileToolStripMenuItem.Text = "Native.h file";
			this.nativehFileToolStripMenuItem.Click += new System.EventHandler(this.nativehFileToolStripMenuItem_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.listView1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
			this.panel1.Location = new System.Drawing.Point(622, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(163, 582);
			this.panel1.TabIndex = 3;
			// 
			// listView1
			// 
			this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listView1.FullRowSelect = true;
			this.listView1.GridLines = true;
			this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listView1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.listView1.Location = new System.Drawing.Point(0, 0);
			this.listView1.MultiSelect = false;
			this.listView1.Name = "listView1";
			this.listView1.Size = new System.Drawing.Size(163, 582);
			this.listView1.TabIndex = 0;
			this.listView1.UseCompatibleStateImageBehavior = false;
			this.listView1.View = System.Windows.Forms.View.Details;
			this.listView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDoubleClick);
			this.listView1.MouseEnter += new System.EventHandler(this.listView1_MouseEnter);
			this.listView1.MouseLeave += new System.EventHandler(this.listView1_MouseLeave);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Function";
			this.columnHeader1.Width = 96;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Line";
			this.columnHeader2.Width = 86;
			// 
			// timer1
			// 
			this.timer1.Interval = 5000;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			// 
			// timer2
			// 
			this.timer2.Interval = 1;
			this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Right;
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1});
			this.toolStrip1.Location = new System.Drawing.Point(785, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(25, 582);
			this.toolStrip1.TabIndex = 1;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.AutoToolTip = false;
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripButton1.Font = new System.Drawing.Font("Arial Narrow", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Padding = new System.Windows.Forms.Padding(2);
			this.toolStripButton1.Size = new System.Drawing.Size(22, 103);
			this.toolStripButton1.Text = "Function Locations";
			this.toolStripButton1.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
			this.toolStripButton1.ToolTipText = "Show the line numbers for functions and lets you jump to them";
			this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel3,
            this.toolStripStatusLabel2});
			this.statusStrip1.Location = new System.Drawing.Point(0, 606);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(810, 22);
			this.statusStrip1.TabIndex = 4;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(39, 17);
			this.toolStripStatusLabel1.Text = "Ready";
			// 
			// toolStripStatusLabel3
			// 
			this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
			this.toolStripStatusLabel3.Size = new System.Drawing.Size(756, 17);
			this.toolStripStatusLabel3.Spring = true;
			this.toolStripStatusLabel3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
			this.toolStripStatusLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// timer3
			// 
			this.timer3.Interval = 5000;
			this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(810, 628);
			this.Controls.Add(this.fctb1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "GTA V High Level Decompiler";
			((System.ComponentModel.ISupportInitialize)(this.fctb1)).EndInit();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private FastColoredTextBoxNS.FastColoredTextBox fctb1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem directoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem intStyleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem intToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem uintToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hexToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showArraySizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem reverseHashesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem declareVariablesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem shiftVariablesToolStripMenuItem;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.Timer timer2;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripMenuItem exportTablesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem entitiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nativesToolStripMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem expandAllBlocksToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem collaspeAllBlocksToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem showLineNumbersToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
		private System.Windows.Forms.Timer timer3;
		private System.Windows.Forms.ToolStripMenuItem openCFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem saveCFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ContextMenuStrip cmsText;
		private System.Windows.Forms.ToolStripMenuItem fullNativeInfoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem extractToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stringsTableToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nativeTableToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem nativehFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem navigateForwardToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem navigateBackwardsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem useMultiThreadingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showFuncPointerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem consoleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pCToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem findHashFromStringsToolStripMenuItem;
	}
}

