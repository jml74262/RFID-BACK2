//#region Assembly SATOPrinterAPI, Version=3.0.2.0, Culture=neutral, PublicKeyToken=null
//// C:\Program Files (x86)\SATO\SATO Printer API\Assembly\x86\SATOPrinterAPI.dll
//// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
//#endregion

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Management;
//using System.Printing;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Security.AccessControl;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.Win32;
//using static System.Management.ManagementObjectCollection;

//namespace SATOPrinterAPI;

//public class Driver
//{
//    [Serializable]
//    public class Info
//    {
//        public string PrinterModel { get; set; }

//        public string DriverName { get; set; }

//        public string PortName { get; set; }

//        public bool Online { get; set; }

//        public bool Default { get; set; }

//        public bool Bidirectional { get; set; }
//    }

//    [Serializable]
//    public class PortInfo
//    {
//        public string Name { get; set; }

//        public string IPAddress { get; set; }
//        public string Port { get; set; }

//        public Printer.InterfaceType Interface { get; set; }
//    }

//    private struct PORT_INFO_2
//    {
//        public string pPortName;

//        public string pMonitorName;

//        public string pDescription;

//        public PortType fPortType;

//        internal int Reserved;
//    }

//    [Flags]
//    private enum PortType
//    {
//        write = 1,
//        read = 2,
//        redirected = 4,
//        net_attached = 8
//    }

//    [DllImport("winspool.drv", CharSet = CharSet.Ansi, EntryPoint = "EnumPortsA", ExactSpelling = true, SetLastError = true)]
//    private static extern int EnumPorts(string pName, int Level, IntPtr lpbPorts, int cbBuf, ref int pcbNeeded, ref int pcReturned);

//    public List<Info> GetDriverList()
//    {
//        //IL_000e: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0014: Expected O, but got Unknown
//        List<Info> list = new List<Info>();
//        string text = "SELECT Name,DriverName,PortName,WorkOffline,Default,EnableBIDI from Win32_Printer WHERE DriverName LIKE '%SATO%'";
//        ManagementObjectSearcher val = new ManagementObjectSearcher(text);
//        IEnumerable<ManagementObject> enumerable = from ManagementObject x in (IEnumerable)val.Get()
//                                                   select (x);
//        foreach (ManagementObject item in enumerable)
//        {
//            Info info = new Info();
//            info.DriverName = ((ManagementBaseObject)item).Properties["Name"].Value.ToString();
//            info.PrinterModel = ((ManagementBaseObject)item).Properties["DriverName"].Value.ToString();
//            info.PortName = ((ManagementBaseObject)item).Properties["PortName"].Value.ToString();
//            info.Online = !((ManagementBaseObject)item).Properties["WorkOffline"].Value.ToString().ToLower().Equals("true");
//            info.Default = bool.Parse(((ManagementBaseObject)item).Properties["Default"].Value.ToString());
//            info.Bidirectional = ((ManagementBaseObject)item).Properties["EnableBIDI"].Value.ToString().ToLower().Equals("true");
//            list.Add(info);
//        }

//        if (list.Count > 0)
//        {
//            return list;
//        }

//        return null;
//    }

//    public Info GetDriverInfo(string DriverName)
//    {
//        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0016: Expected O, but got Unknown
//        Info info = null;
//        string text = $"SELECT Name,DriverName,PortName,WorkOffline,Default,EnableBIDI FROM Win32_Printer Where Name = '{DriverName}'";
//        ManagementObjectSearcher val = new ManagementObjectSearcher(text);
//        ManagementObjectCollection val2 = val.Get();
//        if (val2 != null && val2.Count > 0)
//        {
//            ManagementObject val3 = ((IEnumerable)val2).OfType<ManagementObject>().FirstOrDefault();
//            info = new Info();
//            info.DriverName = ((ManagementBaseObject)val3).Properties["Name"].Value.ToString();
//            info.PrinterModel = ((ManagementBaseObject)val3).Properties["DriverName"].Value.ToString();
//            info.PortName = ((ManagementBaseObject)val3).Properties["PortName"].Value.ToString();
//            info.Online = !((ManagementBaseObject)val3).Properties["WorkOffline"].Value.ToString().ToLower().Equals("true");
//            info.Default = bool.Parse(((ManagementBaseObject)val3).Properties["Default"].Value.ToString());
//            info.Bidirectional = ((ManagementBaseObject)val3).Properties["EnableBIDI"].Value.ToString().ToLower().Equals("true");
//            return info;
//        }

