using System.Data;
using System.Reflection;

namespace DraftosaurusClient.Helpers;

/// <summary>
/// Utilitarios para lidar com retornos da DLL DraftServer.
/// A DLL retorna DataTables / tipos simples a este helper normaliza isso.
/// </summary>
internal static class DllHelper
{
    /// <summary>
    /// Converte qualquer retorno da DLL para um DataTable, se possivel.
    /// A maioria dos metodos da DLL retorna DataTable ou DataSet.
    /// </summary>
    public static DataTable? AsDataTable(object? retorno)
    {
        if (retorno is null) return null;
        if (retorno is DataTable tabela) return tabela;
        if (retorno is DataSet conjuntoTabelas && conjuntoTabelas.Tables.Count > 0) return conjuntoTabelas.Tables[0];
        return null;
    }

    /// <summary>
    /// LA uma coluna como string, com fallback seguro.
    /// </summary>
    public static string Texto(DataRow linha, string coluna)
    {
        try
        {
            if (!linha.Table.Columns.Contains(coluna)) return "";
            var valor = linha[coluna];
            return valor == DBNull.Value ? "" : valor.ToString()?.Trim() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>LA uma coluna como int.</summary>
    public static int Inteiro(DataRow linha, string coluna)
    {
        try
        {
            if (!linha.Table.Columns.Contains(coluna)) return 0;
            var valor = linha[coluna];
            if (valor == DBNull.Value) return 0;
            return Convert.ToInt32(valor);
        }
        catch { return 0; }
    }

    /// <summary>LA uma coluna como DateTime.</summary>
    public static DateTime Data(DataRow linha, string coluna)
    {
        try
        {
            if (!linha.Table.Columns.Contains(coluna)) return DateTime.MinValue;
            var valor = linha[coluna];
            if (valor == DBNull.Value) return DateTime.MinValue;
            return Convert.ToDateTime(valor);
        }
        catch { return DateTime.MinValue; }
    }

    /// <summary>
    /// LA uma coluna por indice posicional (0, 1, 2...).
    /// util quando os nomes das colunas sao desconhecidos.
    /// </summary>
    public static string TextoNaPosicao(DataRow linha, int indice)
    {
        try
        {
            if (indice < 0 || indice >= linha.Table.Columns.Count) return "";
            var valor = linha[indice];
            return valor == DBNull.Value ? "" : valor.ToString()?.Trim() ?? "";
        }
        catch { return ""; }
    }

    // Esta funcao cuida de ler uma coluna pela posicao e converter para numero inteiro.
    public static int InteiroNaPosicao(DataRow linha, int indice)
    {
        try
        {
            if (indice < 0 || indice >= linha.Table.Columns.Count) return 0;
            var valor = linha[indice];
            if (valor == DBNull.Value) return 0;
            return Convert.ToInt32(valor);
        }
        catch { return 0; }
    }

    /// <summary>
    /// Imprime no console as colunas + primeiras 3 linhas a util para
    /// descobrir o esquema real retornado pela DLL durante o desenvolvimento.
    /// </summary>
    public static void DumpSchema(DataTable? tabela, string contexto = "")
    {
        if (tabela == null)
        {
            Console.WriteLine($"[DUMP {contexto}] DataTable nulo");
            return;
        }
        Console.WriteLine($"[DUMP {contexto}] {tabela.Rows.Count} linhas, {tabela.Columns.Count} colunas:");
        foreach (DataColumn coluna in tabela.Columns)
            Console.Write($"  [{coluna.ColumnName}:{coluna.DataType.Name}]");
        Console.WriteLine();
        for (int indiceLinha = 0; indiceLinha < Math.Min(3, tabela.Rows.Count); indiceLinha++)
        {
            for (int indiceColuna = 0; indiceColuna < tabela.Columns.Count; indiceColuna++)
                Console.Write($"  {tabela.Rows[indiceLinha][indiceColuna]}");
            Console.WriteLine();
        }
    }
}

