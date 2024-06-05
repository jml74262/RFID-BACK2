using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurnController : ControllerBase
    {
        private readonly DataContext _context;

        public TurnController(DataContext context)
        {
            _context = context;
        }

        // Create get method that returns 'Cat_Turnos' table
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatTurno>>> GetCatTurno()
        {
            try
            {
                // Get all the turns from the 'Cat_Turnos' table
                var turns = await _context.Cat_Turnos.ToListAsync();
                return Ok(turns);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


    }
}
