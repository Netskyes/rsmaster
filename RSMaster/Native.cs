using System.Runtime.InteropServices;

namespace RSMaster
{
    static class Native
    {
#if !RELEASE
        [DllImport("Raindropz.dll")]
        internal extern static void Hook();
#endif
    }
}
