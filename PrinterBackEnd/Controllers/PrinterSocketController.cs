//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Http;
//using System.Net.WebSockets;
//using System.Threading.Tasks;

//namespace PrinterBackEnd.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class PrinterSocketController : ControllerBase
//    {
//        private readonly PrinterSocketService _printerSocketService;

//        public PrinterSocketController(PrinterSocketService printerSocketService)
//        {
//            _printerSocketService = printerSocketService;
//        }

//        [HttpGet("connect")]
//        public async Task<IActionResult> Connect()
//        {
//            if (HttpContext.WebSockets.IsWebSocketRequest)
//            {
//                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
//                await _printerSocketService.HandleWebSocketAsync(webSocket);
//                return new EmptyResult();
//            }
//            else
//            {
//                return BadRequest();
//            }
//        }
//    }
//}
