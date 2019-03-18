using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RSMaster.UI;
using RSMaster.UI.Models;
using System.Management;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;

namespace RSMaster
{
    using Objects;
    using Helpers;
    using Utility;

    internal class AccountManager
    {
        public string[] AccountsRunning
        {
            get
            {
                lock (accountsRunningLock)
                {
                    return accountsRunning.Keys.ToArray();
                }
            }
        }

        public string[] AccountsLaunching { get => launching.ToArray(); }

        private List<string> launching = new List<string>();
        private bool processingQueue;
        private DateTime lastLaunchTime;

        private Dictionary<string, Account> accountsRunning = new Dictionary<string, Account>();
        private readonly object accountsRunningLock = new object();

        private readonly Queue<AccountModel> accountsQueue = new Queue<AccountModel>();
        private readonly object accountsQueueLock = new object();

        public AccountManager()
        {
        }

        public void QueueAccount(AccountModel account)
        {
            lock (accountsQueueLock)
            {
                accountsQueue.Enqueue(account);
                if (!processingQueue)
                {
                    processingQueue = true;
                    ThreadPool.QueueUserWorkItem(ProcessQueue);
                }
            }
        }

        public void ClearQueue()
        {
            lock (accountsQueueLock) accountsQueue.Clear();
        }

        private void ProcessQueue(object state)
        {
            while (true)
            {
                AccountModel account = null;
                lock (accountsQueueLock)
                {
                    if (!accountsQueue.Any())
                    {
                        processingQueue = false;
                        break;
                    }

                    if ((DateTime.Now - lastLaunchTime).TotalSeconds <= MainWindow.Settings.AccountLaunchDelayBetween)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    account = accountsQueue.Dequeue();
                    Task.Run(async () => await LaunchAccount(account));

                    lastLaunchTime = DateTime.Now;
                }
            }
        }

        private int GetErrorCode(string response)
        {
            int statusCode = -1;
            if (response.Contains("Your OSBot Client is out of date"))
            {
                statusCode = 2;
            }
            else if (response.Contains("Invalid username or password"))
            {
                statusCode = 3;
            }
            else if (response.Contains("You must update Web Walking through the Boot UI"))
            {
                statusCode = 5;
            }
            else if (response.Contains("You are not permitted to use OSBot"))
            {
                statusCode = 6;
            }

            return statusCode;
        }

        public async Task<(bool success, int errorCode)> LaunchAccount(AccountModel account, bool autoLaunch = false)
        {
            if (launching.Contains(account.Username))
                return (false, 0);

            try
            {
                if (account.PID.HasValue)
                {
                    if (Process.GetProcessById(account.PID.Value) != null)
                        return (false, 1);
                }
                else
                {
                    var existingInstance = GetInstance(account.Username);
                    existingInstance?.Kill();
                }
            }
            catch (ArgumentException)
            {
            }

            MainWindow.LogHandler("Launching account: " + account.Username);
            launching.Add(account.Username);

            var launchResponse = await Task.Run(() =>
            {
                var proc = new Process();
                proc.StartInfo.FileName = "CMD.exe";
                proc.StartInfo.Arguments = $"/c java -jar \"" + OSBotHelper.JarLocation + "\" " + GenerateCommand(account);
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();

                return proc.StandardOutput.ReadToEnd();
            });

            var errorCode = GetErrorCode(launchResponse);

            if (MainWindow.Settings.DebugMode)
            {
                MainWindow.LogHandler("DEBUG: " + launchResponse);
            }

            if (errorCode > 0)
            {
                launching.Remove(account.Username);
                return (false, errorCode);
            }

            var instance = await Task.Run(() =>
            {
                var begin = DateTime.Now;
                while (true)
                {
                    if (((DateTime.Now - begin)).TotalMilliseconds > 10*1000)
                        return null;

                    var process = GetInstance(account.Username);
                    if (process is null 
                        || process.HasExited)
                        return null;

                    if (process.MainWindowTitle?.Contains("OSBot") ?? false)
                    {
                        return process;
                    }
                }
            });

            launching.Remove(account.Username);
            if (instance != null)
            {
                account.Visible = true;
                account.PID = instance.Id;
                account.WindowHandle = instance.MainWindowHandle;
                account.AutoLaunched = autoLaunch;

                lock (accountsRunningLock)
                {
                    accountsRunning.Add(account.Username, new Account(instance));
                }

                var title = "RSMaster - " + account.Username;
                if (account.ProxyEnabled > 0 && account.Proxy != null)
                {
                    title += $" - Proxy: {account.Proxy.Alias} [{account.Proxy.Host}]";
                }

                WinAPI.SetWindowText
                    (instance.MainWindowHandle, title);

                if (MainWindow.Settings.ClientLaunchHidden)
                {
                    WinAPI.ShowWindow(instance.MainWindowHandle, WinAPI.ShowWindowCommands.Hide);
                    account.Visible = false;
                }

                TrackAccountStatus(account);
            }

            return (instance != null, 4);
        }
        
