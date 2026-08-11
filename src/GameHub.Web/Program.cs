using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GameHub.Web.Components;
using GameHub.Web.Components.Account;
using GameHub.Web.Data;
using GameHub.Web.Endpoints;
using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Repositories;
using GameHub.Infrastructure.Services;
using GameHub.Infrastructure.NHib;
using GameHub.Domain.Interfaces;
using GameHub.Domain.Services;
using GameHub.Web.Services;
using GameHub.Web.Hubs;
using GameHub.Web.Autorizacao;
using GameHub.Web.Seguranca;
using Microsoft.AspNetCore.Authorization;
using NHibernate;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR para o nosso Hub de chat (Fase 5) + a "caixa" de mensagens em memória (Singleton).
builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatTrocaStore>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Autenticação: os cookies do Identity + (opcional) login externo com Google.
// Guardamos o AuthenticationBuilder porque AddIdentityCookies() devolve OUTRO tipo
// (IdentityCookiesBuilder) que não tem AddGoogle — o AddGoogle vive no AuthenticationBuilder.
var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });
authBuilder.AddIdentityCookies();

// Fase 6 · Login com Google (OAuth 2.0).
// As credenciais (ClientId/ClientSecret) vêm de USER-SECRETS, NUNCA do appsettings/Git
// (princípio de segurança do Sistema.md — segredo não vai pro repositório).
//   dotnet user-secrets set "Authentication:Google:ClientId" "SEU_ID"
//   dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU_SEGREDO"
// Só registramos o Google SE as credenciais existirem — assim o app sobe normalmente
// mesmo antes de você configurar (o botão "Google" só aparece quando estiver pronto).
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        // Rota de callback padrão do handler: /signin-google
        // (precisa bater com a "URI de redirecionamento autorizada" no Google Cloud Console).
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// "Quem está logado agora" (lê o HttpContext) + auditoria automática.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddScoped<AuditoriaInterceptor>();

// Contexto da LOJA (jogos, clientes, pedidos, aluguéis, trocas).
// Usa o MESMO banco (GameHubDb) e a MESMA conexão do login.
// O overload (sp, options) permite injetar o AuditoriaInterceptor (que depende do IUsuarioAtual):
// assim TODO SaveChanges deste contexto carimba a auditoria sozinho.
builder.Services.AddDbContext<GameHubDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()));

// FÁBRICA de DbContext — para componentes que rodam no LAYOUT (em toda página).
//
// O PROBLEMA que isto resolve: o DbContext Scoped é UM por requisição/circuito, e ele NÃO
// suporta duas operações ao mesmo tempo. No Blazor, componentes renderizam de forma
// assíncrona: a página consulta jogos enquanto o layout consulta o termo vigente — mesma
// instância, duas queries simultâneas → "A second operation was started on this context".
//
// Com a fábrica, quem precisa consultar CRIA e DESCARTA seu próprio contexto: cada operação
// fica isolada. Lifetime Scoped (não o Singleton padrão) porque o interceptor de auditoria
// que configuramos acima é Scoped — um factory singleton não conseguiria resolvê-lo.
builder.Services.AddDbContextFactory<GameHubDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>()),
    lifetime: ServiceLifetime.Scoped);

// Repositórios da loja (Injeção de Dependência).
// Scoped = uma instância por requisição/página.
builder.Services.AddScoped<IJogoRepository, JogoRepository>();

// Agenda de endereços do cliente (Scoped, usa o DbContext).
builder.Services.AddScoped<IEnderecoService, EnderecoService>();

// Dados fiscais do cliente (CPF/CNPJ validado no servidor — Fase 8).
builder.Services.AddScoped<IClienteService, ClienteService>();

// Cupom de desconto: prévia no carrinho (a validação que vale é a do PedidoService).
builder.Services.AddScoped<ICupomService, CupomService>();

