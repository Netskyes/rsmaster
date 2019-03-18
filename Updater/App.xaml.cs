using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    public partial class App : Application
    {
        public App()
        {
            AppPath = AssemblyDirectory + @"\" + (IsUpdateDir() ? @"..\" : @"\");
        }

        public static string AppPath { get; set; }

        public static string RSMasterPath 
            => Path.Combine(AppPath, @"RSMaster.exe");

        public static string UpdaterNewPath 
            => Path.Combine(AppPath, @"Updater\Updater.exe");

        public static string UpdaterExistingPath 
            => Path.Combine(AppPath, @"Updater.exe");

        public static bool UpdateSelf;

        private Mutex mutex;
        private string mutexHash = "c9cb85745bee424e9cd534feec6cb87f";

        private bool IsSingleInstance()
        {
            try
            {
                Mutex.OpenExisting(mutexHash);
            }
            catch
            {
                mutex = new Mutex(true, mutexHash);
                return true;
            }

            return false;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            UpdateSelf = (e.Args.Length > 0 && e.Args[0] == "Update");

            if (UpdateSelf)
            {
                ExecuteUpdate();
                return;
            }

            if (!IsSingleInstance())
            {
                Application.Current.Shutdown();
            }
        }

        private void ExecuteUpdate()
        {
            try
            {
                File.Delete(UpdaterExistingPath);
                File.Copy(UpdaterNewPath, UpdaterExistingPath);

                Current.Shutdown();
                Process.Start(UpdaterExistingPath);
            }
            catch (Exception)
            {
                throw new Exception("An error occured updating Updater. Error Code: 2");
            }
        }

        private bool IsUpdateDir()
        {
            return Path.GetFileName(AssemblyDirectory).Equals("Updater");
        }

        private static string AssemblyDirectory
        {
            get
            {
                string path = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(path);
            }
        }
    }
}
