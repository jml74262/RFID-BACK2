namespace PrinterBackEnd.Models.Dto.RFIDLabel
{
    public class PostRFIDLabeldto
    {
        public string Area { get; set; }
        public string ClaveProducto { get; set; }
        public string NombreProducto { get; set; }
        public string ClaveOperador { get; set; }
        public string Operador { get; set; }
        public string Turno { get; set; }
        public float PesoTarima { get; set; } = 0;
        public float PesoBruto { get; set; } = 0;
        public float PesoNeto { get; set; } = 0;
        public int Piezas { get; set; } = 0;
        public string Trazabilidad { get; set; }
        public string Orden { get; set; }
        public string RFID { get; set; }
        public int Status { get; set; }
    }
}