// Estoque: extrato + ajuste manual (Scoped, usa o DbContext).
builder.Services.AddScoped<IEstoqueService, EstoqueService>();

// Frete (Fase 10): tabela PRÓPRIA — fórmula no código, VALORES NO BANCO (editáveis em
// /admin/frete, sem deploy). Correios real exige contrato pago; provider plugável.
builder.Services.AddScoped<IFreteService, FreteTabelaService>();

// Vender pra loja: workflow de aprovação (Scoped, usa o DbContext).
builder.Services.AddScoped<IPropostaVendaService, PropostaVendaService>();

// LGPD (Fase 11): exportar/anonimizar os dados do titular que vivem na LOJA. O Identity
// só conhece o AspNetUsers — este serviço cobre Cliente, endereços, pedidos e notas.
builder.Services.AddScoped<IDadosPessoaisService, DadosPessoaisService>();

// LGPD (Fase 11): consentimento VERSIONADO — guarda qual versão do termo cada pessoa
// aceitou, com data, IP e navegador (o ônus de provar o consentimento é do controlador).
builder.Services.AddScoped<IConsentimentoService, ConsentimentoService>();

// RATE LIMITING (Fase 11 · P5): limites por endpoint. NÃO global — o WebSocket do Blazor
// (/_blazor) seria estrangulado e a interface travaria em uso normal.
builder.Services.AddRateLimiter(options => options.AddLimitesGameHub());

// AUTORIZAÇÃO (Fase 11 · P4): políticas nomeadas + o handler da classificação indicativa.
// O handler é Scoped porque recebe um ILogger; ele não guarda estado entre chamadas.
builder.Services.AddAuthorization(options => options.AddPoliticasGameHub());
builder.Services.AddScoped<IAuthorizationHandler, ClassificacaoIndicativaHandler>();

// Dashboard do admin: consultas agregadas (Scoped, usa o DbContext).
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Busca de CEP via ViaCEP (grátis). HttpClient TIPADO: a DI cria o ViaCepService já com
// um HttpClient configurado com a BaseAddress do ViaCEP. Trocar de provedor = trocar aqui.
builder.Services.AddHttpClient<ICepService, ViaCepService>(c =>
    c.BaseAddress = new Uri("https://viacep.com.br/"));

// reCAPTCHA v3 (anti-robô no cadastro). Mesmo padrão condicional do Google/Gmail:
// com a SECRET (user-secrets "Recaptcha:SecretKey") usa a verificação real no Google;
// sem ela, o serviço "desativado" (sempre aprova) — o app roda nos dois modos.
// A SiteKey (pública) fica no appsettings ("Recaptcha:SiteKey") e vai pro script da página.
var recaptchaSecret = builder.Configuration["Recaptcha:SecretKey"];
if (!string.IsNullOrWhiteSpace(recaptchaSecret))
{
    builder.Services.AddHttpClient("recaptcha", c => c.BaseAddress = new Uri("https://www.google.com/"));
    builder.Services.AddScoped<IRecaptchaService>(sp => new RecaptchaGoogleService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("recaptcha"),
        recaptchaSecret,
        sp.GetRequiredService<ILogger<RecaptchaGoogleService>>()));
}
else
{
    builder.Services.AddSingleton<IRecaptchaService, RecaptchaDesativadoService>();
}

// Carrinho de compras: Scoped = um carrinho por usuário (por circuito SignalR).
// Se fosse Singleton, todos os usuários dividiriam o mesmo carrinho.
builder.Services.AddScoped<CarrinhoService>();

// Serviço que fecha a compra (cria Pedido + baixa estoque numa transação).
// Scoped porque usa o GameHubDbContext.
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Aluguel: o serviço é Scoped (usa o DbContext); a calculadora é Transient
// (só faz conta, sem estado). Aqui os 3 tempos de vida convivem no projeto:
// Singleton (IEmailSender) · Scoped (repos/serviços/carrinho) · Transient (calculadora).
builder.Services.AddTransient<CalculadoraAluguel>();
// Rastreio (Fase 10): regra PURA (função do tempo), sem estado e sem I/O → Transient.
builder.Services.AddTransient<CalculadoraRastreio>();
builder.Services.AddScoped<IAluguelService, AluguelService>();

