using System.Data;
using System.Globalization;
using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;
using Draft; // namespace da DLL DraftServer

namespace DraftosaurusClient.Services;

/// <summary>
/// Camada de serviço que encapsula todas as chamadas à DLL DraftServer.
/// Centraliza tratamento de erros, conversão de tipos e logging.
///
/// IMPORTANTE: a DLL é stateless e SINGLETON. Em multi-jogador real
/// (cada jogador na sua máquina), todos os clientes batem no mesmo
/// servidor por trás da DLL — então não há sincronização local: o
/// estado da partida é sempre puxado da DLL via VerificarPartida/Turno.
/// </summary>
public class DraftService
{

    public string Versao
    {
        get
        {
            try
            {
                var t = typeof(Jogo);
                var f = t.GetField("versao",
                    System.Reflection.BindingFlags.Public
                  | System.Reflection.BindingFlags.NonPublic
                  | System.Reflection.BindingFlags.Static
                  | System.Reflection.BindingFlags.Instance);
                if (f == null) return "?";
                // A DLL deste projeto expõe os membros como estáticos
                var v = f.GetValue(null);
                return v?.ToString() ?? "?";
            }
            catch { return "?"; }
        }
    }

    // ============================================================
    // PARTIDA
    // ============================================================

    /// <summary>
    /// Cria uma nova partida.
    /// </summary>
    /// <param name="nome">Nome (até 15 chars)</param>
    /// <param name="senha">Senha de acesso (até 10 chars)</param>
    /// <param name="grupo">Nome do grupo</param>
    /// <returns>Id da partida criada</returns>
    public int CriarPartida(string nome, string senha, string grupo)
    {
        var ret = Jogo.CriarPartida(nome, senha, grupo);

        if (ret != null && int.TryParse(ret.ToString(), out var id))
            return id;

        var dt = DllHelper.AsDataTable(ret);
        if (dt != null && dt.Rows.Count > 0)
        {
            var r = dt.Rows[0];
            return DllHelper.IntAt(r, 0);
        }

        throw new InvalidOperationException(
            "Resposta inesperada de CriarPartida(): " + (ret?.ToString() ?? "NULL")
        );
    }

    /// <summary>
    /// Lista partidas. Status: T(odas), A(bertas), J(ogando), E(ncerradas).
    /// </summary>
    public List<Partida> ListarPartidas(char status = 'T')
    {
        var ret = Jogo.ListarPartidas(status.ToString());
        var lista = new List<Partida>();

        if (ret is string texto)
            return ParsePartidasTexto(texto);

        var dt = DllHelper.AsDataTable(ret);
        if (dt == null) return lista;

        foreach (DataRow r in dt.Rows)
        {
            // Tenta primeiro pelo nome conhecido, depois posicional
            int id = dt.Columns.Contains("Id") ? DllHelper.Int(r, "Id") : DllHelper.IntAt(r, 0);
            string nome = dt.Columns.Contains("Nome") ? DllHelper.Str(r, "Nome") : DllHelper.StrAt(r, 1);
            DateTime data = dt.Columns.Contains("DataCriacao")
                ? DllHelper.DateT(r, "DataCriacao")
                : (DateTime.TryParse(DllHelper.StrAt(r, 2), out var d) ? d : DateTime.MinValue);
            string st = dt.Columns.Contains("Status") ? DllHelper.Str(r, "Status") : DllHelper.StrAt(r, 3);

            lista.Add(new Partida
            {
                Id = id,
                Nome = nome,
                DataCriacao = data,
                Status = string.IsNullOrEmpty(st) ? 'A' : char.ToUpperInvariant(st[0])
            });
        }
        return lista;
    }

