using Microsoft.AspNetCore.SignalR;

namespace GameHub.Web.Hubs;

/// <summary>
/// Hub de notificações em tempo real. Cada usuário entra num grupo com o seu id
/// (<c>user-{id}</c>), então dá pra empurrar um aviso para UMA pessoa específica
/// (ex.: "sua troca foi aceita") ou para TODOS (<c>Clients.All</c>, ex.: "nova troca").
/// </summary>
public class NotificacaoHub : Hub
{
    public static string GrupoDoUsuario(string userId) => $"user-{userId}";

    // O navegador chama isto ao conectar, informando de quem é a conexão.
    public Task Registrar(string userId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GrupoDoUsuario(userId));
}
