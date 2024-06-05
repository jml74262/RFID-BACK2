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
                return Ok(rfidLabels);
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
