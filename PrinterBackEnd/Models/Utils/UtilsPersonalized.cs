using System.Net;
using System.Reflection;
using System.Text;

namespace PrinterBackEnd.Models.Utils
{
    public class UtilsPersonalized
    {
        private static bool assemblyDefined = false;

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }

        private static void DefineAssembly()
        {
            EmbeddedAssembly.Load("SATOPrinterAPI.Resources.AnyCPU.SatoGraphicUtils.dll", "SatoGraphicUtils.dll");
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            assemblyDefined = true;
        }

        //private static void ConvertGraphicToFile(string GraphicFile, string TempDir)
        //{
        //    SbplConverter.Convert(GraphicFile, TempDir, true);
        //}

        //public static string ConvertGraphicToSBPL(string GraphicFilePath, bool deleteFile = false)
        //{
        //    if (!assemblyDefined)
        //    {
        //        DefineAssembly();
        //    }

        //    string text = string.Concat(Path.GetTempPath(), "SBPL_", Guid.NewGuid(), ".txt");
        //    ConvertGraphicToFile(GraphicFilePath, text);
        //    if (deleteFile)
        //    {
        //        File.Delete(GraphicFilePath);
        //    }

        //    byte[] array = File.ReadAllBytes(text);
        //    File.Delete(text);
        //    Encoding encoding = new ASCIIEncoding();
        //    string @string = encoding.GetString(array);
        //    string text2 = "";
        //    if (array != null && array.Length > 8)
        //    {
        //        text2 = @string.Substring(0, 9);
        //        for (int i = 9; i < array.Length; i++)
        //        {
        //            text2 += IntToHex(array[i], 1);
        //        }
        //    }

        //    return text2.Replace("\u001bGB", "\u001bGH");
        //}

        //public static string ConvertGraphicToSBPL(Uri GraphicFilePath)
        //{
        //    if (GraphicFilePath.Scheme == Uri.UriSchemeFile)
        //    {
        //        return ConvertGraphicToSBPL(GraphicFilePath.LocalPath);
        //    }

        //    if (GraphicFilePath.Scheme == Uri.UriSchemeHttp || GraphicFilePath.Scheme == Uri.UriSchemeHttps)
        //    {
        //        byte[] array = HTTPGetFile(GraphicFilePath);
        //        string text = string.Concat(Path.GetTempPath(), "GP_", Guid.NewGuid(), ".txt");
        //        using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write))
        //        {
        //            fileStream.Write(array, 0, array.Length);
        //        }

        //        return ConvertGraphicToSBPL(text, deleteFile: true);
        //    }

        //    return null;
        //}

        public static string CommandDataReplace(string CommandFilePath, Dictionary<string, string> VariablesValue, string encoding = "ansi", bool deleteFile = false)
        {
            byte[] data = File.ReadAllBytes(CommandFilePath);
            if (deleteFile)
            {
                File.Delete(CommandFilePath);
            }

            string text = ByteArrayToString(data, encoding);
            foreach (string key in VariablesValue.Keys)
            {
                text = text.Replace(key, VariablesValue[key]);
            }

            return text;
        }

        public static string CommandDataReplace(Uri CommandFilePath, Dictionary<string, string> VariablesValue, string encoding = "ansi")
        {
            if (CommandFilePath.Scheme == Uri.UriSchemeFile)
            {
                return CommandDataReplace(CommandFilePath.LocalPath, VariablesValue, encoding);
            }

            if (CommandFilePath.Scheme == Uri.UriSchemeHttp || CommandFilePath.Scheme == Uri.UriSchemeHttps)
            {
                byte[] array = HTTPGetFile(CommandFilePath);
                string text = string.Concat(Path.GetTempPath(), "CD_", Guid.NewGuid(), ".txt");
                using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(array, 0, array.Length);
                }

                return CommandDataReplace(text, VariablesValue, encoding, deleteFile: true);
            }

