using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Updater.Helpers
{
    public static class OSBotHelper
    {
        public static string JarLocation 
            => MainWindow.AppPath + @"osbot.jar";

        public static bool LocalJarExists()
            => File.Exists(JarLocation);

        public static string GetLocalBotVersion()
        {
            if (!LocalJarExists())
                return string.Empty;

            using (var archive = ZipFile.Open(JarLocation, ZipArchiveMode.Read))
            {
                var manifest = archive.Entries.FirstOrDefault(x => x.FullName == "META-INF/MANIFEST.MF");
                if (manifest != null)
                {
                    using (var stream = manifest.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var lines = reader.ReadToEnd()?.Split
                            (new string[] { Environment.NewLine }, StringSplitOptions.None);

                        if (lines != null
                            && lines.Length >= 4)
                        {
                            return lines[4].Split(':')[1].Trim();
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}
