using DraftosaurusClient.Controls;
using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;
using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

/// <summary>
/// Tela principal de jogo. SincronizaÃ§Ã£o via Timer (polling).
/// Suporta lado VerÃ£o e Inverno (detectado automaticamente).
/// </summary>
public class FormJogo : Form
{
    private readonly DraftService _svc;
    private readonly int _idPartida;
    private readonly int _idJogador;
    private readonly string _senha;
    private readonly string _nome;

    private readonly TabuleiroControl _tab;
    private readonly MaoControl _mao;
    private readonly Label _lblTurno, _lblDado, _lblFaceDesc, _lblStatus, _lblJogadorVez;
    private readonly ListBox _lstJogadores;
    private readonly TextBox _txtHistorico;
    private readonly Button _btnJogar, _btnVerOutro, _btnHistorico, _btnMudo, _btnPontuacao, _btnAuto;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly System.Windows.Forms.Timer _autoTimer;

    private int _turnoConhecido = -1;
    private char _statusTurnoConhecido = ' ';
    private bool _jaJogueiNesteTurno = false;
    private List<Jogador> _jogadores = new();
    private int? _jogadorVisualizado = null;
    private bool _ladoDetectado = false;
    private bool _fimDeJogoTratado = false;
    private bool _autoLigado = false;
    // Para detectar nova jogada visÃ­vel e animar
    private int _jogadasNoTurnoConhecidas = 0;

    public FormJogo(DraftService svc, int idPartida, int idJogador, string senha, string nome)
    {
        _svc = svc;
        _idPartida = idPartida;
        _idJogador = idJogador;
        _senha = senha;
        _nome = nome;

        Text = $"Draftosaurus â€” Partida {idPartida} â€” {nome} (#{idJogador})";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 240, 230);
        Font = new Font("Segoe UI", 9.5f);
        MinimumSize = new Size(900, 620);

