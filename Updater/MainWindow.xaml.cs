using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using System.Net.Http;
using System.Diagnostics;

namespace Updater
{
    using Helpers;

    public partial class MainWindow : MetroWindow
    {
        const string UpdatesHost = "http://185.5.54.101:8080";
        private bool updateRequested;
        
        public MainWindow()
        {
            InitializeComponent();
            HttpHelper.SetBearerToken("ScAuEoPASozwIBUYOHz1sYcRmFnLdk2kLYOr0Xi1NnfK9k044x0CJApDq2ajKB9b");
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(Execute);
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            if (updateRequested)
            {
                Process.Start(App.UpdaterNewPath, "Update");
            }
        }

        private async Task Execute()
        {
            UpdateStatusText("Checking for updates...");
            var updates = await GetUpdates();

            if (updates.Any())
            {
                UpdateProgressLine(0);
                UpdateStatusText("Downloading updates, please wait...");

                await FetchUpdates(updates.ToArray());
                UpdateStatusText("Complete");
            }

            if (File.Exists(App.UpdaterNewPath))
            {
                var newHash = string.Empty;
                var existingHash = string.Empty;
                var backupPath = Path.Combine(App.AppPath, @"Updater.bak");

                try
                {
                    File.Copy(App.UpdaterExistingPath, backupPath, true);

                    using (var stream1 = new FileStream(App.UpdaterNewPath, FileMode.Open))
                    using (var stream2 = new FileStream(backupPath, FileMode.Open))
                    using (var md5 = MD5.Create())
                    {
                        newHash = BitConverter.ToString(md5.ComputeHash(stream1)).Replace("-", "").ToLower();
                        existingHash = BitConverter.ToString(md5.ComputeHash(stream2)).Replace("-", "").ToLower();

                        if (!existingHash.Equals(newHash))
                        {
                            updateRequested = true;
                            Invoke(Close);

                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    UpdateStatusText("An error occured updating Updater. Error Code: 1");

                    return;
                }
                finally
                {
                    File.Delete(Path.Combine(App.AppPath, @"Updater.bak"));
                }
            }

            if (File.Exists(App.RSMasterPath))
            {
                try
                {
                    Process.Start(App.RSMasterPath, "Raindropz");
                }
                catch (Exception)
                {
                    UpdateStatusText("An error occured launching RSMaster");
                    return;
                }
            }

            Invoke(Close);
        }

        private async Task<string> GetOSBotVersion()
        {
            return (await HttpHelper.GetRequest($"{UpdatesHost}/api/osbotversion"));
        }

        private async Task<List<string>> GetUpdates()
        {
            var updateList = new List<string>();
            var osbVersion = OSBotHelper.GetLocalBotVersion();
            var osbLatestVersion = await GetOSBotVersion();

            if (osbVersion == string.Empty || osbVersion != osbLatestVersion)
            {
                await DownloadFile(App.AppPath + "osbot.jar", $"{UpdatesHost}/api/osbotlatest");
            }

            var request = await HttpHelper.GetRequest($"{UpdatesHost}/api/updates");
            if (string.IsNullOrEmpty(request))
                return updateList;

            var files = request.Split
                (new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (files.Any())
            {
                double ppToUp = Math.Ceiling((100 / (double)files.Length));

                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i].Split('|');
                    var path = (App.AppPath + file[0]);
                    var hash = string.Empty;

                    if (File.Exists(path))
                    {
                        try
                        {
                            using (var sha1 = SHA1.Create())
                            using (var stream = File.OpenRead(path))
                            {
                                hash = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (hash != file[1])
                    {
                        updateList.Add(file[0]);
                    }

                    // Update progress
                    AddToProgressLine(ppToUp);
                }
            }

            return updateList;
        }

        private async Task FetchUpdates(string[] paths)
        {
            foreach (var path in paths)
            {
                UpdateProgressLine(0);
                var name = Path.GetFileName(path);
                var pathLocal = Path.Combine(App.AppPath, path);

                await DownloadFile(pathLocal, $"{UpdatesHost}/api/download/{name}");
            }
        }

        private async Task DownloadFile(string path, string requestUrl)
        {
            var response = await HttpHelper.Client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                return;
            }

            if (response == null)
                return;

            var contentLength = HttpHelper.GetHeader("Content-Length", response.Content);
            int.TryParse(contentLength, out int bufferSize);

            if (bufferSize < 1)
                return;

            try
            {
                var dirPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
                {
                    var buffer = new byte[bufferSize];
                    var beginTime = DateTime.Now;
                    int bytesWritten = 0;

                    string name = Path.GetFileName(path);

                    while (true)
                    {
                        int dataLen = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (dataLen < 1)
                            break;

                        await fs.WriteAsync(buffer, 0, dataLen);
                        bytesWritten += dataLen;


                        // Update stats
                        UpdateProgress(name, bufferSize, bytesWritten, beginTime);
                    }
                }
            }
            catch
            {
            }
        }

        private void UpdateProgress(string name, int bufferSize, int bytesWritten, DateTime beginTime)
        {
            double kbTotal = (bytesWritten / 1024f); // KB's
            double mbTotal = (kbTotal / 1024f); // MB's
            double mbps = Math.Max(mbTotal, 1) / Math.Max((DateTime.Now - beginTime).TotalSeconds, 1);

            UpdateProgressLine((int)((kbTotal / (double)(bufferSize / 1024f)) * 100));
            UpdateStatusText(string.Format("{0} - {1:n1}/{2:n1}MB {3:n1}MB/s", name, mbTotal, (bufferSize / 1024f) / 1024f, mbps));
        }

        private void UpdateStatusText(string text)
            => Invoke(() => LabelStatus.Text = text);

        private void UpdateProgressLine(double value)
            => Invoke(() => ProgressLine.Value = value);

        private void AddToProgressLine(double value)
            => Invoke(() => ProgressLine.Value += value);
            
        private void Invoke(Action a)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, a);
        }
    }
}
