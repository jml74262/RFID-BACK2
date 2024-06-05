using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PrinterBackEnd.Models;
using PrinterBackEnd.Services;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class PrinterController : ControllerBase
{
    private readonly PrinterService _printerService;
    private readonly ILogger<PrinterController> _logger;

    public PrinterController(PrinterService printerService, ILogger<PrinterController> logger)
    {
        _printerService = printerService;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetPrinterStatus()
    {
        try
        {
            var status = _printerService.GetPrinterStatus();
            if (status == null)
            {
                return NotFound("Printer status not available.");
            }
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the printer status.");
            return StatusCode(500, $"An error occurred while retrieving the printer status: {ex.Message}");
        }
    }

    [HttpPost("sendsimple")]
    public async Task<IActionResult> SendSimpleCommand(LabelData labelData)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(labelData.PrinterIp, 9100); // Connect to printer

            using var stream = client.GetStream();
            var sbplCommand = GenerateSbplCommand(labelData); // Construct SBPL command
            var data = System.Text.Encoding.ASCII.GetBytes(sbplCommand);
            await stream.WriteAsync(data, 0, data.Length); // Send data to printer

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while sending the command to the printer: {ex.Message}");
        }
    }

    string GenerateSbplCommand(LabelData labelData)
    {
        var sbplCommand = new StringBuilder();
        sbplCommand.AppendLine("^XA"); // Start of label

        // Print text command
        sbplCommand.AppendLine($"^FO{labelData.XPosition},{labelData.YPosition}");
        sbplCommand.AppendLine("^A0N,50,50"); // Font settings (adjust as needed)
        sbplCommand.AppendLine($"^FD{labelData.TextToPrint}^FS");

        // Add more SBPL commands for barcodes, images, etc. here

        sbplCommand.AppendLine("^XZ"); // End of label
        return sbplCommand.ToString();
    }

    [HttpPost("send-command")]
    public async Task<IActionResult> SendCommandFromFile([FromQuery] string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest("File path is required.");
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"The file at path '{filePath}' was not found.");
        }

        try
        {
            string command = await System.IO.File.ReadAllTextAsync(filePath);
            bool result = _printerService.SendCommand(command);

            if (!result)
            {
                return StatusCode(500, "Failed to send the command to the printer.");
            }

            return Ok("Command sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the command from file.");
            return StatusCode(500, $"An error occurred while sending the command from file: {ex.Message}");
        }
    }

    [HttpGet("GetActiveDeviceNames")]
    public IActionResult GetActiveDeviceNames()
    {
        UsbInfo[] devices = USBSender.GetActiveDeviceNames();
        if (devices.Length > 0)
        {
            return Ok(devices);
        }
        else
        {
            return NotFound("No active USB devices found.");
        }
    }

    [HttpGet("GetSATODrivers")]
    public IActionResult GetSATODrivers()
    {
        List<InfoConx> list = USBSender.GetSATODrivers();

        if (list.Count > 0)
        {
            return Ok(list);
        }
        else
        {
            return NotFound("No SATO drivers found.");
        }
    }

    // Post method to send command to SATO printer
    [HttpPost("SendSATOCommand")]
    public IActionResult SendSATOCommand(string satoCommand)
    {
        try
        {
            bool result = USBSender.SendSATOCommand(satoCommand);
            if (result)
            {
                return Ok("Command sent successfully.");
            }
            else
            {
                return StatusCode(500, "Failed to send the command to the printer.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the SATO command.");
            return StatusCode(500, $"An error occurred while sending the SATO command: {ex.Message}");
        }
    }
}
