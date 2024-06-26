﻿using PrinterBackEnd.Models.Domain;

namespace PrinterBackEnd.Models.Dto.QualityLabel
{
    public class QualityLabelResponse
    {
        public int Id { get; set; }
        public string? Area { get; set; }
        public DateTime Fecha { get; set; }
        public string? ClaveProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? ClaveOperador { get; set; }
        public string? Operador { get; set; }
        public string? Turno { get; set; }
        public float PesoTarima { get; set; }
        public float PesoBruto { get; set; }
        public float PesoNeto { get; set; }
        public float Piezas { get; set; }
        public string? Trazabilidad { get; set; }
        public string? Orden { get; set; }
        public string? RFID { get; set; }
        public int Status { get; set; }
        public string? UOM { get; set; }
        public ProdExtrasQuality prodExtrasQuality { get; set; }
    }
}
