using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace GameHub.Web.Seguranca;

/// <summary>
/// Limites de requisição (rate limiting) do GameHub.
///
/// O QUE ISSO IMPEDE: um script tentando 10 mil senhas no login, um robô criando contas em
/// massa, alguém baixando o pacote inteiro da contabilidade em looping. Sem limite, um
/// atacante testa à vontade — e a senha mais fraca da base é só questão de tempo.
///
/// ⚠️ POR QUE NÃO É GLOBAL: o Blazor Server conversa por um WebSocket em /_blazor. Se um
/// limitador global contasse cada mensagem do circuito, a interface seria estrangulada em
/// uso normal (e o usuário veria a página "congelar"). Aqui os limites são aplicados
/// SELETIVAMENTE, nos endpoints de formulário e download.
/// </summary>
public static class RateLimiting
{
    /// <summary>Login, cadastro, recuperação de senha — onde vale a pena tentar força bruta.</summary>
    public const string Autenticacao = "autenticacao";

    /// <summary>Downloads pesados (pacote da contabilidade, XML/PDF de nota).</summary>
    public const string Downloads = "downloads";

    /// <summary>Webhook do provedor de pagamento — aberto ao mundo, logo precisa de teto.</summary>
    public const string Webhook = "webhook";

    public static void AddLimitesGameHub(this RateLimiterOptions options)
    {
        // Resposta padrão: 429 Too Many Requests (e não 500). O código correto importa —
        // é assim que um cliente legítimo sabe que deve esperar, não que houve erro.
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (contexto, _) =>
        {
            // Retry-After diz QUANTO esperar. Sem ele, o cliente fica no escuro e insiste.
            if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
            {
                contexto.HttpContext.Response.Headers.RetryAfter =
                    ((int)espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }

            await contexto.HttpContext.Response.WriteAsync(
                "Muitas tentativas em pouco tempo. Aguarde um momento e tente novamente.");
        };

        // ---- AUTENTICAÇÃO: 10 tentativas por 5 minutos, por IP ----
        // Janela FIXA (não deslizante) de propósito: é simples de raciocinar e suficiente
        // aqui. O limite é generoso para quem erra a senha e apertado para quem varre.
        options.AddPolicy(Autenticacao, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ChaveDoCliente(contexto),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0   // não enfileira: rejeita na hora (fila só atrasaria o ataque)
                }));

        // ---- O LIMITADOR GLOBAL, que só limita o que interessa ----
        //
        // As telas de login/cadastro são componentes Razor, não endpoints próprios: não dá
        // para pendurar .RequireRateLimiting() nelas sem atingir TODA página do site.
        //
        // A saída é um limitador global que PARTICIONA POR CAMINHO: as rotas de autenticação
        // caem numa janela apertada; todo o resto — inclusive /_blazor, o WebSocket do
        // circuito — cai em NoLimiter, que não conta nada.
        //
        // Este é o coração do rate limiting: a PARTIÇÃO. Cada chave tem seu próprio balde de
        // permissões. Errar a partição é o que produz os dois desastres clássicos: partição
        // ampla demais estrangula usuário legítimo; estreita demais não barra ninguém.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        {
            var caminho = contexto.Request.Path;

            var ehAutenticacao =
                caminho.StartsWithSegments("/Account/Login") ||
                caminho.StartsWithSegments("/Account/Register") ||
                caminho.StartsWithSegments("/Account/ForgotPassword") ||
                caminho.StartsWithSegments("/Account/ResetPassword") ||
                caminho.StartsWithSegments("/Account/PerformExternalLogin");

            // SÓ CONTA O ENVIO DO FORMULÁRIO, não a visita à tela.
            //
            // A primeira versão contava GET também — e um teste mostrou o estrago: abrir a
            // página de login 11 vezes (recarregar, voltar, hesitar) já bloqueava a pessoa,
            // sem que ela tivesse digitado uma senha. Ver a tela não é tentativa.
            //
            // Esse é o erro mais comum em rate limiting: calibrar contra o atacante e
            // esquecer o usuário legítimo. Bloquear cliente que quer comprar custa mais
            // caro que permitir dez tentativas de senha a mais — e o Identity ainda tem o
            // lockout por conta como segunda camada.
            var ehTentativa = !HttpMethods.IsGet(contexto.Request.Method);

            if (!ehAutenticacao || !ehTentativa) return RateLimitPartition.GetNoLimiter("livre");

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"auth:{ChaveDoCliente(contexto)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                });
        });

        // ---- DOWNLOADS: 20 por minuto, por IP ----
        options.AddPolicy(Downloads, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ChaveDoCliente(contexto),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // ---- WEBHOOK: 60 por minuto, com pequena fila ----
        // Aqui a fila FAZ sentido: um provedor legítimo pode disparar uma rajada de avisos
        // (vários pagamentos confirmados juntos). Enfileirar entrega tudo com um atraso
        // mínimo, em vez de recusar notificação de pagamento — que é dinheiro.
        options.AddPolicy(Webhook, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ChaveDoCliente(contexto),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    }

    /// <summary>
    /// Quem é o "cliente" para efeito de limite: o usuário logado, ou o IP se anônimo.
    ///
    /// Usar o usuário quando houver um é melhor que só IP: numa rede compartilhada
    /// (escritório, faculdade) todos saem pelo mesmo IP, e um azarado seria bloqueado
    /// pelo excesso alheio. Para o login, no entanto, ainda não há usuário — sobra o IP.
    /// </summary>
    private static string ChaveDoCliente(HttpContext contexto) =>
        contexto.User.Identity?.IsAuthenticated == true
            ? $"user:{contexto.User.Identity!.Name}"
            : $"ip:{contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";
}
