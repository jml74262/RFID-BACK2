using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaController : ControllerBase
    {
        private readonly DataContext _context;

        public AreaController(DataContext context)
        {
            _context = context;
        }

        // Create get method, it should return 'Cat_Areas' table
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatArea>>> GetCatArea()
        {
            try
            {
                // Get all the areas from the 'Cat_Areas' table
                var areas = await _context.Cat_Areas.ToListAsync();
                return Ok(areas);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
