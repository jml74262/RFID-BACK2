namespace PrinterBackEnd.Models
{
    public class LabelData
    {
        public string PrinterIp { get; set; }
        public string TextToPrint { get; set; }
        public int XPosition { get; set; } = 20;  // Default x-coordinate
        public int YPosition { get; set; } = 20;  // Default y-coordinate
                                                  // Add more properties for barcodes, fonts, etc. as needed
    }
}
