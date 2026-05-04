using System.Data;
using System.Globalization;
using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;
using Draft; // namespace da DLL DraftServer

namespace DraftosaurusClient.Services;

/// <summary>
/// Camada de servico que encapsula todas as chamadas a DLL DraftServer.
/// Centraliza tratamento de erros, conversao de tipos e logging.
///
/// IMPORTANTE: a DLL e stateless e SINGLETON. Em multi-jogador real
/// (cada jogador na sua maquina), todos os clientes batem no mesmo
/// servidor por tras da DLL; entao nao ha sincronizacao local: o
/// estado da partida e sempre puxado da DLL via VerificarPartida/Turno.
/// </summary>
public class DraftService
{

    public string Versao
    {
        get
        {
            try
            {
                var tipoJogo = typeof(Jogo);
                var campoVersao = tipoJogo.GetField("versao",
                    System.Reflection.BindingFlags.Public
                  | System.Reflection.BindingFlags.NonPublic
                  | System.Reflection.BindingFlags.Static
                  | System.Reflection.BindingFlags.Instance);
                if (campoVersao == null) return "?";
                // A DLL deste projeto expoe os membros como estaticos
                var valorVersao = campoVersao.GetValue(null);
                return valorVersao?.ToString() ?? "?";
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
    /// <param name="nome">Nome (ate 15 chars)</param>
    /// <param name="senha">Senha de acesso (ate 10 chars)</param>
    /// <param name="grupo">Nome do grupo</param>
    /// <returns>Id da partida criada</returns>
    public int CriarPartida(string nome, string senha, string grupo)
    {
        var respostaDll = Jogo.CriarPartida(nome, senha, grupo);

        if (respostaDll != null && int.TryParse(respostaDll.ToString(), out var id))
            return id;

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela != null && tabela.Rows.Count > 0)
        {
            var linhaTabela = tabela.Rows[0];
            return DllHelper.InteiroNaPosicao(linhaTabela, 0);
        }

        throw new InvalidOperationException(
            "Resposta inesperada de CriarPartida(): " + (respostaDll?.ToString() ?? "NULL")
        );
    }

    /// <summary>
    /// Lista partidas. Status: T(odas), A(bertas), J(ogando), E(ncerradas).
    /// </summary>
    public List<Partida> ListarPartidas(char status = 'T')
    {
        var respostaDll = Jogo.ListarPartidas(status.ToString());
        var lista = new List<Partida>();

        if (respostaDll is string texto)
            return ParsePartidasTexto(texto);

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null) return lista;

        foreach (DataRow linhaTabela in tabela.Rows)
        {
            // Tenta primeiro pelo nome conhecido, depois posicional
            int id = tabela.Columns.Contains("Id") ? DllHelper.Inteiro(linhaTabela, "Id") : DllHelper.InteiroNaPosicao(linhaTabela, 0);
            string nome = tabela.Columns.Contains("Nome") ? DllHelper.Texto(linhaTabela, "Nome") : DllHelper.TextoNaPosicao(linhaTabela, 1);
            DateTime data = tabela.Columns.Contains("DataCriacao")
                ? DllHelper.Data(linhaTabela, "DataCriacao")
                : (DateTime.TryParse(DllHelper.TextoNaPosicao(linhaTabela, 2), out var d) ? d : DateTime.MinValue);
            string estadoPartida = tabela.Columns.Contains("Status") ? DllHelper.Texto(linhaTabela, "Status") : DllHelper.TextoNaPosicao(linhaTabela, 3);

            lista.Add(new Partida
            {
                Id = id,
                Nome = nome,
                DataCriacao = data,
                Status = string.IsNullOrEmpty(estadoPartida) ? 'A' : char.ToUpperInvariant(estadoPartida[0])
            });
        }
        return lista;
    }

    // A funcao serve para tratar o caso em que a DLL devolve partidas em texto CSV em vez de tabela.
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
        var respostaDll = Jogo.Entrar(idPartida, nomeJogador, senhaPartida);

        if (respostaDll is string texto)
            return ParseEntrarTexto(texto);

        var tabela = DllHelper.AsDataTable(respostaDll);

        if (tabela != null && tabela.Rows.Count > 0)
        {
            var linhaTabela = tabela.Rows[0];
            int id = DllHelper.InteiroNaPosicao(linhaTabela, 0);
            string senha = DllHelper.TextoNaPosicao(linhaTabela, 1);
            return (id, senha);
        }
        throw new InvalidOperationException("Resposta inesperada de Entrar(): " + (respostaDll?.ToString() ?? "NULL"));
    }

