using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrinterBackEnd.Migrations
{
    /// <inheritdoc />
    public partial class v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProdEtiquetasRFID",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Area = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaveProducto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaveOperador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operador = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PesoTarima = table.Column<float>(type: "real", nullable: false),
                    PesoBruto = table.Column<float>(type: "real", nullable: false),
                    PesoNeto = table.Column<float>(type: "real", nullable: false),
                    Piezas = table.Column<float>(type: "real", nullable: false),
                    Trazabilidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Orden = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RFID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdEtiquetasRFID", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdEtiquetasRFID");
        }
    }
}
