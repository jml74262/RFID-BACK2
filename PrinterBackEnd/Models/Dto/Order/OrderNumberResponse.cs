namespace PrinterBackEnd.Models.Dto.Order
{
    public class OrderNumberResponse
    {
        public string Id { get; set; }
        public int Orden { get; set; }
        public string ClaveProducto { get; set; }
        public string Producto { get; set; }
    }
}
