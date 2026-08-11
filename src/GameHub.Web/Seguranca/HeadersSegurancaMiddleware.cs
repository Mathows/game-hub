namespace GameHub.Web.Seguranca;

/// <summary>
/// Cabeçalhos de segurança em toda resposta.
///
/// A ideia geral: o navegador tem defesas embutidas, mas várias ficam DESLIGADAS até o
/// servidor pedir. Estes cabeçalhos são esse pedido — custam três linhas e fecham classes
/// inteiras de ataque (clickjacking, sniffing de tipo, vazamento de referer).
/// </summary>
public class HeadersSegurancaMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly bool _desenvolvimento;

    public HeadersSegurancaMiddleware(RequestDelegate proximo, IWebHostEnvironment ambiente)
    {
        _proximo = proximo;
        _desenvolvimento = ambiente.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var headers = contexto.Response.Headers;

        // nosniff: proíbe o navegador de "adivinhar" o tipo do conteúdo. Sem isso, um
        // arquivo enviado como .txt mas contendo <script> pode acabar executado como HTML.
        headers["X-Content-Type-Options"] = "nosniff";

        // Impede que o site seja carregado dentro de um <iframe> de outro domínio.
        // É a defesa contra CLICKJACKING: o atacante põe o GameHub invisível sobre a
        // página dele e a vítima clica em "Confirmar compra" pensando clicar em outra coisa.
        headers["X-Frame-Options"] = "DENY";

        // Ao sair do site, envia só a origem (https://gamehub...), não a URL inteira.
        // Sem isso, um link em /admin/etiqueta/9 revelaria essa URL ao site de destino.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Desliga APIs que a loja não usa. Se um script malicioso entrar, ele já não
        // consegue nem pedir câmera ou localização — reduz o dano de uma falha futura.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // ---- Content-Security-Policy: a defesa mais forte contra XSS ----
        // Ela diz de ONDE o navegador pode carregar cada tipo de recurso. Se um atacante
        // conseguir injetar <script src="site-dele.com">, o navegador SE RECUSA a baixar.
        //
        // ⚠️ Montar CSP com Blazor exige saber o que o framework precisa:
        //   - 'unsafe-inline' em script-src: o Blazor Server injeta scripts inline no HTML
        //     e o reCAPTCHA também. É uma concessão real — o ideal seria nonce por resposta,
        //     que é trabalhoso com o Blazor e fica anotado como melhoria.
        //   - google.com/gstatic: o reCAPTCHA v3 (Fase 6).
        //   - ws:/wss:: o WebSocket do circuito Blazor. SEM ISTO A INTERFACE NÃO FUNCIONA
        //     (o navegador bloquearia a conexão e nenhum botão responderia).
        //   - data: em img-src: usamos SVG/imagem embutida em alguns pontos.
        var csp = string.Join("; ",
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' https://www.google.com https://www.gstatic.com",
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
            "font-src 'self' https://fonts.gstatic.com data:",
            "img-src 'self' data: https:",
            "frame-src https://www.google.com",            // o iframe do desafio do reCAPTCHA
            "connect-src 'self' ws: wss:",                  // circuito SignalR do Blazor
            "form-action 'self'",                           // formulários não postam para fora
            "base-uri 'self'",                              // ninguém reescreve a base das URLs
            "frame-ancestors 'none'");                      // versão moderna do X-Frame-Options

        // Em desenvolvimento a CSP vai em modo RELATÓRIO: o navegador registra violações no
        // console mas não bloqueia. Assim descobrimos o que quebraria em produção sem
        // travar o trabalho — e sem a tentação de afrouxar a política definitiva.
        headers[_desenvolvimento ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy"] = csp;

        await _proximo(contexto);
    }
}

public static class HeadersSegurancaExtensions
{
    public static IApplicationBuilder UseHeadersSeguranca(this IApplicationBuilder app) =>
        app.UseMiddleware<HeadersSegurancaMiddleware>();
}
