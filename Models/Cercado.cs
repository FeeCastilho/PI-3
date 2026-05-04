namespace DraftosaurusClient.Models;

/// <summary>
/// Representa os cercados do tabuleiro de verao usados pela DLL.
/// </summary>
public class Cercado
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public LadoTabuleiro Lado { get; set; }
    public LateralTabuleiro Lateral { get; set; }
    public TipoCercado Tipo { get; set; } = TipoCercado.Linear;
    public int Capacidade { get; set; }
    public Rectangle Area { get; set; }
    public List<string> Dinossauros { get; set; } = new();

    public static readonly Dictionary<string, CercadoInfo> CercadosVerao = new()
    {
        { "FI", new("Floresta da Igualdade", 6, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "MT", new("Mata Tripla",           3, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "RS", new("Rei da Selva",          1, LadoTabuleiro.Floresta, LateralTabuleiro.Banheiros,   TipoCercado.Unico) },
        { "CD", new("Campina da Diferenca",  6, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Linear) },
        { "PA", new("Pradaria do Amor",      6, LadoTabuleiro.Pradaria, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "IS", new("Ilha Solitaria",        1, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Unico) },
        { "RI", new("Rio",                  12, LadoTabuleiro.Rio,      LateralTabuleiro.Centro,      TipoCercado.Rio) }
    };

    // A funcao serve para iniciar 'CercadosPorLado' do programa.
    public static Dictionary<string, CercadoInfo> CercadosPorLado(LadoMapa lado) => CercadosVerao;
}

// Esta funcao executa a etapa 'CercadoInfo' do programa.
public record CercadoInfo(
    string Nome,
    int Capacidade,
    LadoTabuleiro Lado,
    LateralTabuleiro Lateral,
    TipoCercado Tipo);

public enum LadoTabuleiro { Floresta, Pradaria, Rio }
public enum LateralTabuleiro { Alimentacao, Banheiros, Centro }
public enum LadoMapa { Verao }

public enum TipoCercado
{
    Linear,
    Unico,
    Rio
}