//        return null;
//    }

//    public void SetDriverInfo(Info Driver)
//    {
//        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0007: Expected O, but got Unknown
//        //IL_0021: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0027: Expected O, but got Unknown
//        //IL_0043: Unknown result type (might be due to invalid IL or missing references)
//        //IL_004a: Expected O, but got Unknown
//        PutOptions val = new PutOptions();
//        val.Type = (PutType)1;
//        string text = $"Select * from Win32_Printer Where Name = '{Driver.DriverName}'";
//        ManagementObjectSearcher val2 = new ManagementObjectSearcher(text);
//        ManagementObjectCollection val3 = val2.Get();
//        ManagementObjectEnumerator enumerator = val3.GetEnumerator();
//        try
//        {
//            while (enumerator.MoveNext())
//            {
//                ManagementObject val4 = (ManagementObject)enumerator.Current;
//                if (Driver.DriverName != null && Driver.DriverName.Length > 0)
//                {
//                    ((ManagementBaseObject)val4).Properties["Name"].Value = Driver.DriverName;
//                }

//                if (Driver.PortName != null && Driver.PortName.Length > 0)
//                {
//                    ((ManagementBaseObject)val4).Properties["PortName"].Value = Driver.PortName;
//                }

//                ((ManagementBaseObject)val4).Properties["WorkOffline"].Value = !Driver.Online;
//                ((ManagementBaseObject)val4).Properties["Default"].Value = Driver.Default;
//                ((ManagementBaseObject)val4).Properties["EnableBIDI"].Value = Driver.Bidirectional;
//                val4.Put(val);
//            }
//        }
//        finally
//        {
//            ((IDisposable)enumerator)?.Dispose();
//        }
//    }

//    public int GetSpoolerPrintJobsNumber(string DriverName)
//    {
//        //IL_000e: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0014: Expected O, but got Unknown
//        LocalPrintServer val = new LocalPrintServer();
//        PrintQueue val2 = (from PrintQueue x in (IEnumerable)((PrintServer)val).GetPrintQueues()
//                           where x.FullName == DriverName
//                           select x).FirstOrDefault();
//        int result = -1;
//        if (val2 != null)
//        {
//            result = val2.NumberOfJobs;
//        }

//        return result;
//    }

//    public void ClearSpoolerPrintJobs(string DriverName)
//    {
//        //IL_000e: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0014: Expected O, but got Unknown
//        LocalPrintServer val = new LocalPrintServer();
//        PrintQueue val2 = (from PrintQueue x in (IEnumerable)((PrintServer)val).GetPrintQueues()
//                           where x.FullName == DriverName
//                           select x).FirstOrDefault();
//        if (val2 == null)
//        {
//            return;
//        }

//        ((PrintSystemObject)val2).Refresh();
//        PrintJobInfoCollection printJobInfoCollection = val2.GetPrintJobInfoCollection();
//        foreach (PrintSystemJobInfo item in printJobInfoCollection)
//        {
//            item.Cancel();
//        }
//    }

//    public PortInfo GetPortInfoByDriverName(string DriverName)
//    {
//        return RetrievePortInfo(DriverName, null);
//    }

//    public PortInfo GetPortInfoByName(string PortName)
//    {
//        return RetrievePortInfo(null, PortName);
//    }

