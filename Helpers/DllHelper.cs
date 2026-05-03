using System.Data;
using System.Reflection;

namespace DraftosaurusClient.Helpers;

/// <summary>
/// Utilitários para lidar com retornos da DLL DraftServer.
/// A DLL retorna DataTables / tipos simples — este helper normaliza isso.
/// </summary>
internal static class DllHelper
{
    /// <summary>
    /// Converte qualquer retorno da DLL para um DataTable, se possível.
    /// A maioria dos métodos da DLL retorna DataTable ou DataSet.
    /// </summary>
    public static DataTable? AsDataTable(object? retorno)
    {
        if (retorno is null) return null;
        if (retorno is DataTable dt) return dt;
        if (retorno is DataSet ds && ds.Tables.Count > 0) return ds.Tables[0];
        return null;
    }

    /// <summary>
    /// Lê uma coluna como string, com fallback seguro.
    /// </summary>
    public static string Str(DataRow row, string col)
    {
        try
        {
            if (!row.Table.Columns.Contains(col)) return "";
            var v = row[col];
            return v == DBNull.Value ? "" : v.ToString()?.Trim() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>Lê uma coluna como int.</summary>
    public static int Int(DataRow row, string col)
    {
        try
        {
            if (!row.Table.Columns.Contains(col)) return 0;
            var v = row[col];
            if (v == DBNull.Value) return 0;
            return Convert.ToInt32(v);
        }
        catch { return 0; }
    }

    /// <summary>Lê uma coluna como DateTime.</summary>
    public static DateTime DateT(DataRow row, string col)
    {
        try
        {
            if (!row.Table.Columns.Contains(col)) return DateTime.MinValue;
            var v = row[col];
            if (v == DBNull.Value) return DateTime.MinValue;
            return Convert.ToDateTime(v);
        }
        catch { return DateTime.MinValue; }
    }

    /// <summary>
    /// Lê uma coluna por índice posicional (0, 1, 2...).
    /// Útil quando os nomes das colunas são desconhecidos.
    /// </summary>
    public static string StrAt(DataRow row, int idx)
    {
        try
        {
            if (idx < 0 || idx >= row.Table.Columns.Count) return "";
            var v = row[idx];
            return v == DBNull.Value ? "" : v.ToString()?.Trim() ?? "";
        }
        catch { return ""; }
    }

    public static int IntAt(DataRow row, int idx)
    {
        try
        {
            if (idx < 0 || idx >= row.Table.Columns.Count) return 0;
            var v = row[idx];
            if (v == DBNull.Value) return 0;
            return Convert.ToInt32(v);
        }
        catch { return 0; }
    }

    /// <summary>
    /// Imprime no console as colunas + primeiras 3 linhas — útil para
    /// descobrir o esquema real retornado pela DLL durante o desenvolvimento.
    /// </summary>
    public static void DumpSchema(DataTable? dt, string contexto = "")
    {
        if (dt == null)
        {
            Console.WriteLine($"[DUMP {contexto}] DataTable nulo");
            return;
        }
        Console.WriteLine($"[DUMP {contexto}] {dt.Rows.Count} linhas, {dt.Columns.Count} colunas:");
        foreach (DataColumn c in dt.Columns)
            Console.Write($"  [{c.ColumnName}:{c.DataType.Name}]");
        Console.WriteLine();
        for (int i = 0; i < Math.Min(3, dt.Rows.Count); i++)
        {
            for (int j = 0; j < dt.Columns.Count; j++)
                Console.Write($"  {dt.Rows[i][j]}");
            Console.WriteLine();
        }
    }
}
