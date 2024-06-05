namespace PrinterBackEnd.Models
{
    public class PrinterSettings
    {
        public string IPAddress { get; set; }
        public string Port { get; set; }
        public int Timeout { get; set; }
        public bool IsUDP { get; set; }
        public bool PermanentConnect { get; set; }
    }

}
