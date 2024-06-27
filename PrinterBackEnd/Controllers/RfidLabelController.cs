using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;
using PrinterBackEnd.Models.Dto.RFIDLabel;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RfidLabelController : ControllerBase
    {
        private readonly DataContext _context;

        public RfidLabelController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdEtiquetasRFID>>> GetRFIDLabels()
        {
            try
            {
                var rfidLabels = await _context.ProdEtiquetasRFID.ToListAsync();

                // Put "" if UOM is null
                foreach (var rfidLabel in rfidLabels)
                {
                    if (rfidLabel.UOM == null)
                    {
                        rfidLabel.UOM = "";
                    }
                }
                return Ok(rfidLabels);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Create an endpoint that receives a txt file with a list of rfidLabels.RFID elements and returns the rfidLabels that match the rfidLabels.RFID elements
        [HttpPost("GetRFIDLabelsByRFID")]
        public async Task<ActionResult<IEnumerable<ProdEtiquetasRFID>>> GetRFIDLabelsByRFID(IFormFile file)
        {
            try
            {
                // Read the file
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    // Create a list of rfidLabels.RFID elements
                    var rfidLabels = new List<string>();
                    while (reader.Peek() >= 0)
                    {
                        rfidLabels.Add(reader.ReadLine());
                    }

                    // Get the rfidLabels that match the rfidLabels.RFID elements
                    var rfidLabelsList = await _context.ProdEtiquetasRFID.Where(x => rfidLabels.Contains(x.RFID)).ToListAsync();

                    // Put "" if UOM is null
                    foreach (var rfidLabel in rfidLabelsList)
                    {
                        if (rfidLabel.UOM == null)
                        {
                            rfidLabel.UOM = "";
                        }
                    }
                    return Ok(rfidLabelsList);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Create an endpoint that receives a txt file with a list of rfidLabels.RFID elements and returns the rfidLabels that match the rfidLabels.RFID elements and creates an excel file using the ClosedXML library
        [HttpPost("GetRFIDLabelsByRFIDExcel")]
        public async Task<ActionResult> GetRFIDLabelsByRFIDExcel(IFormFile file)
        {
            try
            {
                // Read the file
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    // Create a list of rfidLabels.RFID elements
                    var rfidLabels = new List<string>();
                    while (reader.Peek() >= 0)
                    {
                        rfidLabels.Add(reader.ReadLine());
                    }

                    // Get the rfidLabels that match the rfidLabels.RFID elements
                    var rfidLabelsList = await _context.ProdEtiquetasRFID.Where(x => rfidLabels.Contains(x.RFID)).ToListAsync();

                    // Put "" if UOM is null
                    foreach (var rfidLabel in rfidLabelsList)
                    {
                        if (rfidLabel.UOM == null)
                        {
                            rfidLabel.UOM = "";
                        }
                    }

                    // Create an excel file using the ClosedXML library
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("RFID Labels");
                        worksheet.Cell(1, 1).Value = "Area";
                        worksheet.Cell(1, 2).Value = "ClaveProducto";
                        worksheet.Cell(1, 3).Value = "NombreProducto";
                        worksheet.Cell(1, 4).Value = "ClaveOperador";
                        worksheet.Cell(1, 5).Value = "Operador";
                        worksheet.Cell(1, 6).Value = "Turno";
                        worksheet.Cell(1, 7).Value = "PesoTarima";
                        worksheet.Cell(1, 8).Value = "PesoBruto";
                        worksheet.Cell(1, 9).Value = "PesoNeto";
                        worksheet.Cell(1, 10).Value = "Piezas";
                        worksheet.Cell(1, 11).Value = "Trazabilidad";
                        worksheet.Cell(1, 12).Value = "Orden";
                        worksheet.Cell(1, 13).Value = "RFID";
                        worksheet.Cell(1, 14).Value = "Status";

                        for (int i = 0; i < rfidLabelsList.Count; i++)
                        {
                            worksheet.Cell(i + 2, 1).Value = rfidLabelsList[i].Area;
                            worksheet.Cell(i + 2, 2).Value = rfidLabelsList[i].ClaveProducto;
                            worksheet.Cell(i + 2, 3).Value = rfidLabelsList[i].NombreProducto;
                            worksheet.Cell(i + 2, 4).Value = rfidLabelsList[i].ClaveOperador;
                            worksheet.Cell(i + 2, 5).Value = rfidLabelsList[i].Operador;
                            worksheet.Cell(i + 2, 6).Value = rfidLabelsList[i].Turno;
                            worksheet.Cell(i + 2, 7).Value = rfidLabelsList[i].PesoTarima;
                            worksheet.Cell(i + 2, 8).Value = rfidLabelsList[i].PesoBruto;
                            worksheet.Cell(i + 2, 9).Value = rfidLabelsList[i].PesoNeto;
                            worksheet.Cell(i + 2, 10).Value = rfidLabelsList[i].Piezas;
                            worksheet.Cell(i + 2, 11).Value = rfidLabelsList[i].Trazabilidad;
                            worksheet.Cell(i + 2, 12).Value = rfidLabelsList[i].Orden;
                            worksheet.Cell(i + 2, 13).Value = rfidLabelsList[i].RFID;
                            worksheet.Cell(i + 2, 14).Value = rfidLabelsList[i].Status;
                        }
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RFIDLabels.xlsx");
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Create a post method that receives a 'ProdEtiquetasRFID' object and adds it to the 'ProdEtiquetasRFID' table
        [HttpPost]
        public async Task<ActionResult<ProdEtiquetasRFID>> PostRFIDLabel(PostRFIDLabeldto postRFIDLabeldto)
        {
            try
            {
                // Create a new 'ProdEtiquetasRFID' object
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

                // Add the 'ProdEtiquetasRFID' object to the 'ProdEtiquetasRFID' table
                _context.ProdEtiquetasRFID.Add(postRFIDLabel);
                await _context.SaveChangesAsync();
                return Ok(postRFIDLabeldto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Create a put method that receives a 'ProdEtiquetasRFID' object and updates it in the 'ProdEtiquetasRFID' table
        [HttpPut]
        public async Task<ActionResult<ProdEtiquetasRFID>> PutRFIDLabel(PostRFIDLabeldto postRFIDLabeldto)
        {
            try
            {
                // Get the 'ProdEtiquetasRFID' object where 'RFID' matches the 'RFID' parameter
                var rfidLabel = await _context.ProdEtiquetasRFID.FirstOrDefaultAsync(x => x.RFID == postRFIDLabeldto.RFID);

                // Update the 'ProdEtiquetasRFID' object
                rfidLabel.Area = postRFIDLabeldto.Area;
                rfidLabel.ClaveProducto = postRFIDLabeldto.ClaveProducto;
                rfidLabel.NombreProducto = postRFIDLabeldto.NombreProducto;
                rfidLabel.ClaveOperador = postRFIDLabeldto.ClaveOperador;
                rfidLabel.Operador = postRFIDLabeldto.Operador;
                rfidLabel.Turno = postRFIDLabeldto.Turno;
                rfidLabel.PesoTarima = postRFIDLabeldto.PesoTarima;
                rfidLabel.PesoBruto = postRFIDLabeldto.PesoBruto;
                rfidLabel.PesoNeto = postRFIDLabeldto.PesoNeto;
                rfidLabel.Piezas = postRFIDLabeldto.Piezas;
                rfidLabel.Trazabilidad = postRFIDLabeldto.Trazabilidad;
                rfidLabel.Orden = postRFIDLabeldto.Orden;
                rfidLabel.RFID = postRFIDLabeldto.RFID;
                rfidLabel.Status = postRFIDLabeldto.Status;

                await _context.SaveChangesAsync();
                return Ok(postRFIDLabeldto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
