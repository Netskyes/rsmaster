using System;
using System.Reflection;

namespace RSMaster.Helpers
{
    internal sealed class PluginsValidator : MarshalByRefObject
    {
        public bool IsPluginValid(string assemblyPath, Type type)
        {
            bool result;

            try
            {
                var types = Assembly.LoadFrom(assemblyPath).GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i].IsSubclassOf(type) && types[i].GetMethod("PluginRun") != null)
                    {
                        return true;
                    }
                }

                result = false;
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}
