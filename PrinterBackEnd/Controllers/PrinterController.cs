using System;
using System.IO.Ports;
using System.Management;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace PrinterBackEnd.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class PrinterController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PrinterController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //[HttpPost("PrintLabel")]
        //public IActionResult PrintLabel()
        //{
        //    var printResult = SendToPrinter();
        //    if (printResult)
        //    {
        //        return Ok("Print job successful.");
        //    }
        //    else
        //    {
        //        return StatusCode(500, "Print job failed.");
        //    }
        //}

        //private bool SendToPrinter()
        //{
        //    string ipAddress = _configuration["PrinterSettings:IPAddress"];
        //    int port = int.Parse(_configuration["PrinterSettings:Port"]);

        //    try
        //    {
        //        // Predefined print command
        //        string printCommand = "\x02\x1BA\x1BA3V+00000H+0000\x1BCS4\x1B#F5\x1BA1V00889H1248\x1BZ\x03\x02\x1BA\x1BPS\x1BWKLabel\x1B%0\x1BH0538\x1BV00410\x1BGB0090030\x03 þ\x01€ 8 0\x03\x03ÿ\x81€ 8 0\x03 ƒÁ€ l 0\x03\x06 Á€ l 0\x03\x0C a€ l 0\x03\x0C a€ Æ 0\x03\x18 1€ Æ 0\x03\x18 1€\x01Ç ?ÿ\x18 1€\x01ƒ ?ÿ\x18 1€\x01ƒ 0\x03\x18 1€\x03ÿ€0\x03\x18 1€\x03ÿ€0\x03\x0C a€\x03\x01€0\x03\x0C a€\x06 À0\x03\x06 Á€\x06 À0\x03 ƒÁ€\x06 À0\x03\x01ÿ\x81ÿÌ `0\x03 þ\x01ÿÌ `                                                      \x1BQ1\x1BZ\x03";

        //        using (TcpClient client = new TcpClient(ipAddress, port))
        //        using (NetworkStream stream = client.GetStream())
        //        {
        //            byte[] data = Encoding.ASCII.GetBytes(printCommand);
        //            stream.Write(data, 0, data.Length);
        //            stream.Flush();
        //            Console.WriteLine($"Sent to printer: {printCommand.Replace("\x1B", "\\x1B").Replace("\x0A", "\\x0A")}");
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Exception: {ex.Message}");
        //        return false;
        //    }
        //}

        //[HttpGet("ClearBufferAndQueue")]
        //public IActionResult ClearBufferAndQueue()
        //{
        //    string ipAddress = _configuration["PrinterSettings:IPAddress"];
        //    int port = int.Parse(_configuration["PrinterSettings:Port"]);

        //    try
        //    {
        //        using (TcpClient client = new TcpClient(ipAddress, port))
        //        using (NetworkStream stream = client.GetStream())
        //        {
        //            string clearCommand = ClearBufferAndQueue2();
        //            byte[] data = Encoding.ASCII.GetBytes(clearCommand);
        //            stream.Write(data, 0, data.Length);
        //            stream.Flush();
        //            Console.WriteLine($"Sent to printer: {clearCommand.Replace("\x1B", "\\x1B").Replace("\x0A", "\\x0A")}");
        //        }
        //        return Ok("Buffer and queue cleared.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Exception: {ex.Message}");
        //        return StatusCode(500, "Failed to clear buffer and queue.");
        //    }
        //}

        //private string ClearBufferAndQueue2()
        //{
        //    return "\x1B" + "CS\x0A";
        //}

        //[HttpGet("PrinterStatus")]
        //public IActionResult PrinterStatus()
        //{
        //    string ipAddress = _configuration["PrinterSettings:IPAddress"];
        //    int port = int.Parse(_configuration["PrinterSettings:Port"]);

        //    try
        //    {
        //        using (TcpClient client = new TcpClient(ipAddress, port))
        //        using (NetworkStream stream = client.GetStream())
        //        {
        //            string statusCommand = GetPrinterStatus();
        //            byte[] data = Encoding.ASCII.GetBytes(statusCommand);
        //            stream.Write(data, 0, data.Length);
        //            stream.Flush();
        //            Console.WriteLine($"Sent to printer: {statusCommand.Replace("\x1B", "\\x1B").Replace("\x0A", "\\x0A")}");
        //        }
        //        return Ok("Printer is online.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Exception: {ex.Message}");
        //        return StatusCode(500, "Printer is offline.");
        //    }
        //}

        //private string GetPrinterStatus()
        //{
        //    return "\x1B" + "CS\x0A";
        //}

        //[HttpGet("GetSatoPrinter")]
        //public IActionResult GetSatoPrinter()
        //{
        //    var devices = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'")
        //.Get()
        //.OfType<ManagementObject>(); // Convert to IEnumerable<ManagementObject>

        //    // Filter for "SATO" and get the first one
        //    var satoPrinter = devices
        //        .FirstOrDefault(device => device["Name"]?.ToString().Contains("SATO") == true);

        //    if (satoPrinter != null)
        //    {
        //        var deviceId = satoPrinter["DeviceID"]?.ToString();
        //        return Ok(deviceId); // Return the DeviceID of the SATO printer
        //    }
        //    else
        //    {
        //        return NotFound(); // Return NotFound if no SATO printer is found
        //    }
        //}

        //[HttpPost("SendPrintCommand")]
        //public IActionResult SendPrintCommand([FromBody] string portName)
        //{
        //    if (string.IsNullOrEmpty(portName) || !portName.StartsWith("COM"))
        //    {
        //        return BadRequest("Invalid port name.");
        //    }

        //    try
        //    {
        //        // Open the serial port
        //        using (var serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One))
        //        {
        //            serialPort.Open();

        //            // Example command to print "Hello, SATO!"
        //            string printCommand = "AV0100H0100L0202XCT4-LXQ1Z";
        //            serialPort.Write(printCommand);

        //            serialPort.Close();
        //        }
        //        return Ok("Print command sent successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error sending print command: {ex.Message}");
        //    }
        //}

    }
}
