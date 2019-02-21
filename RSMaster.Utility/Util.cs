using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Management;
using System.Text;
using System.Security.Cryptography;

namespace RSMaster.Utility
{
    public static class Util
    {
        private static readonly HttpClient httpClient;

        static Util()
        {
            httpClient = new HttpClient();
        }

        public static void Log(string text, string outputName = "debug")
        {
            try
            {
                File.AppendAllText(AssemblyDirectory + @"\" + outputName + ".log", text + Environment.NewLine);
            }
            catch
            {
            }
        }

        public static void LogException(Exception e)
        {
#if DEBUG
            var error = string.Format
                ("Timestamp: {4}" + Environment.NewLine +
                "Source: {5}" + Environment.NewLine +
                "Message: {0} " + Environment.NewLine +
                "TargetSite: {1} " + Environment.NewLine +
                "StackTrace: {2} " + Environment.NewLine +
                "Inner Exception: {3}" + Environment.NewLine +
                "--------", e.Message, e.TargetSite, e.StackTrace, e.InnerException, DateTime.Now.ToString(), e.Source);

            Log(error);
#endif
        }

        public static string AssemblyDirectory
        {
            get
            {
                string path = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(path);
            }
        }

        public static bool MatchPattern(this byte[] array, byte[] pattern)
        {
            if (array is null || array.Length < 1 
                || pattern is null || pattern.Length < 1)
                return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                if (i < array.Length && array[i] != pattern[i])
                    return false;
            }

            return true;
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static byte[] HexToByteArrayNoSpaces(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static byte[] HexToByteArray(string hex)
        {
            var hexStrings = hex.Split(new string[] { " " }, StringSplitOptions.None);
            byte[] bytes = new byte[hexStrings.Length];
            for (int i = 0; i < hexStrings.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexStrings[i], 16);
            }

            return bytes;
        }

        public static bool AnyStringNullOrEmpty(params string[] strings)
        {
            return strings != null && strings.Any(x => string.IsNullOrEmpty(x));
        }

        public static async Task<string> GetRequest(string requestUri)
        {
            try
            {
                var response = await httpClient.GetAsync(requestUri);
                return await
                    response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<string> PostRequest(string requestUri, HttpContent content)
        {
            try
            {
                var request = await httpClient.PostAsync(requestUri, content);
                return await
                    request.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void UpdateObjByProps(object srcObj, object targObj, bool clearUp)
        {
            if (targObj is null)
                return;

            var props = targObj.GetType().GetProperties().Where(x => x.SetMethod != null);
            foreach (var prop in props)
            {
                try
                {
                    if (clearUp)
                    {
                        prop.SetValue(targObj, null);
                    }
                    else
                    {
                        prop.SetValue(targObj, prop.GetValue(srcObj));
                    }
                }
                catch (Exception e)
                {
                    LogException(e);
                }
            }
        }

        public static string Shuffle(this string input)
            => new string(RandomPermutation(input.ToCharArray()));

        public static T[] RandomPermutation<T>(T[] array)
        {
            T[] retArray = new T[array.Length];
            array.CopyTo(retArray, 0);

            Random random = new Random();
            for (int i = 0; i < array.Length; i += 1)
            {
                int swapIndex = random.Next(i, array.Length);
                if (swapIndex != i)
                {
                    T temp = retArray[i];
                    retArray[i] = retArray[swapIndex];
                    retArray[swapIndex] = temp;
                }
            }

            return retArray;
        }

        public static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string getGraphicDevice()
        {
            string result;
            try
            {
                string text = "";
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        text = Convert.ToString(managementBaseObject["Description"]);
                        managementBaseObject.Dispose();
                    }
                }
                result = text;
            }
            catch
            {
                result = "";
            }
            return result;
        }

        private static string getMotherManufacturer()
        {
            string result;
            try
            {
                string text = "";
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        text = Convert.ToString(managementBaseObject["Manufacturer"]);
                        managementBaseObject.Dispose();
                    }
                }
                result = text;
            }
            catch
            {
                result = "";
            }
            return result;
        }

        private static string getRamID()
        {
            try
            {
                using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory").Get().GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        return (string)((ManagementObject)enumerator.Current)["PartNumber"];
                    }
                }
            }
            catch
            {
            }
            return "";
        }

