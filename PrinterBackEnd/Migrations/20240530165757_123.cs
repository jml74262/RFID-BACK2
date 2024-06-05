using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrinterBackEnd.Migrations
{
    /// <inheritdoc />
    public partial class _123 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "ProdEtiquetasRFID");

            migrationBuilder.AlterColumn<string>(
                name: "Area",
                table: "ProdEtiquetasRFID",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "Fecha",
                table: "ProdEtiquetasRFID",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "ProdEtiquetasRFID");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Area",
                table: "ProdEtiquetasRFID",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DateTime",
                table: "ProdEtiquetasRFID",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
