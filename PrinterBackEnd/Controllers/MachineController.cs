using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineController : ControllerBase
    {
        private readonly DataContext _context;

        public MachineController(DataContext context)
        {
            _context = context;
        }

        // Create a get method that returns 'Cat_Maquinas' table
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatMaquina>>> GetCatMaquina()
        {
            try
            {
                // Get all the machines from the 'Cat_Maquinas' table
                var machines = await _context.Cat_Maquinas.ToListAsync();
                return Ok(machines);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Create a get method that returns 'Cat_Maquinas' table where 'Area' matches the 'Id' parameter
        [HttpGet("{Id}")]
        public async Task<ActionResult<IEnumerable<CatMaquina>>> GetCatMaquina(int Id)
        {
            try
            {
                // Get the machines where 'Area' matches the 'Id' parameter
                var machines = await _context.Cat_Maquinas
                    .Where(x => x.Area == Id)
                    .ToListAsync();
                return Ok(machines);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
