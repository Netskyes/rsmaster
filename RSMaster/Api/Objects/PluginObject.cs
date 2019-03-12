using System;

namespace RSMaster.Api.Objects
{
    public class PluginObject : MarshalByRefObject, IDisposable
    {
        public sealed override object InitializeLifetimeService()
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
