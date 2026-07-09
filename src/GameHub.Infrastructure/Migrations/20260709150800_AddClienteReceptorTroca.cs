using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteReceptorTroca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClienteReceptorId",
                table: "Trocas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trocas_ClienteReceptorId",
                table: "Trocas",
                column: "ClienteReceptorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trocas_Clientes_ClienteReceptorId",
                table: "Trocas",
                column: "ClienteReceptorId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trocas_Clientes_ClienteReceptorId",
                table: "Trocas");

            migrationBuilder.DropIndex(
                name: "IX_Trocas_ClienteReceptorId",
                table: "Trocas");

            migrationBuilder.DropColumn(
                name: "ClienteReceptorId",
                table: "Trocas");
        }
    }
}
