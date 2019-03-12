using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.IO;

namespace RSMaster.Api
{
    using Objects;
    using Utility;
    
    public class PluginHandler : PluginObject
    {
        public bool IsLoaded { get; set; }
        public object CorePlugin { get; set; }
        public string PluginPath { get; set; }
        public CoreBase ProxyBase { get; set; }

        private AppDomain appDomain;
        private AssemblyLoader assemblyLoader;
        private Thread thread;

        public PluginHandler(CoreBase proxyBase, string path)
        {
            PluginPath = path;
            ProxyBase = proxyBase;
        }

        public void Launch()
        {
            appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
            assemblyLoader = appDomain.CreateInstanceAndUnwrap
                (Assembly.GetExecutingAssembly().FullName, typeof(AssemblyLoader).FullName) as AssemblyLoader;

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            thread = new Thread(() => 
                assemblyLoader.LoadAssembly(PluginPath, ProxyBase, this));
            thread.Start();
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var parts = args.Name.Split(',');
            var assemblyPath = Util.AssemblyDirectory + @"\Plugins\" + parts[0].Trim() + ".dll";

            try
            {
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFile(assemblyPath);
                }
            }
            catch
            {
                // Do nothing
            }

            return null;
        }

        public void Stop()
        {
            if (IsLoaded)
            {
                try
                {
                    thread.Abort();
                    AppDomain.Unload(appDomain);
                    appDomain = null;
                }
                catch (Exception e)
                {
                    Util.LogException(e);
                }
            }
        }
    }
}
