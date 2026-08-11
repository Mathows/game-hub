using Microsoft.AspNetCore.Authorization;

namespace GameHub.Web.Autorizacao;

/// <summary>
/// As políticas de autorização do GameHub, num só lugar.
///
/// POR QUE POLICY EM VEZ DE Roles="Admin" ESPALHADO:
/// `[Authorize(Roles = "Admin")]` grava a REGRA em cada tela. No dia em que a loja tiver
/// um papel "Operador" que também despacha pedidos, é preciso caçar todos os arquivos e
/// editar cada um — e o que ficar esquecido se transforma em falha de segurança silenciosa.
///
/// Com policy, a tela declara a INTENÇÃO ("PodeDespachar") e a regra vive aqui. Muda em um
/// lugar, vale em todos. É o mesmo princípio de programar contra interface, aplicado à
/// autorização: o consumidor depende do "o quê", não do "como".
/// </summary>
public static class Policies
{
    /// <summary>Administração da loja em geral.</summary>
    public const string Administrar = "Administrar";

    /// <summary>Despachar pedidos (expedição/etiqueta).</summary>
    public const string PodeDespachar = "PodeDespachar";

    /// <summary>Emitir e gerenciar documentos fiscais / contabilidade.</summary>
    public const string PodeEmitirFiscal = "PodeEmitirFiscal";

    /// <summary>Comprar um jogo respeitando a classificação indicativa (baseada em recurso).</summary>
    public const string RespeitaClassificacao = "RespeitaClassificacao";

    public static void AddPoliticasGameHub(this AuthorizationOptions options)
    {
        // Hoje as três de admin exigem o mesmo papel. Estão separadas de propósito: quando
        // aparecer um papel "Operador" (que despacha mas não emite nota), muda-se UMA linha
        // aqui e nenhuma tela precisa ser tocada.
        options.AddPolicy(Administrar, p => p.RequireRole("Admin"));
        options.AddPolicy(PodeDespachar, p => p.RequireRole("Admin"));
        options.AddPolicy(PodeEmitirFiscal, p => p.RequireRole("Admin"));

        // Esta NÃO é sobre papel: é sobre idade × classificação do jogo. Precisa do recurso
        // (o Jogo) para decidir, então é avaliada via IAuthorizationService.AuthorizeAsync
        // passando o jogo — não dá para usar [Authorize] em cima da página, porque o
        // atributo não conhece o objeto sendo acessado.
        options.AddPolicy(RespeitaClassificacao,
            p => p.AddRequirements(new ClassificacaoIndicativaRequirement()));
    }
}
