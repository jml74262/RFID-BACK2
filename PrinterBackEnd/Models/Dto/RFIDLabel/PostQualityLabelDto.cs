using System.ComponentModel.DataAnnotations;

namespace PrinterBackEnd.Models.Dto.RFIDLabel
{
    public class PostQualityLabelDto
    {
        public string Area { get; set; }
        public string ClaveProducto { get; set; }
        public string NombreProducto { get; set; }
        public string ClaveOperador { get; set; }
        public string Operador { get; set; }
        public string Turno { get; set; }
        public float PesoTarima { get; set; }
        public float PesoBruto { get; set; }
        public float PesoNeto { get; set; }
        public int Piezas { get; set; }
        public string Trazabilidad { get; set; }
        public string Orden { get; set; }
        public string RFID { get; set; }
        public int Status { get; set; }
        [Required]
        public DateTime Fecha { get; set; }
        public PostExtraQualityDto postExtraQuality { get; set; }
    }

    public class PostExtraQualityDto
    {
        public int IndividualUnits { get; set; }
        public string ItemDescription { get; set; }
        public string ItemNumber { get; set; }
        public int TotalUnits { get; set; }
        public int ShippingUnits { get; set; }
        public string InventoryLot { get; set; }
        public string Customer { get; set; }
        public string Traceability { get; set; }
        //public string PalletId { get; set; }
        //public string CustomerPo { get; set; }
        //public string ProductDescription { get; set; }
    }
}
