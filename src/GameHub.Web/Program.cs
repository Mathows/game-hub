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

// Repositórios da loja (Injeção de Dependência).
// Scoped = uma instância por requisição/página.
builder.Services.AddScoped<IJogoRepository, JogoRepository>();

// Agenda de endereços do cliente (Scoped, usa o DbContext).
builder.Services.AddScoped<IEnderecoService, EnderecoService>();

// Busca de CEP via ViaCEP (grátis). HttpClient TIPADO: a DI cria o ViaCepService já com
// um HttpClient configurado com a BaseAddress do ViaCEP. Trocar de provedor = trocar aqui.
builder.Services.AddHttpClient<ICepService, ViaCepService>(c =>
    c.BaseAddress = new Uri("https://viacep.com.br/"));

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
builder.Services.AddScoped<IAluguelService, AluguelService>();

// Nota fiscal simulada: só faz formatação (sem estado) → Transient, como a calculadora.
builder.Services.AddTransient<NotaFiscalService>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Endpoint do webhook de pagamento (POST /webhooks/pagamento).
app.MapWebhookEndpoints();

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