//    private PortInfo RetrievePortInfo(string DriverName, string PortName)
//    {
//        //IL_0032: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0039: Expected O, but got Unknown
//        PortInfo portInfo = new PortInfo();
//        string portName = "";
//        if (DriverName != null)
//        {
//            string text = $"SELECT PortName from Win32_Printer WHERE Name = '{DriverName}'";
//            ManagementObjectSearcher val = new ManagementObjectSearcher(text);
//            ManagementObject val2 = ((IEnumerable)val.Get()).OfType<ManagementObject>().FirstOrDefault();
//            if (val2 == null)
//            {
//                return null;
//            }

//            portName = ((ManagementBaseObject)val2).Properties["PortName"].Value.ToString();
//        }
//        else if (PortName != null)
//        {
//            portName = PortName;
//        }

//        RegistryKey registryKey = null;
//        RegistryKey registryKey2 = null;
//        bool flag = false;
//        string name = "System\\CurrentControlSet\\Control\\Print\\Monitors";
//        using (RegistryKey registryKey3 = Registry.LocalMachine.OpenSubKey(name))
//        {
//            List<string> list = (from x in registryKey3.GetSubKeyNames()
//                                 where x.ToUpper().Contains("SATO") && x.ToUpper().Contains("ADVANCED PORT")
//                                 select x).ToList();
//            foreach (string item in list)
//            {
//                using (RegistryKey registryKey4 = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Print\\Monitors\\" + item + "\\Ports"))
//                {
//                    List<string> list2 = (from x in registryKey4.GetSubKeyNames()
//                                          where x == portName
//                                          select x).ToList();
//                    using List<string>.Enumerator enumerator2 = list2.GetEnumerator();
//                    if (enumerator2.MoveNext())
//                    {
//                        string current2 = enumerator2.Current;
//                        registryKey2 = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Print\\Monitors\\" + item + "\\Ports\\" + portName, RegistryKeyPermissionCheck.Default, RegistryRights.QueryValues);
//                        flag = true;
//                    }
//                }

//                if (flag)
//                {
//                    break;
//                }
//            }

//            if (!flag)
//            {
//                List<string> list3 = (from x in registryKey3.GetSubKeyNames()
//                                      where x.ToUpper().Contains("STANDARD TCP/IP")
//                                      select x).ToList();
//                foreach (string item2 in list3)
//                {
//                    using RegistryKey registryKey5 = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Print\\Monitors\\" + item2 + "\\Ports");
//                    List<string> list4 = (from x in registryKey5.GetSubKeyNames()
//                                          where x == portName
//                                          select x).ToList();
//                    using List<string>.Enumerator enumerator4 = list4.GetEnumerator();
//                    if (enumerator4.MoveNext())
//                    {
//                        string current4 = enumerator4.Current;
//                        registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Print\\Monitors\\" + item2 + "\\Ports\\" + portName, RegistryKeyPermissionCheck.Default, RegistryRights.QueryValues);
//                    }
//                }
//            }
//        }

//        portInfo.Name = portName;
//        if (registryKey2 != null)
//        {
//            portInfo.IPAddress = (string)registryKey2.GetValue("IPAddress", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
//            portInfo.Port = ((int)registryKey2.GetValue("PortNumber")).ToString();
//            portInfo.Interface = Printer.InterfaceType.TCPIP;
//            return portInfo;
//        }

//        if (registryKey != null)
//        {
//            portInfo.IPAddress = (string)registryKey.GetValue("HostName", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
//            portInfo.Port = ((int)registryKey.GetValue("PortNumber")).ToString();
//            portInfo.Interface = Printer.InterfaceType.TCPIP;
//            return portInfo;
//        }

//        if (portName.Contains("USB"))
//        {
//            string text2 = USBPortMatch(portName);
//            if (text2 != null)
//            {
//                portInfo.IPAddress = null;
//                portInfo.Port = text2;
//                portInfo.Interface = Printer.InterfaceType.USB;
//                return portInfo;
//            }
//        }
//        else
//        {
//            if (portName.Contains("COM"))
//            {
//                portInfo.IPAddress = null;
//                portInfo.Port = portName.Replace(":", "");
//                portInfo.Interface = Printer.InterfaceType.COM;
//                return portInfo;
//            }

