using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentimentoVersionado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TermosDeUso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Versao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResumoDasMudancas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermosDeUso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AceitesTermo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TermoDeUsoId = table.Column<int>(type: "int", nullable: false),
                    AceitoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpOrigem = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceitesTermo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AceitesTermo_TermosDeUso_TermoDeUsoId",
                        column: x => x.TermoDeUsoId,
                        principalTable: "TermosDeUso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceitesTermo_TermoDeUsoId",
                table: "AceitesTermo",
                column: "TermoDeUsoId");

            migrationBuilder.CreateIndex(
                name: "UX_AceiteTermo_Usuario_Termo",
                table: "AceitesTermo",
                columns: new[] { "ApplicationUserId", "TermoDeUsoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TermoDeUso_Versao",
                table: "TermosDeUso",
                column: "Versao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TermoDeUso_Vigente",
                table: "TermosDeUso",
                column: "Vigente",
                unique: true,
                filter: "[Vigente] = 1");

            // ---- A primeira versão do termo, publicada junto com a estrutura ----
            // Vai NA MIGRATION (e não num seed em código) porque é dado de referência:
            // qualquer ambiente novo — máquina de outro dev, CI, produção — nasce com o
            // mesmo termo 1.0, sem depender de alguém rodar um script à parte.
            migrationBuilder.InsertData(
                table: "TermosDeUso",
                columns: new[] { "Versao", "Titulo", "Conteudo", "ResumoDasMudancas", "PublicadoEm", "Vigente" },
                values: new object[]
                {
                    "1.0",
                    "Termos de Uso e Política de Privacidade",
                    TEXTO_V1,
                    null!,
                    new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Unspecified),
                    true
                });
        }

        /// <summary>
        /// Texto da versão 1.0. Fica como constante da migration porque um termo publicado
        /// é IMUTÁVEL: mudar este texto depois falsificaria o que as pessoas aceitaram.
        /// Alteração real = nova versão (1.1), nunca edição da anterior.
        /// </summary>
        private const string TEXTO_V1 = """
            ## 1. Quem somos
            O GameHub é uma loja de jogos que vende, aluga e intermedeia trocas entre usuários.
            O controlador dos dados é o GameHub Comércio de Jogos.

            ## 2. Quais dados coletamos e por quê
            - **Nome, e-mail e senha:** identificar você e proteger sua conta.
            - **CPF ou CNPJ:** obrigatório para emitir a nota fiscal da sua compra
              (exigência da legislação fiscal, não uma escolha nossa).
            - **Endereço:** calcular o frete e entregar o pedido.
            - **Histórico de pedidos, aluguéis e trocas:** prestar o serviço e cumprir a
              obrigação legal de guardar documentos fiscais por 5 anos.
            - **IP, data e navegador do seu aceite:** provar que este consentimento existiu.

            ## 3. Com quem compartilhamos
            Apenas com quem é necessário para o serviço funcionar: o emissor da nota fiscal,
            o meio de pagamento e a transportadora. Nunca vendemos seus dados.

            ## 4. Seus direitos
            Você pode, a qualquer momento, em "Configurações da conta → Dados pessoais":
            - **baixar** tudo o que guardamos sobre você, num arquivo único;
            - **encerrar sua conta**, apagando seus dados pessoais.

            Ao encerrar, pedidos e notas fiscais já emitidos permanecem registrados **sem
            identificar você** — a lei fiscal nos obriga a guardá-los por 5 anos, e a LGPD
            permite conservar dados para cumprir obrigação legal (art. 16, I).

            ## 5. Segurança
            Sua senha é guardada como hash (não temos como lê-la, nem nós). O acesso ao
            painel administrativo é restrito e todas as alterações de dados ficam registradas.

            ## 6. Mudanças neste termo
            Se este texto mudar, publicamos uma nova versão e pedimos seu aceite novamente,
            mostrando o que mudou. As versões anteriores continuam guardadas.
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceitesTermo");

            migrationBuilder.DropTable(
                name: "TermosDeUso");
        }
    }
}
