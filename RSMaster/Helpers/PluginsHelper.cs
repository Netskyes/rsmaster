using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Policy;

namespace RSMaster.Helpers
{
    using Api;
    using Utility;

    internal sealed class PluginsHelper
    {
        private List<string> pluginsList;
        private AppDomain appDomain;
        private AppDomainSetup domainSetup;
        private PluginsValidator pluginValidator;

        public PluginsHelper()
        {
            pluginsList = new List<string>();
        }

        public List<string> GetPlugins() => (pluginsList = FetchPlugins());

        private void CreateSandbox()
        {
            domainSetup = new AppDomainSetup();
            domainSetup.ApplicationBase = Util.AssemblyDirectory + @"\";
            Evidence evidence = AppDomain.CurrentDomain.Evidence;
            //PermissionSet perms = new PermissionSet(System.Security.Permissions.PermissionState.None);
            Type pvType = typeof(PluginsValidator);

            appDomain = AppDomain.CreateDomain("sandbox", evidence, domainSetup);
            pluginValidator = (PluginsValidator)appDomain.CreateInstanceAndUnwrap(pvType.Assembly.FullName, pvType.FullName);
        }

        private bool IsPluginValid(string path)
        {
            try
            {
                return pluginValidator.IsPluginValid(path, typeof(Core));
            }
            catch (Exception e)
            {
                Util.LogException(e);
            }

            return false;
        }

        internal List<string> FetchPlugins()
        {
            var tempPlugins = new List<string>();

            try
            {
                CreateSandbox();

                var array = Directory.GetFiles(Util.AssemblyDirectory + @"\Plugins", "*.dll", SearchOption.AllDirectories);
                for (int i = 0; i < array.Length; i++)
                {
                    if (IsPluginValid(array[i]) && !pluginsList.Contains(array[i]))
                    {
                        tempPlugins.Add(array[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Util.LogException(e);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }

            return tempPlugins;
        }
    }
}