        // ============ Barra superior ============
        var barra = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(60, 50, 40) };
        Controls.Add(barra);

        _lblTurno = new Label
        {
            Text = "Turno: -",
            Location = new Point(15, 8), AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold)
        };
        barra.Controls.Add(_lblTurno);

        _lblDado = new Label
        {
            Text = "Dado: -",
            Location = new Point(15, 36), AutoSize = true,
            ForeColor = Color.Yellow,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        barra.Controls.Add(_lblDado);

        _lblFaceDesc = new Label
        {
            Location = new Point(180, 36), AutoSize = true,
            ForeColor = Color.LightYellow,
            Font = new Font("Segoe UI", 9f, FontStyle.Italic)
        };
        barra.Controls.Add(_lblFaceDesc);

        _lblJogadorVez = new Label
        {
            Location = new Point(180, 8), AutoSize = true,
            ForeColor = Color.LightGreen,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        barra.Controls.Add(_lblJogadorVez);

        // BotÃ£o de mudo + pontuaÃ§Ã£o no canto direito
        _btnMudo = new Button
        {
            Text = "ðŸ”Š",
            Width = 36, Height = 32,
            Location = new Point(0, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 70, 60),
            ForeColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnMudo.Click += (_, _) =>
        {
            SomHelper.Silenciado = !SomHelper.Silenciado;
            _btnMudo.Text = SomHelper.Silenciado ? "ðŸ”‡" : "ðŸ”Š";
        };
        barra.Controls.Add(_btnMudo);

        _btnPontuacao = new Button
        {
            Text = "ðŸ† PontuaÃ§Ã£o",
            Width = 110, Height = 32,
            Location = new Point(0, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 70, 60),
            ForeColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnPontuacao.Click += (_, _) => AbrirPontuacao();
        barra.Controls.Add(_btnPontuacao);

        _btnAuto = new Button
        {
            Text = "Auto: OFF",
            Width = 90, Height = 32,
            Location = new Point(0, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 70, 60),
            ForeColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnAuto.Click += (_, _) => AlternarAutomatico();
        barra.Controls.Add(_btnAuto);

        _lblStatus = new Label
        {
            Location = new Point(380, 8),
            Size = new Size(400, 50),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 12, 0),
            Text = "Carregando..."
        };
        barra.Controls.Add(_lblStatus);

        // Posicionamento inicial dos botÃµes da direita
        void ReposicionarBarra()
        {
            _btnMudo.Left = barra.Width - _btnMudo.Width - 12;
            _btnPontuacao.Left = _btnMudo.Left - _btnPontuacao.Width - 8;
            _btnAuto.Left = _btnPontuacao.Left - _btnAuto.Width - 8;
            _lblStatus.Width = Math.Max(200, _btnAuto.Left - 380);
        }
        barra.Resize += (_, _) => ReposicionarBarra();
        ReposicionarBarra();

        // ============ Layout principal ============
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        Controls.Add(split);
        split.BringToFront();

        void AjustarDivisor()
        {
            if (split.Width <= 0) return;
            int desejado = Math.Min(180, Math.Max(120, split.Width / 5));
            int maximo = Math.Max(0, split.Width - split.SplitterWidth - 260);
            split.SplitterDistance = Math.Max(0, Math.Min(desejado, maximo));
        }

        split.HandleCreated += (_, _) => AjustarDivisor();
        split.Resize += (_, _) => AjustarDivisor();

        // ----- Painel esquerdo
        var pnlEsq = split.Panel1;
        pnlEsq.BackColor = Color.FromArgb(235, 225, 210);

        var lblJogs = new Label
        {
            Text = "JOGADORES",
            Dock = DockStyle.Top, Height = 24,
            Padding = new Padding(8, 4, 0, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            BackColor = Color.FromArgb(120, 90, 60),
            ForeColor = Color.White
        };
        pnlEsq.Controls.Add(lblJogs);

        _lstJogadores = new ListBox { Dock = DockStyle.Top, Height = 130 };
        _lstJogadores.DoubleClick += (_, _) => AlternarVisualizacao();
        pnlEsq.Controls.Add(_lstJogadores);
        _lstJogadores.BringToFront();

        _btnVerOutro = new Button { Text = "ðŸ‘ Ver tabuleiro do selecionado", Dock = DockStyle.Top, Height = 32 };
        _btnVerOutro.Text = "Ver tabuleiro";
        _btnVerOutro.Click += (_, _) => AlternarVisualizacao();
        pnlEsq.Controls.Add(_btnVerOutro);
        _btnVerOutro.BringToFront();

        _btnHistorico = new Button { Text = "ðŸ“œ Atualizar histÃ³rico", Dock = DockStyle.Top, Height = 32 };
        _btnHistorico.Click += (_, _) => CarregarHistorico();
        pnlEsq.Controls.Add(_btnHistorico);
        _btnHistorico.BringToFront();

        _txtHistorico = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = Color.FromArgb(250, 245, 230),
            Font = new Font("Consolas", 8.5f)
        };
        pnlEsq.Controls.Add(_txtHistorico);
        _txtHistorico.BringToFront();

        // ----- Painel direito
        var pnlDir = split.Panel2;
        pnlDir.BackColor = Color.FromArgb(245, 240, 230);
        pnlDir.AutoScroll = true;

        var conteudoJogo = new Panel
        {
            Location = Point.Empty,
            BackColor = Color.FromArgb(245, 240, 230)
        };
        pnlDir.Controls.Add(conteudoJogo);

        var pnlTopo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        _tab = new TabuleiroControl { Dock = DockStyle.Fill };
        _tab.CercadoClicado += Tabuleiro_CercadoClicado;
        pnlTopo.Controls.Add(_tab);

        var pnlBaixo = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 210,
            BackColor = Color.FromArgb(60, 50, 40),
            Padding = new Padding(4)
        };
        var lblMao = new Label
        {
            Text = "ðŸ¦• SUA MÃƒO â€” clique em uma espÃ©cie e depois em um cercado vÃ¡lido (verde) do tabuleiro:",
            Dock = DockStyle.Top, Height = 22,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(6, 4, 0, 0)
        };
        pnlBaixo.Controls.Add(lblMao);

        _mao = new MaoControl { Dock = DockStyle.Fill };
        _mao.DinossauroSelecionado += (_, cod) => AtualizarBotaoJogar();
        pnlBaixo.Controls.Add(_mao);
        _mao.BringToFront();

        _btnJogar = new Button
        {
            Text = "Selecione um dinossauro e um cercado",
            Dock = DockStyle.Bottom, Height = 36,
            BackColor = Color.Gray, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Enabled = false
        };
        _btnJogar.Click += (_, _) => Jogar();
        pnlBaixo.Controls.Add(_btnJogar);
        _btnJogar.BringToFront();

        conteudoJogo.Controls.Add(pnlTopo);
        conteudoJogo.Controls.Add(pnlBaixo);

        void AjustarConteudoJogo()
        {
            int largura = Math.Max(620, pnlDir.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
            int altura = Math.Max(780, pnlDir.ClientSize.Height);
            conteudoJogo.Size = new Size(largura, altura);
        }

        pnlDir.Resize += (_, _) => AjustarConteudoJogo();
        AjustarConteudoJogo();

        _timer = new System.Windows.Forms.Timer { Interval = 1500 };
        _timer.Tick += (_, _) => AtualizarEstado();
        _timer.Start();

        _autoTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _autoTimer.Tick += (_, _) => FluxoAutomatico();

        Load += (_, _) => { DetectarLado(); AtualizarEstado(); };
        FormClosing += (_, _) => { _timer.Stop(); _autoTimer.Stop(); };
    }

    /// <summary>Descobre verÃ£o ou inverno via ListarCercados().</summary>
    private void DetectarLado()
    {
        if (_ladoDetectado) return;
        try
        {
            var cercs = _svc.ListarCercados();
            var codigos = cercs.Select(c => c.Codigo);
            var lado = DraftService.DetectarLado(codigos);
            _tab.Lado = lado;
            _ladoDetectado = true;
            _tab.Invalidate();
        }
        catch { /* tenta de novo na prÃ³xima atualizaÃ§Ã£o */ }
    }

    // ===========================================================
    // SINCRONIZAÃ‡ÃƒO
    // ===========================================================

    private void AtualizarEstado()
    {
        if (!_ladoDetectado) DetectarLado();

        try
        {
            var estado = _svc.VerificarPartida(_idPartida);
            _jogadores = _svc.ListarJogadores(_idPartida);

            _lblTurno.Text = estado.Status == 'E'
                ? "PARTIDA ENCERRADA"
                : $"Turno: {estado.TurnoAtual} / 12";
            _lblDado.Text = $"Dado: {estado.FaceDado} ({FaceDado.NomePorCodigo(estado.FaceDado)})";
            _lblFaceDesc.Text = FaceDado.DescricaoPorCodigo(estado.FaceDado);

            string nomeDoDado = _jogadores.FirstOrDefault(j => j.Id == estado.IdJogadorComDado)?.Nome
                                ?? $"#{estado.IdJogadorComDado}";
            _lblJogadorVez.Text = estado.Status == 'E' ? "ðŸ Fim de jogo" : $"Dado com: {nomeDoDado}";

            AtualizarListaJogadores(estado);

            // Detecta novo turno
            if (estado.TurnoAtual != _turnoConhecido || estado.StatusTurno != _statusTurnoConhecido)
            {
                bool turnoMudou = estado.TurnoAtual != _turnoConhecido && _turnoConhecido != -1;
                _turnoConhecido = estado.TurnoAtual;
                _statusTurnoConhecido = estado.StatusTurno;
                _jaJogueiNesteTurno = false;
                _jogadasNoTurnoConhecidas = 0;
                _cercadoSelecionado = null;
                _tab.CercadoSelecionado = null;
                _mao.LimparSelecao();

                // Som de novo turno (nÃ£o no carregamento inicial)
                if (turnoMudou && estado.Status == 'J')
                    SomHelper.NovoTurno();
            }

            _tab.FaceDadoAtual = estado.FaceDado;
            _tab.IgnoraDado = (_idJogador == estado.IdJogadorComDado);

            // Detecta jogadas novas (de outros jogadores) e anima a mais recente
            var jogadasDoTurno = BuscarJogadasDoTurnoAtual();
            if (JogadorJaJogou(jogadasDoTurno, _idJogador))
                _jaJogueiNesteTurno = true;

            DetectarJogadaNova(jogadasDoTurno);

            // Final de jogo
            if (estado.Status == 'E')
            {
                _tab.Interativo = false;
                _btnJogar.Enabled = false;
                _btnJogar.Text = "Partida encerrada";
                _lblStatus.Text = MontarRanking();

                if (!_fimDeJogoTratado)
                {
                    _fimDeJogoTratado = true;
                    SomHelper.FimDeJogo();
                    BeginInvoke((Action)(() => AbrirPontuacao()));
                }

                AtualizarTabuleiroExibido();
                AtualizarMao();
                return;
            }

            bool minhaVezDeJogar = !_jaJogueiNesteTurno && estado.StatusTurno == 'A';
            _tab.Interativo = minhaVezDeJogar && _jogadorVisualizado == null;
            _lblStatus.Text = minhaVezDeJogar
                ? "âœ… Sua vez â€” escolha um dinossauro e um cercado."
                : (_jaJogueiNesteTurno ? "â³ Aguardando outros jogadores..." : "â³ Turno sendo finalizado...");

            AtualizarTabuleiroExibido();
            AtualizarMao();
            AtualizarBotaoJogar();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Erro: " + ex.Message;
        }
    }

    /// <summary>
    /// Compara as jogadas visÃ­veis do turno atual com o que conhecÃ­amos.
    /// Se uma nova apareceu (de outro jogador) e ela Ã© no tabuleiro que estamos
    /// vendo, dispara animaÃ§Ã£o.
    /// </summary>
    private List<JogadaTurno> BuscarJogadasDoTurnoAtual()
    {
        try
        {
            var (_, jogadas) = _svc.VerificarTurno(_idPartida);
            return jogadas;
        }
        catch
        {
            return new List<JogadaTurno>();
        }
    }

    private static bool JogadorJaJogou(List<JogadaTurno> jogadas, int idJogador)
    {
        return jogadas.Any(j =>
            j.IdJogador == idJogador &&
            !string.IsNullOrWhiteSpace(j.CodigoDinossauro) &&
            !j.CodigoDinossauro.Equals("XX", StringComparison.OrdinalIgnoreCase));
    }

    private void DetectarJogadaNova(List<JogadaTurno> jogadas)
    {
        try
        {
            // Filtra jogadas com dados visÃ­veis (nÃ£o XX)
            var visiveis = jogadas.Where(j =>
                !string.IsNullOrEmpty(j.CodigoDinossauro) &&
                j.CodigoDinossauro != "XX").ToList();

            if (visiveis.Count > _jogadasNoTurnoConhecidas)
            {
                int idAlvo = _jogadorVisualizado ?? _idJogador;
                // Pega a Ãºltima nova (do jogador que estamos olhando)
                for (int i = _jogadasNoTurnoConhecidas; i < visiveis.Count; i++)
                {
                    var j = visiveis[i];
                    if (j.IdJogador == idAlvo)
                    {
                        _tab.AnimarColocacao(j.CodigoCercado, j.CodigoDinossauro);
                        break; // anima sÃ³ uma por ciclo de tick
                    }
                }
                _jogadasNoTurnoConhecidas = visiveis.Count;
            }
            else if (visiveis.Count < _jogadasNoTurnoConhecidas)
            {
                // Turno virou â€” reseta contador
                _jogadasNoTurnoConhecidas = visiveis.Count;
            }
        }
        catch { }
    }

    private void AtualizarListaJogadores(EstadoPartida estado)
    {
        _lstJogadores.BeginUpdate();
        int? idSelecionado = null;
        if (_lstJogadores.SelectedIndex >= 0 && _lstJogadores.SelectedIndex < _jogadores.Count)
            idSelecionado = _jogadores[_lstJogadores.SelectedIndex].Id;

        _lstJogadores.Items.Clear();
        foreach (var j in _jogadores)
        {
            string marca = j.Id == _idJogador ? " (vocÃª)" : "";
            string dado = j.Id == estado.IdJogadorComDado ? " ðŸŽ²" : "";
            string vis = (_jogadorVisualizado.HasValue && _jogadorVisualizado.Value == j.Id) ? " ðŸ‘" : "";
            string pts = estado.Status == 'E' ? $"  [{j.Pontuacao} pts]" : "";
            _lstJogadores.Items.Add($"#{j.Id} {j.Nome}{marca}{dado}{vis}{pts}");
        }
        int? idParaSelecionar = _jogadorVisualizado ?? idSelecionado;
        if (idParaSelecionar.HasValue)
        {
            int indice = _jogadores.FindIndex(j => j.Id == idParaSelecionar.Value);
            if (indice >= 0 && indice < _lstJogadores.Items.Count)
                _lstJogadores.SelectedIndex = indice;
        }

        _lstJogadores.EndUpdate();
    }

    private void AtualizarTabuleiroExibido()
    {
        try
        {
            int idAlvo = _jogadorVisualizado ?? _idJogador;
            var tab = (idAlvo == _idJogador)
                ? _svc.ExibirTabuleiro(idAlvo, _senha)
                : _svc.ExibirTabuleiro(idAlvo);
            _tab.AtualizarEstado(tab);
        }
        catch { }
    }

    private void AtualizarMao()
    {
        try
        {
            var mao = _svc.ExibirMao(_idJogador, _senha);
            _mao.AtualizarMao(mao);
        }
        catch (Exception ex)
        {
            _mao.AtualizarMao(new Dictionary<string, int>());
            _lblStatus.Text = "Erro ao carregar mao: " + ex.Message;
        }
    }

    // ===========================================================
    // INTERAÃ‡ÃƒO DO JOGADOR
    // ===========================================================

    // ===========================================================
    // AUTOMACAO TEMPORAL
    // ===========================================================

    private void AlternarAutomatico()
    {
        _autoLigado = !_autoLigado;
        _btnAuto.Text = _autoLigado ? "Auto: ON" : "Auto: OFF";
        _btnAuto.BackColor = _autoLigado ? Color.DarkGreen : Color.FromArgb(80, 70, 60);

        if (_autoLigado)
        {
            FluxoAutomatico();
            _autoTimer.Start();
        }
        else
        {
            _autoTimer.Stop();
        }
    }

    /// <summary>
    /// Fluxo do diagrama: a cada 5 segundos verifica a partida; se for
    /// momento de jogar, busca mao/tabuleiro, escolhe uma jogada simples,
    /// chama Jogar e atualiza a tela.
    /// </summary>
    private void FluxoAutomatico()
    {
        if (!_autoLigado || _fimDeJogoTratado || _jogadorVisualizado != null) return;

        try
        {
            var estado = _svc.VerificarPartida(_idPartida);
            if (!PodeAutomacaoJogar(estado))
            {
                AtualizarEstado();
                return;
            }

            var mao = _svc.ExibirMao(_idJogador, _senha);
            var tabuleiro = _svc.ExibirTabuleiro(_idJogador, _senha);
            bool jogou = TentarJogadaAutomatica(mao, tabuleiro, estado);

            _lblStatus.Text = jogou
                ? "Automacao: jogada enviada."
                : "Automacao: nenhuma jogada simples foi aceita pela DLL.";

            AtualizarEstado();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Automacao: " + ex.Message;
        }
    }

    private bool PodeAutomacaoJogar(EstadoPartida estado)
    {
        if (JogadorJaJogou(BuscarJogadasDoTurnoAtual(), _idJogador))
        {
            _jaJogueiNesteTurno = true;
            return false;
        }

        return estado.Status == 'J'
            && estado.StatusTurno == 'A'
            && !_jaJogueiNesteTurno;
    }

    private bool TentarJogadaAutomatica(
        Dictionary<string, int> mao,
        Dictionary<string, List<string>> tabuleiro,
        EstadoPartida estado)
    {
        var candidatos = MontarJogadasCandidatas(mao, tabuleiro, estado)
            .OrderByDescending(j => j.Pontos)
            .ThenBy(j => j.Cercado)
            .ThenBy(j => j.Dino)
            .ToList();

        foreach (var jogada in candidatos)
        {
            try
            {
                _svc.Jogar(_idJogador, _senha, jogada.Dino, jogada.Cercado);
                _jaJogueiNesteTurno = true;
                _mao.LimparSelecao();
                _cercadoSelecionado = null;
                _tab.CercadoSelecionado = null;
                _tab.AnimarColocacao(jogada.Cercado, jogada.Dino);
                return true;
            }
            catch
            {
                // A DLL valida as regras completas. Se falhar, tenta a proxima jogada do ranking.
            }
        }

        return false;
    }

    private List<JogadaAutomatica> MontarJogadasCandidatas(
        Dictionary<string, int> mao,
        Dictionary<string, List<string>> tabuleiro,
        EstadoPartida estado)
    {
        var candidatos = new List<JogadaAutomatica>();
        var dinosNaMao = mao
            .Where(kv => kv.Value > 0)
            .Select(kv => kv.Key)
            .OrderBy(cod => cod)
            .ToList();

        foreach (var dino in dinosNaMao)
        {
            foreach (var cercado in CercadosPossiveisParaAutomacao(estado, tabuleiro))
            {
                int? pontos = PontuarJogadaAutomatica(dino, cercado, tabuleiro);
                if (pontos.HasValue)
                    candidatos.Add(new JogadaAutomatica(dino, cercado, pontos.Value));
            }
        }

        return candidatos;
    }

    /// <summary>
    /// Heuristica deterministica, nao aleatoria:
    /// - respeita regras simples dos cercados antes de chamar a DLL;
    /// - prioriza cercados com melhor potencial de pontuacao;
    /// - deixa o Rio como ultima opcao, por valer pouco.
    /// </summary>
    private int? PontuarJogadaAutomatica(
        string dino,
        string cercado,
        Dictionary<string, List<string>> tabuleiro)
    {
        tabuleiro.TryGetValue(cercado, out var dinosNoCercado);
        dinosNoCercado ??= new List<string>();

        if (cercado == "RI")
            return 1;

        var mapa = Cercado.CercadosPorLado(_tab.Lado);
        if (!mapa.TryGetValue(cercado, out var info))
            return null;

        if (dinosNoCercado.Count >= info.Capacidade)
            return null;

        return cercado switch
        {
            "FI" => PontuarFlorestaIgualdade(dino, dinosNoCercado),
            "CD" => PontuarCampinaDiferenca(dino, dinosNoCercado),
            "MT" => 35 + dinosNoCercado.Count * 10,
            "PA" => 30 + dinosNoCercado.Count(x => x == dino) * 20,
            "RS" => dinosNoCercado.Count == 0 ? 25 : null,
            "IS" => dinosNoCercado.Count == 0 ? PontuarIlhaSolitaria(dino, tabuleiro) : null,
            _ => 10 + dinosNoCercado.Count
        };
    }

    private static int? PontuarFlorestaIgualdade(string dino, List<string> dinosNoCercado)
    {
        if (dinosNoCercado.Count == 0)
            return 40;

        return dinosNoCercado.All(x => x == dino)
            ? 50 + dinosNoCercado.Count * 10
            : null;
    }

    private static int? PontuarCampinaDiferenca(string dino, List<string> dinosNoCercado)
    {
        return dinosNoCercado.Contains(dino)
            ? null
            : 45 + dinosNoCercado.Count * 10;
    }

    private static int PontuarIlhaSolitaria(
        string dino,
        Dictionary<string, List<string>> tabuleiro)
    {
        bool especieJaEstaNoZoo = tabuleiro.Values.Any(lista => lista.Contains(dino));
        return especieJaEstaNoZoo ? 15 : 45;
    }

    private IEnumerable<string> CercadosPossiveisParaAutomacao(
        EstadoPartida estado,
        Dictionary<string, List<string>> tabuleiro)
    {
        foreach (var cod in Cercado.CercadosPorLado(_tab.Lado).Keys)
            if (PodeUsarCercadoPeloDado(cod, estado, tabuleiro))
                yield return cod;
    }

    private bool PodeUsarCercadoPeloDado(
        string cod,
        EstadoPartida estado,
        Dictionary<string, List<string>> tabuleiro)
    {
        if (cod == "RI") return true;
        if (_idJogador == estado.IdJogadorComDado) return true;
        if (string.IsNullOrEmpty(estado.FaceDado)) return true;

        var mapa = Cercado.CercadosPorLado(_tab.Lado);
        if (!mapa.TryGetValue(cod, out var info)) return true;

        return estado.FaceDado switch
        {
            "FL" => info.Lado == LadoTabuleiro.Floresta,
            "PR" => info.Lado == LadoTabuleiro.Pradaria,
            "AL" => info.Lateral == LateralTabuleiro.Alimentacao,
            "WC" => info.Lateral == LateralTabuleiro.Banheiros,
            "VZ" => !tabuleiro.TryGetValue(cod, out var dinos) || dinos.Count == 0,
            "TI" => !tabuleiro.TryGetValue(cod, out var dinos) || !dinos.Contains("Ti"),
            _ => true
        };
    }

    private record JogadaAutomatica(string Dino, string Cercado, int Pontos);

    private string? _cercadoSelecionado;

    private void Tabuleiro_CercadoClicado(object? sender, string cercadoCod)
    {
        _cercadoSelecionado = cercadoCod;
        _tab.CercadoSelecionado = cercadoCod;
        _tab.Invalidate();
        AtualizarBotaoJogar();

        if (_mao.CodigoSelecionado != null)
            Jogar();
    }

    private void AtualizarBotaoJogar()
    {
        bool podeJogar = !_jaJogueiNesteTurno
                       && _mao.CodigoSelecionado != null
                       && _cercadoSelecionado != null
                       && _jogadorVisualizado == null;

        _btnJogar.Enabled = podeJogar;
        if (podeJogar)
        {
            string nomeDino = Dinossauro.NomePorCodigo(_mao.CodigoSelecionado!);
            string nomeCerc = NomeCercado(_cercadoSelecionado!);
            _btnJogar.Text = $"â–¶ Colocar {nomeDino} em {nomeCerc}";
            _btnJogar.BackColor = Color.LimeGreen;
        }
        else
        {
            _btnJogar.BackColor = Color.Gray;
            if (_jaJogueiNesteTurno) _btnJogar.Text = "Aguardando prÃ³ximo turno...";
            else if (_mao.CodigoSelecionado == null) _btnJogar.Text = "Escolha um dinossauro da sua mÃ£o";
            else if (_cercadoSelecionado == null) _btnJogar.Text = "Escolha um cercado no tabuleiro";
        }
    }

    private string NomeCercado(string cod)
    {
        if (cod == "RI") return "Rio";
        var mapa = Cercado.CercadosPorLado(_tab.Lado);
        return mapa.TryGetValue(cod, out var info) ? info.Nome : cod;
    }

    private void Jogar()
    {
        if (_mao.CodigoSelecionado == null || _cercadoSelecionado == null) return;
        try
        {
            string codDino = _mao.CodigoSelecionado;
            string codCerc = _cercadoSelecionado;

            int prox = _svc.Jogar(_idJogador, _senha, codDino, codCerc);
            _jaJogueiNesteTurno = true;
            _cercadoSelecionado = null;
            _tab.CercadoSelecionado = null;
            _mao.LimparSelecao();

            _tab.AnimarColocacao(codCerc, codDino);

            AtualizarEstado();
            if (prox == 0)
            {
                MessageBox.Show("Partida encerrada!\nA tela de pontuacao sera exibida.",
                    "Fim de jogo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            SomHelper.Erro();
            if (JogadaJaRealizadaErro(ex))
            {
                _jaJogueiNesteTurno = true;
                _cercadoSelecionado = null;
                _tab.CercadoSelecionado = null;
                _mao.LimparSelecao();
                _tab.Interativo = false;
                _lblStatus.Text = "Aguardando outros jogadores...";
                AtualizarBotaoJogar();
                AtualizarEstado();
                _jaJogueiNesteTurno = true;
                _tab.Interativo = false;
                _lblStatus.Text = "Aguardando outros jogadores...";
                AtualizarBotaoJogar();
            }

            MessageBox.Show("Jogada invalida:\n" + ex.Message + "\n\n" +
                            "Verifique se o cercado e compativel com o dado, " +
                            "se o cercado tem espaco, e a regra do cercado.",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool JogadaJaRealizadaErro(Exception ex)
    {
        string msg = ex.Message.ToLowerInvariant();
        return msg.Contains("ja realizou")
            || msg.Contains("já realizou")
            || msg.Contains("jogada neste turno");
    }

    // ===========================================================
    // VISUALIZAÃ‡ÃƒO E PONTUAÃ‡ÃƒO
    // ===========================================================

    private void AlternarVisualizacao()
    {
        if (_jogadorVisualizado.HasValue)
        {
            _jogadorVisualizado = null;
            _btnVerOutro.Text = "Ver tabuleiro";
            Text = $"Draftosaurus - Partida {_idPartida} - {_nome} (#{_idJogador})";
            AtualizarEstado();
            return;
        }

        if (_lstJogadores.SelectedIndex < 0 || _lstJogadores.SelectedIndex >= _jogadores.Count)
        {
            MessageBox.Show("Selecione um jogador na lista primeiro.", "Tabuleiro",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var alvo = _jogadores[_lstJogadores.SelectedIndex];
        if (alvo.Id == _idJogador)
        {
            _jogadorVisualizado = null;
            _btnVerOutro.Text = "Ver tabuleiro";
            Text = $"Draftosaurus - Partida {_idPartida} - {_nome} (#{_idJogador})";
        }
        else
        {
            _jogadorVisualizado = alvo.Id;
            _btnVerOutro.Text = "Voltar ao meu tabuleiro";
            Text = $"Visualizando: {alvo.Nome} #{alvo.Id}";
        }
        AtualizarEstado();
    }

    private void CarregarHistorico()
    {
        try
        {
            _txtHistorico.Text = _svc.ListarHistorico(_idPartida);
            _txtHistorico.SelectionStart = _txtHistorico.Text.Length;
            _txtHistorico.ScrollToCaret();
        }
        catch (Exception ex)
        {
            _txtHistorico.Text = "Erro: " + ex.Message;
        }
    }

    private void AbrirPontuacao()
    {
        if (_jogadores.Count == 0) return;
        using var dlg = new FormPontuacao(_svc, _idPartida, _jogadores);
        dlg.ShowDialog(this);
    }

    private string MontarRanking()
    {
        if (_jogadores.Count == 0) return "Fim de jogo";
        var ord = _jogadores.OrderByDescending(j => j.Pontuacao).ToList();
        var sb = new System.Text.StringBuilder("ðŸ† ");
        for (int i = 0; i < ord.Count; i++)
        {
            sb.Append($"{i + 1}Âº {ord[i].Nome}={ord[i].Pontuacao}");
            if (i < ord.Count - 1) sb.Append(" | ");
        }
        return sb.ToString();
    }
}