        public void StopAccount(AccountModel account)
        {
            if (account != null && account.PID.HasValue)
            {
                try
                {
                    Process.GetProcessById(account.PID.Value)?.Kill();
                }
                catch (Exception e)
                {
                    Util.LogException(e);
                }
                finally
                {
                    account.PID = null;
                    account.Visible = false;
                    account.AutoLaunched = false;
                }
            }
        }

        public bool IsRunning(string username)
        {
            return AccountsRunning.Contains(username);
        }

        private void TrackAccountStatus(AccountModel account)
        {
            Task.Run(() =>
            {
                while (account.PID.HasValue)
                {
                    try
                    {
                        Process.GetProcessById(account.PID.Value);
                    }
                    catch (Exception)
                    {
                        account.PID = null;
                        account.Visible = false;
                        account.AutoLaunched = false;
                        break;
                    }

                    Thread.Sleep(100);
                }

                lock (accountsRunningLock)
                {
                    accountsRunning.Remove(account.Username);
                }
            });
        }

        public BitmapImage GetAccountImageByUsername(string username)
        {
            lock (accountsRunningLock)
            {
                if (accountsRunning.ContainsKey(username))
                {
                    try
                    {
                        var handle = accountsRunning[username].Process.MainWindowHandle;

                        WinAPI.ShowWindow(handle, WinAPI.ShowWindowCommands.Show);
                        var image = GetWindowBitmapImage(handle);
                        WinAPI.ShowWindow(handle, WinAPI.ShowWindowCommands.Hide);

                        return BitmapToImageSource(image);
                    }
                    catch (Exception e)
                    {
                        Util.LogException(e);
                    }
                }
            }

            return null;
        }

        public List<BitmapImage> GetAccountImages()
        {
            var screens = new List<BitmapImage>();
            lock (accountsRunningLock)
            {
                foreach (var account in accountsRunning.Values)
                {
                    try
                    {
                        WinAPI.ShowWindow(account.Process.MainWindowHandle, WinAPI.ShowWindowCommands.Show);
                        var image = GetWindowBitmapImage(account.Process.MainWindowHandle);
                        WinAPI.ShowWindow(account.Process.MainWindowHandle, WinAPI.ShowWindowCommands.Hide);

                        screens.Add(BitmapToImageSource(image));
                    }
                    catch (Exception e)
                    {
                        Util.LogException(e);
                    }
                }
            }

            return screens;
        }

        private Bitmap GetWindowBitmapImage(IntPtr windowHandle)
        {
            var rect = new WinAPI.Rect();
            WinAPI.GetWindowRect(windowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(image);

            graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

            return image;
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private Process GetInstance(string username)
        {
            var query = "SELECT * FROM Win32_Process WHERE CommandLine LIKE '%" + username + "%'";

            Process process = null;
            using (var results = new ManagementObjectSearcher(query).Get())
            {
                process = results.Cast<ManagementObject>().Select
                    (mo => Process.GetProcessById((int)(uint)mo["ProcessId"])).FirstOrDefault();
            }

            return process;
        }

        private string GenerateCommand(AccountModel account)
        {
            var settings = MainWindow.Settings;

            var command = string.Format("-login \"{0}:{1}\" -bot {2}:{3}:{4}",
                settings.Username, settings.Password, 
                account.Username, account.Password, string.IsNullOrEmpty(account.BankPIN) ? "0000" : account.BankPIN);

            if (!string.IsNullOrEmpty(account.Script))
            {
                command += " -script \"" + account.Script + "\"";
                var scriptParams = account.Script.Split(':');
                if (scriptParams.Length == 1)
                {
                    command += ":params";
                }
            }

            if (account.World != null && (account.World >= 301 && account.World <= 525))
            {
                command += " -world " + account.World;
            }

            if (account.Proxy != null)
            {
                command += string.Format(" -proxy {0}:{1}", account.Proxy.Host, account.Proxy.Port);

                if (!string.IsNullOrEmpty(account.Proxy.Username) && !string.IsNullOrEmpty(account.Proxy.Password))
                {
                    command += string.Format(":{0}:{1}",
                        account.Proxy.Username, account.Proxy.Password);
                }
            }

            if (!string.IsNullOrEmpty(settings.ClientMemory))
            {
                int.TryParse(settings.ClientMemory, out int memory);
                if (memory > 0)
                {
                    command += " -mem " + memory;
                }
            }

            //if (settings.DebugMode)
            //{
            //    command += " -debug 5006";
            //}

            if (settings.ClientDataCollection)
            {
                command += " -data 1";
            }

            var permissions = new Dictionary<string, bool>
            {
                { "reflection", settings.ClientReflection },
                { "lowcpu", settings.ClientLowCPU },
                { "lowresource", settings.ClientLowResource },
                { "norandoms", settings.ClientNoRandoms },
                { "nointerface", settings.ClientNoInterface },
                { "norender", settings.ClientNoRender }
            };

            if (permissions.Any(x => x.Value))
            {
                command += " -allow " + string.Join(",", permissions.Where(x => x.Value).Select(x => x.Key)).TrimEnd(',');
            }

            return command;
        }
    }
}