//            if (portName.Contains("LPT"))
//            {
//                portInfo.IPAddress = null;
//                portInfo.Port = portName.Replace(":", "");
//                portInfo.Interface = Printer.InterfaceType.LPT;
//                return portInfo;
//            }
//        }

//        return null;
//    }

//    public List<string> GetPortNames()
//    {
//        int pcbNeeded = 0;
//        int pcReturned = 0;
//        IntPtr intPtr = IntPtr.Zero;
//        IntPtr zero = IntPtr.Zero;
//        List<string> list = new List<string>();
//        PORT_INFO_2[] array = null;
//        int num = EnumPorts("", 2, intPtr, 0, ref pcbNeeded, ref pcReturned);
//        try
//        {
//            intPtr = Marshal.AllocHGlobal(Convert.ToInt32(pcbNeeded + 1));
//            if (EnumPorts("", 2, intPtr, pcbNeeded, ref pcbNeeded, ref pcReturned) == 0)
//            {
//                throw new Win32Exception(Marshal.GetLastWin32Error());
//            }

//            zero = intPtr;
//            array = new PORT_INFO_2[pcReturned];
//            for (int i = 0; i < pcReturned; i++)
//            {
//                array[i] = (PORT_INFO_2)Marshal.PtrToStructure(zero, typeof(PORT_INFO_2));
//                zero = (IntPtr)(zero.ToInt32() + Marshal.SizeOf(typeof(PORT_INFO_2)));
//            }

//            zero = IntPtr.Zero;
//            for (int j = 0; j < pcReturned; j++)
//            {
//                list.Add(array[j].pPortName);
//            }

//            list.Sort();
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error getting available ports: {ex.Message}");
//        }
//        finally
//        {
//            if (intPtr != IntPtr.Zero)
//            {
//                Marshal.FreeHGlobal(intPtr);
//                intPtr = IntPtr.Zero;
//                zero = IntPtr.Zero;
//            }
//        }

//        return list;
//    }

//    public string GetVersion(string DriverName)
//    {
//        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0016: Expected O, but got Unknown
//        //IL_0065: Unknown result type (might be due to invalid IL or missing references)
//        //IL_006c: Expected O, but got Unknown
//        string text = null;
//        string text2 = $"Select * from Win32_Printer Where Name = '{DriverName}'";
//        ManagementObjectSearcher val = new ManagementObjectSearcher(text2);
//        string text3 = (from ManagementObject x in (IEnumerable)val.Get()
//                        select ((ManagementBaseObject)x).Properties["DriverName"].Value.ToString()).FirstOrDefault();
//        if (text3 != null)
//        {
//            string text4 = $"Select * from Win32_PrinterDriver Where Name Like '%{text3}%'";
//            ManagementObjectSearcher val2 = new ManagementObjectSearcher(text4);
//            string text5 = (from ManagementObject x in (IEnumerable)val2.Get()
//                            select ((ManagementBaseObject)x).Properties["DriverPath"].Value.ToString()).FirstOrDefault();
//            if (text5 != null)
//            {
//                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text5);
//                text = versionInfo.CompanyName + "|" + versionInfo.FileVersion;
//            }
//        }

//        return text.ToUpper();
//    }

//    public bool SendRawData(string DriverName, string Data)
//    {
//        return RawPrinterHelper.SendStringToPrinter(DriverName, Data);
//    }

//    public bool SendRawData(string DriverName, byte[] Data)
//    {
//        IntPtr intPtr = new IntPtr(0);
//        int num = Data.Length;
//        intPtr = Marshal.AllocCoTaskMem(num);
//        Marshal.Copy(Data, 0, intPtr, num);
//        return RawPrinterHelper.SendBytesToPrinter(DriverName, intPtr, num);
//    }

