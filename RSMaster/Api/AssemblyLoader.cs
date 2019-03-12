using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.IO;

namespace RSMaster.Api
{
    using Utility;
    using Objects;

    public class AssemblyLoader : PluginObject
    {
        private object pluginObj;
        private MethodInfo pluginStop;

        [HandleProcessCorruptedStateExceptions]
        public bool LoadAssembly(string path, CoreBase proxyBase, PluginHandler pluginHandler)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(path));
            var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(Core))).ToArray();

            for (int i = 0; i < types.Length; i++)
            {
                FieldInfo coreBase = types[i].GetField
                    ("proxyBase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo pluginRun = types[i].GetMethod("PluginRun");
                pluginStop = types[i].GetMethod("PluginStop");

                if (pluginRun != null)
                {
                    pluginObj = assembly.CreateInstance(types[i].FullName);
                    coreBase.SetValue(pluginObj, proxyBase);
                    pluginHandler.IsLoaded = true;
                    pluginHandler.CorePlugin = pluginObj;

                    if (!proxyBase.LaunchedPlugins.Contains(pluginHandler))
                    {
                        proxyBase.LaunchedPlugins.Add(pluginHandler);
                    }

                    pluginRun.Invoke(pluginObj, null);

                    return true;
                }
            }

            return false;
        }

        [HandleProcessCorruptedStateExceptions]
        public void StopAssembly()
        {
            try
            {
                if (pluginStop != null)
                {
                    pluginStop.Invoke(pluginObj, null);
                }
            }
            catch (Exception e)
            {
                Util.LogException(e);
            }
        }
    }
}
