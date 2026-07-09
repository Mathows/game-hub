namespace GameHub.Web.Services;

/// <summary>Uma mensagem do chat de uma troca.</summary>
public record MensagemChat(string Autor, string Texto, DateTime Quando);

/// <summary>
/// Guarda as mensagens de chat de cada troca EM MEMÓRIA (histórico enquanto o app está no ar).
/// Singleton — compartilhado por todos → precisa de trava (lock), igual à caixa de e-mails.
/// (Persistir no banco poderia vir depois; aqui o foco é aprender SignalR.)
/// </summary>
public class ChatTrocaStore
{
    private readonly Dictionary<int, List<MensagemChat>> _porTroca = new();
    private readonly object _trava = new();

    public void Adicionar(int trocaId, MensagemChat msg)
    {
        lock (_trava)
        {
            if (!_porTroca.TryGetValue(trocaId, out var lista))
                _porTroca[trocaId] = lista = new List<MensagemChat>();
            lista.Add(msg);
        }
    }

    public IReadOnlyList<MensagemChat> Obter(int trocaId)
    {
        lock (_trava)
            return _porTroca.TryGetValue(trocaId, out var lista)
                ? lista.ToList()
                : new List<MensagemChat>();
    }
}
