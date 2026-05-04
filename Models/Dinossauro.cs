namespace DraftosaurusClient.Models;

/// <summary>
/// Representa uma especie de dinossauro.
/// </summary>
public class Dinossauro
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public Color Cor { get; set; } = Color.Gray;

    public static readonly Dictionary<string, Color> CoresPadrao = new()
    {
        { "Br", Color.MediumPurple },
        { "Ep", Color.DarkOrange },
        { "Et", Color.RoyalBlue },
        { "Pa", Color.ForestGreen },
        { "Ti", Color.Firebrick },
        { "Tr", Color.Gold }
    };

    public static readonly Dictionary<string, string> NomesPadrao = new()
    {
        { "Br", "Braquiossauro" },
        { "Ep", "Espinossauro" },
        { "Et", "Estegossauro" },
        { "Pa", "Parassaurolofo" },
        { "Ti", "Tiranossauro" },
        { "Tr", "Triceratops" }
    };

    // Esta funcao cuida de iniciar 'CorPorCodigo' do programa.
    public static Color CorPorCodigo(string codigo) =>
        CoresPadrao.TryGetValue(codigo, out var c) ? c : Color.Gray;

    // A funcao serve para iniciar 'NomePorCodigo' do programa.
    public static string NomePorCodigo(string codigo) =>
        NomesPadrao.TryGetValue(codigo, out var n) ? n : codigo;
}

