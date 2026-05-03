using System.Text.Json;

namespace DraftosaurusClient.Services;

public class JogadorSessao
{
    public int IdPartida { get; set; }
    public string NomeJogador { get; set; } = "";
    public int IdJogador { get; set; }
    public string SenhaJogador { get; set; } = "";
}

public static class SessionStore
{
    private static readonly string Pasta = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DraftosaurusClient");

    private static readonly string Arquivo = Path.Combine(Pasta, "sessoes.json");

    public static JogadorSessao? Buscar(int idPartida, string nomeJogador)
    {
        return Carregar().FirstOrDefault(s =>
            s.IdPartida == idPartida &&
            string.Equals(s.NomeJogador, nomeJogador, StringComparison.OrdinalIgnoreCase));
    }

    public static void Salvar(JogadorSessao sessao)
    {
        var sessoes = Carregar();
        sessoes.RemoveAll(s =>
            s.IdPartida == sessao.IdPartida &&
            string.Equals(s.NomeJogador, sessao.NomeJogador, StringComparison.OrdinalIgnoreCase));

        sessoes.Add(sessao);

        Directory.CreateDirectory(Pasta);
        var json = JsonSerializer.Serialize(sessoes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Arquivo, json);
    }

    public static void Remover(int idPartida, string nomeJogador)
    {
        var sessoes = Carregar();
        sessoes.RemoveAll(s =>
            s.IdPartida == idPartida &&
            string.Equals(s.NomeJogador, nomeJogador, StringComparison.OrdinalIgnoreCase));

        Directory.CreateDirectory(Pasta);
        var json = JsonSerializer.Serialize(sessoes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Arquivo, json);
    }

    private static List<JogadorSessao> Carregar()
    {
        try
        {
            if (!File.Exists(Arquivo)) return new List<JogadorSessao>();
            var json = File.ReadAllText(Arquivo);
            return JsonSerializer.Deserialize<List<JogadorSessao>>(json) ?? new List<JogadorSessao>();
        }
        catch
        {
            return new List<JogadorSessao>();
        }
    }
}
