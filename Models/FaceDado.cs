namespace DraftosaurusClient.Models;

/// <summary>
/// Faces do dado de colocação. Códigos: AL, FL, PR, TI, VZ, WC.
/// </summary>
public class FaceDado
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";

    public static readonly Dictionary<string, (string nome, string desc)> Faces = new()
    {
        { "AL", ("Alimentação",     "Posicionar no lado da praça de alimentação (esquerda)") },
        { "FL", ("Floresta",        "Posicionar na seção florestal (topo)") },
        { "PR", ("Pradaria",        "Posicionar na seção de pradarias (baixo)") },
        { "TI", ("Cuidado T-Rex",   "Posicionar em cercado SEM T-Rex") },
        { "VZ", ("Cercado Vazio",   "Posicionar em cercado vazio") },
        { "WC", ("Banheiros",       "Posicionar no lado dos banheiros (direita)") }
    };

    public static string NomePorCodigo(string codigo) =>
        Faces.TryGetValue(codigo, out var v) ? v.nome : codigo;

    public static string DescricaoPorCodigo(string codigo) =>
        Faces.TryGetValue(codigo, out var v) ? v.desc : "";
}
