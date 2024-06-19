using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;
using PrinterBackEnd.Models.Dto.RFIDLabel;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        [HttpPost("PostPrintDestiny")]
        public async Task<ActionResult<ProdExtrasDestiny>> PostDestinyLabel(PostDestinyLabelDto postDestinyLabelDto)
        {
            try
            {

                CultureInfo cultureInfo = new CultureInfo("es-MX");

                //// Check the maxIds table to get the next bioFlexLabelId where Tarima = BIOFLEX
                //var maxId = await _context.MaxIds.ToListAsync();
                //// Get the register with Tarima = BIOFLEX
                //var maxIdBioFlex = maxId.Find(x => x.Tarima == "BIOFLEX");

                //// if the maxIdBioFlex is null, return a 404 error
                //if (maxIdBioFlex == null)
                //{
                //    return NotFound("No se encontró el registro con Tarima = BIOFLEX");
                //}

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
                    //Id = (maxIdBioFlex?.MaxId+1) ?? 0,
                    ShippingUnits = postDestinyLabelDto.postExtraDestinyDto.ShippingUnits,
                    UOM = postDestinyLabelDto.postExtraDestinyDto.UOM,
                    InventoryLot = postDestinyLabelDto.postExtraDestinyDto.InventoryLot,
                    //prodEtiquetaRFId = ProdEtiquetasRFID,
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

                //maxIdBioFlex.MaxId += 1;
                //_context.MaxIds.Update(maxIdBioFlex);

                var date = postDestinyLabelDto.Fecha.ToString("dd-MM-yy", cultureInfo);


                await _context.SaveChangesAsync();

                // Crear la cadena de comando SATO usando los valores del DTO
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
                ^FO55,353^A0N,25,25^FDCUSTOMER PO^FS //label customer po
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
                ^FO838,545^BQN,3,4^FDQA^FDAREA: {postDestinyLabelDto.Area}, FECHA: {date}, PRODUCTO: {postDestinyLabelDto.ClaveProducto} / {postDestinyLabelDto.NombreProducto}, EMPACADORA / TURNO: {postDestinyLabelDto.Operador} / {postDestinyLabelDto.Turno}, PESO BRUTO(KG): {postDestinyLabelDto.PesoBruto}, PESO NETO(KG): {postDestinyLabelDto.PesoNeto}, PESO TARIMA(KG): {postDestinyLabelDto.PesoTarima}, # PIEZAS (ROLLS, BULKS, BOXES): {postDestinyLabelDto.Piezas}, CODIGO DE TRAZABILIDAD: {postDestinyLabelDto.Trazabilidad}, OT Y/O LOTE: {postDestinyLabelDto.Orden}, REVISIÓN: 01^FS
                // EPC Hex
                ^RFW,H,1,8,64^FD{postDestinyLabelDto.RFID}^FS
                ^XZ";



                return Ok(postDestinyLabel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
