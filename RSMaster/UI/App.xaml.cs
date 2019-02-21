using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RSMaster.UI
{
    public partial class App : Application
    {
        private Mutex mutex;
        private string mutexHash = "c9cb85745bee424e9cd526feec9cb27f";

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
            if (!IsSingleInstance())
            {
                Application.Current.Shutdown();
            }

            #if !DEBUG

            if (e.Args.Length < 1
                || e.Args[0] != "Raindropz")
            {
                Current.Shutdown();
            }

            #endif
        }
    }
}
