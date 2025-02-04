using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System;
using static System.Reflection.Assembly;

namespace TheorySDK.Views
{
    public partial class MainForm : Form
    {
        private readonly App _app = null;

        private readonly ComboBox ipComboBox = null;
        private readonly TextBox port = null;
        private readonly TextBox theoryPath = null;
        private readonly Label serverStatus = null;
        private readonly RichTextArea log = null;
        private readonly TextBox commandLine = null;

        public MainForm()
        {
            XamlReader.Load(this);
            Icon = new Icon(GetExecutingAssembly().GetManifestResourceStream("TheorySDK.Resources.icon48x48.ico"));
            _app = new App();

            _app.TcpServerChanging += OnTcpServerChanging;
            _app.TcpServerChanged += OnTcpServerChanged;
            _app.DataLoaded += UpdateFields;
            _app.Logger.MessageLogged += OnMessageLogged;

            _app.OnStart();

            UpdateFields();
            UpdateServerStatus();
            OnTcpServerChanged();
        }

        private void UpdateFields()
        {
            ipComboBox.Items.Clear();
            ipComboBox.Items.Add("Any", "");

            foreach (var ip in TcpServer.GetIPAddressList())
                ipComboBox.Items.Add(ip, ip);

            ipComboBox.SelectedKey = _app.Data.IpAddress;
            port.Text = _app.Data.Port.ToString();
            theoryPath.Text = _app.Data.TheoryPath;
        }

        private void UpdateServerStatus()
        {
            Application.Instance.AsyncInvoke(() =>
            {
                bool hasClient = _app.TcpServer?.HasClient ?? false;
                serverStatus.Text = hasClient ? "Connected" : "Waiting for client...";
                commandLine.Visible = hasClient;
            });
        }

        private void OnPortTextChanging(object sender, TextChangingEventArgs e)
        {
            string data = e.NewText.Trim();
            
            if (!string.IsNullOrEmpty(data))
            {
                if (int.TryParse(data, out int p) && p >= 0 && p <= 0xFFFF)
                    _app.Data.Port = p;
                else
                    e.Cancel = true;
            }
        }

        private void OnTcpServerChanging()
        {
            if (_app.TcpServer != null)
            {
                _app.TcpServer.ClientConnected -= UpdateServerStatus;
                _app.TcpServer.ClientDisconnected -= UpdateServerStatus;
            }
        }

        private void OnTcpServerChanged()
        {
            if (_app.TcpServer != null)
            {
                _app.TcpServer.ClientConnected += UpdateServerStatus;
                _app.TcpServer.ClientDisconnected += UpdateServerStatus;
            }
        }

        private void OnIpChanged(object sender, EventArgs e)
        {
            _app.Data.IpAddress = ipComboBox.SelectedKey;
        }

        private void OnTheoryPathClicked(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filters.Add(new FileFilter("Theory files (*.js)", new string[] { "js" }));
            openFileDialog.Filters.Add(new FileFilter("All files (*.*)", new string[] { "*" }));

            if (openFileDialog.ShowDialog(null) == DialogResult.Ok)
            {
                _app.Data.TheoryPath = openFileDialog.FileName;
                theoryPath.Text = openFileDialog.FileName;
            }

            // Deselect the text
            theoryPath.Selection = new Range<int>(0, -1);
        }

        private void OnMessageLogged(string message)
        {
            Application.Instance.AsyncInvoke(new Action(() =>
            {
                var time = DateTime.Now.ToString("hh:mm:ss");
                string value = (log.Text.Length == 0 ? "" : "\n") + "[" + time + "] " + message;
                log.Append(value, true);
            }));
        }

        private void OnCommandEntered(object sender, TextChangingEventArgs e)
        {
            if (e.Text == "\r" || e.Text == "\n" || e.Text == "\r\n")
            {
                _app.ExecuteScript(e.OldText);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _app.OnExit();
        }
    }
}