        private static string getProcessorSerial()
        {
            string result;
            try
            {
                string text = "";
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("Select ProcessorId From Win32_processor"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        text = Convert.ToString(managementBaseObject["ProcessorId"]);
                    }
                }
                result = text;
            }
            catch
            {
                result = "";
            }
            return result;
        }

        private static string getMotherSerial()
        {
            string result;
            try
            {
                string text = "";
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        text = Convert.ToString(managementBaseObject["SerialNumber"]);
                    }
                }
                result = text;
            }
            catch
            {
                result = "";
            }
            return result;
        }

        private static bool DetectVirtualMachine()
        {
            bool result = false;
            try
            {
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        string text = managementBaseObject["Manufacturer"].ToString().ToLower();
                        if (text.Contains("microsoft corporation") || text.Contains("vmware"))
                        {
                            result = true;
                        }
                        if (managementBaseObject["Model"] != null)
                        {
                            string text2 = managementBaseObject["Model"].ToString().ToLower();
                            if (text2.Contains("microsoft corporation") || text2.Contains("vmware"))
                            {
                                result = true;
                            }
                        }
                        managementBaseObject.Dispose();
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        private static string getUUID()
        {
            string result;
            try
            {
                string text = "";
                using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("Select UUID From Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementBaseObject managementBaseObject in managementObjectSearcher.Get())
                    {
                        text = Convert.ToString(managementBaseObject["UUID"]);
                    }
                }
                result = text;
            }
            catch
            {
                result = "";
            }
            return result;
        }

        public static string GetHWID()
        {
            string text = "";
            if (Util.DetectVirtualMachine())
            {
                text += "VM_";
            }
            string text2 = Util.getRamID();
            if (text2 == null)
            {
                text2 = "UNKNOWN";
            }
            string text3 = Util.getGraphicDevice();
            if (text3 == null)
            {
                text3 = "";
            }
            string text4 = Util.getMotherManufacturer();
            if (text4 == null)
            {
                text4 = "";
            }
            string text5 = Util.getMotherSerial();
            if (text5 == null)
            {
                text5 = "";
            }
            string text6 = Util.getProcessorSerial();
            if (text6 == null)
            {
                text6 = "";
            }
            string text7 = Util.getUUID();
            if (text7 == null)
            {
                text7 = "";
            }
            text += text2.Replace(" ", "");
            text += text3.Replace(" ", "");
            text += text4.Replace(" ", "");
            text += text5.Replace(" ", "");
            text += text6.Replace(" ", "");
            text += text7.Replace(" ", "");
            string result;
            using (MD5 md = MD5.Create())
            {
                result = Util.ByteArrayToOneStringNoSpace(md.ComputeHash(Encoding.UTF8.GetBytes(text)));
            }
            return result;
        }

        public static bool IsNetAssembly(string path)
        {
            StringBuilder stringBuilder = new StringBuilder(512);
            int num;
            return WinAPI.GetFileVersion(path, stringBuilder, stringBuilder.Capacity, out num) == IntPtr.Zero;
        }

        public static string MD5Hash(string _in)
        {
            string result = "";
            using (MD5 md = MD5.Create())
            {
                result = Util.ByteArrayToOneStringNoSpace(md.ComputeHash(Encoding.GetEncoding(1251).GetBytes(_in)));
            }
            return result;
        }

        public static bool CompareByteArray(byte[] arr0, byte[] arr1)
        {
            if (arr0 == null || arr1 == null)
            {
                return false;
            }
            if (arr0.Length != arr1.Length)
            {
                return false;
            }
            for (int i = 0; i < arr0.Length; i++)
            {
                if (arr0[i] != arr1[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static string GetMD5String(string input)
        {
            string result;
            using (MD5 md = MD5.Create())
            {
                byte[] array = md.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < array.Length; i++)
                {
                    stringBuilder.Append(array[i].ToString("X2"));
                }
                result = stringBuilder.ToString();
            }
            return result;
        }

        public static bool WindowExists(int processId, string className)
        {
            bool found = false;
            int dwProcessId = 0;
            WinAPI.EnumWindows(delegate (IntPtr hWnd, IntPtr param)
            {
                WinAPI.GetWindowThreadProcessId(hWnd, out dwProcessId);
                if (dwProcessId == processId)
                {
                    StringBuilder stringBuilder = new StringBuilder(256);
                    if (WinAPI.GetClassName(hWnd, stringBuilder, stringBuilder.Capacity) != 0 && stringBuilder.ToString().IndexOf(className, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        found = true;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public static DateTime UnixTimeStampToDateTimeS(long unixTimeStamp)
        {
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            result = result.AddSeconds((double)unixTimeStamp).ToLocalTime();
            return result;
        }

        public static DateTime UnixTimeStampToDateTimeMS(long unixTimeStamp)
        {
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            result = result.AddMilliseconds((double)unixTimeStamp).ToLocalTime();
            return result;
        }

        public static long DateTimeToUnixTimeStampS(DateTime timeStamp)
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static long DateTimeToUnixTimeStampMS(DateTime timeStamp)
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static int GetUnixTimeInSeconds()
        {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static long GetUnixTime()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static string ByteArrayToOneString(byte[] ba, int len)
        {
            string result;
            try
            {
                StringBuilder stringBuilder = new StringBuilder(len * 2);
                for (int i = 0; i < len; i++)
                {
                    stringBuilder.AppendFormat("{0:X2} ", ba[i]);
                }
                result = stringBuilder.ToString();
            }
            catch
            {
                result = "";
            }
            return result;
        }

        public static byte[] HexNoSpaceToByteArray(string hex)
        {
            return (from x in Enumerable.Range(0, hex.Length)
                    where x % 2 == 0
                    select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
        }

        public static string ByteArrayToOneString(byte[] ba)
        {
            string result;
            try
            {
                StringBuilder stringBuilder = new StringBuilder(ba.Length * 2);
                foreach (byte b in ba)
                {
                    stringBuilder.AppendFormat("{0:X2} ", b);
                }
                result = stringBuilder.ToString();
            }
            catch
            {
                result = "";
            }
            return result;
        }

        public static string ByteArrayToString(byte[] ba, int len)
        {
            StringBuilder stringBuilder = new StringBuilder(ba.Length * 2);
            stringBuilder.Append("\n");
            for (int i = 0; i < len; i++)
            {
                stringBuilder.AppendFormat("{0:X2} ", ba[i]);
                if (i % 16 == 15)
                {
                    stringBuilder.Append("\n");
                }
            }
            return stringBuilder.ToString();
        }

        public static string ByteArrayToOneStringNoSpace(byte[] ba)
        {
            string result;
            try
            {
                StringBuilder stringBuilder = new StringBuilder(ba.Length * 2);
                foreach (byte b in ba)
                {
                    stringBuilder.AppendFormat("{0:X2}", b);
                }
                result = stringBuilder.ToString();
            }
            catch
            {
                result = "";
            }
            return result;
        }
    }
}
