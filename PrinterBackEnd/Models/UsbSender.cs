using Microsoft.Win32;
using Org.LLRP.LTK.LLRPV1.DataType;
using PrinterBackEnd.Models;
using PrinterBackEnd.Models.Helpers;
using PrinterBackEnd.Models.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PrinterBackEnd.Models
{
    internal class USBSender
    {
        public static UsbInfo[] GetActiveDeviceNames()
        {
            return GetDeviceList(USBIDs.Valid_VIDs);
        }

        private static UsbInfo[] GetDeviceList(int[] VIDs)
        {
            string[] array = default(string[]);
            string[] array2 = default(string[]);
            int usbDeviceList = USBPort.GetUsbDeviceList(ref array, ref array2);

            var devices = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%SATO%'")
                            .Get()
                            .OfType<ManagementObject>();

            // Filter for "SATO" and get the first one
            var satoPrinter = devices
                .FirstOrDefault(device => device["Name"]?.ToString().Contains("SATO") == true);

            var deviceId = "";

            if (satoPrinter != null)
            {
                deviceId = satoPrinter["DeviceID"]?.ToString();
            }

            // Here deviceId is something like "USB\\VID_0828&PID_014C\\YAFM1350", we need to extract VID and PID from it
            var vid = "";
            var pid = "";
            var sid = "";
            if (!string.IsNullOrEmpty(deviceId))
            {
                var match = Regex.Match(deviceId, @"VID_([0-9A-F]+)&PID_([0-9A-F]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    vid = match.Groups[1].Value;
                    pid = match.Groups[2].Value;
                    sid = deviceId.Split('\\').Last();
                }
            }

            using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\DeviceClasses\\{28d78fad-5a12-11d1-ae5b-0000f803a8c2}");

            var portName = GetPortName(vid, pid, sid);

            return new UsbInfo[] { new UsbInfo(vid, pid, sid, portName) };

            //return new UsbInfo[] { new UsbInfo(vid, pid, sid) };






        }

        

        public static List<InfoConx> GetSATODrivers()
        {
            List<InfoConx> list = new List<InfoConx>();
            string text = "SELECT Name,DriverName,PortName,WorkOffline,Default,EnableBIDI from Win32_Printer WHERE DriverName LIKE '%SATO%'";

            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(text);

            foreach (ManagementObject item in managementObjectSearcher.Get())
            {
                InfoConx info = new InfoConx();
                info.PrinterModel = item["Name"].ToString();
                info.DriverName = item["DriverName"].ToString();
                info.PortName = item["PortName"].ToString();
                info.Online = !Convert.ToBoolean(item["WorkOffline"]);
                info.Default = Convert.ToBoolean(item["Default"]);
                info.Bidirectional = Convert.ToBoolean(item["EnableBIDI"]);
                list.Add(info);
            }

            return list;
        }

        // Create a method that gets SATODrives, selects the one where InfoConx.Online is true and sends the command to the printer
        public static bool SendSATOCommand(string command)
        {

            byte[] cmddata = UtilsPersonalized.StringToByteArray(ControlCharReplace(command));
            List<InfoConx> list = GetSATODrivers();
            // If list is empty, return false
            if (list.Count == 0)
            {
                return false;
            }

            InfoConx infoConx = list.FirstOrDefault(x => x.Online && x.PortName.Contains("USB"));
            if (infoConx != null)
            {
                SendRawData(infoConx.DriverName, cmddata);
                return true;
            }
            return false;
        }

        public static string ControlCharReplace(string data)
        {
            Dictionary<string, char> chrList = ControlCharList();
            foreach (string key in chrList.Keys)
            {
                data = data.Replace(key, chrList[key].ToString());
            }
            return data;
        }

        public static Dictionary<string, char> ControlCharList()
        {
            Dictionary<string, char> ctr = new Dictionary<string, char>();
            ctr.Add("[NUL]", '\u0000');
            ctr.Add("[SOH]", '\u0001');
            ctr.Add("[STX]", '\u0002');
            ctr.Add("[ETX]", '\u0003');
            ctr.Add("[EOT]", '\u0004');
            ctr.Add("[ENQ]", '\u0005');
            ctr.Add("[ACK]", '\u0006');
            ctr.Add("[BEL]", '\u0007');
            ctr.Add("[BS]", '\u0008');
            ctr.Add("[HT]", '\u0009');
            ctr.Add("[LF]", '\u000A');
            ctr.Add("[VT]", '\u000B');
            ctr.Add("[FF]", '\u000C');
            ctr.Add("[CR]", '\u000D');
            ctr.Add("[SO]", '\u000E');
            ctr.Add("[SI]", '\u000F');
            ctr.Add("[DLE]", '\u0010');
            ctr.Add("[DC1]", '\u0011');
            ctr.Add("[DC2]", '\u0012');
            ctr.Add("[DC3]", '\u0013');
            ctr.Add("[DC4]", '\u0014');
            ctr.Add("[NAK]", '\u0015');
            ctr.Add("[SYN]", '\u0016');
            ctr.Add("[ETB]", '\u0017');
            ctr.Add("[CAN]", '\u0018');
            ctr.Add("[EM]", '\u0019');
            ctr.Add("[SUB]", '\u001A');
            ctr.Add("[ESC]", '\u001B');
            ctr.Add("[FS]", '\u001C');
            ctr.Add("[GS]", '\u001D');
            ctr.Add("[RS]", '\u001E');
            ctr.Add("[US]", '\u001F');
            ctr.Add("[DEL]", '\u007F');
            return ctr;
        }



        public static bool SendRawData(string DriverName, string Data)
        {
            return RawPrinterHelper.SendStringToPrinter(DriverName, Data);
        }

        public static bool SendRawData(string DriverName, byte[] Data)
        {
            IntPtr intPtr = new IntPtr(0);
            int num = Data.Length;
            intPtr = Marshal.AllocCoTaskMem(num);
            Marshal.Copy(Data, 0, intPtr, num);
            return RawPrinterHelper.SendBytesToPrinter(DriverName, intPtr, num);
        }

        private static string ExtractIDs(string input, out string vid, out string pid)
        {
            string text = "";
            vid = "";
            pid = "";
            try
            {
                Match match = Regex.Match(input, "usb#vid_([\\da-fA-F]+)&pid_([\\da-fA-F]+)#(.*)#{");
                if (match.Groups.Count >= 4)
                {
                    vid = match.Groups[1].Value;
                    pid = match.Groups[2].Value;
                    text = match.Groups[3].Value;
                }
                else
                {
                    return "Error";
                }
            }
            catch (Exception ex)
            {
                vid = "";
                pid = "";
                text = "Error";
                Console.WriteLine($"Error extracting IDs: {ex.Message}");
            }
            return text;
        }

        private static string GetPortName(string vid, string pid, string sid)
        {
            string text = null;
            try
            {
                using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\DeviceClasses\\{28d78fad-5a12-11d1-ae5b-0000f803a8c2}");
                for (int i = 0; i < registryKey.SubKeyCount; i++)
                {
                    string text2 = registryKey.GetSubKeyNames()[i].ToUpper();
                    string text3 = "VID_" + vid.ToString().PadLeft(4, '0') + "&PID_" + pid.ToString().PadLeft(4, '0') + "#" + sid;
                    if (text2.Contains(text3.ToUpper()))
                    {
                        text = registryKey.OpenSubKey(text2 + "\\#\\Device Parameters").GetValue("Port Description").ToString();
                        if (text.Contains("SATO") && !text.Contains("SATO "))
                        {
                            text = text.Replace("SATO", "SATO ");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                text = null;
                Console.WriteLine($"Error getting port name: {ex.Message}");
            }
            return text;
        }
    }

    internal static class USBIDs
    {
        public static readonly int[] Valid_VIDs = { /* Add valid VIDs here */ };
    }

    internal static class USBPort
    {
        public static int GetUsbDeviceList(ref string[] array, ref string[] array2)
        {
            // Implement this method to get the list of USB devices
            // For demonstration, returning dummy data
            array = new string[] { "USB\\VID_1234&PID_5678\\0001" };
            array2 = new string[] { "USB Device 1" };
            return 1;
        }
    }


}
