using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrinterBackEnd.Data;
using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperatorController : ControllerBase
    {
        private readonly DataContext _context;

        public OperatorController(DataContext context)
        {
            _context = context;
        }

        // Create a method that receives IdArea, IdTurno and returns 'Cat_Operadores' where 'IdArea' and 'IdTurno' match 'IdArea' and 'IdTurno'
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatOperador>>> GetCatOperador(int IdArea, int IdTurno)
        {
            try
            {
                // Get all the operators from the 'Cat_Operadores' table where 'IdArea' and 'IdTurno' match 'IdArea' and 'IdTurno'
                var operators = await _context.Cat_Operadores.Where(o => o.Id_Area == IdArea && o.Id_Turno == IdTurno).ToListAsync();
                return Ok(operators);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Get all the operators from the 'Cat_Operadores' table
        [HttpGet("all-operators")]
        public async Task<ActionResult<IEnumerable<CatOperador>>> GetCatOperadores()
        {
            try
            {
                // Get all the operators from the 'Cat_Operadores' table
                var operators = await _context.Cat_Operadores.ToListAsync();
                return Ok(operators);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
