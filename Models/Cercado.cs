namespace DraftosaurusClient.Models;

/// <summary>
/// Representa um cercado do tabuleiro.
/// Lado Verão: FI, MT, RS, CD, PA, IS, RI
/// Lado Inverno: FB (Floresta Bem Ordenada), PE + PD (Ponte dos Amantes
///   esquerda/direita), PI (Pirâmide), VG (Vigia), QU (Quarentena), RI
///
/// IMPORTANTE: os códigos do inverno são CHUTES baseados nos nomes do manual.
/// A DLL pode usar códigos diferentes — ao iniciar a partida,
/// `DraftService.ListarCercados()` consulta os códigos REAIS da DLL.
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

    // ============================================================
    // VERÃO
    // ============================================================
    public static readonly Dictionary<string, CercadoInfo> CercadosVerao = new()
    {
        { "FI", new("Floresta da Igualdade", 6, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "MT", new("Mata Tripla",           3, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "RS", new("Rei da Selva",          1, LadoTabuleiro.Floresta, LateralTabuleiro.Banheiros,   TipoCercado.Unico)  },
        { "CD", new("Campina da Diferença",  6, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Linear) },
        { "PA", new("Pradaria do Amor",      6, LadoTabuleiro.Pradaria, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "IS", new("Ilha Solitária",        1, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Unico)  },
        { "RI", new("Rio",                  12, LadoTabuleiro.Rio,      LateralTabuleiro.Centro,      TipoCercado.Rio)    }
    };

    // ============================================================
    // INVERNO
    // ============================================================
    public static readonly Dictionary<string, CercadoInfo> CercadosInverno = new()
    {
        { "FB", new("Floresta Bem Ordenada", 6, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Alternada) },
        { "PE", new("Ponte (esquerda)",      6, LadoTabuleiro.Floresta, LateralTabuleiro.Alimentacao, TipoCercado.Linear) },
        { "PD", new("Ponte (direita)",       6, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Linear) },
        { "PI", new("Pirâmide",              6, LadoTabuleiro.Pradaria, LateralTabuleiro.Alimentacao, TipoCercado.Piramide) },
        { "VG", new("Vigia",                 1, LadoTabuleiro.Floresta, LateralTabuleiro.Banheiros,   TipoCercado.Unico) },
        { "QU", new("Quarentena",            1, LadoTabuleiro.Pradaria, LateralTabuleiro.Banheiros,   TipoCercado.Unico) },
        { "RI", new("Rio",                  12, LadoTabuleiro.Rio,      LateralTabuleiro.Centro,      TipoCercado.Rio) }
    };

    public static Dictionary<string, CercadoInfo> CercadosPorLado(LadoMapa lado) =>
        lado == LadoMapa.Inverno ? CercadosInverno : CercadosVerao;
}

public record CercadoInfo(
    string Nome,
    int Capacidade,
    LadoTabuleiro Lado,
    LateralTabuleiro Lateral,
    TipoCercado Tipo);

public enum LadoTabuleiro { Floresta, Pradaria, Rio }
public enum LateralTabuleiro { Alimentacao, Banheiros, Centro }
public enum LadoMapa { Verao, Inverno }

/// <summary>Como o cercado é renderizado (afeta layout dos dinos).</summary>
public enum TipoCercado
{
    Linear,     // FI, CD, MT, FB, PE, PD - linha de slots
    Unico,      // RS, IS, VG, QU - 1 só slot grande
    Rio,        // RI - vertical
    Piramide,   // PI - 3+2+1
    Alternada   // FB - linha mas com indicador visual de alternância
}
