using System.ComponentModel.DataAnnotations;

namespace PrinterBackEnd.Models.Dto.RFIDLabel
{
    public class PostDestinyLabelDto
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
        public PostExtraDestinyDto postExtraDestinyDto { get; set; }
    }

    public class PostExtraDestinyDto
    {
        public int ShippingUnits { get; set; }
        public string UOM { get; set; }
        public string InventoryLot { get; set; }
        public int IndividualUnits { get; set; }
        public string PalletId { get; set; }
        public string CustomerPo { get; set; }
        public int TotalUnits { get; set; }
        public string ProductDescription { get; set; }
        public string ItemNumber { get; set; }
    }
}