// Nota fiscal (Fase 8): PROVIDER plugável com INTERRUPTOR no appsettings
// ("NotaFiscal:Provider" = "Simulado" ou "PlugNotasSandbox"). O sandbox do PlugNotas é
// público (token fixo da documentação, sem cadastro/custo) — mesma API do FinFix real.
var providerNota = builder.Configuration["NotaFiscal:Provider"] ?? "Simulado";
if (providerNota.Equals("PlugNotasSandbox", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<INotaFiscalProvider, NotaFiscalProviderPlugNotas>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["NotaFiscal:PlugNotas:Url"] ?? "https://api.sandbox.plugnotas.com.br/");
        // Token PÚBLICO do sandbox (está na documentação do PlugNotas — não é segredo).
        c.DefaultRequestHeaders.Add("x-api-key",
            builder.Configuration["NotaFiscal:PlugNotas:Token"] ?? "2da392a6-79d2-4304-a8b7-959572c7e44d");
        c.Timeout = TimeSpan.FromSeconds(20);
    });
}
else
{
    builder.Services.AddScoped<INotaFiscalProvider, NotaFiscalProviderSimulado>();
}
// O emissor orquestra: número sequencial, status + histórico, transação.
builder.Services.AddScoped<IEmissorNotaFiscal, EmissorNotaFiscal>();

// Trocas (Fase 5): INTERRUPTOR de ORM. "Trocas:Orm" no appsettings escolhe a implementação.
// As telas usam SEMPRE a mesma interface ITrocaService — só a "cozinha" de dados muda.
var ormTrocas = builder.Configuration["Trocas:Orm"] ?? "EF";
if (ormTrocas.Equals("NHibernate", StringComparison.OrdinalIgnoreCase))
{
    // NHibernate: SessionFactory = Singleton (cara de criar); ISession = Scoped (como o DbContext).
    builder.Services.AddSingleton<ISessionFactory>(_ => NHibernateConfig.CriarSessionFactory(connectionString));
    builder.Services.AddScoped(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());
    builder.Services.AddScoped<ITrocaService, TrocaServiceNHibernate>();
}
else
{
    builder.Services.AddScoped<ITrocaService, TrocaService>();   // EF Core (padrão)
}

// Pagamento: confirma o pedido quando o webhook chega (Scoped, usa o DbContext).
builder.Services.AddScoped<IPagamentoService, PagamentoService>();

// Cobrança (Fase 9): provider plugável — simulado hoje (PIX/boleto em formato realista);
// sandbox do Mercado Pago amanhã, trocando só esta linha.
builder.Services.AddScoped<IPagamentoProvider, PagamentoProviderSimulado>();

// Fechamento da contabilidade (Fase 9): CSV índice + XMLs zipados. O HttpClient tipado
// já sai com o x-api-key do PlugNotas para baixar os XMLs (o token fica no servidor).
builder.Services.AddHttpClient<IContabilidadeService, ContabilidadeService>(c =>
{
    c.DefaultRequestHeaders.Add("x-api-key",
        builder.Configuration["NotaFiscal:PlugNotas:Token"] ?? "2da392a6-79d2-4304-a8b7-959572c7e44d");
    c.Timeout = TimeSpan.FromSeconds(30);
});

