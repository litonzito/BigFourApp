using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigFourApp.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesBoleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Notificar",
                table: "Boletos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notificar",
                table: "Boletos");
        }
    }
}
