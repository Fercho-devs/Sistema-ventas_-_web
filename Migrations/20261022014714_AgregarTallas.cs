using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaWeb_Ventas.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTallas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TallaSeleccionada",
                table: "CarritoDetalles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TallaSeleccionada",
                table: "CarritoDetalles");
        }
    }
}
