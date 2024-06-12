using System.ComponentModel.DataAnnotations;

namespace PrinterBackEnd.Models.Dto.RFIDLabel
{
    public class PostRFIDLabeldto
    {
        [Required]
        public string Area { get; set; }
        [Required]
        public string ClaveProducto { get; set; }
        [Required]
        public string NombreProducto { get; set; }
        [Required]
        public string ClaveOperador { get; set; }
        [Required]
        public string Operador { get; set; }
        [Required]
        public string Turno { get; set; }
        public float PesoTarima { get; set; } = 0;
        public float PesoBruto { get; set; } = 0;
        public float PesoNeto { get; set; } = 0;
        public int Piezas { get; set; } = 0;
        [Required]
        public string Trazabilidad { get; set; }
        [Required]
        public string Orden { get; set; }
        [Required]
        public string RFID { get; set; }
        [Required]
        public int Status { get; set; }
        [Required]
        public string UOM { get; set; }
    }
}
