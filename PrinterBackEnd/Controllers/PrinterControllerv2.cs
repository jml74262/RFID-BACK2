using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models;
using PrinterBackEnd.Models.Domain;
using PrinterBackEnd.Models.Dto.RFIDLabel;
using PrinterBackEnd.Services;
using System;
using System.Globalization;
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
    private readonly DataContext _context;

    public PrinterController(PrinterService printerService, ILogger<PrinterController> logger, DataContext context)
    {
        _printerService = printerService;
        _logger = logger;
        _context = context;
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
    public async Task<IActionResult> SendSATOCommand(PostRFIDLabeldto postRFIDLabeldto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");

            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Crear un nuevo objeto 'ProdEtiquetasRFID'
            var postRFIDLabel = new ProdEtiquetasRFID
            {
                Area = postRFIDLabeldto.Area,
                ClaveProducto = postRFIDLabeldto.ClaveProducto,
                NombreProducto = postRFIDLabeldto.NombreProducto,
                ClaveOperador = postRFIDLabeldto.ClaveOperador,
                Operador = postRFIDLabeldto.Operador,
                Turno = postRFIDLabeldto.Turno,
                PesoTarima = postRFIDLabeldto.PesoTarima,
                PesoBruto = postRFIDLabeldto.PesoBruto,
                PesoNeto = postRFIDLabeldto.PesoNeto,
                Piezas = postRFIDLabeldto.Piezas,
                Trazabilidad = postRFIDLabeldto.Trazabilidad,
                Orden = postRFIDLabeldto.Orden,
                RFID = postRFIDLabeldto.RFID,
                Status = postRFIDLabeldto.Status,
            };

            // Agregar el objeto 'ProdEtiquetasRFID' a la tabla 'ProdEtiquetasRFID'
            _context.ProdEtiquetasRFID.Add(postRFIDLabel);
            await _context.SaveChangesAsync();

            // Crear la cadena de comando SATO usando los valores del DTO
            string stringResult = $@"
            ^XA
            ^FO70,50^GFA,1512,1512,24,,:::::::::LF,
            :::::::::::::LFK07IFC3F81IFC1JF3F8003IFE3FC0FF8,LFK07IFE3F83IFE1JFBF8003IFE3FE1FF04,
            LFK07IFE3F87JF1JFBF8003IFE1FF3FE04,LFK07IFE3F87JF1JFBF8003IFE0FF3FE02,LFK07F07E3F8FF07F9FC003F8003F8I07F7FC,
            LFK07F07E3F8FE07F9FC003F8003F8I07EFF8,LFK07F07E3F8FE03F9FC003F8003F8I03DFF,LFK07IFE3F8FE03F9JF3F8003IFE01BFF,
            LFK07IFE3F8FE03F9JF3F8003IFE003FE,LFK07IFE3F8FE07F9JF3F8003IFE007FC,LFK07IFE3F8FE07F9JF3F8003IFE00FF8,LFK07F8FF3F8FE07F9FC003F8003IFC01FF6,
            LFK07F07F3F8FE07F9FC003F8003F8I03FF7,LFK07F07F3F8FF07F9FC003F8003F8I03FEF8,LFK07F07F3F8FF07F9FC003FC003F8I07FDF8,LFK07JF3F8KF9FC003JF3IFE0FFBFC,
            LFK07JF3F87JF1FC003JF3IFE1FF3FE,LFK07JF3F83IFE1FC003JF3IFE1FF1FF,LFK07IFE3F81IFC1FC003JF3IFE3FE1FF,LFgW07F,LF,::::::::,:::::::::^FS
            ^FO40,40^GB1160,820,6^FS   // CAJA
            ^FO40,40^GB500,80,6^FS // 1:1
            ^FO537,40^GB663,80,6^FS // 1:2
            ^FO550,70^A0N,30,30^FDPRODUCTO TERMINADO TARIMA^FS
            ^FO40,115^GB500,80,6^FS // 2:1
            ^FO60,145^A0N,30,30^FDAREA^FS
            ^FO40,115^GB250,80,6^FS // 2:2
            ^FO300,145^A0N,30,30^FD{postRFIDLabeldto.Area}^FS
            ^FO535,115^GB665,80,6^FS // 2:3-4
            ^FO535,115^GB330,80,6^FS // 2:3
            ^FO550,145^A0N,30,30^FDFECHA^FS
            ^FO880,145^A0N,30,30^FD{date}^FS
            ^FO40,190^GB250,80,6^FS // 3:1
            ^FO60,215^A0N,30,30^FDPRODUCTO^FS
            ^FO285,190^GB915,80,6^FS // 3:2
            ^FO300,215^A0N,30,30^FD{postRFIDLabeldto.ClaveProducto} / {postRFIDLabeldto.NombreProducto}^FS
            ^FO40,265^GB250,80,6^FS // 4:1
            ^FO60,295^A0N,30,22^FDEMPACADORA/TURNO^FS
            ^FO285,265^GB915,80,6^FS // 4:2
            ^FO300,295^A0N,30,30^FD{postRFIDLabeldto.Operador} / {postRFIDLabeldto.Turno}^FS
            ^FO40,340^GB500,100,6^FS // 5:1
            ^FO60,375^A0N,30,30^FDPESO BRUTO(KG)^FS
            ^FO40,340^GB250,100,6^FS // 5:2
            ^FO300,375^A0N,30,30^FD{postRFIDLabeldto.PesoBruto}^FS
            ^FO535,340^GB665,100,6^FS // 5:3-4
            ^FO535,340^GB330,100,6^FS // 5:3
            ^FO550,375^A0N,30,30^FDPESO NETO(KG)^FS
            ^FO880,375^A0N,30,30^FD{postRFIDLabeldto.PesoNeto}^FS
            ^FO40,435^GB500,100,6^FS // 6:1
            ^FO60,470^A0N,30,27^FDPESO TARIMAA(KG)^FS
            ^FO40,435^GB250,100,6^FS // 6:2
            ^FO300,470^A0N,30,30^FD{postRFIDLabeldto.PesoTarima}^FS
            ^FO535,435^GB665,100,6^FS // 6:3-4
            ^FO535,435^GB330,100,6^FS // 6:3
            ^FO550,470^A0N,30,27^FD#PIEZAS^FS
            ^FO880,470^A0N,30,27^FD{postRFIDLabeldto.Piezas}^FS
            ^FO40,530^GB250,125,6^FS // 7:1
            ^FO60,565^A0N,30,27^FDCODIGO DE^FS
            ^FO60,595^A0N,30,27^FDTRAZABILIDAD^FS
            ^FO285,530^GB580,125,6^FS // 7:1
            ^FO300,595^A0N,30,30^FD{postRFIDLabeldto.Trazabilidad}^FS
            ^FO40,650^GB250,150,6^FS // 8:1
            ^FO60,710^A0N,30,27^FDOT Y/O LOTE^FS
            ^FO285,650^GB580,150,6^FS // 8:1
            ^FO300,710^A0N,30,30^FD{postRFIDLabeldto.Orden}^FS
            ^FO40,795^GB250,65,6^FS // 9:1
            ^FO60,815^A0N,30,27^FDFECHA: {date}^FS
            ^FO285,795^GB580,65,6^FS // 9:1
            ^FO300,815^A0N,30,27^FDREVISION: 01^FS
            ^RFW,H,1,8,4^FD{postRFIDLabeldto.RFID}^FS
^FO900,550^BQN,1,4^FDQA^FD00{postRFIDLabeldto.Trazabilidad} - OT y/o lote: {postRFIDLabeldto.Orden}, Producto: {postRFIDLabeldto.ClaveProducto} - {postRFIDLabeldto.NombreProducto}, Clave Producto: {postRFIDLabeldto.ClaveProducto}, Peso bruto: {postRFIDLabeldto.PesoBruto}, Peso neto: {postRFIDLabeldto.PesoNeto}, Peso Tarima: {postRFIDLabeldto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postRFIDLabeldto.Piezas}, Área: {postRFIDLabeldto.Area}, Fecha: {date}, Operador: {postRFIDLabeldto.Operador}, Turno: {postRFIDLabeldto.Turno}^FS
            ^XZ";

            // Enviar el comando a la impresora solo si el guardado es exitoso
            bool result = USBSender.SendSATOCommand(stringResult);
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
            _logger.LogError(ex, "An error occurred while saving the label data or sending the SATO command.");
            return StatusCode(500, $"An error occurred while saving the label data or sending the SATO command: {ex.Message}");
        }
    }


}
