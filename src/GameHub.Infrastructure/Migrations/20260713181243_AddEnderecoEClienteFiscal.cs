using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnderecoEClienteFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Clientes");

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Bairro",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Cep",
                table: "Pedidos",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Cidade",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Complemento",
                table: "Pedidos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Logradouro",
                table: "Pedidos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Numero",
                table: "Pedidos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEntrega_Uf",
                table: "Pedidos",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Telefone",
                table: "Clientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpfCnpj",
                table: "Clientes",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoPessoa",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Enderecos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Principal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enderecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enderecos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enderecos_ClienteId",
                table: "Enderecos",
                column: "ClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enderecos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Bairro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Cep",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Cidade",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Complemento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Logradouro",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Numero",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "EnderecoEntrega_Uf",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CpfCnpj",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TipoPessoa",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Telefone",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
