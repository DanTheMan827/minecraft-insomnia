using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftInsomnia
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const string APP_GUID = "60fc6fa8-195d-458d-b7b8-4866dc8094b7";

        private static NotifyIcon _trayIcon;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static ToolStripMenuItem exitMenu = new ToolStripMenuItem("Exit");
        private static ToolStripMenuItem autoStartMenu = new ToolStripMenuItem("Start at logon");
        private static AutoStartManager autoStartManager = new AutoStartManager(APP_GUID);
        private static Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            mutex = new Mutex(true, APP_GUID, out bool createdNew);

            if (!createdNew)
            {
                return; // Exit if another instance is already running
            }

            // Create tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = LoadEmbeddedIcon("MinecraftInsomnia.Resources.AppIcon.ico"),
                Visible = true,
                Text = "Minecraft Insomnia (Idle)"
            };

            autoStartMenu.Checked = autoStartManager.IsAutoStartEnabled;
            autoStartMenu.Click += AutoStartMenu_Click;

            // Context menu
            var contextMenu = new ContextMenuStrip();
            exitMenu.Click += (s, e) => ExitApp();

            contextMenu.Items.Add(autoStartMenu);
            contextMenu.Items.Add(exitMenu);

            _trayIcon.ContextMenuStrip = contextMenu;

            // Start background monitor
            Task.Run(() => MonitorMinecraft(_cts.Token));

            Application.Run(); // keeps UI thread alive
        }

        private static void AutoStartMenu_Click(object sender, EventArgs e)
        {
            autoStartManager.IsAutoStartEnabled = !autoStartManager.IsAutoStartEnabled;
            autoStartMenu.Checked = autoStartManager.IsAutoStartEnabled;
        }

        private static Icon LoadEmbeddedIcon(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();

            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    return new Icon(stream);
                }

                throw new InvalidOperationException($"Missing embedded resource: {resourceName}");
            }
        }

        private static void MonitorMinecraft(CancellationToken token)
        {
            bool active = false;

            while (!token.IsCancellationRequested)
            {
                bool isRunning = Process.GetProcessesByName("Minecraft.Windows").Length > 0;

                if (isRunning && !active)
                {
                    SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);
                    active = true;
                    UpdateTooltip("Minecraft Insomnia (Active)");
                }
                else if (!isRunning && active)
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                    active = false;
                    UpdateTooltip("Minecraft Insomnia (Idle)");
                }

                Thread.Sleep(1000); // check every 1s
            }

            SetThreadExecutionState(ES_CONTINUOUS); // restore default behavior
        }

        private static void UpdateTooltip(string text)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Text = text.Length > 63 ? text.Substring(0, 63) : text;
            }
        }

        private static void ExitApp()
        {
            _cts.Cancel();
            _trayIcon.Visible = false;
            SetThreadExecutionState(ES_CONTINUOUS); // restore default behavior
            Application.Exit();
        }
    }
}