//    private string USBPortMatch(string Port)
//    {
//        //IL_003f: Unknown result type (might be due to invalid IL or missing references)
//        //IL_0046: Expected O, but got Unknown
//        //IL_0065: Unknown result type (might be due to invalid IL or missing references)
//        //IL_006c: Expected O, but got Unknown
//        Printer printer = new Printer();
//        List<Printer.USBInfo> uSBList = printer.GetUSBList();
//        List<string> list = new List<string>();
//        List<string> list2 = new List<string>();
//        string text = "";
//        string text2 = "";
//        string value = "";
//        string text3 = $"SELECT DeviceID,Service FROM Win32_PnPEntity WHERE DeviceID LIKE '%{Port}%' OR Service LIKE '%usbprint%'";
//        ManagementObjectSearcher val = new ManagementObjectSearcher(text3);
//        ManagementObjectCollection val2 = val.Get();
//        ManagementObjectEnumerator enumerator = val2.GetEnumerator();
//        try
//        {
//            while (enumerator.MoveNext())
//            {
//                ManagementObject val3 = (ManagementObject)enumerator.Current;
//                if (((ManagementBaseObject)val3).Properties["DeviceID"].Value != null && ((ManagementBaseObject)val3).Properties["DeviceID"].Value.ToString().Contains(Port))
//                {
//                    string[] array = ((ManagementBaseObject)val3).Properties["DeviceID"].Value.ToString().Split('\\');
//                    text = array[2].Replace(Port, "");
//                    text = text.Substring(0, text.Length - 1);
//                }

//                if (((ManagementBaseObject)val3).Properties["Service"].Value != null && ((ManagementBaseObject)val3).Properties["Service"].Value.ToString().ToLower().Contains("usbprint"))
//                {
//                    string[] array2 = ((ManagementBaseObject)val3).Properties["DeviceID"].Value.ToString().Split('\\');
//                    list.Add(array2[1]);
//                    list2.Add(array2[2]);
//                }
//            }
//        }
//        finally
//        {
//            ((IDisposable)enumerator)?.Dispose();
//        }

//        for (int i = 0; i < list.Count; i++)
//        {
//            try
//            {
//                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Enum\\USB\\" + list[i].ToUpper() + "\\" + list2[i].ToLower());
//                text2 = (string)registryKey.GetValue("ParentIdPrefix", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
//                if (text2.ToUpper() == text.ToUpper())
//                {
//                    value = "USB#" + list[i].ToUpper() + "#" + list2[i].ToUpper();
//                    break;
//                }
//            }
//            catch
//            {
//            }
//        }

//        foreach (Printer.USBInfo item in uSBList)
//        {
//            if (item.PortID.ToUpper().Contains(value))
//            {
//                return item.PortID;
//            }
//        }

//        return null;
//    }
//}
//#if false // Decompilation log
//'12' items in cache
//------------------
//Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
//------------------
//Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
//------------------
//Resolve: 'websocket-sharp, Version=1.0.2.28487, Culture=neutral, PublicKeyToken=5660b08a1845a91e'
//Could not find by name: 'websocket-sharp, Version=1.0.2.28487, Culture=neutral, PublicKeyToken=5660b08a1845a91e'
//------------------
//Resolve: 'Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
//Could not find by name: 'Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
//------------------
//Resolve: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
//Could not find by name: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
//------------------
//Resolve: 'System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
//Could not find by name: 'System.ServiceProcess, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
//------------------
//Resolve: 'System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
//Could not find by name: 'System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
//------------------
//Resolve: 'USBConnectLib, Version=2.0.6915.26606, Culture=neutral, PublicKeyToken=null'
//Could not find by name: 'USBConnectLib, Version=2.0.6915.26606, Culture=neutral, PublicKeyToken=null'
//------------------
//Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
//Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll'
//------------------
//Resolve: 'SatoGraphicUtils, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null'
//Could not find by name: 'SatoGraphicUtils, Version=1.0.0.1, Culture=neutral, PublicKeyToken=null'
//#endif
