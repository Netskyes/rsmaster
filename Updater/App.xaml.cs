using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    public partial class App : Application
    {
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
            if (!IsSingleInstance())
            {
                Application.Current.Shutdown();
            }
        }
    }
}
