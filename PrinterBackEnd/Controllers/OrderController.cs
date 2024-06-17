using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;
using PrinterBackEnd.Models.Dto.Order;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly DataContext _context;

        public OrderController(DataContext context)
        {
            _context = context;
        }

        // Create a get method that returns 'Orden' where 'UltimoProceso' matches the 'UltimoProceso' parameter
        [HttpGet("{UltimoProceso}")]
        public async Task<ActionResult<IEnumerable<CatOrden>>> GetCatOrden(String UltimoProceso)
        {
            try
            {
                // Get the 'Orden' where 'UltimoProceso' matches the 'UltimoProceso' parameter, fill OrderNumberResponse
                var order = await _context.Cat_Ordenes
                    .Where(x => x.UltimoProceso == UltimoProceso)
                    .Select(x => new OrderNumberResponse
                    {
                        //parse the id from string to int
                        Id = x.Id,
                        //parse order number to string, if null return empty string
                        Orden = x.Orden ?? 0,
                        ClaveProducto = x.ClaveProducto ?? "",
                        Producto = x.Producto ?? "",

                    })
                    .ToListAsync();
                return Ok(order);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Create a get method that returns all 'Orden' from the 'Cat_Ordenes' table
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatOrden>>> GetCatOrden()
        {
            try
            {
                // Get all the orders from the 'Cat_Ordenes' table
                var orders = await _context.Cat_Ordenes.ToListAsync();

                if (orders == null)
                {
                    return NotFound();
                }

                return Ok(orders);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
