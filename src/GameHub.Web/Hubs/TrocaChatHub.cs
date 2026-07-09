using Microsoft.AspNetCore.SignalR;
using GameHub.Web.Services;

namespace GameHub.Web.Hubs;

/// <summary>
/// Hub SignalR do chat de trocas. Um "Hub" é a central de mensagens no servidor: o navegador
/// chama métodos aqui (EntrarNaTroca/Enviar) e o servidor empurra métodos de volta pro navegador
/// (ReceberMensagem/Historico) — a "mão dupla" do WebSocket.
///
/// Usamos GRUPOS (um por troca): assim a mensagem só vai para quem está naquela troca.
/// </summary>
public class TrocaChatHub : Hub
{
    private readonly ChatTrocaStore _store;

    public TrocaChatHub(ChatTrocaStore store) => _store = store;

    private static string Grupo(int trocaId) => $"troca-{trocaId}";

    // Navegador → servidor: entrar na "sala" da troca e receber o histórico.
    public async Task EntrarNaTroca(int trocaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(trocaId));
        await Clients.Caller.SendAsync("Historico", _store.Obter(trocaId));
    }

    // Navegador → servidor: enviar uma mensagem, que é empurrada para todos do grupo.
    public async Task Enviar(int trocaId, string autor, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;

        var msg = new MensagemChat(autor, texto.Trim(), DateTime.Now);
        _store.Adicionar(trocaId, msg);

        // Servidor → navegadores do grupo (tempo real!)
        await Clients.Group(Grupo(trocaId)).SendAsync("ReceberMensagem", msg);
    }
}
