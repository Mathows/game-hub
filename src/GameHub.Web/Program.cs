using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GameHub.Web.Components;
using GameHub.Web.Components.Account;
using GameHub.Web.Data;
using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Repositories;
using GameHub.Infrastructure.Services;
using GameHub.Domain.Interfaces;
using GameHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Contexto da LOJA (jogos, clientes, pedidos, aluguéis, trocas).
// Usa o MESMO banco (GameHubDb) e a MESMA conexão do login.
builder.Services.AddDbContext<GameHubDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositórios da loja (Injeção de Dependência).
// Scoped = uma instância por requisição/página.
builder.Services.AddScoped<IJogoRepository, JogoRepository>();

// Carrinho de compras: Scoped = um carrinho por usuário (por circuito SignalR).
// Se fosse Singleton, todos os usuários dividiriam o mesmo carrinho.
builder.Services.AddScoped<CarrinhoService>();

// Serviço que fecha a compra (cria Pedido + baixa estoque numa transação).
// Scoped porque usa o GameHubDbContext.
builder.Services.AddScoped<IPedidoService, PedidoService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

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

app.Run();
