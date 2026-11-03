using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TelegramUnlock
{
    public class ProxyItem
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Secret { get; set; }
        public long PingMs { get; set; }
        public bool IsAlive { get; set; }

        public ProxyItem(string server, int port, string secret)
        {
            Server = server;
            Port = port;
            Secret = secret;
            PingMs = 9999;
            IsAlive = false;
        }

        public string GetTgUrl()
        {
            return string.Format("tg://proxy?server={0}&port={1}&secret={2}", Server, Port, Secret);
        }
    }

    public class MainForm : Form
    {
        private List<ProxyItem> _proxies = new List<ProxyItem>();
        private string _telegramPath = "";
        private Button _btnLaunch;
        private Button _btnScan;
        private ListView _lvServers;
        private Label _lblStatus;

        public MainForm()
        {
            InitProxies();
            FindTelegram();
            SetupUI();
            EnsureDesktopShortcut();
            StartAsyncScan();
        }

        private void InitProxies()
        {
            _proxies.Add(new ProxyItem("akenai.top", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("0xb00b5.top", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("akenai.tg", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("rkn.tg", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("predator-artist.top", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("cover-acid.top", 853, "ee54ce330e4690cc297d2b031ff3f288b06d742e616b656e61692e636c69636b"));
            _proxies.Add(new ProxyItem("sun.lanternmoss.top", 443, "ee2b6eb790d613a1725557ce33b9a87b307777772e676f6f676c652e636f6d"));
            _proxies.Add(new ProxyItem("sierra.hollowfern.sbs", 443, "ee2b6eb790d613a1725557ce33b9a87b307777772e676f6f676c652e636f6d"));
            _proxies.Add(new ProxyItem("ocean.lanternmoss.top", 443, "ee2b6eb790d613a1725557ce33b9a87b307777772e676f6f676c652e636f6d"));
            _proxies.Add(new ProxyItem("iris.driftquill.click", 443, "ee2b6eb790d613a1725557ce33b9a87b307777772e676f6f676c652e636f6d"));
        }

        private void FindTelegram()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] candidates = new string[]
            {
                Path.Combine(appData, @"Telegram Desktop\Telegram.exe"),
                Path.Combine(localAppData, @"Telegram Desktop\Telegram.exe"),
                @"C:\Program Files\Telegram Desktop\Telegram.exe",
                @"C:\Program Files (x86)\Telegram Desktop\Telegram.exe"
            };

            foreach (string p in candidates)
            {
                if (File.Exists(p))
                {
                    _telegramPath = p;
                    break;
                }
            }
        }

        private void SetupUI()
        {
            this.Text = "Telegram Unlock";
            this.Size = new Size(420, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(28, 30, 34);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            if (File.Exists(_telegramPath))
            {
                try { this.Icon = Icon.ExtractAssociatedIcon(_telegramPath); } catch { }
            }

            Label lblTitle = new Label();
            lblTitle.Text = "Telegram Unlock";
            lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 18);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            _btnLaunch = new Button();
            _btnLaunch.Text = "Запустить";
            _btnLaunch.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            _btnLaunch.BackColor = Color.FromArgb(43, 114, 186);
            _btnLaunch.ForeColor = Color.White;
            _btnLaunch.FlatStyle = FlatStyle.Flat;
            _btnLaunch.FlatAppearance.BorderSize = 0;
            _btnLaunch.Location = new Point(20, 60);
            _btnLaunch.Size = new Size(365, 42);
            _btnLaunch.Cursor = Cursors.Hand;
            _btnLaunch.Click += BtnLaunch_Click;
            this.Controls.Add(_btnLaunch);

            _lvServers = new ListView();
            _lvServers.Location = new Point(20, 118);
            _lvServers.Size = new Size(365, 220);
            _lvServers.BackColor = Color.FromArgb(38, 41, 46);
            _lvServers.ForeColor = Color.White;
            _lvServers.View = View.Details;
            _lvServers.FullRowSelect = true;
            _lvServers.GridLines = false;
            _lvServers.BorderStyle = BorderStyle.None;
            _lvServers.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _lvServers.Columns.Add("Сервер", 220);
            _lvServers.Columns.Add("Пинг", 75);
            _lvServers.Columns.Add("Порт", 55);
            _lvServers.DoubleClick += (s, e) => BtnLaunch_Click(s, e);
            this.Controls.Add(_lvServers);

            _btnScan = new Button();
            _btnScan.Text = "Проверить серверы";
            _btnScan.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _btnScan.BackColor = Color.FromArgb(48, 52, 58);
            _btnScan.ForeColor = Color.White;
            _btnScan.FlatStyle = FlatStyle.Flat;
            _btnScan.FlatAppearance.BorderSize = 0;
            _btnScan.Location = new Point(20, 350);
            _btnScan.Size = new Size(365, 34);
            _btnScan.Cursor = Cursors.Hand;
            _btnScan.Click += (s, e) => StartAsyncScan();
            this.Controls.Add(_btnScan);

            _lblStatus = new Label();
            _lblStatus.Text = "Проверка серверов...";
            _lblStatus.Font = new Font("Segoe UI", 8.5f);
            _lblStatus.ForeColor = Color.Gray;
            _lblStatus.Location = new Point(22, 392);
            _lblStatus.AutoSize = true;
            this.Controls.Add(_lblStatus);
        }

        private void StartAsyncScan()
        {
            _lblStatus.Text = "Проверка серверов...";
            _btnScan.Enabled = false;

            Thread worker = new Thread(new ThreadStart(BenchmarkWorker));
            worker.IsBackground = true;
            worker.Start();
        }

        private void BenchmarkWorker()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Mozilla/5.0");
                    string content = wc.DownloadString("https://raw.githubusercontent.com/tgmtproxy/telegram-mtproto-proxy-list/master/README.md");
                    MatchCollection matches = Regex.Matches(content, @"tg://proxy\?server=([^&]+)&port=(\d+)&secret=([a-zA-Z0-9]+)");
                    foreach (Match m in matches)
                    {
                        string s = m.Groups[1].Value;
                        int p = int.Parse(m.Groups[2].Value);
                        string sec = m.Groups[3].Value;
                        bool exists = false;
                        lock (_proxies)
                        {
                            foreach (var it in _proxies)
                            {
                                if (it.Server == s && it.Port == p) { exists = true; break; }
                            }
                            if (!exists && _proxies.Count < 30)
                            {
                                _proxies.Add(new ProxyItem(s, p, sec));
                            }
                        }
                    }
                }
            }
            catch { }

            List<Thread> threads = new List<Thread>();
            lock (_proxies)
            {
                foreach (ProxyItem item in _proxies)
                {
                    ProxyItem target = item;
                    Thread t = new Thread(new ThreadStart(delegate
                    {
                        try
                        {
                            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                            {
                                Stopwatch sw = Stopwatch.StartNew();
                                IAsyncResult iar = sock.BeginConnect(target.Server, target.Port, null, null);
                                bool ok = iar.AsyncWaitHandle.WaitOne(1000);
                                sw.Stop();
                                if (ok && sock.Connected)
                                {
                                    target.PingMs = sw.ElapsedMilliseconds;
                                    target.IsAlive = true;
                                }
                                else
                                {
                                    target.PingMs = 9999;
                                    target.IsAlive = false;
                                }
                            }
                        }
                        catch
                        {
                            target.PingMs = 9999;
                            target.IsAlive = false;
                        }
                    }));
                    t.IsBackground = true;
                    threads.Add(t);
                    t.Start();
                }
            }

            foreach (Thread t in threads) { t.Join(); }

            this.BeginInvoke(new Action(delegate
            {
                UpdateServerList();
            }));
        }

        private void UpdateServerList()
        {
            _lvServers.Items.Clear();
            List<ProxyItem> alive = new List<ProxyItem>();
            lock (_proxies)
            {
                foreach (var p in _proxies)
                {
                    if (p.IsAlive) alive.Add(p);
                }
            }

            alive.Sort(delegate(ProxyItem a, ProxyItem b) { return a.PingMs.CompareTo(b.PingMs); });

            foreach (var p in alive)
            {
                ListViewItem lvi = new ListViewItem(p.Server);
                lvi.SubItems.Add(p.PingMs + " ms");
                lvi.SubItems.Add(p.Port.ToString());
                lvi.Tag = p;
                _lvServers.Items.Add(lvi);
            }

            if (_lvServers.Items.Count > 0)
            {
                _lvServers.Items[0].Selected = true;
                _lblStatus.Text = string.Format("Доступно серверов: {0}", alive.Count);
            }
            else
            {
                _lblStatus.Text = "Нет доступных серверов";
            }

            _btnScan.Enabled = true;
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_telegramPath) || !File.Exists(_telegramPath))
            {
                FindTelegram();
                if (string.IsNullOrEmpty(_telegramPath))
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Title = "Укажите Telegram.exe";
                    ofd.Filter = "Telegram.exe|Telegram.exe";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _telegramPath = ofd.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            ProxyItem target = null;
            if (_lvServers.SelectedItems.Count > 0)
            {
                target = _lvServers.SelectedItems[0].Tag as ProxyItem;
            }
            else if (_lvServers.Items.Count > 0)
            {
                target = _lvServers.Items[0].Tag as ProxyItem;
            }
            else if (_proxies.Count > 0)
            {
                target = _proxies[0];
            }

            if (target != null)
            {
                try
                {
                    Process.Start(_telegramPath, target.GetTgUrl());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message, "Telegram Unlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EnsureDesktopShortcut()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string lnk = Path.Combine(desktop, "Telegram Unlock.lnk");
                string currentExe = Application.ExecutablePath;

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(lnk);
                shortcut.TargetPath = currentExe;
                shortcut.WorkingDirectory = Path.GetDirectoryName(currentExe);
                shortcut.Description = "Telegram Unlock";
                if (File.Exists(_telegramPath))
                {
                    shortcut.IconLocation = _telegramPath + ",0";
                }
                shortcut.Save();

                string oldLnk = Path.Combine(desktop, "Telegram Turbo.lnk");
                if (File.Exists(oldLnk))
                {
                    try { File.Delete(oldLnk); } catch { }
                }
            }
            catch { }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
