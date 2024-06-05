using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrinterBackEnd.Data;

namespace PrinterBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LabelController : ControllerBase
    {
        private readonly DataContext _context;

        public LabelController(DataContext context)
        {
            _context = context;
        }

        // Create get method 
    }
}
