using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificacaoIndicativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Classificacao",
                table: "Jogos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 1,
                column: "Classificacao",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 2,
                column: "Classificacao",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Jogos",
                keyColumn: "Id",
                keyValue: 3,
                column: "Classificacao",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classificacao",
                table: "Jogos");
        }
    }
}