// HttpClient usado SÓ pelo simulador de pagamento (DEV) para chamar o nosso próprio
// webhook. O callback custom de certificado aceita o certificado de desenvolvimento
// do localhost (não usar isso em produção).
builder.Services.AddHttpClient("self")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;   // DEV: registra e já entra (sem confirmar e-mail)
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()                                            // habilita papéis (Admin/Cliente)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()   // adiciona claim "nome" + roles
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Nosso serviço de e-mail (confirmação de pedido). Singleton: caixa de saída única do app.
// AQUI a interface paga o investimento: com credenciais do Gmail (user-secrets) usamos o
// envio REAL (GmailSmtpService); sem elas, o simulado — e NENHUMA tela/serviço muda.
//   dotnet user-secrets set "Gmail:Usuario"  "seuemail@gmail.com"
//   dotnet user-secrets set "Gmail:SenhaApp" "xxxx xxxx xxxx xxxx"   (senha de APP, não a da conta)
var gmailUsuario = builder.Configuration["Gmail:Usuario"];
var gmailSenhaApp = builder.Configuration["Gmail:SenhaApp"];
if (!string.IsNullOrWhiteSpace(gmailUsuario) && !string.IsNullOrWhiteSpace(gmailSenhaApp))
{
    builder.Services.AddSingleton<IEmailService>(sp => new GmailSmtpService(
        gmailUsuario,
        gmailSenhaApp.Replace(" ", ""),   // o Google mostra a senha com espaços; removemos
        sp.GetRequiredService<ILogger<GmailSmtpService>>()));
}
else
{
    builder.Services.AddSingleton<IEmailService, EmailSimuladoService>();
}

var app = builder.Build();

// ---- Seed de papéis (roles) + concede "Admin" ao e-mail configurado (Admin:Email) ----
// Roda no start: garante que as roles existem e que o dono é Admin. Idempotente.
await using (var scope = app.Services.CreateAsyncScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var papel in new[] { "Admin", "Cliente" })
    {
        if (!await roleManager.RoleExistsAsync(papel))
            await roleManager.CreateAsync(new IdentityRole(papel));
    }

    var adminEmail = app.Configuration["Admin:Email"];
    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is not null && !await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}

// ---- Pipeline HTTP ----
// A ORDEM AQUI É A EXECUÇÃO: cada middleware envolve os seguintes. Headers de segurança vêm
// cedo (para valerem em toda resposta, inclusive nas de erro); o tratamento de erro tem de
// estar ANTES do que pode falhar, senão não captura nada.
app.UseHeadersSeguranca();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    // Em DEV mantemos a página de detalhe do erro (stack trace) — é ela que nos deu o
    // diagnóstico dos bugs do DbContext e do bool na query string.
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS: diz ao navegador "deste domínio, só aceite HTTPS" — por 30 dias, mesmo que o
    // usuário digite http://. Fecha a janela do ataque de downgrade na primeira requisição.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Rate limiter DEPOIS da autenticação de cookie (que roda dentro do MapRazorComponents):
// assim a chave do limite pode ser o usuário logado, e não só o IP.
app.UseRateLimiter();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Endpoint do webhook de pagamento (POST /webhooks/pagamento).
app.MapWebhookEndpoints();

// Download de XML/DANFE da nota (autenticado, valida o dono).
app.MapNotaFiscalEndpoints();

// Pacote mensal da contabilidade (só Admin).
app.MapContabilidadeEndpoints();

// Aceite dos termos (LGPD · Fase 11): POST de formulário, para o IP entrar na prova.
app.MapConsentimentoEndpoints();

// Data de nascimento (Fase 11 · P4): POST, porque precisa RENOVAR O COOKIE (claim novo).
app.MapPerfilEndpoints();

// Hub do chat de trocas (SignalR).
app.MapHub<TrocaChatHub>("/hubs/troca-chat");

// Hub de notificações em tempo real (SignalR).
app.MapHub<NotificacaoHub>("/hubs/notificacao");

// Endpoints EDUCATIVOS (só em desenvolvimento) para entender cache do DbContext e DI.
if (app.Environment.IsDevelopment())
{
    app.MapDemoEndpoints();
}

app.Run();
