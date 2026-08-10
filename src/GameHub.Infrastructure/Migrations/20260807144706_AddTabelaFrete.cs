using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTabelaFrete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesFrete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValorBase = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ValorPorItem = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PrazoBaseDias = table.Column<int>(type: "int", nullable: false),
                    FreteGratisAcimaDe = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesFrete", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegioesFrete",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DigitoCep = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Fator = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DiasExtras = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CriadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegioesFrete", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConfiguracoesFrete",
                columns: new[] { "Id", "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor", "FreteGratisAcimaDe", "PrazoBaseDias", "ValorBase", "ValorPorItem" },
                values: new object[] { 1, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 0m, 2, 12.90m, 2.50m });

            migrationBuilder.InsertData(
                table: "RegioesFrete",
                columns: new[] { "Id", "Ativo", "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor", "DiasExtras", "DigitoCep", "Fator", "Nome" },
                values: new object[,]
                {
                    { 1, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 1, 0, 1.0m, "Grande São Paulo" },
                    { 2, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 0, 1, 0.8m, "Interior de SP" },
                    { 3, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 2, 2, 1.2m, "RJ / ES" },
                    { 4, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 2, 3, 1.3m, "Minas Gerais" },
                    { 5, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 4, 4, 1.6m, "BA / SE" },
                    { 6, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 5, 5, 1.8m, "PE / AL / PB / RN" },
                    { 7, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 6, 6, 1.9m, "CE / PI / MA / Norte" },
                    { 8, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 4, 7, 1.5m, "DF / GO / TO / MT / MS" },
                    { 9, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 3, 8, 1.3m, "PR / SC" },
                    { 10, true, null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", 3, 9, 1.4m, "Rio Grande do Sul" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_RegiaoFrete_Digito",
                table: "RegioesFrete",
                column: "DigitoCep",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesFrete");

            migrationBuilder.DropTable(
                name: "RegioesFrete");
        }
    }
}
