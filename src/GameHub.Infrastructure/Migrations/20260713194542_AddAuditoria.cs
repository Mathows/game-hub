using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtualizadoPor",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Pedidos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CriadoPor",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "Jogos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtualizadoPor",
                table: "Jogos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Jogos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CriadoPor",
                table: "Jogos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "Enderecos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtualizadoPor",
                table: "Enderecos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Enderecos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CriadoPor",
                table: "Enderecos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AtualizadoEm",
                table: "Clientes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AtualizadoPor",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CriadoEm",
                table: "Clientes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CriadoPor",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor" },
                values: new object[] { null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed" });

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor" },
                values: new object[] { null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed" });

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AtualizadoEm", "AtualizadoPor", "CriadoEm", "CriadoPor" },
                values: new object[] { null, null, new DateTime(2026, 6, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPor",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CriadoPor",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPor",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "CriadoPor",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "AtualizadoPor",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "CriadoPor",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "AtualizadoEm",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "AtualizadoPor",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "CriadoEm",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "CriadoPor",
                table: "Clientes");
        }
    }
}
