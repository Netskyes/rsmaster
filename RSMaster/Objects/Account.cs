using System.Diagnostics;
using System.Drawing;

namespace RSMaster.Objects
{
    internal class Account
    {
        public Process Process { get; set; }
        public Bitmap ScreenImage { get; set; }

        public Account(Process instance)
        {
            Process = instance;
        }
    }
}
