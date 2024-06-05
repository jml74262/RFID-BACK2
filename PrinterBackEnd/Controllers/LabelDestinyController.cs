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
    public class LabelDestinyController : ControllerBase
    {
        private readonly DataContext _context;

        public LabelDestinyController(DataContext context)
        {
            _context = context;
        }

        // Endpoint to get all the Destiny Labels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProdExtrasDestiny>>> GetDestinyLabels()
        {
            try
            {
                var destinyLabels = await _context.ProdExtrasDestiny.ToListAsync();
                return Ok(destinyLabels);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Endpoint to create a new Destiny Label
        [HttpPost]
        public async Task<ActionResult<ProdExtrasDestiny>> PostDestinyLabel(PostDestinyLabelDto postDestinyLabelDto)
        {
            try
            {
                // Check the maxIds table to get the next bioFlexLabelId where Tarima = BIOFLEX
                var maxId = await _context.MaxIds.ToListAsync();
                // Get the register with Tarima = BIOFLEX
                var maxIdBioFlex = maxId.Find(x => x.Tarima == "BIOFLEX");

                // if the maxIdBioFlex is null, return a 404 error
                if (maxIdBioFlex == null)
                {
                    return NotFound("No se encontró el registro con Tarima = BIOFLEX");
                }

                // Create a new 'ProdEtiquetasRFID' object
                var ProdEtiquetasRFID = new ProdEtiquetasRFID
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

                // Create a new 'ProdExtrasDestiny' object
                var postDestinyLabel = new ProdExtrasDestiny
                {
                    Id = (maxIdBioFlex?.MaxId+1) ?? 0,
                    ShippingUnits = postDestinyLabelDto.postExtraDestinyDto.ShippingUnits,
                    UOM = postDestinyLabelDto.postExtraDestinyDto.UOM,
                    InventoryLot = postDestinyLabelDto.postExtraDestinyDto.InventoryLot,
                    prodEtiquetaRFID = ProdEtiquetasRFID,
                    IndividualUnits = postDestinyLabelDto.postExtraDestinyDto.IndividualUnits,
                    PalletId = postDestinyLabelDto.postExtraDestinyDto.PalletId,
                    CustomerPo = postDestinyLabelDto.postExtraDestinyDto.CustomerPo,
                    TotalUnits = postDestinyLabelDto.postExtraDestinyDto.TotalUnits,
                    ProductDescription = postDestinyLabelDto.postExtraDestinyDto.ProductDescription,
                    ItemNumber = postDestinyLabelDto.postExtraDestinyDto.ItemNumber
                };

                // Add the new 'ProdEtiquetasRFID' object to the database
                _context.ProdEtiquetasRFID.Add(ProdEtiquetasRFID);

                // Add the new 'ProdExtrasDestiny' object to the database
                _context.ProdExtrasDestiny.Add(postDestinyLabel);

                maxIdBioFlex.MaxId += 1;
                _context.MaxIds.Update(maxIdBioFlex);



                await _context.SaveChangesAsync();


                return Ok(postDestinyLabel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
