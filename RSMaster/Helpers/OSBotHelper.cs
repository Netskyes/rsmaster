using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using Microsoft.Win32;

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
                return null;

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

            return null;
        }
        
        public static void SetJavaSystemPath(string path)
        {
            var paths = Environment.GetEnvironmentVariable("PATH");
            var separates = paths.EndsWith(";", StringComparison.InvariantCulture);

            Environment.SetEnvironmentVariable("PATH", (!separates) ? paths + ";" + path : paths + path);
        }

        public static bool JavaInstalled()
            => (GetJavaInstallPath() != null);

        public static bool JavaInPath()
        {
            return Environment.GetEnvironmentVariable("PATH").Contains("Java");
        }

        public static string GetJavaInstallPath()
        {
            string javaHomePath = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHomePath))
            {
                return javaHomePath;
            }

            var javaKey = @"SOFTWARE\JavaSoft\Java Runtime Environment\";
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(javaKey))
            {
                if (rk != null)
                {
                    var currentVersion = rk.GetValue("CurrentVersion")?.ToString() ?? null;
                    if (currentVersion != null)
                    {
                        using (RegistryKey key = rk.OpenSubKey(currentVersion))
                        {
                            return key.GetValue("JavaHome")?.ToString();
                        }
                    }
                }
            }

            return null;
        }
    }
}
