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
    public class LabelQualityController : ControllerBase
    {

        private readonly DataContext _context;

        public LabelQualityController(DataContext context)
        {
            _context = context;
        }

        // Endpoint to get all the Quality Labels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdExtrasQuality>>> GetQualityLabels()
        {
            try
            {
                var qualityLabels = await _context.ProdExtrasQuality.ToListAsync();
                return Ok(qualityLabels);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Endpoint to create a new Quality Label
        //[HttpPost]
        //public async Task<ActionResult<ProdExtrasQuality>> PostQualityLabel(PostQualityLabelDto postQualityLabelDto)
        //{
        //    var postQualityLabel = new ProdExtrasQuality
        //    {
        //        prodEtiquetaRFID = new ProdEtiquetasRFID
        //        {
        //            Area = postQualityLabelDto.Area,
        //            Fecha = DateTime.Now,
        //            ClaveProducto = postQualityLabelDto.ClaveProducto,
        //            NombreProducto = postQualityLabelDto.NombreProducto,
        //            ClaveOperador = postQualityLabelDto.ClaveOperador,
        //            Turno = postQualityLabelDto.Turno,
        //            PesoTarima = postQualityLabelDto.PesoTarima,
        //            PesoBruto = postQualityLabelDto.PesoBruto,
        //            PesoNeto = postQualityLabelDto.PesoNeto,
        //            Piezas = postQualityLabelDto.Piezas,
        //            Trazabilidad = postQualityLabelDto.Trazabilidad,
        //            Orden = postQualityLabelDto.Orden,
        //            RFID = postQualityLabelDto.RFID,
        //            Status = postQualityLabelDto.Status
        //        },

        //    };

            
        //}
    }
}
