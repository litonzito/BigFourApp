using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigFourApp.Migrations
{
    /// <inheritdoc />
    public partial class ActualizacionEstructura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boletos_Asientos_Id_Asiento",
                table: "Boletos");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesVenta_Boletos_Id_Boleto",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_Id_Boleto",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_Boletos_Id_Asiento",
                table: "Boletos");

            migrationBuilder.DropColumn(
                name: "Id_Asiento",
                table: "Boletos");

            migrationBuilder.DropColumn(
                name: "Id_DetalleVenta",
                table: "Boletos");

            migrationBuilder.AddColumn<int>(
                name: "Id_Asiento",
                table: "DetallesVenta",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_Id_Asiento",
                table: "DetallesVenta",
                column: "Id_Asiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_Id_Boleto",
                table: "DetallesVenta",
                column: "Id_Boleto");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesVenta_Asientos_Id_Asiento",
                table: "DetallesVenta",
                column: "Id_Asiento",
                principalTable: "Asientos",
                principalColumn: "Id_Asiento",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesVenta_Boletos_Id_Boleto",
                table: "DetallesVenta",
                column: "Id_Boleto",
                principalTable: "Boletos",
                principalColumn: "Id_Boleto",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesVenta_Asientos_Id_Asiento",
                table: "DetallesVenta");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesVenta_Boletos_Id_Boleto",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_Id_Asiento",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_Id_Boleto",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "Id_Asiento",
                table: "DetallesVenta");

            migrationBuilder.AddColumn<int>(
                name: "Id_Asiento",
                table: "Boletos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id_DetalleVenta",
                table: "Boletos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_Id_Boleto",
                table: "DetallesVenta",
                column: "Id_Boleto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boletos_Id_Asiento",
                table: "Boletos",
                column: "Id_Asiento",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Boletos_Asientos_Id_Asiento",
                table: "Boletos",
                column: "Id_Asiento",
                principalTable: "Asientos",
                principalColumn: "Id_Asiento",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesVenta_Boletos_Id_Boleto",
                table: "DetallesVenta",
                column: "Id_Boleto",
                principalTable: "Boletos",
                principalColumn: "Id_Boleto",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