    // A funcao serve para separar o texto retornado pela DLL ao entrar na partida.
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

    /// <summary>Lista jogadores da partida (com pontuacao se encerrada).</summary>
    public List<Jogador> ListarJogadores(int idPartida)
    {
        var respostaDll = Jogo.ListarJogadores(idPartida);
        var lista = new List<Jogador>();

        if (respostaDll is string texto)
            return ParseJogadoresTexto(texto);

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null) return lista;

        foreach (DataRow linhaTabela in tabela.Rows)
        {
            lista.Add(new Jogador
            {
                Id = DllHelper.InteiroNaPosicao(linhaTabela, 0),
                Nome = DllHelper.TextoNaPosicao(linhaTabela, 1),
                Pontuacao = DllHelper.InteiroNaPosicao(linhaTabela, 2)
            });
        }
        return lista;
    }

    // A funcao serve para tratar o caso em que a DLL devolve jogadores em linhas de texto CSV.
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
        var respostaDll = Jogo.Iniciar(idJogador, senha);

        if (respostaDll is string texto)
            return ParseIniciarTexto(texto);

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela != null && tabela.Rows.Count > 0)
        {
            var linhaTabela = tabela.Rows[0];
            return (DllHelper.InteiroNaPosicao(linhaTabela, 0), DllHelper.TextoNaPosicao(linhaTabela, 1));
        }
        throw new InvalidOperationException("Resposta inesperada de Iniciar(): " + (respostaDll?.ToString() ?? "NULL"));
    }

    // Esta funcao faz interpretar a resposta em texto do metodo Iniciar da DLL.
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

    /// <summary>Mao do jogador: dicionario codigo->quantidade.</summary>
    public Dictionary<string, int> ExibirMao(int idJogador, string senha)
    {
        var respostaDll = Jogo.ExibirMao(idJogador, senha);
        var mao = new Dictionary<string, int>();

        if (respostaDll is string texto)
        {
            if (TextoEhErro(texto)) throw new InvalidOperationException(texto.Trim());
            foreach (var campos in LinhasCsv(texto))
            {
                if (campos.Length < 2) continue;
                var codigo = campos[0].Trim();
                if (codigo.Length == 0) continue;
                mao[codigo] = int.TryParse(campos[1].Trim(), out var quantidade) ? quantidade : 0;
            }
            if (mao.Count == 0 && !string.IsNullOrWhiteSpace(texto))
                throw new InvalidOperationException(texto.Trim());
            return mao;
        }

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null) return mao;

        foreach (DataRow linhaTabela in tabela.Rows)
        {
            string codigo = DllHelper.TextoNaPosicao(linhaTabela, 0);
            int quantidade = DllHelper.InteiroNaPosicao(linhaTabela, 1);
            if (!string.IsNullOrEmpty(codigo))
                mao[codigo] = quantidade;
        }
        return mao;
    }

    /// <summary>
    /// Retorna o tabuleiro do jogador como dicionario [cercado] -> lista de codigos de dinossauros.
    /// Se senha for fornecida, mostra tambem a jogada do turno corrente.
    /// </summary>
    public Dictionary<string, List<string>> ExibirTabuleiro(int idJogador, string? senha = null)
    {
        object respostaDll = senha == null
            ? Jogo.ExibirTabuleiro(idJogador)
            : Jogo.ExibirTabuleiro(idJogador, senha);

        var tab = new Dictionary<string, List<string>>();

        if (respostaDll is string texto)
        {
            if (TextoEhErro(texto)) throw new InvalidOperationException(texto.Trim());
            foreach (var campos in LinhasCsv(texto))
            {
                if (campos.Length < 3) continue;
                string cercado = campos[0].Trim();
                string dino = campos[1].Trim();
                int quantidade = int.TryParse(campos[2].Trim(), out var n) ? n : 0;
                AdicionarDinosTabuleiro(tab, cercado, dino, quantidade);
            }
            return tab;
        }

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null) return tab;

        // Esperado: cercado, dinossauro, quantidade
        foreach (DataRow linhaTabela in tabela.Rows)
        {
            string cercado = DllHelper.TextoNaPosicao(linhaTabela, 0);
            string dino = DllHelper.TextoNaPosicao(linhaTabela, 1);
            int quantidade = DllHelper.InteiroNaPosicao(linhaTabela, 2);
            AdicionarDinosTabuleiro(tab, cercado, dino, quantidade);
        }
        return tab;
    }

    // A funcao auxiliar serve para transformar quantidade em uma lista de dinossauros no cercado.
    private static void AdicionarDinosTabuleiro(Dictionary<string, List<string>> tab, string cercado, string dino, int quantidade)
    {
        if (string.IsNullOrEmpty(cercado) || string.IsNullOrEmpty(dino)) return;

        if (!tab.ContainsKey(cercado))
            tab[cercado] = new List<string>();
        for (int i = 0; i < quantidade; i++)
            tab[cercado].Add(dino);
    }

    /// <summary>Estado atual da partida (status, turno, dado etc.).</summary>
    public EstadoPartida VerificarPartida(int idPartida)
    {
        var respostaDll = Jogo.VerificarPartida(idPartida);
        var estadoPartida = new EstadoPartida();

        if (respostaDll is string texto)
            return ParseEstadoPartidaTexto(texto);

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null || tabela.Rows.Count == 0) return estadoPartida;

        var linhaTabela = tabela.Rows[0];
        // Status partida, turno atual, status turno, idJogadorDado, faceDado
        string statusPartidaTexto = DllHelper.TextoNaPosicao(linhaTabela, 0);
        estadoPartida.Status = string.IsNullOrEmpty(statusPartidaTexto) ? 'J' : statusPartidaTexto[0];
        estadoPartida.TurnoAtual = DllHelper.InteiroNaPosicao(linhaTabela, 1);
        string statusTurnoTexto = DllHelper.TextoNaPosicao(linhaTabela, 2);
        estadoPartida.StatusTurno = string.IsNullOrEmpty(statusTurnoTexto) ? 'A' : statusTurnoTexto[0];
        estadoPartida.IdJogadorComDado = DllHelper.InteiroNaPosicao(linhaTabela, 3);
        estadoPartida.FaceDado = DllHelper.TextoNaPosicao(linhaTabela, 4);
        return estadoPartida;
    }

    // A funcao serve para tratar o caso em que o estado da partida vem como texto separado por virgula ou ponto e virgula.
    private static EstadoPartida ParseEstadoPartidaTexto(string texto)
    {
        var estadoPartida = new EstadoPartida();
        var campos = texto.Trim().Split(new[] { ',', ';' });
        if (campos.Length == 0 || string.IsNullOrWhiteSpace(campos[0])) return estadoPartida;

        estadoPartida.Status = char.ToUpperInvariant(campos[0].Trim()[0]);
        estadoPartida.TurnoAtual = campos.Length > 1 && int.TryParse(campos[1].Trim(), out var turno) ? turno : 0;
        estadoPartida.StatusTurno = campos.Length > 2 && !string.IsNullOrWhiteSpace(campos[2])
            ? char.ToUpperInvariant(campos[2].Trim()[0])
            : 'A';
        estadoPartida.IdJogadorComDado = campos.Length > 3 && int.TryParse(campos[3].Trim(), out var idDado) ? idDado : 0;
        estadoPartida.FaceDado = campos.Length > 4 ? campos[4].Trim() : "";
        return estadoPartida;
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

    // Esta funcao cuida de saber se a resposta textual da DLL e uma mensagem de erro.
    private static bool TextoEhErro(string texto)
    {
        var textoSemEspacoInicial = texto.TrimStart();
        return textoSemEspacoInicial.StartsWith("ERRO", StringComparison.OrdinalIgnoreCase)
            || textoSemEspacoInicial.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica um turno: retorna estado do turno + jogadas realizadas.
    /// </summary>
    public (EstadoPartida estado, List<JogadaTurno> jogadas) VerificarTurno(int idPartida, int? turno = null)
    {
        object respostaDll = turno.HasValue
            ? Jogo.VerificarTurno(idPartida, turno.Value)
            : Jogo.VerificarTurno(idPartida);

        if (respostaDll is string texto)
            return ParseVerificarTurnoTexto(texto);

        // Aqui pode vir um DataSet com 2 tabelas (cabecalho + jogadas) OU um unico DataTable
        var estado = new EstadoPartida();
        var jogadas = new List<JogadaTurno>();

        if (respostaDll is DataSet conjuntoTabelas)
        {
            if (conjuntoTabelas.Tables.Count > 0 && conjuntoTabelas.Tables[0].Rows.Count > 0)
            {
                var linhaTabela = conjuntoTabelas.Tables[0].Rows[0];
                string estadoPartida = DllHelper.TextoNaPosicao(linhaTabela, 0);
                estado.StatusTurno = string.IsNullOrEmpty(estadoPartida) ? 'A' : estadoPartida[0];
                estado.IdJogadorComDado = DllHelper.InteiroNaPosicao(linhaTabela, 1);
                estado.FaceDado = DllHelper.TextoNaPosicao(linhaTabela, 2);
            }
            if (conjuntoTabelas.Tables.Count > 1)
            {
                foreach (DataRow linhaTabela in conjuntoTabelas.Tables[1].Rows)
                {
                    jogadas.Add(new JogadaTurno
                    {
                        IdJogador = DllHelper.InteiroNaPosicao(linhaTabela, 0),
                        CodigoDinossauro = DllHelper.TextoNaPosicao(linhaTabela, 1),
                        CodigoCercado = DllHelper.TextoNaPosicao(linhaTabela, 2)
                    });
                }
            }
        }
        else
        {
            var tabela = DllHelper.AsDataTable(respostaDll);
            if (tabela != null && tabela.Rows.Count > 0)
            {
                // Primeira linha = cabecalho do turno; demais = jogadas
                var linhaCabecalho = tabela.Rows[0];
                string estadoPartida = DllHelper.TextoNaPosicao(linhaCabecalho, 0);
                estado.StatusTurno = string.IsNullOrEmpty(estadoPartida) ? 'A' : estadoPartida[0];
                estado.IdJogadorComDado = DllHelper.InteiroNaPosicao(linhaCabecalho, 1);
                estado.FaceDado = DllHelper.TextoNaPosicao(linhaCabecalho, 2);
                for (int i = 1; i < tabela.Rows.Count; i++)
                {
                    var linhaTabela = tabela.Rows[i];
                    jogadas.Add(new JogadaTurno
                    {
                        IdJogador = DllHelper.InteiroNaPosicao(linhaTabela, 0),
                        CodigoDinossauro = DllHelper.TextoNaPosicao(linhaTabela, 1),
                        CodigoCercado = DllHelper.TextoNaPosicao(linhaTabela, 2)
                    });
                }
            }
        }
        return (estado, jogadas);
    }

    /// <summary>Historico textual da partida (para acompanhamento humano).</summary>
    private static (EstadoPartida estado, List<JogadaTurno> jogadas) ParseVerificarTurnoTexto(string texto)
    {
        if (TextoEhErro(texto))
            throw new InvalidOperationException(texto.Trim());

        var estado = new EstadoPartida();
        var jogadas = new List<JogadaTurno>();
        var linhas = LinhasCsv(texto)
            .Select(campos => campos.Select(campo => campo.Trim()).ToArray())
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

    // A funcao serve para preencher status do turno, jogador do dado e face do dado.
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

    // Esta funcao faz ler varias jogadas que vieram na mesma linha.
    private static void AdicionarJogadasDeCampos(List<JogadaTurno> jogadas, string[] campos, int inicio)
    {
        for (int i = inicio; i + 2 < campos.Length; i += 3)
            AdicionarJogada(jogadas, campos, i);
    }

    // Esta funcao cuida de transformar tres campos em uma jogada do turno.
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

    // A funcao serve para buscar o historico textual da partida.
    public string ListarHistorico(int idPartida)
    {
        var respostaDll = Jogo.ListarHistorico(idPartida);
        if (respostaDll is string historicoEmTexto) return historicoEmTexto;
        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela == null) return "";
        var textoMontado = new System.Text.StringBuilder();
        foreach (DataRow linhaTabela in tabela.Rows)
            textoMontado.AppendLine(DllHelper.TextoNaPosicao(linhaTabela, 0));
        return textoMontado.ToString();
    }

    // ============================================================
    // JOGADA
    // ============================================================

    /// <summary>
    /// Realiza uma jogada. Retorna o numero do proximo turno (0 se a partida acabou).
    /// </summary>
    public int Jogar(int idJogador, string senha, string codDinossauro, string codCercado)
    {
        var respostaDll = Jogo.Jogar(idJogador, senha, codDinossauro, codCercado);
        if (respostaDll == null)
            return -1;

        string texto = respostaDll.ToString() ?? "";
        if (TextoEhErro(texto))
            throw new InvalidOperationException(texto.Trim());

        if (int.TryParse(texto.Trim(), out var proximoTurno))
            return proximoTurno;

        var tabela = DllHelper.AsDataTable(respostaDll);
        if (tabela != null && tabela.Rows.Count > 0)
        {
            var linhaTabela = tabela.Rows[0];
            return DllHelper.InteiroNaPosicao(linhaTabela, 0);
        }

        // Algumas versoes da DLL gravam a jogada e retornam texto vazio/OK.
        // Nesse caso consideramos sucesso e a tela recarrega o estado em seguida.
        return -1;
    }

    // ============================================================
    // METADADOS (estaticos do jogo)
    // ============================================================

    // Esta funcao faz consultar as faces do dado cadastradas na DLL.
    public List<FaceDado> ListarFacesDado()
    {
        var respostaDll = Jogo.ListarFacesDado();
        var tabela = DllHelper.AsDataTable(respostaDll);
        var lista = new List<FaceDado>();
        if (tabela == null) return lista;
        foreach (DataRow linhaTabela in tabela.Rows)
        {
            lista.Add(new FaceDado
            {
                Codigo = DllHelper.TextoNaPosicao(linhaTabela, 0),
                Nome = DllHelper.TextoNaPosicao(linhaTabela, 1),
                Descricao = DllHelper.TextoNaPosicao(linhaTabela, 2)
            });
        }
        return lista;
    }

    // Esta funcao cuida de consultar os cercados cadastrados na DLL.
    public List<Cercado> ListarCercados()
    {
        var respostaDll = Jogo.ListarCercados();
        var tabela = DllHelper.AsDataTable(respostaDll);
        var lista = new List<Cercado>();
        if (tabela == null) return lista;

        // Coleta primeiro todos os codigos retornados para depois detectar lado
        var cercadosRetornados = new List<(string codigo, string nome, string descricao)>();
        foreach (DataRow linhaTabela in tabela.Rows)
        {
            cercadosRetornados.Add((
                DllHelper.TextoNaPosicao(linhaTabela, 0),
                DllHelper.TextoNaPosicao(linhaTabela, 1),
                DllHelper.TextoNaPosicao(linhaTabela, 2)));
        }

        // Detecta o lado
        var mapa = Cercado.CercadosPorLado(LadoMapa.Verao);

        foreach (var (codigo, nome, descricao) in cercadosRetornados)
        {
            var cercado = new Cercado
            {
                Codigo = codigo,
                Nome = nome,
                Descricao = descricao
            };
            if (mapa.TryGetValue(codigo, out var info))
            {
                cercado.Lado = info.Lado;
                cercado.Lateral = info.Lateral;
                cercado.Capacidade = info.Capacidade;
                cercado.Tipo = info.Tipo;
                if (string.IsNullOrEmpty(cercado.Nome)) cercado.Nome = info.Nome;
            }
            else
            {
                // Cercado desconhecido a fallback razoavel
                cercado.Tipo = TipoCercado.Linear;
                cercado.Capacidade = 6;
            }
            lista.Add(cercado);
        }
        return lista;
    }

    /// <summary>
    /// Retorna a pontuacao detalhada de um jogador (etapa por etapa).
    /// </summary>
    public List<(string descricao, int pontos)> ListarPontuacao(int idJogador)
    {
        var lista = new List<(string, int)>();
        try
        {
            var respostaDll = Jogo.ListarPontuacao(idJogador);
            var tabela = DllHelper.AsDataTable(respostaDll);
            if (tabela == null) return lista;
            foreach (DataRow linhaTabela in tabela.Rows)
            {
                // Esperado: descricao, pontos (acumulado ou parcial)
                lista.Add((DllHelper.TextoNaPosicao(linhaTabela, 0), DllHelper.InteiroNaPosicao(linhaTabela, 1)));
            }
        }
        catch { /* metodo pode nao existir em versoes antigas */ }
        return lista;
    }

    // A funcao serve para consultar as especies de dinossauro conhecidas pela DLL.
    public List<Dinossauro> ListarDinossauros()
    {
        // A assinatura aceita um bool; passamos false (formato resumido)
        var respostaDll = Jogo.ListarDinossauros(false);
        var tabela = DllHelper.AsDataTable(respostaDll);
        var lista = new List<Dinossauro>();
        if (tabela == null) return lista;
        foreach (DataRow linhaTabela in tabela.Rows)
        {
            string codigo = DllHelper.TextoNaPosicao(linhaTabela, 0);
            lista.Add(new Dinossauro
            {
                Codigo = codigo,
                Nome = string.IsNullOrEmpty(DllHelper.TextoNaPosicao(linhaTabela, 1))
                    ? Dinossauro.NomePorCodigo(codigo)
                    : DllHelper.TextoNaPosicao(linhaTabela, 1),
                Cor = Dinossauro.CorPorCodigo(codigo)
            });
        }
        return lista;
    }
}


