using System.Reflection;

namespace PrinterBackEnd.Models.Utils
{
    internal class EmbeddedAssembly
    {
        private static Dictionary<string, Assembly> dic = null;

        public static void Load(string Resouces, string FileName)
        {
            if (dic == null)
            {
                dic = new Dictionary<string, Assembly>();
            }

            byte[] array = null;
            Assembly asm = null;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            using (Stream stream = executingAssembly.GetManifestResourceStream(Resouces))
            {
                if (stream == null)
                {
                    throw new Exception(Resouces + " is not found in Embedded Resources.");
                }

                array = new byte[(int)stream.Length];
                stream.Read(array, 0, (int)stream.Length);
                try
                {
                    asm = Assembly.Load(array);
                    if (dic.Where((KeyValuePair<string, Assembly> x) => x.Key == asm.FullName).FirstOrDefault().Equals(default(KeyValuePair<string, Assembly>)))
                    {
                        dic.Add(asm.FullName, asm);
                    }

                    return;
                }
                catch
                {
                }
            }

            bool flag = false;
            string text = "";
            //using (SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider())
            //{
            //    string text2 = BitConverter.ToString(sHA1CryptoServiceProvider.ComputeHash(array)).Replace("-", string.Empty);
            //    if (Environment.Is64BitProcess)
            //    {
            //        text = Path.GetTempPath() + "SATOAPI\\x64\\";
            //        if (!Directory.Exists(text))
            //        {
            //            Directory.CreateDirectory(text);
            //        }
            //    }
            //    else
            //    {
            //        text = Path.GetTempPath() + "SATOAPI\\x86\\";
            //        if (!Directory.Exists(text))
            //        {
            //            Directory.CreateDirectory(text);
            //        }
            //    }

            //    text += FileName;
            //    if (File.Exists(text))
            //    {
            //        byte[] buffer = File.ReadAllBytes(text);
            //        string text3 = BitConverter.ToString(sHA1CryptoServiceProvider.ComputeHash(buffer)).Replace("-", string.Empty);
            //        flag = ((text2 == text3) ? true : false);
            //    }
            //    else
            //    {
            //        flag = false;
            //    }
            //}

            //if (!flag)
            //{
            //    File.WriteAllBytes(text, array);
            //}

            //asm = Assembly.LoadFile(text);
            //if (dic.Where((KeyValuePair<string, Assembly> x) => x.Key == asm.FullName).FirstOrDefault().Equals(default(KeyValuePair<string, Assembly>)))
            //{
            //    dic.Add(asm.FullName, asm);
            //}
        }

        public static Assembly Get(string assemblyFullName)
        {
            if (dic == null || dic.Count == 0)
            {
                return null;
            }

            if (dic.ContainsKey(assemblyFullName))
            {
                return dic[assemblyFullName];
            }

            return null;
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
