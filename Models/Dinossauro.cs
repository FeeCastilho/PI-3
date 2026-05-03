namespace DraftosaurusClient.Models;

/// <summary>
/// Representa um dinossauro do jogo, com código, nome e cor.
/// Códigos: Br, Ep, Et, Pa, Ti, Tr.
/// </summary>
public class Dinossauro
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public Color Cor { get; set; } = Color.Gray;

    /// <summary>Mapa estático código -> cor, usado quando a DLL não devolve o RGB.</summary>
    public static readonly Dictionary<string, Color> CoresPadrao = new()
    {
        { "Br", Color.MediumPurple },   // Braquiossauro - Roxo
        { "Ep", Color.DarkOrange },     // Espinossauro - Laranja
        { "Et", Color.RoyalBlue },      // Estegossauro - Azul
        { "Pa", Color.ForestGreen },    // Parasaurolófo - Verde
        { "Ti", Color.Firebrick },      // Tiranossauro - Vermelho
        { "Tr", Color.Gold }            // Tricerátops - Amarelo
    };

    public static readonly Dictionary<string, string> NomesPadrao = new()
    {
        { "Br", "Braquiossauro" },
        { "Ep", "Espinossauro" },
        { "Et", "Estegossauro" },
        { "Pa", "Parasaurolófo" },
        { "Ti", "Tiranossauro" },
        { "Tr", "Tricerátops" }
    };

    public static Color CorPorCodigo(string codigo) =>
        CoresPadrao.TryGetValue(codigo, out var c) ? c : Color.Gray;

    public static string NomePorCodigo(string codigo) =>
        NomesPadrao.TryGetValue(codigo, out var n) ? n : codigo;
}
