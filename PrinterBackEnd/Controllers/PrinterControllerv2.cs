using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;
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

    public static string ReplaceSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string normalizedString = input.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                if (c == 'ñ')
                {
                    stringBuilder.Append('n');
                }
                else if (c == 'Ñ')
                {
                    stringBuilder.Append('N');
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Post method to send command to SATO printer
    [HttpPost("SendSATOCommand")]
    public async Task<IActionResult> SendSATOCommand(PostRFIDLabeldto postRFIDLabeldto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");


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
                UOM = postRFIDLabeldto.UOM,
                Fecha = postRFIDLabeldto.Fecha

            };

            var date = postRFIDLabel.Fecha.ToString("dd-MM-yy", cultureInfo);

            // Before saving the object, check if Trazabilidad already exists in the database
            var trazabilidadExists = await _context.ProdEtiquetasRFID.AnyAsync(x => x.Trazabilidad == postRFIDLabeldto.Trazabilidad);

            if (!trazabilidadExists)
            {
                // Agregar el objeto 'ProdEtiquetasRFID' a la tabla 'ProdEtiquetasRFID'
                _context.ProdEtiquetasRFID.Add(postRFIDLabel);
                await _context.SaveChangesAsync();
            }

            // Process strings to replace special characters
            postRFIDLabeldto.Area = ReplaceSpecialCharacters(postRFIDLabeldto.Area);
            postRFIDLabeldto.ClaveProducto = ReplaceSpecialCharacters(postRFIDLabeldto.ClaveProducto);
            postRFIDLabeldto.NombreProducto = ReplaceSpecialCharacters(postRFIDLabeldto.NombreProducto);
            postRFIDLabeldto.Operador = ReplaceSpecialCharacters(postRFIDLabeldto.Operador);

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
            ^FO300,375^A0N,50,50^FD{postRFIDLabeldto.PesoBruto}^FS
            ^FO535,340^GB665,100,6^FS // 5:3-4
            ^FO535,340^GB330,100,6^FS // 5:3
            ^FO550,375^A0N,30,30^FDPESO NETO(KG)^FS
            ^FO880,375^A0N,50,50^FD{postRFIDLabeldto.PesoNeto}^FS
            ^FO40,435^GB500,100,6^FS // 6:1
            ^FO60,470^A0N,30,27^FDPESO TARIMA(KG)^FS
            ^FO40,435^GB250,100,6^FS // 6:2
            ^FO300,470^A0N,50,50^FD{postRFIDLabeldto.PesoTarima}^FS
            ^FO535,435^GB665,100,6^FS // 6:3-4
            ^FO535,435^GB330,100,6^FS // 6:3
            ^FO550,470^A0N,30,27^FD{postRFIDLabeldto.UOM}^FS
            ^FO880,470^A0N,50,50^FD{postRFIDLabeldto.Piezas}^FS
            ^FO40,530^GB250,125,6^FS // 7:1
            ^FO60,565^A0N,30,27^FDCODIGO DE^FS
            ^FO60,595^A0N,30,27^FDTRAZABILIDAD^FS
            ^FO285,530^GB580,125,6^FS // 7:1
            ^FO300,595^A0N,50,50^FD{postRFIDLabeldto.Trazabilidad}^FS
            ^FO40,650^GB250,150,6^FS // 8:1
            ^FO60,710^A0N,30,27^FDOT Y/O LOTE^FS
            ^FO285,650^GB580,150,6^FS // 8:1
            ^FO300,710^A0N,50,50^FD{postRFIDLabeldto.Orden}^FS
            ^FO40,795^GB250,65,6^FS // 9:1
            ^FO60,815^A0N,30,27^FDFECHA:11-06-2024^FS
            ^FO285,795^GB580,65,6^FS // 9:1
            ^FO300,815^A0N,27,27^FDREVISION: 01^FS
            ^RFW,H,1,8,4^FD{postRFIDLabeldto.RFID}^FS
            ^FO900,550^BQN,2,4^FDQA^FD000{postRFIDLabeldto.Trazabilidad} - OT y/o lote: {postRFIDLabeldto.Orden}, Producto: {postRFIDLabeldto.ClaveProducto} - {postRFIDLabeldto.NombreProducto}, Clave Producto: {postRFIDLabeldto.ClaveProducto}, Peso bruto: {postRFIDLabeldto.PesoBruto}, Peso neto: {postRFIDLabeldto.PesoNeto}, Peso Tarima: {postRFIDLabeldto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postRFIDLabeldto.Piezas}, Área: {postRFIDLabeldto.Area}, Fecha: {date}, Operador: {postRFIDLabeldto.Operador}, Turno: {postRFIDLabeldto.Turno}^FS
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

    [HttpPost("BioflexImpresora1")]
    public async Task<IActionResult> BioflexImpresora1(PostRFIDLabeldto postRFIDLabeldto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");


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
                UOM = postRFIDLabeldto.UOM,
                Fecha = postRFIDLabeldto.Fecha

            };

            var date = postRFIDLabel.Fecha.ToString("dd-MM-yy", cultureInfo);

            // Before saving the object, check if Trazabilidad already exists in the database
            var trazabilidadExists = await _context.ProdEtiquetasRFID.AnyAsync(x => x.Trazabilidad == postRFIDLabeldto.Trazabilidad);

            if (!trazabilidadExists)
            {
                // Agregar el objeto 'ProdEtiquetasRFID' a la tabla 'ProdEtiquetasRFID'
                _context.ProdEtiquetasRFID.Add(postRFIDLabel);
                await _context.SaveChangesAsync();
            }

            // Process strings to replace special characters
            postRFIDLabeldto.Area = ReplaceSpecialCharacters(postRFIDLabeldto.Area);
            postRFIDLabeldto.ClaveProducto = ReplaceSpecialCharacters(postRFIDLabeldto.ClaveProducto);
            postRFIDLabeldto.NombreProducto = ReplaceSpecialCharacters(postRFIDLabeldto.NombreProducto);
            postRFIDLabeldto.Operador = ReplaceSpecialCharacters(postRFIDLabeldto.Operador);

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
            ^FO300,375^A0N,50,50^FD{postRFIDLabeldto.PesoBruto}^FS
            ^FO535,340^GB665,100,6^FS // 5:3-4
            ^FO535,340^GB330,100,6^FS // 5:3
            ^FO550,375^A0N,30,30^FDPESO NETO(KG)^FS
            ^FO880,375^A0N,50,50^FD{postRFIDLabeldto.PesoNeto}^FS
            ^FO40,435^GB500,100,6^FS // 6:1
            ^FO60,470^A0N,30,27^FDPESO TARIMA(KG)^FS
            ^FO40,435^GB250,100,6^FS // 6:2
            ^FO300,470^A0N,50,50^FD{postRFIDLabeldto.PesoTarima}^FS
            ^FO535,435^GB665,100,6^FS // 6:3-4
            ^FO535,435^GB330,100,6^FS // 6:3
            ^FO550,470^A0N,30,27^FD{postRFIDLabeldto.UOM}^FS
            ^FO880,470^A0N,50,50^FD{postRFIDLabeldto.Piezas}^FS
            ^FO40,530^GB250,125,6^FS // 7:1
            ^FO60,565^A0N,30,27^FDCODIGO DE^FS
            ^FO60,595^A0N,30,27^FDTRAZABILIDAD^FS
            ^FO285,530^GB580,125,6^FS // 7:1
            ^FO300,595^A0N,50,50^FD{postRFIDLabeldto.Trazabilidad}^FS
            ^FO40,650^GB250,150,6^FS // 8:1
            ^FO60,710^A0N,30,27^FDOT Y/O LOTE^FS
            ^FO285,650^GB580,150,6^FS // 8:1
            ^FO300,710^A0N,50,50^FD{postRFIDLabeldto.Orden}^FS
            ^FO40,795^GB250,65,6^FS // 9:1
            ^FO60,815^A0N,30,27^FDFECHA:11-06-2024^FS
            ^FO285,795^GB580,65,6^FS // 9:1
            ^FO300,815^A0N,27,27^FDREVISION: 01^FS
            ^RFW,H,1,8,4^FD{postRFIDLabeldto.RFID}^FS
            ^FO900,550^BQN,2,4^FDQA^FD000{postRFIDLabeldto.Trazabilidad} - OT y/o lote: {postRFIDLabeldto.Orden}, Producto: {postRFIDLabeldto.ClaveProducto} - {postRFIDLabeldto.NombreProducto}, Clave Producto: {postRFIDLabeldto.ClaveProducto}, Peso bruto: {postRFIDLabeldto.PesoBruto}, Peso neto: {postRFIDLabeldto.PesoNeto}, Peso Tarima: {postRFIDLabeldto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postRFIDLabeldto.Piezas}, Área: {postRFIDLabeldto.Area}, Fecha: {date}, Operador: {postRFIDLabeldto.Operador}, Turno: {postRFIDLabeldto.Turno}^FS
            ^XZ";

            // Enviar el comando a la impresora solo si el guardado es exitoso
            bool result = USBSender.SendSATOCommand(stringResult, "172.16.20.57", 9100);
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

    [HttpPost("BioflexImpresora2")]
    public async Task<IActionResult> BioflexImpresora2(PostRFIDLabeldto postRFIDLabeldto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");


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
                UOM = postRFIDLabeldto.UOM,
                Fecha = postRFIDLabeldto.Fecha

            };

            var date = postRFIDLabel.Fecha.ToString("dd-MM-yy", cultureInfo);

            // Before saving the object, check if Trazabilidad already exists in the database
            var trazabilidadExists = await _context.ProdEtiquetasRFID.AnyAsync(x => x.Trazabilidad == postRFIDLabeldto.Trazabilidad);

            if (!trazabilidadExists)
            {
                // Agregar el objeto 'ProdEtiquetasRFID' a la tabla 'ProdEtiquetasRFID'
                _context.ProdEtiquetasRFID.Add(postRFIDLabel);
                await _context.SaveChangesAsync();
            }

            postRFIDLabeldto.Area = ReplaceSpecialCharacters(postRFIDLabeldto.Area);
            postRFIDLabeldto.ClaveProducto = ReplaceSpecialCharacters(postRFIDLabeldto.ClaveProducto);
            postRFIDLabeldto.NombreProducto = ReplaceSpecialCharacters(postRFIDLabeldto.NombreProducto);
            postRFIDLabeldto.Operador = ReplaceSpecialCharacters(postRFIDLabeldto.Operador);

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
            ^FO300,375^A0N,50,50^FD{postRFIDLabeldto.PesoBruto}^FS
            ^FO535,340^GB665,100,6^FS // 5:3-4
            ^FO535,340^GB330,100,6^FS // 5:3
            ^FO550,375^A0N,30,30^FDPESO NETO(KG)^FS
            ^FO880,375^A0N,50,50^FD{postRFIDLabeldto.PesoNeto}^FS
            ^FO40,435^GB500,100,6^FS // 6:1
            ^FO60,470^A0N,30,27^FDPESO TARIMA(KG)^FS
            ^FO40,435^GB250,100,6^FS // 6:2
            ^FO300,470^A0N,50,50^FD{postRFIDLabeldto.PesoTarima}^FS
            ^FO535,435^GB665,100,6^FS // 6:3-4
            ^FO535,435^GB330,100,6^FS // 6:3
            ^FO550,470^A0N,30,27^FD{postRFIDLabeldto.UOM}^FS
            ^FO880,470^A0N,50,50^FD{postRFIDLabeldto.Piezas}^FS
            ^FO40,530^GB250,125,6^FS // 7:1
            ^FO60,565^A0N,30,27^FDCODIGO DE^FS
            ^FO60,595^A0N,30,27^FDTRAZABILIDAD^FS
            ^FO285,530^GB580,125,6^FS // 7:1
            ^FO300,595^A0N,50,50^FD{postRFIDLabeldto.Trazabilidad}^FS
            ^FO40,650^GB250,150,6^FS // 8:1
            ^FO60,710^A0N,30,27^FDOT Y/O LOTE^FS
            ^FO285,650^GB580,150,6^FS // 8:1
            ^FO300,710^A0N,50,50^FD{postRFIDLabeldto.Orden}^FS
            ^FO40,795^GB250,65,6^FS // 9:1
            ^FO60,815^A0N,30,27^FDFECHA:11-06-2024^FS
            ^FO285,795^GB580,65,6^FS // 9:1
            ^FO300,815^A0N,27,27^FDREVISION: 01^FS
            ^RFW,H,1,8,4^FD{postRFIDLabeldto.RFID}^FS
            ^FO900,550^BQN,2,4^FDQA^FD000{postRFIDLabeldto.Trazabilidad} - OT y/o lote: {postRFIDLabeldto.Orden}, Producto: {postRFIDLabeldto.ClaveProducto} - {postRFIDLabeldto.NombreProducto}, Clave Producto: {postRFIDLabeldto.ClaveProducto}, Peso bruto: {postRFIDLabeldto.PesoBruto}, Peso neto: {postRFIDLabeldto.PesoNeto}, Peso Tarima: {postRFIDLabeldto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postRFIDLabeldto.Piezas}, Área: {postRFIDLabeldto.Area}, Fecha: {date}, Operador: {postRFIDLabeldto.Operador}, Turno: {postRFIDLabeldto.Turno}^FS
            ^XZ";

            // Enviar el comando a la impresora solo si el guardado es exitoso
            bool result = USBSender.SendSATOCommand(stringResult, "172.16.20.56", 9100);
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

    [HttpPost("SendSATOCommandNoSave")]
    public async Task<IActionResult> SendSATOCommand2(PostRFIDLabeldto postRFIDLabeldto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");

            var date = postRFIDLabeldto.Fecha.ToString("dd-MM-yy", cultureInfo);

            postRFIDLabeldto.Area = ReplaceSpecialCharacters(postRFIDLabeldto.Area);
            postRFIDLabeldto.ClaveProducto = ReplaceSpecialCharacters(postRFIDLabeldto.ClaveProducto);
            postRFIDLabeldto.NombreProducto = ReplaceSpecialCharacters(postRFIDLabeldto.NombreProducto);
            postRFIDLabeldto.Operador = ReplaceSpecialCharacters(postRFIDLabeldto.Operador);

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
            ^FO300,375^A0N,50,50^FD{postRFIDLabeldto.PesoBruto}^FS
            ^FO535,340^GB665,100,6^FS // 5:3-4
            ^FO535,340^GB330,100,6^FS // 5:3
            ^FO550,375^A0N,30,30^FDPESO NETO(KG)^FS
            ^FO880,375^A0N,50,50^FD{postRFIDLabeldto.PesoNeto}^FS
            ^FO40,435^GB500,100,6^FS // 6:1
            ^FO60,470^A0N,30,27^FDPESO TARIMA(KG)^FS
            ^FO40,435^GB250,100,6^FS // 6:2
            ^FO300,470^A0N,50,50^FD{postRFIDLabeldto.PesoTarima}^FS
            ^FO535,435^GB665,100,6^FS // 6:3-4
            ^FO535,435^GB330,100,6^FS // 6:3
            ^FO550,470^A0N,30,27^FD{postRFIDLabeldto.UOM}^FS
            ^FO880,470^A0N,50,50^FD{postRFIDLabeldto.Piezas}^FS
            ^FO40,530^GB250,125,6^FS // 7:1
            ^FO60,565^A0N,30,27^FDCODIGO DE^FS
            ^FO60,595^A0N,30,27^FDTRAZABILIDAD^FS
            ^FO285,530^GB580,125,6^FS // 7:1
            ^FO300,595^A0N,50,50^FD{postRFIDLabeldto.Trazabilidad}^FS
            ^FO40,650^GB250,150,6^FS // 8:1
            ^FO60,710^A0N,30,27^FDOT Y/O LOTE^FS
            ^FO285,650^GB580,150,6^FS // 8:1
            ^FO300,710^A0N,50,50^FD{postRFIDLabeldto.Orden}^FS
            ^FO40,795^GB250,65,6^FS // 9:1
            ^FO60,815^A0N,20,20^FDFECHA: 11-06-2024^FS
            ^FO285,795^GB580,65,6^FS // 9:1
            ^FO300,815^A0N,20,27^FDREVISION: 01^FS
            ^RFW,H,1,8,4^FD{postRFIDLabeldto.RFID}^FS
            ^FO900,550^BQN,2,4^FDQA^FD000{postRFIDLabeldto.Trazabilidad} - OT y/o lote: {postRFIDLabeldto.Orden}, Producto: {postRFIDLabeldto.ClaveProducto} - {postRFIDLabeldto.NombreProducto}, Clave Producto: {postRFIDLabeldto.ClaveProducto}, Peso bruto: {postRFIDLabeldto.PesoBruto}, Peso neto: {postRFIDLabeldto.PesoNeto}, Peso Tarima: {postRFIDLabeldto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postRFIDLabeldto.Piezas}, Área: {postRFIDLabeldto.Area}, Fecha: {date}, Operador: {postRFIDLabeldto.Operador}, Turno: {postRFIDLabeldto.Turno}^FS
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

    // POST METHOD TO PRINT ProdExtrasDestiny LABEL
    [HttpPost("SendSATOCommandProdExtrasDestiny")]
    public async Task<IActionResult> SendSATOCommandProdExtrasDestiny(PostDestinyLabelDto postDestinyLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Destiny
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdDestiny = maxId.Find(x => x.Tarima == "DESTINY");

            if (maxIdDestiny == null)
            {
                return NotFound("No se encontró el registro con Tarima = DESTINY");
            }

            // Normalizar los valores del DTO
            postDestinyLabelDto.Area = ReplaceSpecialCharacters(postDestinyLabelDto.Area);
            postDestinyLabelDto.ClaveProducto = ReplaceSpecialCharacters(postDestinyLabelDto.ClaveProducto);
            postDestinyLabelDto.NombreProducto = ReplaceSpecialCharacters(postDestinyLabelDto.NombreProducto);
            postDestinyLabelDto.Operador = ReplaceSpecialCharacters(postDestinyLabelDto.Operador);

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postDestinyLabelDto.Area,
                Fecha = DateTime.Now,
                ClaveProducto = postDestinyLabelDto.ClaveProducto,
                NombreProducto = postDestinyLabelDto.NombreProducto,
                ClaveOperador = postDestinyLabelDto.ClaveOperador,
                Operador = postDestinyLabelDto.Operador,
                Turno = postDestinyLabelDto.Turno,
                PesoTarima = postDestinyLabelDto.PesoTarima,
                PesoBruto = postDestinyLabelDto.PesoBruto,
                PesoNeto = postDestinyLabelDto.PesoNeto,
                Piezas = postDestinyLabelDto.Piezas,
                Trazabilidad = postDestinyLabelDto.Trazabilidad,
                Orden = postDestinyLabelDto.Orden,
                RFID = postDestinyLabelDto.RFID,
                Status = postDestinyLabelDto.Status
            };

            // Agregar y guardar ProdEtiquetasRFID
            _context.ProdEtiquetasRFID.Add(prodEtiquetaBioflex);
            await _context.SaveChangesAsync();

            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasDestiny
            var postRFIDLabel = new ProdExtrasDestiny
            {
                Id = maxIdDestiny.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                ShippingUnits = postDestinyLabelDto.postExtraDestinyDto.ShippingUnits,
                UOM = postDestinyLabelDto.postExtraDestinyDto.UOM,
                InventoryLot = postDestinyLabelDto.postExtraDestinyDto.InventoryLot,
                IndividualUnits = postDestinyLabelDto.postExtraDestinyDto.IndividualUnits,
                PalletId = postDestinyLabelDto.postExtraDestinyDto.PalletId,
                CustomerPo = postDestinyLabelDto.postExtraDestinyDto.CustomerPo,
                TotalUnits = postDestinyLabelDto.postExtraDestinyDto.TotalUnits,
                ProductDescription = postDestinyLabelDto.postExtraDestinyDto.ProductDescription,
                ItemNumber = postDestinyLabelDto.postExtraDestinyDto.ItemNumber
            };

            // Actualizar maxIdDestiny y agregar ProdExtrasDestiny
            maxIdDestiny.MaxId += 1;
            _context.MaxIds.Update(maxIdDestiny);
            _context.ProdExtrasDestiny.Add(postRFIDLabel);

            await _context.SaveChangesAsync();

            // Generar y enviar el comando SATO
            string stringResult = $@"
            ^XA
            ^FO40,40^GB1160,820,6^FS   // Dibuja una caja alrededor de todo el contenido
            ^FO40,40^GB400,105,6^FS // Arriba izquierda
            ^FO90,79^A0N,40,40^FDPALLET PLACARD^FS //LABEL TARIMA
            ^FO435,40^GB380,105,6^FS // Arriba en medio
            ^FO475,59^A0N,25,25^FDSHIPPING UNITS/PALLET^FS
            ^FO590,89^A0N,50,50^FD{postDestinyLabelDto.postExtraDestinyDto.ShippingUnits}^FS //label piezas
            ^FO812,40^GB385,105,6^FS // Arriba derecha
            ^FO940,89^A0N,45,45^FDCASES^FS //label cases
            ^FO870,59^A0N,25,25^FD{postDestinyLabelDto.postExtraDestinyDto.UOM}^FS
            ^FO40,140^GB400,105,6^FS // 2:1 segundo nivel izquierda
            ^FO55,153^A0N,25,25^FDINVENTORY LOT^FS
            ^FO175,190^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.InventoryLot}^FS //label lote
            ^FO435,140^GB760,105,6^FS // 2:1 segundo nivel largo
            ^FO475,153^A0N,25,25^FDQTY/UOM (EACHES)^FS
            ^FO725,190^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.IndividualUnits}^FS //label piezas por caja
            ^FO40,240^GB400,100,6^FS // 2:1 tercer nivel izquierda
            ^FO55,253^A0N,25,25^FDPALLET ID^FS //label pallet id
            ^FO175,290^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.PalletId}^FS //label pallet id
            ^FO55,353^A0N,25,25^FDCUSTOMER PO^FS //label customer po
            ^FO175,390^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.CustomerPo}^FS //label pallet id
            ^FO475,253^A0N,25,25^FDTOTAL QTY/PALLET (EACHES)^FS
            ^FO685,310^BY3 // coordenadas
            ^BCN,65,Y,N,N // Define un código de barras de tipo Code 128
            ^FD{postDestinyLabelDto.postExtraDestinyDto.PalletId}^FS //contenido
            ^FO40,335^GB400,100,6^FS // 2:1 cuarto nivel izquierda
            ^FO55,445^A0N,25,25^FDITEM DESCRIPTION^FS
            ^FO235,475^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.ProductDescription}^FS
            ^FO435,239^GB765,196,6^FS // 2:1 tercer y cuarto nivel derecha
            ^FO55,545^A0N,25,25^FDITEM #^FS
            ^FO95,580^BY3 // coordenadas
            ^BCN,65,Y,N,N // Define un código de barras de tipo Code 128
            ^FD{postDestinyLabelDto.postExtraDestinyDto.ItemNumber}^FS //contenido
            ^FO40,430^GB1160,100,6^FS // 2:1 5to nivel largo
            ^FO95,720^A0N,25,25^FDGROSS WEIGHT ^FS
            ^FO125,770^A0N,65,65^FD{postDestinyLabelDto.PesoBruto} ^FS //peso bruto
            ^FO425,720^A0N,25,25^FDNET WEIGHT ^FS
            ^FO445,770^A0N,65,65^FD{postDestinyLabelDto.PesoNeto} ^FS //peso neto
            ^FO40,523^GB715,170,6^FS // 2:1 6to nivel izquierda
            ^FO40,688^GB715,170,6^FS // 2:1 6to nivel izquierda
            // Código QR en la esquina inferior derecha
            ^FO838,545^BQN,2,4^FDQA^FD000{postDestinyLabelDto.Trazabilidad} - OT y/o lote: {postDestinyLabelDto.Orden}, Producto: {postDestinyLabelDto.ClaveProducto} - {postDestinyLabelDto.NombreProducto}, Clave Producto: {postDestinyLabelDto.ClaveProducto}, Peso bruto: {postDestinyLabelDto.PesoBruto}, Peso neto: {postDestinyLabelDto.PesoNeto}, Peso Tarima: {postDestinyLabelDto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postDestinyLabelDto.Piezas}, Área: {postDestinyLabelDto.Area}, Fecha: {date}, Operador: {postDestinyLabelDto.Operador}, Turno: {postDestinyLabelDto.Turno}^FS
            // EPC Hex
            ^RFW,H,1,8,4^FD{postDestinyLabelDto.RFID}^FS
            ^XZ";

            bool result = USBSender.SendSATOCommand(stringResult);
            if (result)
            {
                return Ok(postRFIDLabel);
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

    [HttpPost("TestSaveDestiny")]
    public async Task<IActionResult> TestSaveDestiny(PostDestinyLabelDto postDestinyLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Destiny
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdDestiny = maxId.Find(x => x.Tarima == "DESTINY");

            if (maxIdDestiny == null)
            {
                return NotFound("No se encontró el registro con Tarima = DESTINY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postDestinyLabelDto.Area,
                Fecha = DateTime.Now,
                ClaveProducto = postDestinyLabelDto.ClaveProducto,
                NombreProducto = postDestinyLabelDto.NombreProducto,
                ClaveOperador = postDestinyLabelDto.ClaveOperador,
                Operador = postDestinyLabelDto.Operador,
                Turno = postDestinyLabelDto.Turno,
                PesoTarima = postDestinyLabelDto.PesoTarima,
                PesoBruto = postDestinyLabelDto.PesoBruto,
                PesoNeto = postDestinyLabelDto.PesoNeto,
                Piezas = postDestinyLabelDto.Piezas,
                Trazabilidad = postDestinyLabelDto.Trazabilidad,
                Orden = postDestinyLabelDto.Orden,
                RFID = postDestinyLabelDto.RFID,
                Status = postDestinyLabelDto.Status
            };

            // Agregar y guardar ProdEtiquetasRFID
            _context.ProdEtiquetasRFID.Add(prodEtiquetaBioflex);
            await _context.SaveChangesAsync();

            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasDestiny
            var postRFIDLabel = new ProdExtrasDestiny
            {
                Id = maxIdDestiny.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                ShippingUnits = postDestinyLabelDto.postExtraDestinyDto.ShippingUnits,
                UOM = postDestinyLabelDto.postExtraDestinyDto.UOM,
                InventoryLot = postDestinyLabelDto.postExtraDestinyDto.InventoryLot,
                IndividualUnits = postDestinyLabelDto.postExtraDestinyDto.IndividualUnits,
                PalletId = postDestinyLabelDto.postExtraDestinyDto.PalletId,
                CustomerPo = postDestinyLabelDto.postExtraDestinyDto.CustomerPo,
                TotalUnits = postDestinyLabelDto.postExtraDestinyDto.TotalUnits,
                ProductDescription = postDestinyLabelDto.postExtraDestinyDto.ProductDescription,
                ItemNumber = postDestinyLabelDto.postExtraDestinyDto.ItemNumber
            };

            // Actualizar maxIdDestiny y agregar ProdExtrasDestiny
            maxIdDestiny.MaxId += 1;
            _context.MaxIds.Update(maxIdDestiny);
            _context.ProdExtrasDestiny.Add(postRFIDLabel);

            await _context.SaveChangesAsync();

            return Ok(postRFIDLabel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving the label data or sending the SATO command.");
            return StatusCode(500, $"An error occurred while saving the label data or sending the SATO command: {ex.Message}");
        }
    }

    [HttpPost("TestPrintDestiny")]
    public async Task<IActionResult> TestPrintDestiny(PostDestinyLabelDto postDestinyLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Destiny
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdDestiny = maxId.Find(x => x.Tarima == "DESTINY");

            if (maxIdDestiny == null)
            {
                return NotFound("No se encontró el registro con Tarima = DESTINY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postDestinyLabelDto.Area,
                Fecha = DateTime.Now,
                ClaveProducto = postDestinyLabelDto.ClaveProducto,
                NombreProducto = postDestinyLabelDto.NombreProducto,
                ClaveOperador = postDestinyLabelDto.ClaveOperador,
                Operador = postDestinyLabelDto.Operador,
                Turno = postDestinyLabelDto.Turno,
                PesoTarima = postDestinyLabelDto.PesoTarima,
                PesoBruto = postDestinyLabelDto.PesoBruto,
                PesoNeto = postDestinyLabelDto.PesoNeto,
                Piezas = postDestinyLabelDto.Piezas,
                Trazabilidad = postDestinyLabelDto.Trazabilidad,
                Orden = postDestinyLabelDto.Orden,
                RFID = postDestinyLabelDto.RFID,
                Status = postDestinyLabelDto.Status
            };

            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasDestiny
            var postRFIDLabel = new ProdExtrasDestiny
            {
                Id = maxIdDestiny.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                ShippingUnits = postDestinyLabelDto.postExtraDestinyDto.ShippingUnits,
                UOM = postDestinyLabelDto.postExtraDestinyDto.UOM,
                InventoryLot = postDestinyLabelDto.postExtraDestinyDto.InventoryLot,
                IndividualUnits = postDestinyLabelDto.postExtraDestinyDto.IndividualUnits,
                PalletId = postDestinyLabelDto.postExtraDestinyDto.PalletId,
                CustomerPo = postDestinyLabelDto.postExtraDestinyDto.CustomerPo,
                TotalUnits = postDestinyLabelDto.postExtraDestinyDto.TotalUnits,
                ProductDescription = postDestinyLabelDto.postExtraDestinyDto.ProductDescription,
                ItemNumber = postDestinyLabelDto.postExtraDestinyDto.ItemNumber
            };


            // Generar y enviar el comando SATO
            string stringResult = $@"
        ^XA
        ^FO40,40^GB1160,820,6^FS   // Dibuja una caja alrededor de todo el contenido
        ^FO40,40^GB400,105,6^FS // Arriba izquierda
        ^FO90,79^A0N,40,40^FDPALLET PLACARD^FS //LABEL TARIMA
        ^FO435,40^GB380,105,6^FS // Arriba en medio
        ^FO475,59^A0N,25,25^FDSHIPPING UNITS/PALLET^FS
        ^FO590,89^A0N,50,50^FD{postDestinyLabelDto.postExtraDestinyDto.ShippingUnits}^FS //label piezas
        ^FO812,40^GB385,105,6^FS // Arriba derecha
        ^FO940,89^A0N,45,45^FDCASES^FS //label cases
        ^FO870,59^A0N,25,25^FD{postDestinyLabelDto.postExtraDestinyDto.UOM}^FS
        ^FO40,140^GB400,105,6^FS // 2:1 segundo nivel izquierda
        ^FO55,153^A0N,25,25^FDINVENTORY LOT^FS
        ^FO175,190^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.InventoryLot}^FS //label lote
        ^FO435,140^GB760,105,6^FS // 2:1 segundo nivel largo
        ^FO475,153^A0N,25,25^FDQTY/UOM (EACHES)^FS
        ^FO725,190^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.IndividualUnits}^FS //label piezas por caja
        ^FO40,240^GB400,100,6^FS // 2:1 tercer nivel izquierda
        ^FO55,253^A0N,25,25^FDPALLET ID^FS //label pallet id
        ^FO175,290^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.PalletId}^FS //label pallet id
        ^FO55,353^A0N,25,25^FDCUSTOMER PO^FS //label customer po
        ^FO175,390^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.CustomerPo}^FS //label pallet id
        ^FO475,253^A0N,25,25^FDTOTAL QTY/PALLET (EACHES)^FS
        ^FO685,310^BY3 // coordenadas
        ^BCN,65,Y,N,N // Define un código de barras de tipo Code 128
        ^FD{postDestinyLabelDto.postExtraDestinyDto.PalletId}^FS //contenido
        ^FO40,335^GB400,100,6^FS // 2:1 cuarto nivel izquierda
        ^FO55,445^A0N,25,25^FDITEM DESCRIPTION^FS
        ^FO235,475^A0N,45,45^FD{postDestinyLabelDto.postExtraDestinyDto.ProductDescription}^FS
        ^FO435,239^GB765,196,6^FS // 2:1 tercer y cuarto nivel derecha
        ^FO55,545^A0N,25,25^FDITEM #^FS
        ^FO95,580^BY3 // coordenadas
        ^BCN,65,Y,N,N // Define un código de barras de tipo Code 128
        ^FD{postDestinyLabelDto.postExtraDestinyDto.ItemNumber}^FS //contenido
        ^FO40,430^GB1160,100,6^FS // 2:1 5to nivel largo
        ^FO95,720^A0N,25,25^FDGROSS WEIGHT ^FS
        ^FO125,770^A0N,65,65^FD{postDestinyLabelDto.PesoBruto} ^FS //peso bruto
        ^FO425,720^A0N,25,25^FDNET WEIGHT ^FS
        ^FO445,770^A0N,65,65^FD{postDestinyLabelDto.PesoNeto} ^FS //peso neto
        ^FO40,523^GB715,170,6^FS // 2:1 6to nivel izquierda
        ^FO40,688^GB715,170,6^FS // 2:1 6to nivel izquierda
        // Código QR en la esquina inferior derecha
        ^FO838,545^BQN,2,4^FDQA^FD000{postDestinyLabelDto.Trazabilidad} - OT y/o lote: {postDestinyLabelDto.Orden}, Producto: {postDestinyLabelDto.ClaveProducto} - {postDestinyLabelDto.NombreProducto}, Clave Producto: {postDestinyLabelDto.ClaveProducto}, Peso bruto: {postDestinyLabelDto.PesoBruto}, Peso neto: {postDestinyLabelDto.PesoNeto}, Peso Tarima: {postDestinyLabelDto.PesoTarima}, #Piezas (rollos, bultos, cajas): {postDestinyLabelDto.Piezas}, Área: {postDestinyLabelDto.Area}, Fecha: {date}, Operador: {postDestinyLabelDto.Operador}, Turno: {postDestinyLabelDto.Turno}^FS
        // EPC Hex
        ^RFW,H,1,8,4^FD{postDestinyLabelDto.RFID}^FS
        ^XZ";

            bool result = USBSender.SendSATOCommand(stringResult);
            if (result)
            {
                return Ok(postRFIDLabel);
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

    //POST METHOD TO PRINT ProdExtrasQuality LABEL
    [HttpPost("SendSATOCommandProdExtrasQuality")]
    public async Task<IActionResult> SendSATOCommandProdExtrasQuality(PostQualityLabelDto postQualityLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Quality
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdQuality = maxId.Find(x => x.Tarima == "QUALITY");

            if (maxIdQuality == null)
            {
                return NotFound("No se encontró el registro con Tarima = QUALITY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postQualityLabelDto.Area,
                Fecha = postQualityLabelDto.Fecha,
                ClaveProducto = postQualityLabelDto.ClaveProducto,
                NombreProducto = postQualityLabelDto.NombreProducto,
                ClaveOperador = postQualityLabelDto.ClaveOperador,
                Operador = postQualityLabelDto.Operador,
                Turno = postQualityLabelDto.Turno,
                PesoTarima = postQualityLabelDto.PesoTarima,
                PesoBruto = postQualityLabelDto.PesoBruto,
                PesoNeto = postQualityLabelDto.PesoNeto,
                Piezas = postQualityLabelDto.Piezas,
                Trazabilidad = postQualityLabelDto.Trazabilidad,
                Orden = postQualityLabelDto.Orden,
                RFID = postQualityLabelDto.RFID,
                Status = postQualityLabelDto.Status
            };

            // Agregar y guardar ProdEtiquetasRFID
            _context.ProdEtiquetasRFID.Add(prodEtiquetaBioflex);
            await _context.SaveChangesAsync();

            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasQuality
            var postRFIDLabel = new ProdExtrasQuality
            {
                Id = maxIdQuality.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                IndividualUnits = postQualityLabelDto.postExtraQuality.IndividualUnits,
                ItemNumber = postQualityLabelDto.postExtraQuality.ItemNumber,
                ItemDescription = postQualityLabelDto.postExtraQuality.ItemDescription,
                TotalUnits = postQualityLabelDto.postExtraQuality.TotalUnits,
                ShippingUnits = postQualityLabelDto.postExtraQuality.ShippingUnits,
                InventoryLot = postQualityLabelDto.postExtraQuality.InventoryLot,
                Customer = postQualityLabelDto.postExtraQuality.Customer,
                Traceability = postQualityLabelDto.postExtraQuality.Traceability,
            };

            // Actualizar maxIdQuality y agregar ProdExtrasQuality
            maxIdQuality.MaxId += 1;
            _context.MaxIds.Update(maxIdQuality);
            _context.ProdExtrasQuality.Add(postRFIDLabel);

            await _context.SaveChangesAsync();

            // Normaliza los valores del DTO
            postQualityLabelDto.Area = ReplaceSpecialCharacters(postQualityLabelDto.Area);
            postQualityLabelDto.ClaveProducto = ReplaceSpecialCharacters(postQualityLabelDto.ClaveProducto);
            postQualityLabelDto.NombreProducto = ReplaceSpecialCharacters(postQualityLabelDto.NombreProducto);
            postQualityLabelDto.Operador = ReplaceSpecialCharacters(postQualityLabelDto.Operador);
            postRFIDLabel.Customer = ReplaceSpecialCharacters(postRFIDLabel.Customer);
            postRFIDLabel.ItemDescription = ReplaceSpecialCharacters(postRFIDLabel.ItemDescription);

            // Generar y enviar el comando SATO
            string stringResult = $@"
            ^XA
            ^FO410,34^GFA,9000,9000,50,,::::::::::::hO0FF,hL01LF,hJ01PF8,hH03TFC,h07OF00MFE,gX0SFE007LFE,gV0XF007MF,gS01gHF003MF8,gQ03gLF801MFC,gO07gPFC00MFC,gM07gTFE00MFE,gK0gYFE007MF,gH01hJF003MF8,g03hNF801MF8,X03hRFC01MFC,V07hVFC00MFE,T0iGFE007MF,Q01hFE07gIF003MF,O01hHFE007gJF803MF8,M03gSFE01NFE007gLF801MFC,K07gUF001NFE007gNFC00MFE,J01gVFI0NFE007gPFE007KF8,J01gVFI0NFE007MFE03gIF007IF8,J01gMFE01LFI0NFE007MFE007gJF003F8,J01gMF001KFEI0NFE007MFE007FE0gIF808,J01gMF001KFEI0NFE007MFE007FEI07gHF8,J01gGF8KF001KFEI07MFE007MFE007FEK03gF8,J01YFC00KF001KFEI07MFE007MFE007FEM01XF8,J01YFC00KF001KFCI07MFE007MFE007FEO01VF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F87SF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F800RF8,J01OF8003LFC00KF001KFCI03MFE007MFE007IF8N0F8007QF8,J01NF8J03KFC00KF001KFCI03MFE007MFE007KFCL0FC007QF8,J01MFEL0KFC00KF001KF8I03MFE007MFE007KFCL0FE003KF87JF8,J01MF8L03JFC00KF001KF8I03MFE007MFE007KFC007F00FE001KF003IF8,J01MFM01JFC00KF001KF80401MFE007MFE007KFC007KF001KF003IF8,J01LFEN0JFC00KF001KF80401MFE007MFE007KFC007KFI0JFE003IF8,J01LFCI03J07IFC00KF001KF00401MFE007MFE007KFC007KF800JFC007IF8,J01LF8003FF8003IFC00KF001KF00401MFE007MFE007KFC007KFC007IFC00JF8,J01LF800IFC003IFC00KF001KF00401MFE007MFE007KFC007KFC007IF800JF8,J01LF001IFE001IFC00KF001KF00E00MFE007MFE007KFC007KFE003IF801JF8,J01LF003JF001IFC00KF001KF00E00MFE007MFE007KFC007LF003IF003JF8,J01LF003JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF001IF003JF8,J01LF007JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF801FFE007JF8,J01LF007JF800IFC00KF001JFE00E00MFE007MFE007KFC007LF800FFE007JF8,J01LF007JF800IFC00KF001JFE00E007LFE007MFE007KFC007LFC00FFC00KF8,J01LF007JF800IFC00KF001JFC01E007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF800IFC00KF001JFC01F007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F003KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F007KF8,J01LF007JF801IFC00KF001JFC01F003LFE007MFE007KFC007MF801E007KF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC01E00LF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC00C00LF8,J01LF007JF801IFC00KF001JF803F803LFE007MFE007KFC007MFE00C01LF8,J01LF007JF801IFC00KF001JF803F801LFE007MFE007KFC007MFEJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I07LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I0MF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFCI0MF8,J01LF007JF801IFC00KF001JF007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007OF003MF8,::J01LF007JF801IFC00KF001IFC007FC007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC007FE007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC00FFE007KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IFC00FFE003KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IF800FFE003KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IFE001IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IF8001IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF801FFEI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF800FFCI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFC003FEI03IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFCI07EI07IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFEN07IFC007JF001FFEO0KFE007MFE007KFC007OF003MF8,J01MF8M01IFC007JF003FFE003IFC00KFE007MFE007KFC007OF003MF8,J01MFCN01FFE007JF003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFN01FFE003IFE003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFCM01FFE001IF8007FFC007IFC007JFE007MFE007KFC007OF003MF8,J01OFCI0E001IFI03FEI07FFC007IFC007JFE007MFE007KFC007OF01NF8,J01TF801IF8N0IFC00JFE007JFE007MFE007KFC007YF8,J01UF8JFCM01IFC00JFE007JFE007MFE007KFC007YF8,J01gFEM07IFC00JFE007JFE007MFE007KFC007YF8,J01gGF8L0JF801JFE003JFE007MFE007KFC007YF8,J01gHFK07JF801KF003JFE007JF0FFE007KFC007YF8,J01gHFEI07KF801KF003JFE007FEI0FFE007KFE7gGF8,J01gRF801KF003JFE004K0FFE007gMF08,J01gRF003KF003JFEN0FFE007gJFE0078,J01gRF003KF001JFEN0FFE007gHFC00IF8,J01gRF003KF801JFEN0FFE7gHFC01KF,L03gQFE3KF801JFEN0gJF801KF8,N01gVF801JFEL01gIF003KF8,P01gTF800JFEJ03gHFE007KF8,R01gRFC00JFE003gHFE00LF8,T01gQFE0JFE7gHFC00LF8,V01hWF801LF,Y0hSF003LF,gG0hOF007LF,gI0hJFE007LF,gK0hFC00MF,gM0gVF801LFE,gO07gQF803LFE,gQ07gMF003LFE,gS07gHFE007LFE,gU07XFC00MFE,gW07TFC01MFC,gY03PF801MFC,hG03NF3MFC,hI03RFC,hK03NFC,hM03JF8,hO018,,::::::::::::::Q0FI0C003C00C1803800780300E1801EJ0MFEJ0E0061807800F00300300FC03CI0C18,P01FE01E007F01C3C03801FE0381F1C07F8I0NFI03F80E1C0FF01FE0780381FE07F007C1F,P01FF01E00FF81C3807C03FE0381F1C0FF8I0NFI07FC0E1C0FF81FF0780381FE0FF806003,P01E701E00E381C7807C038F0381F9C0E3CI0NFI071C0E1C0F381C70780381C00E380E0038,P01E781F00E381C7007C038F0381F9C0E3CI0NFI071C0E1C0F3C1C70780381C00E380E003,P01E781F00E381CF007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3807007,P01E783F00E381CE007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3803FFE,P01E783F00E381DC007C038F0381F9C0E1CI0IFC7IFI079C0E1C0F3C1C70780381C00E3,P01E783B00E001DC00EE03800381F9C0EK0IFC7IFI07800E1C0F3C1C70780381C00F,P01E783B80E001FC00EE03800381DDC0EK0IFC7IFI03C00E1C0F3C1C70780381C0078,P01E703B80E001FC00EE039E0381DDC0E78I0FEJ07FI01E00E1C0F381C70780381FC07C,P01EF03380E001FC00EE039F0381DDC0E7CI0FCJ03FJ0F00E1C0F781CF0780381FE03E007IF,P01FF07380E001FE00EE039F0381DDC0E7CI0FCJ03FJ0780E1C0FF81FE0780381FC01EJ07E,P01FC07380E001EE01C7038F0381DDC0E3CI0IFC7IFJ0380E1C0FE01FC0780381CI0FJ0F8,P01E0073C0E001CE01C7038F0381CFC0E3CI0IFC7IFJ03C0E1C0F001C00780381CI078003C,P01E007FC0E001C701FF038F0381CFC0E3CI0IFC7IFJ01C0E1C0F001C00780381CI07801F,P01E007FC0E381C701FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00FFC0E381C781FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00E1C0E381C383C7838F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C383C7838F0381CFC0E3CI0NFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C3C383838F0381C7C0F3CI0NFI079C0F3C0F001C00780381E00E78,P01E00E1E0FF81C1C38383FE0381C7C07F8I0NFI03FC07F80F001C007F8381FE0FF007IF,P01C00E0E07F01C1C38381FC0381C7C03FJ0NFI03F803F00E001C007F8381FE07F007IF,,::::::::::::^FS
            ^FO40,40^GB1160,820,6^FS   // Dibuja una caja alrededor de todo el contenido
            ^FO40,200^GB1160,70,6^FS //fila 2
            ^FO55,230^A0N,25,25^FDCUSTOMER :^FS //label customer
            ^FO235,225^A0N,35,35^FD{postRFIDLabel.Customer}^FS 
            ^FO55,300^A0N,25,25^FDITEM: ^FS //label item
            ^FO235,290^A0N,35,35^FD{postRFIDLabel.ItemDescription}^FS 
            ^FO40,330^GB570,371,6^FS // fila4:1
            ^FO55,360^A0N,25,25^FDQPS ITEM # ^FS //label QPS ITEM #
            // Código QR QPS ITEM #
            ^FO230,410^BQN,10,10^FDQA^FD{postRFIDLabel.ItemNumber}^FS
            ^FO280,650^A0N,33,33^FD{postRFIDLabel.ItemNumber}^FS //label item
            ^FO604,330^GB595,180,6^FS // 4.2
            ^FO620,360^A0N,25,25^FDLOT: ^FS //label item
            // Código QR LOT
            ^FO850,340^BQN,5,5^FDQA^FD{postRFIDLabel.InventoryLot}^FS
            ^FO855,470^A0N,33,33^FD{postRFIDLabel.InventoryLot}^FS //label item
            ^FO620,520^A0N,25,25^FDTotal Qty/Pallet (Eaches)^FS //label total qty/pallet
            // Código QR total qty/pallet eaches
            ^FO850,540^BQN,5,5^FDQA^FD{postRFIDLabel.TotalUnits}^FS
            ^FO860,665^A0N,33,33^FD{postRFIDLabel.TotalUnits}^FS // label total qty/pallet
            ^FO40,695^GB1160,75,6^FS // fila5
            ^FO55,720^A0N,25,25^FDTraceability code:^FS //label traceability code
            ^FO240,715^A0N,35,35^FD{postRFIDLabel.Traceability}^FS 
            ^FO40,764^GB580,96,6^FS // fila5:1
            ^FO55,780^A0N,25,25^FDGROSS WEIGHT: ^FS //label gross weight
            ^FO630,780^A0N,25,25^FDNET WEIGHT: ^FS //label net weight
            ^FO300,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoBruto}^FS //label peso bruto
            ^FO850,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoNeto}^FS //label peso neto
            // EPC Hex
            ^RFW,H,1,8,4^FD{prodEtiquetaBioflex.RFID}^FS
            ^XZ";

            bool result = USBSender.SendSATOCommand(stringResult);
            if (result)
            {
                return Ok(postRFIDLabel);
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

    [HttpPost("TestSaveQuality")]
    public async Task<IActionResult> TestSaveQuality(PostQualityLabelDto postQualityLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Quality
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdQuality = maxId.Find(x => x.Tarima == "QUALITY");

            if (maxIdQuality == null)
            {
                return NotFound("No se encontró el registro con Tarima = QUALITY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postQualityLabelDto.Area,
                Fecha = postQualityLabelDto.Fecha,
                ClaveProducto = postQualityLabelDto.ClaveProducto,
                NombreProducto = postQualityLabelDto.NombreProducto,
                ClaveOperador = postQualityLabelDto.ClaveOperador,
                Operador = postQualityLabelDto.Operador,
                Turno = postQualityLabelDto.Turno,
                PesoTarima = postQualityLabelDto.PesoTarima,
                PesoBruto = postQualityLabelDto.PesoBruto,
                PesoNeto = postQualityLabelDto.PesoNeto,
                Piezas = postQualityLabelDto.Piezas,
                Trazabilidad = postQualityLabelDto.Trazabilidad,
                Orden = postQualityLabelDto.Orden,
                RFID = postQualityLabelDto.RFID,
                Status = postQualityLabelDto.Status
            };

            // Agregar y guardar ProdEtiquetasRFID
            _context.ProdEtiquetasRFID.Add(prodEtiquetaBioflex);
            await _context.SaveChangesAsync();

            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasQuality
            var postRFIDLabel = new ProdExtrasQuality
            {
                Id = maxIdQuality.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                IndividualUnits = postQualityLabelDto.postExtraQuality.IndividualUnits,
                ItemNumber = postQualityLabelDto.postExtraQuality.ItemNumber,
                ItemDescription = postQualityLabelDto.postExtraQuality.ItemDescription,
                TotalUnits = postQualityLabelDto.postExtraQuality.TotalUnits,
                ShippingUnits = postQualityLabelDto.postExtraQuality.ShippingUnits,
                InventoryLot = postQualityLabelDto.postExtraQuality.InventoryLot,
                Customer = postQualityLabelDto.postExtraQuality.Customer,
                Traceability = postQualityLabelDto.postExtraQuality.Traceability,
            };

            // Actualizar maxIdQuality y agregar ProdExtrasQuality
            maxIdQuality.MaxId += 1;
            _context.MaxIds.Update(maxIdQuality);
            _context.ProdExtrasQuality.Add(postRFIDLabel);

            await _context.SaveChangesAsync();

            return Ok(postRFIDLabel);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving the label data or sending the SATO command.");
            return StatusCode(500, $"An error occurred while saving the label data or sending the SATO command: {ex.Message}");
        }
    }

    [HttpPost("TestPrintQuality")]
    public async Task<IActionResult> SendSATOCommandProdExtrasQualityNoPrint(PostQualityLabelDto postQualityLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Quality
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdQuality = maxId.Find(x => x.Tarima == "QUALITY");

            if (maxIdQuality == null)
            {
                return NotFound("No se encontró el registro con Tarima = QUALITY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postQualityLabelDto.Area,
                Fecha = postQualityLabelDto.Fecha,
                ClaveProducto = postQualityLabelDto.ClaveProducto,
                NombreProducto = postQualityLabelDto.NombreProducto,
                ClaveOperador = postQualityLabelDto.ClaveOperador,
                Operador = postQualityLabelDto.Operador,
                Turno = postQualityLabelDto.Turno,
                PesoTarima = postQualityLabelDto.PesoTarima,
                PesoBruto = postQualityLabelDto.PesoBruto,
                PesoNeto = postQualityLabelDto.PesoNeto,
                Piezas = postQualityLabelDto.Piezas,
                Trazabilidad = postQualityLabelDto.Trazabilidad,
                Orden = postQualityLabelDto.Orden,
                RFID = postQualityLabelDto.RFID,
                Status = postQualityLabelDto.Status
            };
            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasQuality
            var postRFIDLabel = new ProdExtrasQuality
            {
                Id = maxIdQuality.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                IndividualUnits = postQualityLabelDto.postExtraQuality.IndividualUnits,
                ItemNumber = postQualityLabelDto.postExtraQuality.ItemNumber,
                ItemDescription = postQualityLabelDto.postExtraQuality.ItemDescription,
                TotalUnits = postQualityLabelDto.postExtraQuality.TotalUnits,
                ShippingUnits = postQualityLabelDto.postExtraQuality.ShippingUnits,
                InventoryLot = postQualityLabelDto.postExtraQuality.InventoryLot,
                Customer = postQualityLabelDto.postExtraQuality.Customer,
                Traceability = postQualityLabelDto.postExtraQuality.Traceability,
            };

            // Generar y enviar el comando SATO
            string stringResult = $@"
            ^XA
            ^FO410,34^GFA,9000,9000,50,,::::::::::::hO0FF,hL01LF,hJ01PF8,hH03TFC,h07OF00MFE,gX0SFE007LFE,gV0XF007MF,gS01gHF003MF8,gQ03gLF801MFC,gO07gPFC00MFC,gM07gTFE00MFE,gK0gYFE007MF,gH01hJF003MF8,g03hNF801MF8,X03hRFC01MFC,V07hVFC00MFE,T0iGFE007MF,Q01hFE07gIF003MF,O01hHFE007gJF803MF8,M03gSFE01NFE007gLF801MFC,K07gUF001NFE007gNFC00MFE,J01gVFI0NFE007gPFE007KF8,J01gVFI0NFE007MFE03gIF007IF8,J01gMFE01LFI0NFE007MFE007gJF003F8,J01gMF001KFEI0NFE007MFE007FE0gIF808,J01gMF001KFEI0NFE007MFE007FEI07gHF8,J01gGF8KF001KFEI07MFE007MFE007FEK03gF8,J01YFC00KF001KFEI07MFE007MFE007FEM01XF8,J01YFC00KF001KFCI07MFE007MFE007FEO01VF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F87SF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F800RF8,J01OF8003LFC00KF001KFCI03MFE007MFE007IF8N0F8007QF8,J01NF8J03KFC00KF001KFCI03MFE007MFE007KFCL0FC007QF8,J01MFEL0KFC00KF001KF8I03MFE007MFE007KFCL0FE003KF87JF8,J01MF8L03JFC00KF001KF8I03MFE007MFE007KFC007F00FE001KF003IF8,J01MFM01JFC00KF001KF80401MFE007MFE007KFC007KF001KF003IF8,J01LFEN0JFC00KF001KF80401MFE007MFE007KFC007KFI0JFE003IF8,J01LFCI03J07IFC00KF001KF00401MFE007MFE007KFC007KF800JFC007IF8,J01LF8003FF8003IFC00KF001KF00401MFE007MFE007KFC007KFC007IFC00JF8,J01LF800IFC003IFC00KF001KF00401MFE007MFE007KFC007KFC007IF800JF8,J01LF001IFE001IFC00KF001KF00E00MFE007MFE007KFC007KFE003IF801JF8,J01LF003JF001IFC00KF001KF00E00MFE007MFE007KFC007LF003IF003JF8,J01LF003JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF001IF003JF8,J01LF007JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF801FFE007JF8,J01LF007JF800IFC00KF001JFE00E00MFE007MFE007KFC007LF800FFE007JF8,J01LF007JF800IFC00KF001JFE00E007LFE007MFE007KFC007LFC00FFC00KF8,J01LF007JF800IFC00KF001JFC01E007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF800IFC00KF001JFC01F007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F003KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F007KF8,J01LF007JF801IFC00KF001JFC01F003LFE007MFE007KFC007MF801E007KF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC01E00LF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC00C00LF8,J01LF007JF801IFC00KF001JF803F803LFE007MFE007KFC007MFE00C01LF8,J01LF007JF801IFC00KF001JF803F801LFE007MFE007KFC007MFEJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I07LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I0MF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFCI0MF8,J01LF007JF801IFC00KF001JF007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007OF003MF8,::J01LF007JF801IFC00KF001IFC007FC007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC007FE007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC00FFE007KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IFC00FFE003KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IF800FFE003KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IFE001IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IF8001IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF801FFEI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF800FFCI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFC003FEI03IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFCI07EI07IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFEN07IFC007JF001FFEO0KFE007MFE007KFC007OF003MF8,J01MF8M01IFC007JF003FFE003IFC00KFE007MFE007KFC007OF003MF8,J01MFCN01FFE007JF003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFN01FFE003IFE003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFCM01FFE001IF8007FFC007IFC007JFE007MFE007KFC007OF003MF8,J01OFCI0E001IFI03FEI07FFC007IFC007JFE007MFE007KFC007OF01NF8,J01TF801IF8N0IFC00JFE007JFE007MFE007KFC007YF8,J01UF8JFCM01IFC00JFE007JFE007MFE007KFC007YF8,J01gFEM07IFC00JFE007JFE007MFE007KFC007YF8,J01gGF8L0JF801JFE003JFE007MFE007KFC007YF8,J01gHFK07JF801KF003JFE007JF0FFE007KFC007YF8,J01gHFEI07KF801KF003JFE007FEI0FFE007KFE7gGF8,J01gRF801KF003JFE004K0FFE007gMF08,J01gRF003KF003JFEN0FFE007gJFE0078,J01gRF003KF001JFEN0FFE007gHFC00IF8,J01gRF003KF801JFEN0FFE7gHFC01KF,L03gQFE3KF801JFEN0gJF801KF8,N01gVF801JFEL01gIF003KF8,P01gTF800JFEJ03gHFE007KF8,R01gRFC00JFE003gHFE00LF8,T01gQFE0JFE7gHFC00LF8,V01hWF801LF,Y0hSF003LF,gG0hOF007LF,gI0hJFE007LF,gK0hFC00MF,gM0gVF801LFE,gO07gQF803LFE,gQ07gMF003LFE,gS07gHFE007LFE,gU07XFC00MFE,gW07TFC01MFC,gY03PF801MFC,hG03NF3MFC,hI03RFC,hK03NFC,hM03JF8,hO018,,::::::::::::::Q0FI0C003C00C1803800780300E1801EJ0MFEJ0E0061807800F00300300FC03CI0C18,P01FE01E007F01C3C03801FE0381F1C07F8I0NFI03F80E1C0FF01FE0780381FE07F007C1F,P01FF01E00FF81C3807C03FE0381F1C0FF8I0NFI07FC0E1C0FF81FF0780381FE0FF806003,P01E701E00E381C7807C038F0381F9C0E3CI0NFI071C0E1C0F381C70780381C00E380E0038,P01E781F00E381C7007C038F0381F9C0E3CI0NFI071C0E1C0F3C1C70780381C00E380E003,P01E781F00E381CF007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3807007,P01E783F00E381CE007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3803FFE,P01E783F00E381DC007C038F0381F9C0E1CI0IFC7IFI079C0E1C0F3C1C70780381C00E3,P01E783B00E001DC00EE03800381F9C0EK0IFC7IFI07800E1C0F3C1C70780381C00F,P01E783B80E001FC00EE03800381DDC0EK0IFC7IFI03C00E1C0F3C1C70780381C0078,P01E703B80E001FC00EE039E0381DDC0E78I0FEJ07FI01E00E1C0F381C70780381FC07C,P01EF03380E001FC00EE039F0381DDC0E7CI0FCJ03FJ0F00E1C0F781CF0780381FE03E007IF,P01FF07380E001FE00EE039F0381DDC0E7CI0FCJ03FJ0780E1C0FF81FE0780381FC01EJ07E,P01FC07380E001EE01C7038F0381DDC0E3CI0IFC7IFJ0380E1C0FE01FC0780381CI0FJ0F8,P01E0073C0E001CE01C7038F0381CFC0E3CI0IFC7IFJ03C0E1C0F001C00780381CI078003C,P01E007FC0E001C701FF038F0381CFC0E3CI0IFC7IFJ01C0E1C0F001C00780381CI07801F,P01E007FC0E381C701FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00FFC0E381C781FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00E1C0E381C383C7838F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C383C7838F0381CFC0E3CI0NFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C3C383838F0381C7C0F3CI0NFI079C0F3C0F001C00780381E00E78,P01E00E1E0FF81C1C38383FE0381C7C07F8I0NFI03FC07F80F001C007F8381FE0FF007IF,P01C00E0E07F01C1C38381FC0381C7C03FJ0NFI03F803F00E001C007F8381FE07F007IF,,::::::::::::^FS
            ^FO40,40^GB1160,820,6^FS   // Dibuja una caja alrededor de todo el contenido
            ^FO40,200^GB1160,70,6^FS //fila 2
            ^FO55,230^A0N,25,25^FDCUSTOMER :^FS //label customer
            ^FO235,225^A0N,35,35^FD{postRFIDLabel.Customer}^FS 
            ^FO55,300^A0N,25,25^FDITEM: ^FS //label item
            ^FO235,290^A0N,35,35^FD{postRFIDLabel.ItemDescription}^FS 
            ^FO40,330^GB570,371,6^FS // fila4:1
            ^FO55,360^A0N,25,25^FDQPS ITEM # ^FS //label QPS ITEM #
            // Código QR QPS ITEM #
            ^FO230,410^BQN,10,10^FDQA^FD{postRFIDLabel.ItemNumber}^FS
            ^FO280,650^A0N,33,33^FD{postRFIDLabel.ItemNumber}^FS //label item
            ^FO604,330^GB595,180,6^FS // 4.2
            ^FO620,360^A0N,25,25^FDLOT: ^FS //label item
            // Código QR LOT
            ^FO850,340^BQN,5,5^FDQA^FD{postRFIDLabel.InventoryLot}^FS
            ^FO855,470^A0N,33,33^FD{postRFIDLabel.InventoryLot}^FS //label item
            ^FO620,520^A0N,25,25^FDTotal Qty/Pallet (Eaches)^FS //label total qty/pallet
            // Código QR total qty/pallet eaches
            ^FO850,540^BQN,5,5^FDQA^FD{postRFIDLabel.TotalUnits}^FS
            ^FO860,665^A0N,33,33^FD{postRFIDLabel.TotalUnits}^FS // label total qty/pallet
            ^FO40,695^GB1160,75,6^FS // fila5
            ^FO55,720^A0N,25,25^FDTraceability code:^FS //label traceability code
            ^FO240,715^A0N,35,35^FD{postRFIDLabel.Traceability}^FS 
            ^FO40,764^GB580,96,6^FS // fila5:1
            ^FO55,780^A0N,25,25^FDGROSS WEIGHT: ^FS //label gross weight
            ^FO630,780^A0N,25,25^FDNET WEIGHT: ^FS //label net weight
            ^FO300,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoBruto}^FS //label peso bruto
            ^FO850,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoNeto}^FS //label peso neto
            // EPC Hex
            ^RFW,H,1,8,4^FD{prodEtiquetaBioflex.RFID}^FS
            ^XZ";

            bool result = USBSender.SendSATOCommand(stringResult);
            if (result)
            {
                return Ok(postRFIDLabel);
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

    [HttpPost("TestPrintInternet")]
    public async Task<IActionResult> TestPrintInternet(PostQualityLabelDto postQualityLabelDto)
    {
        try
        {
            CultureInfo cultureInfo = new CultureInfo("es-MX");
            var date = DateTime.Now.ToString("dd-MM-yy", cultureInfo);

            // Verificar el MaxId de Quality
            var maxId = await _context.MaxIds.ToListAsync();
            var maxIdQuality = maxId.Find(x => x.Tarima == "QUALITY");

            if (maxIdQuality == null)
            {
                return NotFound("No se encontró el registro con Tarima = QUALITY");
            }

            // Crear objeto ProdEtiquetasRFID
            var prodEtiquetaBioflex = new ProdEtiquetasRFID
            {
                Area = postQualityLabelDto.Area,
                Fecha = postQualityLabelDto.Fecha,
                ClaveProducto = postQualityLabelDto.ClaveProducto,
                NombreProducto = postQualityLabelDto.NombreProducto,
                ClaveOperador = postQualityLabelDto.ClaveOperador,
                Operador = postQualityLabelDto.Operador,
                Turno = postQualityLabelDto.Turno,
                PesoTarima = postQualityLabelDto.PesoTarima,
                PesoBruto = postQualityLabelDto.PesoBruto,
                PesoNeto = postQualityLabelDto.PesoNeto,
                Piezas = postQualityLabelDto.Piezas,
                Trazabilidad = postQualityLabelDto.Trazabilidad,
                Orden = postQualityLabelDto.Orden,
                RFID = postQualityLabelDto.RFID,
                Status = postQualityLabelDto.Status
            };
            // Obtener el ID generado para ProdEtiquetasRFID
            var prodEtiquetaRFIDId = prodEtiquetaBioflex.Id;

            // Crear objeto ProdExtrasQuality
            var postRFIDLabel = new ProdExtrasQuality
            {
                Id = maxIdQuality.MaxId + 1,
                prodEtiquetaRFIDId = prodEtiquetaRFIDId,
                IndividualUnits = postQualityLabelDto.postExtraQuality.IndividualUnits,
                ItemNumber = postQualityLabelDto.postExtraQuality.ItemNumber,
                ItemDescription = postQualityLabelDto.postExtraQuality.ItemDescription,
                TotalUnits = postQualityLabelDto.postExtraQuality.TotalUnits,
                ShippingUnits = postQualityLabelDto.postExtraQuality.ShippingUnits,
                InventoryLot = postQualityLabelDto.postExtraQuality.InventoryLot,
                Customer = postQualityLabelDto.postExtraQuality.Customer,
                Traceability = postQualityLabelDto.postExtraQuality.Traceability,
            };

            // Generar y enviar el comando SATO
            string stringResult = $@"
            ^XA
            ^FO410,34^GFA,9000,9000,50,,::::::::::::hO0FF,hL01LF,hJ01PF8,hH03TFC,h07OF00MFE,gX0SFE007LFE,gV0XF007MF,gS01gHF003MF8,gQ03gLF801MFC,gO07gPFC00MFC,gM07gTFE00MFE,gK0gYFE007MF,gH01hJF003MF8,g03hNF801MF8,X03hRFC01MFC,V07hVFC00MFE,T0iGFE007MF,Q01hFE07gIF003MF,O01hHFE007gJF803MF8,M03gSFE01NFE007gLF801MFC,K07gUF001NFE007gNFC00MFE,J01gVFI0NFE007gPFE007KF8,J01gVFI0NFE007MFE03gIF007IF8,J01gMFE01LFI0NFE007MFE007gJF003F8,J01gMF001KFEI0NFE007MFE007FE0gIF808,J01gMF001KFEI0NFE007MFE007FEI07gHF8,J01gGF8KF001KFEI07MFE007MFE007FEK03gF8,J01YFC00KF001KFEI07MFE007MFE007FEM01XF8,J01YFC00KF001KFCI07MFE007MFE007FEO01VF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F87SF8,J01YFC00KF001KFCI07MFE007MFE007FEP0F800RF8,J01OF8003LFC00KF001KFCI03MFE007MFE007IF8N0F8007QF8,J01NF8J03KFC00KF001KFCI03MFE007MFE007KFCL0FC007QF8,J01MFEL0KFC00KF001KF8I03MFE007MFE007KFCL0FE003KF87JF8,J01MF8L03JFC00KF001KF8I03MFE007MFE007KFC007F00FE001KF003IF8,J01MFM01JFC00KF001KF80401MFE007MFE007KFC007KF001KF003IF8,J01LFEN0JFC00KF001KF80401MFE007MFE007KFC007KFI0JFE003IF8,J01LFCI03J07IFC00KF001KF00401MFE007MFE007KFC007KF800JFC007IF8,J01LF8003FF8003IFC00KF001KF00401MFE007MFE007KFC007KFC007IFC00JF8,J01LF800IFC003IFC00KF001KF00401MFE007MFE007KFC007KFC007IF800JF8,J01LF001IFE001IFC00KF001KF00E00MFE007MFE007KFC007KFE003IF801JF8,J01LF003JF001IFC00KF001KF00E00MFE007MFE007KFC007LF003IF003JF8,J01LF003JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF001IF003JF8,J01LF007JF801IFC00KF001JFE00E00MFE007MFE007KFC007LF801FFE007JF8,J01LF007JF800IFC00KF001JFE00E00MFE007MFE007KFC007LF800FFE007JF8,J01LF007JF800IFC00KF001JFE00E007LFE007MFE007KFC007LFC00FFC00KF8,J01LF007JF800IFC00KF001JFC01E007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF800IFC00KF001JFC01F007LFE007MFE007KFC007LFE007F801KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F003KF8,J01LF007JF801IFC00KF001JFC01F007LFE007MFE007KFC007MF003F007KF8,J01LF007JF801IFC00KF001JFC01F003LFE007MFE007KFC007MF801E007KF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC01E00LF8,J01LF007JF801IFC00KF001JF801F003LFE007MFE007KFC007MFC00C00LF8,J01LF007JF801IFC00KF001JF803F803LFE007MFE007KFC007MFE00C01LF8,J01LF007JF801IFC00KF001JF803F801LFE007MFE007KFC007MFEJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFJ03LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I07LF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NF8I0MF8,J01LF007JF801IFC00KF001JF003F801LFE007MFE007KFC007NFCI0MF8,J01LF007JF801IFC00KF001JF007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007NFE001MF8,J01LF007JF801IFC00KF001IFE007FC00LFE007MFE007KFC007OF003MF8,::J01LF007JF801IFC00KF001IFC007FC007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC007FE007KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IFC00FFE007KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IFC00FFE003KFE007MFE007KFC007OF003MF8,J01LF007JF801IFC00KF001IF800FFE003KFE007MFE007KFC007OF003MF8,:J01LF007JF801IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IFE001IFC00KF001IF8M03KFE007MFE007KFC007OF003MF8,J01LF003IF8001IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF801FFEI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LF800FFCI01IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFC003FEI03IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFCI07EI07IFC00KF001IFN01KFE007MFE007KFC007OF003MF8,J01LFEN07IFC007JF001FFEO0KFE007MFE007KFC007OF003MF8,J01MF8M01IFC007JF003FFE003IFC00KFE007MFE007KFC007OF003MF8,J01MFCN01FFE007JF003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFN01FFE003IFE003FFE007IFC00KFE007MFE007KFC007OF003MF8,J01NFCM01FFE001IF8007FFC007IFC007JFE007MFE007KFC007OF003MF8,J01OFCI0E001IFI03FEI07FFC007IFC007JFE007MFE007KFC007OF01NF8,J01TF801IF8N0IFC00JFE007JFE007MFE007KFC007YF8,J01UF8JFCM01IFC00JFE007JFE007MFE007KFC007YF8,J01gFEM07IFC00JFE007JFE007MFE007KFC007YF8,J01gGF8L0JF801JFE003JFE007MFE007KFC007YF8,J01gHFK07JF801KF003JFE007JF0FFE007KFC007YF8,J01gHFEI07KF801KF003JFE007FEI0FFE007KFE7gGF8,J01gRF801KF003JFE004K0FFE007gMF08,J01gRF003KF003JFEN0FFE007gJFE0078,J01gRF003KF001JFEN0FFE007gHFC00IF8,J01gRF003KF801JFEN0FFE7gHFC01KF,L03gQFE3KF801JFEN0gJF801KF8,N01gVF801JFEL01gIF003KF8,P01gTF800JFEJ03gHFE007KF8,R01gRFC00JFE003gHFE00LF8,T01gQFE0JFE7gHFC00LF8,V01hWF801LF,Y0hSF003LF,gG0hOF007LF,gI0hJFE007LF,gK0hFC00MF,gM0gVF801LFE,gO07gQF803LFE,gQ07gMF003LFE,gS07gHFE007LFE,gU07XFC00MFE,gW07TFC01MFC,gY03PF801MFC,hG03NF3MFC,hI03RFC,hK03NFC,hM03JF8,hO018,,::::::::::::::Q0FI0C003C00C1803800780300E1801EJ0MFEJ0E0061807800F00300300FC03CI0C18,P01FE01E007F01C3C03801FE0381F1C07F8I0NFI03F80E1C0FF01FE0780381FE07F007C1F,P01FF01E00FF81C3807C03FE0381F1C0FF8I0NFI07FC0E1C0FF81FF0780381FE0FF806003,P01E701E00E381C7807C038F0381F9C0E3CI0NFI071C0E1C0F381C70780381C00E380E0038,P01E781F00E381C7007C038F0381F9C0E3CI0NFI071C0E1C0F3C1C70780381C00E380E003,P01E781F00E381CF007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3807007,P01E783F00E381CE007C038F0381F9C0E3CI0IFC7IFI071C0E1C0F3C1C70780381C00E3803FFE,P01E783F00E381DC007C038F0381F9C0E1CI0IFC7IFI079C0E1C0F3C1C70780381C00E3,P01E783B00E001DC00EE03800381F9C0EK0IFC7IFI07800E1C0F3C1C70780381C00F,P01E783B80E001FC00EE03800381DDC0EK0IFC7IFI03C00E1C0F3C1C70780381C0078,P01E703B80E001FC00EE039E0381DDC0E78I0FEJ07FI01E00E1C0F381C70780381FC07C,P01EF03380E001FC00EE039F0381DDC0E7CI0FCJ03FJ0F00E1C0F781CF0780381FE03E007IF,P01FF07380E001FE00EE039F0381DDC0E7CI0FCJ03FJ0780E1C0FF81FE0780381FC01EJ07E,P01FC07380E001EE01C7038F0381DDC0E3CI0IFC7IFJ0380E1C0FE01FC0780381CI0FJ0F8,P01E0073C0E001CE01C7038F0381CFC0E3CI0IFC7IFJ03C0E1C0F001C00780381CI078003C,P01E007FC0E001C701FF038F0381CFC0E3CI0IFC7IFJ01C0E1C0F001C00780381CI07801F,P01E007FC0E381C701FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00FFC0E381C781FF038F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E3807IF,P01E00E1C0E381C383C7838F0381CFC0E3CI0IFC7IFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C383C7838F0381CFC0E3CI0NFI071C0E1C0F001C00780381C00E38,P01E00E1E0E381C3C383838F0381C7C0F3CI0NFI079C0F3C0F001C00780381E00E78,P01E00E1E0FF81C1C38383FE0381C7C07F8I0NFI03FC07F80F001C007F8381FE0FF007IF,P01C00E0E07F01C1C38381FC0381C7C03FJ0NFI03F803F00E001C007F8381FE07F007IF,,::::::::::::^FS
            ^FO40,40^GB1160,820,6^FS   // Dibuja una caja alrededor de todo el contenido
            ^FO40,200^GB1160,70,6^FS //fila 2
            ^FO55,230^A0N,25,25^FDCUSTOMER :^FS //label customer
            ^FO235,225^A0N,35,35^FD{postRFIDLabel.Customer}^FS 
            ^FO55,300^A0N,25,25^FDITEM: ^FS //label item
            ^FO235,290^A0N,35,35^FD{postRFIDLabel.ItemDescription}^FS 
            ^FO40,330^GB570,371,6^FS // fila4:1
            ^FO55,360^A0N,25,25^FDQPS ITEM # ^FS //label QPS ITEM #
            // Código QR QPS ITEM #
            ^FO230,410^BQN,10,10^FDQA^FD{postRFIDLabel.ItemNumber}^FS
            ^FO280,650^A0N,33,33^FD{postRFIDLabel.ItemNumber}^FS //label item
            ^FO604,330^GB595,180,6^FS // 4.2
            ^FO620,360^A0N,25,25^FDLOT: ^FS //label item
            // Código QR LOT
            ^FO850,340^BQN,5,5^FDQA^FD{postRFIDLabel.InventoryLot}^FS
            ^FO855,470^A0N,33,33^FD{postRFIDLabel.InventoryLot}^FS //label item
            ^FO620,520^A0N,25,25^FDTotal Qty/Pallet (Eaches)^FS //label total qty/pallet
            // Código QR total qty/pallet eaches
            ^FO850,540^BQN,5,5^FDQA^FD{postRFIDLabel.TotalUnits}^FS
            ^FO860,665^A0N,33,33^FD{postRFIDLabel.TotalUnits}^FS // label total qty/pallet
            ^FO40,695^GB1160,75,6^FS // fila5
            ^FO55,720^A0N,25,25^FDTraceability code:^FS //label traceability code
            ^FO240,715^A0N,35,35^FD{postRFIDLabel.Traceability}^FS 
            ^FO40,764^GB580,96,6^FS // fila5:1
            ^FO55,780^A0N,25,25^FDGROSS WEIGHT: ^FS //label gross weight
            ^FO630,780^A0N,25,25^FDNET WEIGHT: ^FS //label net weight
            ^FO300,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoBruto}^FS //label peso bruto
            ^FO850,790^A0N,60,60^FD{prodEtiquetaBioflex.PesoNeto}^FS //label peso neto
            // EPC Hex
            ^RFW,H,1,8,4^FD{prodEtiquetaBioflex.RFID}^FS
            ^XZ";

            bool result = USBSender.SendSATOCommand(stringResult, "172.16.21.131", 9100);
            if (result)
            {
                return Ok(postRFIDLabel);
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
