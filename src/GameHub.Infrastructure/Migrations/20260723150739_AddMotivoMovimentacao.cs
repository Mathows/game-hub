using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMotivoMovimentacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotivoId",
                table: "MovimentacoesEstoque",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MotivosMovimentacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operacao = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotivosMovimentacao", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MotivosMovimentacao",
                columns: new[] { "Id", "Ativo", "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor", "Descricao", "Operacao" },
                values: new object[,]
                {
                    { 1, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Compra de fornecedor", 1 },
                    { 2, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Devolução de cliente", 1 },
                    { 3, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Ajuste de inventário (sobra)", 1 },
                    { 4, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Produto danificado", 2 },
                    { 5, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Devolução ao fornecedor", 2 },
                    { 6, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Ajuste de inventário (falta)", 2 },
                    { 7, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", "Perda/extravio", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_MotivoId",
                table: "MovimentacoesEstoque",
                column: "MotivoId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_MotivosMovimentacao_MotivoId",
                table: "MovimentacoesEstoque",
                column: "MotivoId",
                principalTable: "MotivosMovimentacao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_MotivosMovimentacao_MotivoId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropTable(
                name: "MotivosMovimentacao");

            migrationBuilder.DropIndex(
                name: "IX_MovimentacoesEstoque_MotivoId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropColumn(
                name: "MotivoId",
                table: "MovimentacoesEstoque");
        }
    }
}
