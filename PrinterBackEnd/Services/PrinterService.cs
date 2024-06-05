using PrinterBackEnd.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using static PrinterBackEnd.Models.Printer;
using System.Net.Sockets;
using System.Text;

namespace PrinterBackEnd.Services
{
    public class PrinterService
    {
        private readonly Printer _printer;
        private readonly ILogger<PrinterService> _logger;

        public PrinterService(IOptions<PrinterSettings> settings, ILogger<PrinterService> logger)
        {
            var printerSettings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _printer = new Printer
            {
                Interface = Printer.InterfaceType.TCPIP,
                TCPIPAddress = printerSettings.IPAddress,
                TCPIPPort = printerSettings.Port,
                Timeout = printerSettings.Timeout // Usar el timeout configurado
            };
            _logger = logger;
        }

        public Status GetPrinterStatus()
        {
            try
            {
                _logger.LogInformation("Attempting to get printer status...");
                var status = _printer.GetPrinterStatus();
                _logger.LogInformation("Printer status retrieved successfully.");
                return status;
            }
            catch (SocketException se)
            {
                _logger.LogError(se, "Network error while retrieving printer status.");
                throw new Exception("Network error while retrieving printer status.", se);
            }
            catch (TimeoutException te)
            {
                _logger.LogError(te, "Timeout while retrieving printer status.");
                throw new Exception("Timeout while retrieving printer status.", te);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving printer status.");
                throw; // Rethrow to allow the controller to catch and log it
            }
        }

        public void SendPrintJob(byte[] data)
        {
            try
            {
                _logger.LogInformation("Attempting to send print job...");
                _printer.Send(data);
                _logger.LogInformation("Print job sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending print job.");
                throw; // Rethrow to allow the controller to catch and log it
            }
        }

        public bool SendCommand(string command)
        {
            try
            {
                _logger.LogInformation("Attempting to send command...");
                byte[] data = Encoding.ASCII.GetBytes(command);
                _printer.Send(data);
                _logger.LogInformation("Command sent successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command.");
                return false;
            }
        }
    }
}
