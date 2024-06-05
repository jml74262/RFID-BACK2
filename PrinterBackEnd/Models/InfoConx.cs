namespace PrinterBackEnd.Models
{
    [Serializable]
    public class InfoConx
    {
        public string PrinterModel { get; set; }

        public string DriverName { get; set; }

        public string PortName { get; set; }

        public bool Online { get; set; }

        public bool Default { get; set; }

        public bool Bidirectional { get; set; }
    }
}
