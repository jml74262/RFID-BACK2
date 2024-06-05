using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly DataContext _context;

        public ProductController(DataContext context)
        {
            _context = context;
        }

        // Create get method that receives an 'Id' parameter from 'Cat_Maquina' and returns 'Cat_Productos' where 'Id' matches 'Maquina'

        // Create get method that returns 'Cat_Productos' table
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatProducto>>> GetCatProducto()
        {
            try
            {
                // Get all the products from the 'Cat_Productos' table
                var products = await _context.Cat_Productos.ToListAsync();
                return Ok(products);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