            return null;
        }

        private static string IntToHex(int data, int numBytes)
        {
            string text = $"X{numBytes * 2}";
            if (data < 0)
            {
                data += (int)Math.Pow(2.0, numBytes * 8);
            }

            string data2 = data.ToString(text);
            byte[] array = ParseByteString(data2);
            StringBuilder stringBuilder = new StringBuilder();
            if (array != null && array.Length != 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    stringBuilder.Append(array[i].ToString("X2"));
                }
            }

            return stringBuilder.ToString();
        }

        private static byte[] ParseByteString(string data)
        {
            byte[] array = null;
            int num = 0;
            if (data != null && data.Length > 0)
            {
                if (data.Length % 2 != 0)
                {
                    data = "0" + data;
                }

                num = data.Length / 2;
                array = new byte[num];
                for (int i = 0; i < num; i++)
                {
                    string value = data.Substring(i * 2, 2);
                    array[i] = Convert.ToByte(value, 16);
                }
            }

            return array;
        }

        public static byte[] StringToByteArray(string Data, string encoding = "ansi")
        {
            byte[] array = null;
            if (encoding == null)
            {
                return Encoding.Default.GetBytes(Data);
            }

            return encoding.ToLower() switch
            {
                "ascii" => Encoding.ASCII.GetBytes(Data),
                "utf7" => Encoding.UTF7.GetBytes(Data),
                "utf8" => Encoding.UTF8.GetBytes(Data),
                "ansi" => Encoding.Default.GetBytes(Data),
                "utf16" => Encoding.Unicode.GetBytes(Data),
                "utf32" => Encoding.UTF32.GetBytes(Data),
                _ => Encoding.Default.GetBytes(Data),
            };
        }

        public static string ByteArrayToString(byte[] Data, string encoding = "ansi")
        {
            string text = null;
            if (encoding == null)
            {
                return Encoding.Default.GetString(Data);
            }

            return encoding.ToLower() switch
            {
                "ascii" => Encoding.ASCII.GetString(Data),
                "utf7" => Encoding.UTF7.GetString(Data),
                "utf8" => Encoding.UTF8.GetString(Data),
                "ansi" => Encoding.Default.GetString(Data),
                "utf16" => Encoding.Unicode.GetString(Data),
                "utf32" => Encoding.UTF32.GetString(Data),
                _ => Encoding.Default.GetString(Data),
            };
        }

        private static byte[] LocalGetFile(Uri url)
        {
            string localPath = url.LocalPath;
            byte[] array = null;
            try
            {
                if (File.Exists(localPath))
                {
                    FileStream fileStream = File.OpenRead(localPath);
                    array = new byte[fileStream.Length];
                    int num = fileStream.Read(array, 0, array.Length);
                    fileStream.Close();
                    if (num != array.Length)
                    {
                        throw new Exception("Unable to read all data from file : " + url.ToString());
                    }
                }
            }
            catch (Exception innerException)
            {
                throw new Exception("Fail to load local file : " + localPath, innerException);
            }

            return array;
        }

        private static byte[] HTTPGetFile(Uri url)
        {
            byte[] result = null;
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                HttpWebResponse httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
                if (httpWebResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("HTTP Reply : [" + httpWebResponse.StatusCode.ToString() + "]" + httpWebResponse.StatusDescription);
                }

                Stream responseStream = httpWebResponse.GetResponseStream();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    responseStream.CopyTo(memoryStream);
                    result = memoryStream.ToArray();
                }

                responseStream.Close();
                httpWebResponse.Close();
            }
            catch (Exception innerException)
            {
                throw new Exception("Fail to load remote file : " + url.ToString(), innerException);
            }

            return result;
        }
    }
#if false // Decompilation log
'12' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
------------------
Resolve: 'websocket-sharp, Version=1.0.2.28487, Culture=neutral, PublicKeyToken=5660b08a1845a91e'
Could not find by name: 'websocket-sharp, Version=1.0.2.28487, Culture=neutral, PublicKeyToken=5660b08a1845a91e'
------------------
Resolve: 'Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'USBConnectLib, Version=2.0.6915.26606, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'USBConnectLib, Version=2.0.6915.26606, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll'
------------------
Resolve: 'SatoGraphicUtils, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'SatoGraphicUtils, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null'
#endif

}
