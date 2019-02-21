using System;
using System.IO;
using System.Linq;
using System.IO.Compression;

namespace RSMaster.Helpers
{
    using Utility;

    internal static class OSBotHelper
    {
        public static string JarLocation
            => Path.Combine(Util.AssemblyDirectory, "osbot.jar");

        public static string UserLocation
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OSBot");

        public static string ScriptsLocation
            => GetPathByName("Scripts");

        public static bool LocalBotExists()
            => File.Exists(JarLocation);

        public static bool UserLocationExists()
            => Directory.Exists(UserLocation);

        private static string GetPathByName(string folderName)
        {
            return Path.Combine(UserLocation, folderName + @"\");
        }

        public static string GetLocalBotVersion()
        {
            if (!LocalBotExists())
                return string.Empty;

            try
            {
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
                                return lines[4].Split(':')[1];
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Util.LogException(e);
            }

            return string.Empty;
        }
    }
}
