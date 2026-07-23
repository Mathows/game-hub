using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNomeSobrenomeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sobrenome",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nome",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Sobrenome",
                table: "AspNetUsers");
        }
    }
}
