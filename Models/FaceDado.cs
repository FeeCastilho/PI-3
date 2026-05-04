namespace DraftosaurusClient.Models;

/// <summary>
/// Faces do dado de colocacao.
/// </summary>
public class FaceDado
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";

    public static readonly Dictionary<string, (string nome, string desc)> Faces = new()
    {
        { "AL", ("Alimentacao",   "Posicionar no lado da praca de alimentacao (esquerda)") },
        { "FL", ("Floresta",      "Posicionar na secao florestal (topo)") },
        { "PR", ("Pradaria",      "Posicionar na secao de pradarias (baixo)") },
        { "TI", ("Cuidado T-Rex", "Posicionar em cercado SEM T-Rex") },
        { "VZ", ("Cercado Vazio", "Posicionar em cercado vazio") },
        { "WC", ("Banheiros",     "Posicionar no lado dos banheiros (direita)") }
    };

    // Esta funcao executa a etapa 'NomePorCodigo' do programa.
    public static string NomePorCodigo(string codigo) =>
        Faces.TryGetValue(codigo, out var v) ? v.nome : codigo;

    // Esta funcao cuida de iniciar 'DescricaoPorCodigo' do programa.
    public static string DescricaoPorCodigo(string codigo) =>
        Faces.TryGetValue(codigo, out var v) ? v.desc : "";
}