    private static List<Partida> ParsePartidasTexto(string texto)
    {
        var lista = new List<Partida>();
        if (string.IsNullOrWhiteSpace(texto)) return lista;

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var linhas = texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var linha in linhas)
        {
            var campos = linha.Split(',', 4);
            if (campos.Length < 4) continue;
            if (!int.TryParse(campos[0].Trim(), out var id)) continue;

            var dataTexto = campos[2].Trim();
            var data = DateTime.TryParse(dataTexto, cultura, DateTimeStyles.None, out var d)
                ? d
                : DateTime.MinValue;

            var statusTexto = campos[3].Trim();
            lista.Add(new Partida
            {
                Id = id,
                Nome = campos[1].Trim(),
                DataCriacao = data,
                Status = string.IsNullOrEmpty(statusTexto) ? 'A' : char.ToUpperInvariant(statusTexto[0])
            });
        }

        return lista;
    }

    // ============================================================
    // JOGADOR
    // ============================================================

    /// <summary>
    /// Entra em uma partida. Retorna (idJogador, senhaGerada).
    /// </summary>
    public (int idJogador, string senhaJogador) Entrar(int idPartida, string nomeJogador, string senhaPartida)
    {
        var ret = Jogo.Entrar(idPartida, nomeJogador, senhaPartida);

        if (ret is string texto)
            return ParseEntrarTexto(texto);

        var dt = DllHelper.AsDataTable(ret);

        if (dt != null && dt.Rows.Count > 0)
        {
            var r = dt.Rows[0];
            int id = DllHelper.IntAt(r, 0);
            string senha = DllHelper.StrAt(r, 1);
            return (id, senha);
        }
        throw new InvalidOperationException("Resposta inesperada de Entrar(): " + (ret?.ToString() ?? "NULL"));
    }

    private static (int idJogador, string senhaJogador) ParseEntrarTexto(string texto)
    {
        texto = texto.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            throw new InvalidOperationException("Entrar() retornou vazio.");

        var campos = texto.Split(new[] { ',', ';' }, 2);
        if (campos.Length >= 2 && int.TryParse(campos[0].Trim(), out var id))
            return (id, campos[1].Trim());

        if (int.TryParse(texto, out id))
            return (id, "");

        throw new InvalidOperationException(texto);
    }

    /// <summary>Lista jogadores da partida (com pontuação se encerrada).</summary>
    public List<Jogador> ListarJogadores(int idPartida)
    {
        var ret = Jogo.ListarJogadores(idPartida);
        var lista = new List<Jogador>();

        if (ret is string texto)
            return ParseJogadoresTexto(texto);

        var dt = DllHelper.AsDataTable(ret);
        if (dt == null) return lista;

        foreach (DataRow r in dt.Rows)
        {
            lista.Add(new Jogador
            {
                Id = DllHelper.IntAt(r, 0),
                Nome = DllHelper.StrAt(r, 1),
                Pontuacao = DllHelper.IntAt(r, 2)
            });
        }
        return lista;
    }

    private static List<Jogador> ParseJogadoresTexto(string texto)
    {
        var lista = new List<Jogador>();
        foreach (var campos in LinhasCsv(texto))
        {
            if (campos.Length < 2) continue;
            if (!int.TryParse(campos[0].Trim(), out var id)) continue;

            lista.Add(new Jogador
            {
                Id = id,
                Nome = campos[1].Trim(),
                Pontuacao = campos.Length > 2 && int.TryParse(campos[2].Trim(), out var pts) ? pts : 0
            });
        }
        return lista;
    }

    /// <summary>
    /// Inicia a partida. Retorna (idJogadorComDado, faceDado).
    /// </summary>
    public (int idJogadorComDado, string faceDado) Iniciar(int idJogador, string senha)
    {
        var ret = Jogo.Iniciar(idJogador, senha);

        if (ret is string texto)
            return ParseIniciarTexto(texto);

        var dt = DllHelper.AsDataTable(ret);
        if (dt != null && dt.Rows.Count > 0)
        {
            var r = dt.Rows[0];
            return (DllHelper.IntAt(r, 0), DllHelper.StrAt(r, 1));
        }
        throw new InvalidOperationException("Resposta inesperada de Iniciar(): " + (ret?.ToString() ?? "NULL"));
    }

    private static (int idJogadorComDado, string faceDado) ParseIniciarTexto(string texto)
    {
        texto = texto.Trim();
        var campos = texto.Split(new[] { ',', ';' }, 2);
        if (campos.Length >= 2 && int.TryParse(campos[0].Trim(), out var id))
            return (id, campos[1].Trim());

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(texto)
            ? "Iniciar() retornou vazio."
            : texto);
    }

    // ============================================================
    // ESTADO DO JOGO
    // ============================================================

    /// <summary>Mão do jogador: dicionário código->quantidade.</summary>
    public Dictionary<string, int> ExibirMao(int idJogador, string senha)
    {
        var ret = Jogo.ExibirMao(idJogador, senha);
        var mao = new Dictionary<string, int>();

        if (ret is string texto)
        {
            if (TextoEhErro(texto)) throw new InvalidOperationException(texto.Trim());
            foreach (var campos in LinhasCsv(texto))
            {
                if (campos.Length < 2) continue;
                var cod = campos[0].Trim();
                if (cod.Length == 0) continue;
                mao[cod] = int.TryParse(campos[1].Trim(), out var qtd) ? qtd : 0;
            }
            if (mao.Count == 0 && !string.IsNullOrWhiteSpace(texto))
                throw new InvalidOperationException(texto.Trim());
            return mao;
        }

        var dt = DllHelper.AsDataTable(ret);
        if (dt == null) return mao;

        foreach (DataRow r in dt.Rows)
        {
            string cod = DllHelper.StrAt(r, 0);
            int qtd = DllHelper.IntAt(r, 1);
            if (!string.IsNullOrEmpty(cod))
                mao[cod] = qtd;
        }
        return mao;
    }

    /// <summary>
    /// Retorna o tabuleiro do jogador como dicionário [cercado] -> lista de códigos de dinossauros.
    /// Se senha for fornecida, mostra também a jogada do turno corrente.
    /// </summary>
    public Dictionary<string, List<string>> ExibirTabuleiro(int idJogador, string? senha = null)
    {
        object ret = senha == null
            ? Jogo.ExibirTabuleiro(idJogador)
            : Jogo.ExibirTabuleiro(idJogador, senha);

        var tab = new Dictionary<string, List<string>>();

        if (ret is string texto)
        {
            if (TextoEhErro(texto)) throw new InvalidOperationException(texto.Trim());
            foreach (var campos in LinhasCsv(texto))
            {
                if (campos.Length < 3) continue;
                string cercado = campos[0].Trim();
                string dino = campos[1].Trim();
                int qtd = int.TryParse(campos[2].Trim(), out var n) ? n : 0;
                AdicionarDinosTabuleiro(tab, cercado, dino, qtd);
            }
            return tab;
        }

        var dt = DllHelper.AsDataTable(ret);
        if (dt == null) return tab;

        // Esperado: cercado, dinossauro, qtd
        foreach (DataRow r in dt.Rows)
        {
            string cercado = DllHelper.StrAt(r, 0);
            string dino = DllHelper.StrAt(r, 1);
            int qtd = DllHelper.IntAt(r, 2);
            AdicionarDinosTabuleiro(tab, cercado, dino, qtd);
        }
        return tab;
    }

    private static void AdicionarDinosTabuleiro(Dictionary<string, List<string>> tab, string cercado, string dino, int qtd)
    {
        if (string.IsNullOrEmpty(cercado) || string.IsNullOrEmpty(dino)) return;

        if (!tab.ContainsKey(cercado))
            tab[cercado] = new List<string>();
        for (int i = 0; i < qtd; i++)
            tab[cercado].Add(dino);
    }

    /// <summary>Estado atual da partida (status, turno, dado etc.).</summary>
    public EstadoPartida VerificarPartida(int idPartida)
    {
        var ret = Jogo.VerificarPartida(idPartida);
        var st = new EstadoPartida();

        if (ret is string texto)
            return ParseEstadoPartidaTexto(texto);

        var dt = DllHelper.AsDataTable(ret);
        if (dt == null || dt.Rows.Count == 0) return st;

        var r = dt.Rows[0];
        // Status partida, turno atual, status turno, idJogadorDado, faceDado
        string sp = DllHelper.StrAt(r, 0);
        st.Status = string.IsNullOrEmpty(sp) ? 'J' : sp[0];
        st.TurnoAtual = DllHelper.IntAt(r, 1);
        string stt = DllHelper.StrAt(r, 2);
        st.StatusTurno = string.IsNullOrEmpty(stt) ? 'A' : stt[0];
        st.IdJogadorComDado = DllHelper.IntAt(r, 3);
        st.FaceDado = DllHelper.StrAt(r, 4);
        return st;
    }

    private static EstadoPartida ParseEstadoPartidaTexto(string texto)
    {
        var st = new EstadoPartida();
        var campos = texto.Trim().Split(new[] { ',', ';' });
        if (campos.Length == 0 || string.IsNullOrWhiteSpace(campos[0])) return st;

        st.Status = char.ToUpperInvariant(campos[0].Trim()[0]);
        st.TurnoAtual = campos.Length > 1 && int.TryParse(campos[1].Trim(), out var turno) ? turno : 0;
        st.StatusTurno = campos.Length > 2 && !string.IsNullOrWhiteSpace(campos[2])
            ? char.ToUpperInvariant(campos[2].Trim()[0])
            : 'A';
        st.IdJogadorComDado = campos.Length > 3 && int.TryParse(campos[3].Trim(), out var idDado) ? idDado : 0;
        st.FaceDado = campos.Length > 4 ? campos[4].Trim() : "";
        return st;
    }

    private static IEnumerable<string[]> LinhasCsv(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) yield break;

        foreach (var linha in texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var campos = linha.Split(',');
            if (campos.Length == 1)
                campos = linha.Split(';');
            yield return campos;
        }
    }

    private static bool TextoEhErro(string texto)
    {
        var t = texto.TrimStart();
        return t.StartsWith("ERRO", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica um turno: retorna estado do turno + jogadas realizadas.
    /// </summary>
    public (EstadoPartida estado, List<JogadaTurno> jogadas) VerificarTurno(int idPartida, int? turno = null)
    {
        object ret = turno.HasValue
            ? Jogo.VerificarTurno(idPartida, turno.Value)
            : Jogo.VerificarTurno(idPartida);

        if (ret is string texto)
            return ParseVerificarTurnoTexto(texto);

        // Aqui pode vir um DataSet com 2 tabelas (cabeçalho + jogadas) OU um único DataTable
        var estado = new EstadoPartida();
        var jogadas = new List<JogadaTurno>();

        if (ret is DataSet ds)
        {
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var r = ds.Tables[0].Rows[0];
                string st = DllHelper.StrAt(r, 0);
                estado.StatusTurno = string.IsNullOrEmpty(st) ? 'A' : st[0];
                estado.IdJogadorComDado = DllHelper.IntAt(r, 1);
                estado.FaceDado = DllHelper.StrAt(r, 2);
            }
            if (ds.Tables.Count > 1)
            {
                foreach (DataRow r in ds.Tables[1].Rows)
                {
                    jogadas.Add(new JogadaTurno
                    {
                        IdJogador = DllHelper.IntAt(r, 0),
                        CodigoDinossauro = DllHelper.StrAt(r, 1),
                        CodigoCercado = DllHelper.StrAt(r, 2)
                    });
                }
            }
        }
        else
        {
            var dt = DllHelper.AsDataTable(ret);
            if (dt != null && dt.Rows.Count > 0)
            {
                // Primeira linha = cabeçalho do turno; demais = jogadas
                var r0 = dt.Rows[0];
                string st = DllHelper.StrAt(r0, 0);
                estado.StatusTurno = string.IsNullOrEmpty(st) ? 'A' : st[0];
                estado.IdJogadorComDado = DllHelper.IntAt(r0, 1);
                estado.FaceDado = DllHelper.StrAt(r0, 2);
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    var r = dt.Rows[i];
                    jogadas.Add(new JogadaTurno
                    {
                        IdJogador = DllHelper.IntAt(r, 0),
                        CodigoDinossauro = DllHelper.StrAt(r, 1),
                        CodigoCercado = DllHelper.StrAt(r, 2)
                    });
                }
            }
        }
        return (estado, jogadas);
    }

    /// <summary>Histórico textual da partida (para acompanhamento humano).</summary>
    private static (EstadoPartida estado, List<JogadaTurno> jogadas) ParseVerificarTurnoTexto(string texto)
    {
        if (TextoEhErro(texto))
            throw new InvalidOperationException(texto.Trim());

        var estado = new EstadoPartida();
        var jogadas = new List<JogadaTurno>();
        var linhas = LinhasCsv(texto)
            .Select(campos => campos.Select(c => c.Trim()).ToArray())
            .Where(campos => campos.Length > 0 && !string.IsNullOrWhiteSpace(campos[0]))
            .ToList();

        if (linhas.Count == 0)
            return (estado, jogadas);

        PreencherCabecalhoTurno(estado, linhas[0]);

        if (linhas.Count == 1 && linhas[0].Length > 3)
        {
            AdicionarJogadasDeCampos(jogadas, linhas[0], 3);
            return (estado, jogadas);
        }

        for (int i = 1; i < linhas.Count; i++)
            AdicionarJogada(jogadas, linhas[i], 0);

        return (estado, jogadas);
    }

    private static void PreencherCabecalhoTurno(EstadoPartida estado, string[] campos)
    {
        estado.StatusTurno = campos.Length > 0 && !string.IsNullOrWhiteSpace(campos[0])
            ? char.ToUpperInvariant(campos[0][0])
            : 'A';
        estado.IdJogadorComDado = campos.Length > 1 && int.TryParse(campos[1], out var idDado)
            ? idDado
            : 0;
        estado.FaceDado = campos.Length > 2 ? campos[2] : "";
    }

    private static void AdicionarJogadasDeCampos(List<JogadaTurno> jogadas, string[] campos, int inicio)
    {
        for (int i = inicio; i + 2 < campos.Length; i += 3)
            AdicionarJogada(jogadas, campos, i);
    }

    private static void AdicionarJogada(List<JogadaTurno> jogadas, string[] campos, int inicio)
    {
        if (campos.Length <= inicio + 2)
            return;

        if (!int.TryParse(campos[inicio], out var idJogador))
            return;

        jogadas.Add(new JogadaTurno
        {
            IdJogador = idJogador,
            CodigoDinossauro = campos[inicio + 1],
            CodigoCercado = campos[inicio + 2]
        });
    }

    public string ListarHistorico(int idPartida)
    {
        var ret = Jogo.ListarHistorico(idPartida);
        if (ret is string s) return s;
        var dt = DllHelper.AsDataTable(ret);
        if (dt == null) return "";
        var sb = new System.Text.StringBuilder();
        foreach (DataRow r in dt.Rows)
            sb.AppendLine(DllHelper.StrAt(r, 0));
        return sb.ToString();
    }

    // ============================================================
    // JOGADA
    // ============================================================

    /// <summary>
    /// Realiza uma jogada. Retorna o número do próximo turno (0 se a partida acabou).
    /// </summary>
    public int Jogar(int idJogador, string senha, string codDinossauro, string codCercado)
    {
        var ret = Jogo.Jogar(idJogador, senha, codDinossauro, codCercado);
        if (ret == null)
            return -1;

        string texto = ret.ToString() ?? "";
        if (TextoEhErro(texto))
            throw new InvalidOperationException(texto.Trim());

        if (int.TryParse(texto.Trim(), out var proxTurno))
            return proxTurno;

        var dt = DllHelper.AsDataTable(ret);
        if (dt != null && dt.Rows.Count > 0)
        {
            var r = dt.Rows[0];
            return DllHelper.IntAt(r, 0);
        }

        // Algumas versoes da DLL gravam a jogada e retornam texto vazio/OK.
        // Nesse caso consideramos sucesso e a tela recarrega o estado em seguida.
        return -1;
    }

    // ============================================================
    // METADADOS (estáticos do jogo)
    // ============================================================

    public List<FaceDado> ListarFacesDado()
    {
        var ret = Jogo.ListarFacesDado();
        var dt = DllHelper.AsDataTable(ret);
        var lista = new List<FaceDado>();
        if (dt == null) return lista;
        foreach (DataRow r in dt.Rows)
        {
            lista.Add(new FaceDado
            {
                Codigo = DllHelper.StrAt(r, 0),
                Nome = DllHelper.StrAt(r, 1),
                Descricao = DllHelper.StrAt(r, 2)
            });
        }
        return lista;
    }

    public List<Cercado> ListarCercados()
    {
        var ret = Jogo.ListarCercados();
        var dt = DllHelper.AsDataTable(ret);
        var lista = new List<Cercado>();
        if (dt == null) return lista;

        // Coleta primeiro todos os códigos retornados — para depois detectar lado
        var bruto = new List<(string cod, string nome, string desc)>();
        foreach (DataRow r in dt.Rows)
        {
            bruto.Add((
                DllHelper.StrAt(r, 0),
                DllHelper.StrAt(r, 1),
                DllHelper.StrAt(r, 2)));
        }

        // Detecta o lado
        var lado = DetectarLado(bruto.Select(b => b.cod));
        var mapa = Cercado.CercadosPorLado(lado);

        foreach (var (cod, nome, desc) in bruto)
        {
            var c = new Cercado
            {
                Codigo = cod,
                Nome = nome,
                Descricao = desc
            };
            if (mapa.TryGetValue(cod, out var info))
            {
                c.Lado = info.Lado;
                c.Lateral = info.Lateral;
                c.Capacidade = info.Capacidade;
                c.Tipo = info.Tipo;
                if (string.IsNullOrEmpty(c.Nome)) c.Nome = info.Nome;
            }
            else
            {
                // Cercado desconhecido — fallback razoável
                c.Tipo = TipoCercado.Linear;
                c.Capacidade = 6;
            }
            lista.Add(c);
        }
        return lista;
    }

    /// <summary>
    /// Detecta o lado do tabuleiro pelos códigos de cercado retornados.
    /// Se contém códigos típicos de inverno (PI, FB, VG, QU, PE/PD), é inverno;
    /// caso contrário, verão.
    /// </summary>
    public static LadoMapa DetectarLado(IEnumerable<string> codigos)
    {
        var set = new HashSet<string>(codigos, StringComparer.OrdinalIgnoreCase);
        // Códigos exclusivos do inverno
        string[] marcasInverno = { "PI", "FB", "VG", "QU", "PE", "PD" };
        if (marcasInverno.Any(m => set.Contains(m))) return LadoMapa.Inverno;
        return LadoMapa.Verao;
    }

    /// <summary>
    /// Retorna a pontuação detalhada de um jogador (etapa por etapa).
    /// </summary>
    public List<(string descricao, int pontos)> ListarPontuacao(int idJogador)
    {
        var lista = new List<(string, int)>();
        try
        {
            var ret = Jogo.ListarPontuacao(idJogador);
            var dt = DllHelper.AsDataTable(ret);
            if (dt == null) return lista;
            foreach (DataRow r in dt.Rows)
            {
                // Esperado: descrição, pontos (acumulado ou parcial)
                lista.Add((DllHelper.StrAt(r, 0), DllHelper.IntAt(r, 1)));
            }
        }
        catch { /* método pode não existir em versões antigas */ }
        return lista;
    }

    public List<Dinossauro> ListarDinossauros()
    {
        // A assinatura aceita um bool — passamos false (formato resumido)
        var ret = Jogo.ListarDinossauros(false);
        var dt = DllHelper.AsDataTable(ret);
        var lista = new List<Dinossauro>();
        if (dt == null) return lista;
        foreach (DataRow r in dt.Rows)
        {
            string cod = DllHelper.StrAt(r, 0);
            lista.Add(new Dinossauro
            {
                Codigo = cod,
                Nome = string.IsNullOrEmpty(DllHelper.StrAt(r, 1))
                    ? Dinossauro.NomePorCodigo(cod)
                    : DllHelper.StrAt(r, 1),
                Cor = Dinossauro.CorPorCodigo(cod)
            });
        }
        return lista;
    }
}
