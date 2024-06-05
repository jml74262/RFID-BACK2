using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Management;
using System.Runtime.InteropServices;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsbController : ControllerBase
    {
        public class PrinterController : ControllerBase
        {
            [HttpGet("GetSatoPrinter")]
            public IActionResult GetSatoPrinter()
            {
                var devices = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%SATO%'")
                    .Get()
                    .OfType<ManagementObject>();

                var satoPrinter = devices
                    .FirstOrDefault(device =>
                        device["Name"]?.ToString().Contains("SATO") == true &&
                        (device["PNPClass"]?.ToString().Equals("Printer") == true ||
                         device["PNPClass"]?.ToString().Equals("Ports") == true)
                    );

                if (satoPrinter != null)
                {
                    var deviceId = satoPrinter["DeviceID"]?.ToString();
                    var portName = satoPrinter["Name"]?.ToString();
                    return Ok(new { deviceId, portName });
                }
                else
                {
                    return NotFound();
                }
            }

            [HttpPost("SendPrintCommand")]
            public IActionResult SendPrintCommand([FromBody] string printerName)
            {
                try
                {
                    // Example command to print "Hello, SATO!"
                    string printCommand = "AV0100H0100L0202XCT4-LXQ1Z";

                    bool success = RawPrinterHelper.SendStringToPrinter(printerName, printCommand);

                    if (success)
                    {
                        return Ok("Print command sent successfully.");
                    }
                    else
                    {
                        return StatusCode(500, "Failed to send print command.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error sending print command: {ex.Message}");
                }
            }
        }

        public class RawPrinterHelper
        {
            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool ClosePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, [In] ref DOCINFOA pDocInfo);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool EndDocPrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool StartPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool EndPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct DOCINFOA
            {
                [MarshalAs(UnmanagedType.LPStr)]
                public string pDocName;
                [MarshalAs(UnmanagedType.LPStr)]
                public string pOutputFile;
                [MarshalAs(UnmanagedType.LPStr)]
                public string pDataType;
            }

            public static bool SendBytesToPrinter(string printerName, IntPtr pBytes, int dwCount)
            {
                IntPtr hPrinter;
                DOCINFOA di = new DOCINFOA
                {
                    pDocName = "Raw Document",
                    pDataType = "RAW"
                };

                bool success = false;
                if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                {
                    if (StartDocPrinter(hPrinter, 1, ref di))
                    {
                        if (StartPagePrinter(hPrinter))
                        {
                            success = WritePrinter(hPrinter, pBytes, dwCount, out _);
                            EndPagePrinter(hPrinter);
                        }
                        EndDocPrinter(hPrinter);
                    }
                    ClosePrinter(hPrinter);
                }
                return success;
            }

            public static bool SendStringToPrinter(string printerName, string data)
            {
                IntPtr pBytes;
                int dwCount = data.Length;
                pBytes = Marshal.StringToCoTaskMemAnsi(data);
                bool success = SendBytesToPrinter(printerName, pBytes, dwCount);
                Marshal.FreeCoTaskMem(pBytes);
                return success;
            }
        }
    }
}
