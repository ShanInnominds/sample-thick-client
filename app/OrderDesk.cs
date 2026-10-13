// Order Desk - a sample "legacy" Windows thick client (WinForms, .NET Framework 4.x, C# 5).
//
// It has the shape of a typical line-of-business desktop app so desktop test automation can be shown
// against something realistic: a modal sign-in, a menu bar and toolbar, tabs, an order-entry form,
// a data grid, a details dialog, message boxes and a status bar.
//
// Some controls are deliberately "legacy" (badly exposed to UI Automation), the way real thick clients
// often are:
//   - toolbar buttons show only an icon (no text, no control name)
//   - the Notes box has no control name and no label
//   - the sales chart is drawn by hand, so UI Automation sees an empty pane
//
// Build: app\build.bat (uses the C# compiler that ships with Windows; no SDK needed).
// Sign-in: any user name; the password is ORDERDESK_PASSWORD (default "orderdesk").

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Order Desk (sample thick client)")]
[assembly: System.Reflection.AssemblyVersion("2.3.0.0")]

namespace OrderDesk
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class Product
    {
        public string Name;
        public decimal Price;
        public Product(string name, decimal price) { Name = name; Price = price; }
        public override string ToString() { return Name; }
    }

    static class Money
    {
        public static string Format(decimal value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
    }

    // ── sign-in dialog ──────────────────────────────────────────────────────
    class LoginForm : Form
    {
        readonly TextBox txtUser = new TextBox();
        readonly TextBox txtPassword = new TextBox();
        readonly ComboBox cboRegion = new ComboBox();
        readonly Label lblError = new Label();
        public string UserName { get { return txtUser.Text.Trim(); } }
        public string SalesRegion { get { return (string)cboRegion.SelectedItem; } }

        public LoginForm()
        {
            Name = "LoginForm";
            Text = "Sign in - Order Desk";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(340, 200);

            AddRow("User name:", txtUser, "txtUser", 20, 0);
            txtPassword.PasswordChar = '*';
            AddRow("Password:", txtPassword, "txtPassword", 55, 2);
            cboRegion.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRegion.Items.AddRange(new object[] { "North", "South", "East", "West" });
            cboRegion.SelectedIndex = 0;
            AddRow("Region:", cboRegion, "cboRegion", 90, 4);

            lblError.Name = "lblError";
            lblError.ForeColor = Color.Firebrick;
            lblError.SetBounds(20, 122, 300, 20);
            Controls.Add(lblError);

            var btnLogin = new Button { Name = "btnLogin", Text = "Sign in", TabIndex = 6 };
            btnLogin.SetBounds(150, 150, 80, 28);
            btnLogin.Click += delegate { TrySignIn(); };
            var btnCancel = new Button { Name = "btnCancel", Text = "Cancel", TabIndex = 7, DialogResult = DialogResult.Cancel };
            btnCancel.SetBounds(240, 150, 80, 28);
            Controls.Add(btnLogin);
            Controls.Add(btnCancel);
            AcceptButton = btnLogin;
            CancelButton = btnCancel;
        }

        void AddRow(string caption, Control input, string name, int top, int tabIndex)
        {
            var label = new Label { Text = caption, TabIndex = tabIndex, AutoSize = true };
            label.Location = new Point(20, top + 3);
            input.Name = name;
            input.TabIndex = tabIndex + 1;
            input.SetBounds(110, top, 210, 24);
            Controls.Add(label);
            Controls.Add(input);
        }

        void TrySignIn()
        {
            string expected = Environment.GetEnvironmentVariable("ORDERDESK_PASSWORD");
            if (string.IsNullOrEmpty(expected)) expected = "orderdesk";
            if (UserName.Length == 0) { lblError.Text = "Enter your user name."; return; }
            if (txtPassword.Text != expected) { lblError.Text = "Invalid user name or password."; txtPassword.SelectAll(); return; }
            DialogResult = DialogResult.OK;
        }
    }

    // ── order details dialog ────────────────────────────────────────────────
    class OrderDetailsForm : Form
    {
        public bool Approved;

        public OrderDetailsForm(DataGridViewRow row)
        {
            Name = "OrderDetailsForm";
            Text = "Order " + row.Cells["colOrder"].Value;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 250);
            string[] columns = { "colOrder", "colCustomer", "colProduct", "colQty", "colTotal", "colStatus" };
            string[] captions = { "Order:", "Customer:", "Product:", "Quantity:", "Total:", "Status:" };
            for (int i = 0; i < columns.Length; i++)
            {
                var label = new Label { Text = captions[i], AutoSize = true, Location = new Point(20, 20 + i * 28), TabIndex = i * 2 };
                var value = new TextBox { Name = "txtDetail" + columns[i].Substring(3), ReadOnly = true, TabIndex = i * 2 + 1,
                                          Text = Convert.ToString(row.Cells[columns[i]].Value, CultureInfo.InvariantCulture) };
                value.SetBounds(110, 17 + i * 28, 230, 24);
                Controls.Add(label);
                Controls.Add(value);
            }
            bool pending = Convert.ToString(row.Cells["colStatus"].Value) == "Pending";
            var btnApprove = new Button { Name = "btnApprove", Text = "Approve", Enabled = pending, TabIndex = 20 };
            btnApprove.SetBounds(170, 205, 80, 28);
            btnApprove.Click += delegate { Approved = true; DialogResult = DialogResult.OK; };
            var btnClose = new Button { Name = "btnClose", Text = "Close", TabIndex = 21, DialogResult = DialogResult.Cancel };
            btnClose.SetBounds(260, 205, 80, 28);
            Controls.Add(btnApprove);
            Controls.Add(btnClose);
            CancelButton = btnClose;
        }
    }

    // ── a hand-drawn chart: UI Automation sees an empty pane ────────────────
    class SalesChart : Panel
    {
        public readonly Dictionary<string, decimal> Totals = new Dictionary<string, decimal>();

        public SalesChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawString("Sales by product", Font, Brushes.Black, 8, 6);
            decimal max = 1;
            foreach (var value in Totals.Values) max = Math.Max(max, value);
            int x = 20, barWidth = 50, bottom = Height - 24;
            foreach (var pair in Totals)
            {
                int height = (int)((bottom - 40) * (pair.Value / max));
                e.Graphics.FillRectangle(Brushes.SteelBlue, x, bottom - height, barWidth, height);
                e.Graphics.DrawString(pair.Key, Font, Brushes.Black, x - 4, bottom + 4);
                x += barWidth + 30;
            }
        }
    }

    // ── main window ─────────────────────────────────────────────────────────
    class MainForm : Form
    {
        static readonly Product[] Products =
        {
            new Product("Widget A", 12.50m), new Product("Widget B", 20.00m),
            new Product("Gadget X", 99.90m), new Product("Service Plan", 150.00m),
        };
        const decimal ExpressFee = 15.00m;
        const decimal WholesaleDiscount = 0.10m;

        readonly TabControl tabs = new TabControl();
        readonly TextBox txtCustomer = new TextBox();
        readonly ComboBox cboCustomerType = new ComboBox();
        readonly ComboBox cboProduct = new ComboBox();
        readonly NumericUpDown numQty = new NumericUpDown();
        readonly DateTimePicker dtpDelivery = new DateTimePicker();
        readonly CheckBox chkExpress = new CheckBox();
        readonly RadioButton rbInvoice = new RadioButton();
        readonly RadioButton rbCard = new RadioButton();
        readonly TextBox notes = new TextBox();          // legacy: no control name, no label
        readonly Label lblTotal = new Label();
        readonly DataGridView dgvOrders = new DataGridView();
        readonly TextBox txtFilter = new TextBox();
        readonly SalesChart chart = new SalesChart();
        readonly ToolStripStatusLabel lblStatus = new ToolStripStatusLabel();
        readonly ToolStripStatusLabel lblUser = new ToolStripStatusLabel();
        readonly ToolStripStatusLabel lblCount = new ToolStripStatusLabel();
        int nextOrder = 1004;

        public MainForm()
        {
            Name = "MainForm";
            Text = "Order Desk 2.3";
            ClientSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            tabs.Name = "tabMain";
            tabs.Dock = DockStyle.Fill;
            tabs.TabPages.Add(BuildEntryPage());
            tabs.TabPages.Add(BuildOrdersPage());
            Controls.Add(tabs);
            Controls.Add(BuildToolbar());
            Controls.Add(BuildMenu());
            Controls.Add(BuildStatusBar());

            AddOrder("1001", "Acme Retail", "Widget A", 10, 125.00m, "Shipped");
            AddOrder("1002", "Globex Wholesale", "Gadget X", 3, 269.73m, "Pending");
            AddOrder("1003", "Initech", "Service Plan", 1, 165.00m, "Pending");
            Shown += delegate { SignIn(); };
        }

        void SignIn()
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog(this) != DialogResult.OK) { Close(); return; }
                lblUser.Text = "User: " + login.UserName + " (" + login.SalesRegion + ")";
                SetStatus("Signed in");
            }
        }

        MenuStrip BuildMenu()
        {
            var menu = new MenuStrip { Name = "menuMain" };
            var file = new ToolStripMenuItem("&File") { Name = "mnuFile" };
            file.DropDownItems.Add(new ToolStripMenuItem("&New order", null, delegate { NewOrder(); }) { Name = "mnuNewOrder", ShortcutKeys = Keys.Control | Keys.N });
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, delegate { Close(); }) { Name = "mnuExit" });
            var orders = new ToolStripMenuItem("&Orders") { Name = "mnuOrders" };
            orders.DropDownItems.Add(new ToolStripMenuItem("&Find...", null, delegate { tabs.SelectedIndex = 1; txtFilter.Focus(); }) { Name = "mnuFind", ShortcutKeys = Keys.Control | Keys.F });
            var help = new ToolStripMenuItem("&Help") { Name = "mnuHelp" };
            help.DropDownItems.Add(new ToolStripMenuItem("&About Order Desk", null, delegate
            {
                MessageBox.Show(this, "Order Desk 2.3\nSample thick client for desktop test automation.", "About Order Desk",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }) { Name = "mnuAbout" });
            menu.Items.AddRange(new ToolStripItem[] { file, orders, help });
            MainMenuStrip = menu;
            return menu;
        }

        ToolStrip BuildToolbar()
        {
            // Legacy: icon-only buttons with no control names; only the tooltips say what they do.
            var bar = new ToolStrip { Name = "toolbarMain" };
            bar.Items.Add(IconButton(Color.SeaGreen, "New order", delegate { NewOrder(); }));
            bar.Items.Add(IconButton(Color.RoyalBlue, "Save order", delegate { SaveOrder(); }));
            bar.Items.Add(IconButton(Color.Firebrick, "Clear form", delegate { ClearForm(); }));
            return bar;
        }

        static ToolStripButton IconButton(Color color, string tooltip, EventHandler onClick)
        {
            var image = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(image)) { g.Clear(Color.Transparent); g.FillEllipse(new SolidBrush(color), 1, 1, 14, 14); }
            return new ToolStripButton(null, image, onClick) { DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = tooltip };
        }

        StatusStrip BuildStatusBar()
        {
            var bar = new StatusStrip { Name = "statusMain" };
            lblStatus.Name = "lblStatus";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblCount.Name = "lblCount";
            lblUser.Name = "lblUser";
            lblUser.Text = "Not signed in";
            bar.Items.AddRange(new ToolStripItem[] { lblStatus, lblCount, lblUser });
            return bar;
        }

        TabPage BuildEntryPage()
        {
            var page = new TabPage("Order entry") { Name = "tabEntry", Padding = new Padding(10) };
            var customer = new GroupBox { Text = "Customer", Name = "grpCustomer" };
            customer.SetBounds(12, 12, 380, 100);
            Field(customer, "Customer:", txtCustomer, "txtCustomer", 28, 0);
            cboCustomerType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCustomerType.Items.AddRange(new object[] { "Retail", "Wholesale" });
            cboCustomerType.SelectedIndex = 0;
            Field(customer, "Type:", cboCustomerType, "cboCustomerType", 60, 2);

            var item = new GroupBox { Text = "Item", Name = "grpItem" };
            item.SetBounds(12, 120, 380, 200);
            cboProduct.DropDownStyle = ComboBoxStyle.DropDownList;
            cboProduct.Items.AddRange(Products);
            cboProduct.SelectedIndex = 0;
            Field(item, "Product:", cboProduct, "cboProduct", 28, 0);
            numQty.Minimum = 1;
            numQty.Maximum = 999;
            numQty.Value = 1;
            Field(item, "Quantity:", numQty, "numQty", 60, 2);
            dtpDelivery.Format = DateTimePickerFormat.Short;
            dtpDelivery.Value = DateTime.Today.AddDays(7);
            Field(item, "Delivery:", dtpDelivery, "dtpDelivery", 92, 4);
            chkExpress.Name = "chkExpress";
            chkExpress.Text = "Express delivery (+" + Money.Format(ExpressFee) + ")";
            chkExpress.SetBounds(100, 124, 260, 24);
            chkExpress.TabIndex = 6;
            rbInvoice.Name = "rbInvoice";
            rbInvoice.Text = "Invoice";
            rbInvoice.Checked = true;
            rbInvoice.SetBounds(100, 156, 90, 24);
            rbCard.Name = "rbCard";
            rbCard.Text = "Card";
            rbCard.SetBounds(200, 156, 90, 24);
            item.Controls.AddRange(new Control[] { chkExpress, rbInvoice, rbCard });

            notes.Multiline = true;                       // legacy: no Name, no label
            notes.SetBounds(410, 20, 380, 90);
            notes.TabIndex = 50;

            lblTotal.Name = "lblTotal";
            lblTotal.Font = new Font(Font.FontFamily, 12f, FontStyle.Bold);
            lblTotal.SetBounds(12, 335, 300, 28);
            var btnCalculate = new Button { Name = "btnCalculate", Text = "Calculate" };
            btnCalculate.SetBounds(12, 372, 100, 30);
            btnCalculate.Click += delegate { Calculate(); };
            var btnSave = new Button { Name = "btnSave", Text = "Save order" };
            btnSave.SetBounds(122, 372, 100, 30);
            btnSave.Click += delegate { SaveOrder(); };

            chart.Name = "pnlChart";
            chart.SetBounds(410, 120, 380, 280);
            page.Controls.AddRange(new Control[] { customer, item, notes, lblTotal, btnCalculate, btnSave, chart });
            Calculate();
            return page;
        }

        static void Field(Control parent, string caption, Control input, string name, int top, int tabIndex)
        {
            var label = new Label { Text = caption, AutoSize = true, TabIndex = tabIndex, Location = new Point(12, top + 3) };
            input.Name = name;
            input.TabIndex = tabIndex + 1;
            input.SetBounds(100, top, 260, 24);
            parent.Controls.Add(label);
            parent.Controls.Add(input);
        }

        TabPage BuildOrdersPage()
        {
            var page = new TabPage("Orders") { Name = "tabOrders", Padding = new Padding(10) };
            var label = new Label { Text = "Find customer:", AutoSize = true, Location = new Point(12, 15), TabIndex = 0 };
            txtFilter.Name = "txtFilter";
            txtFilter.TabIndex = 1;
            txtFilter.SetBounds(110, 12, 220, 24);
            var btnFind = new Button { Name = "btnFind", Text = "Find", TabIndex = 2 };
            btnFind.SetBounds(340, 11, 80, 26);
            btnFind.Click += delegate { ApplyFilter(); };
            var btnOpen = new Button { Name = "btnOpenOrder", Text = "Open order", TabIndex = 3 };
            btnOpen.SetBounds(430, 11, 100, 26);
            btnOpen.Click += delegate { OpenSelectedOrder(); };

            dgvOrders.Name = "dgvOrders";
            dgvOrders.SetBounds(12, 48, 780, 400);
            dgvOrders.AllowUserToAddRows = false;
            dgvOrders.AllowUserToDeleteRows = false;
            dgvOrders.ReadOnly = true;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.MultiSelect = false;
            dgvOrders.RowHeadersVisible = false;
            dgvOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            string[,] columns = { { "colOrder", "Order" }, { "colCustomer", "Customer" }, { "colProduct", "Product" },
                                  { "colQty", "Qty" }, { "colTotal", "Total" }, { "colStatus", "Status" } };
            for (int i = 0; i < columns.GetLength(0); i++) dgvOrders.Columns.Add(columns[i, 0], columns[i, 1]);
            dgvOrders.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0) OpenSelectedOrder(); };
            page.Controls.AddRange(new Control[] { label, txtFilter, btnFind, btnOpen, dgvOrders });
            return page;
        }

        // ── behaviour ────────────────────────────────────────────────────────
        decimal CurrentTotal()
        {
            var product = (Product)cboProduct.SelectedItem;
            decimal total = product.Price * numQty.Value;
            if ((string)cboCustomerType.SelectedItem == "Wholesale") total -= Math.Round(total * WholesaleDiscount, 2);
            if (chkExpress.Checked) total += ExpressFee;
            return total;
        }

        void Calculate()
        {
            lblTotal.Text = "Total: " + Money.Format(CurrentTotal());
        }

        void SaveOrder()
        {
            tabs.SelectedIndex = 0;
            if (txtCustomer.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Customer is required.", "Order Desk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomer.Focus();
                return;
            }
            Calculate();
            string number = (nextOrder++).ToString(CultureInfo.InvariantCulture);
            AddOrder(number, txtCustomer.Text.Trim(), ((Product)cboProduct.SelectedItem).Name, (int)numQty.Value,
                     CurrentTotal(), "Pending");
            SetStatus("Order " + number + " saved");
        }

        void NewOrder()
        {
            ClearForm();
            tabs.SelectedIndex = 0;
            txtCustomer.Focus();
            SetStatus("New order");
        }

        void ClearForm()
        {
            txtCustomer.Clear();
            cboCustomerType.SelectedIndex = 0;
            cboProduct.SelectedIndex = 0;
            numQty.Value = 1;
            chkExpress.Checked = false;
            rbInvoice.Checked = true;
            notes.Clear();
            Calculate();
        }

        void AddOrder(string number, string customer, string product, int qty, decimal total, string status)
        {
            dgvOrders.Rows.Add(number, customer, product, qty, Money.Format(total), status);
            decimal sum;
            chart.Totals.TryGetValue(product, out sum);
            chart.Totals[product] = sum + total;
            chart.Invalidate();
            UpdateCount();
        }

        void ApplyFilter()
        {
            string filter = txtFilter.Text.Trim();
            int shown = 0;
            foreach (DataGridViewRow row in dgvOrders.Rows)
            {
                row.Visible = filter.Length == 0 ||
                              Convert.ToString(row.Cells["colCustomer"].Value).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (row.Visible) shown++;
            }
            SetStatus(shown + " order(s) match");
        }

        void OpenSelectedOrder()
        {
            if (dgvOrders.CurrentRow == null || !dgvOrders.CurrentRow.Visible)
            {
                MessageBox.Show(this, "Select an order first.", "Order Desk", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var row = dgvOrders.CurrentRow;
            using (var details = new OrderDetailsForm(row))
            {
                if (details.ShowDialog(this) == DialogResult.OK && details.Approved)
                {
                    row.Cells["colStatus"].Value = "Approved";
                    SetStatus("Order " + row.Cells["colOrder"].Value + " approved");
                }
            }
        }

        void UpdateCount() { lblCount.Text = dgvOrders.Rows.Count + " orders"; }
        void SetStatus(string text) { lblStatus.Text = text; }
    }
}
