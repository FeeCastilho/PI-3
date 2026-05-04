using System.Drawing.Drawing2D;
using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;

namespace DraftosaurusClient.Controls;

/// <summary>
/// Controle que desenha o tabuleiro de verao do jogador.
///
/// Verao (3 linhas x 2 colunas + Rio vertical):
///   FI | RS
///   MT | CD
///   PA | IS
///
/// Eventos:
///   - CercadoClicado: dispara quando o usuario clica em um cercado.
///
/// Animacoes:
///   - AnimarColocacao(cercado, dino): faz uma "queda" do dino ate o cercado.
/// </summary>
public class TabuleiroControl : Control
{
    private readonly Dictionary<string, Rectangle> _areasCercados = new();
    private readonly Dictionary<string, GraphicsPath> _formasCercados = new();
    private readonly Image? _imagemTabuleiro;
    private Dictionary<string, List<string>> _estado = new();
    private Rectangle _areaTabuleiro = Rectangle.Empty;
    private string? _cercadoHover;

    /// <summary>Este projeto usa apenas o lado verao.</summary>
    public LadoMapa Lado { get; set; } = LadoMapa.Verao;

    public bool Interativo { get; set; } = false;
    public string? FaceDadoAtual { get; set; }
    public bool IgnoraDado { get; set; } = false;
    public string? CercadoSelecionado { get; set; }
    public bool MostrarInstrucoes { get; set; } = false;

    public event EventHandler<string>? CercadoClicado;

    // Animacao
    private System.Windows.Forms.Timer? _animTimer;
    private string? _animCercado;
    private string? _animDino;
    private float _animProgresso; // 0..1

    // Esta funcao cuida de iniciar 'TabuleiroControl' do programa.
    public TabuleiroControl()
    {
        DoubleBuffered = true;
        AllowDrop = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
               | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(180, 220, 180);
        MinimumSize = new Size(520, 360);
        _imagemTabuleiro = CarregarImagemTabuleiro();
    }

    // A funcao serve para carregar a imagem real do tabuleiro de verao.
    private static Image? CarregarImagemTabuleiro()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "assets", "tabuleiro-verao.jpg");
        if (!File.Exists(caminho)) return null;

        using var fs = new FileStream(caminho, FileMode.Open, FileAccess.Read);
        using var img = Image.FromStream(fs);
        return new Bitmap(img);
    }

    // A funcao serve para fazer a sincronizacao principal da tela com o backend.
    public void AtualizarEstado(Dictionary<string, List<string>> estado)
    {
        var novo = estado ?? new();
        if (EstadosIguais(_estado, novo)) return;
        _estado = novo;
        Invalidate();
    }

    // Esta funcao evita redesenhar o tabuleiro se nada mudou.
    private static bool EstadosIguais(
        Dictionary<string, List<string>> a,
        Dictionary<string, List<string>> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var l)) return false;
            if (l.Count != kv.Value.Count) return false;
            for (int i = 0; i < l.Count; i++)
                if (l[i] != kv.Value[i]) return false;
        }
        return true;
    }

    /// <summary>
    /// Anima a colocacao de um dinossauro em um cercado. O dino "cai"
    /// do topo da tela ate a posicao final no cercado.
    /// </summary>
    public void AnimarColocacao(string codCercado, string codDino)
    {
        _animCercado = codCercado;
        _animDino = codDino;
        _animProgresso = 0f;

        _animTimer?.Stop();
        _animTimer?.Dispose();
        _animTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _animTimer.Tick += (s, e) =>
        {
            _animProgresso += 0.05f;
            if (_animProgresso >= 1f)
            {
                _animProgresso = 1f;
                _animTimer!.Stop();
                _animTimer.Dispose();
                _animTimer = null;
                _animCercado = null;
                _animDino = null;
                SomHelper.ColocarDino();
            }
            Invalidate();
        };
        _animTimer.Start();
    }

    // Esta funcao cuida de recalcular areas clicaveis quando o tamanho muda.
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CalcularAreas();
        Invalidate();
    }

    // A funcao serve para definir onde ficam os cercados na tela.
    private void CalcularAreas()
    {
        foreach (var forma in _formasCercados.Values)
            forma.Dispose();

        _areasCercados.Clear();
        _formasCercados.Clear();
        _areaTabuleiro = CalcularRetanguloTabuleiro();

        if (_imagemTabuleiro != null)
        {
            CalcularAreasImagemVerao();
            return;
        }

        int W = _areaTabuleiro.Width;
        int H = _areaTabuleiro.Height;
        int margem = Math.Max(6, Math.Min(W, H) / 70);
        int larguraRio = Math.Max(30, W / 13);

        int alturaTopo = Math.Max(22, H / 18);
        int alturaUtil = H - alturaTopo - 4 * margem;
        int alturaCercado = alturaUtil / 3;
        int larguraColuna = (W - 4 * margem - larguraRio) / 2;

        int xEsq = _areaTabuleiro.Left + margem;
        int xRio = xEsq + larguraColuna + margem;
        int xDir = xRio + larguraRio + margem;

        int yLinha1 = _areaTabuleiro.Top + alturaTopo + margem;
        int yLinha2 = yLinha1 + alturaCercado + margem;
        int yLinha3 = yLinha2 + alturaCercado + margem;

        _areasCercados["FI"] = new Rectangle(xEsq, yLinha1, larguraColuna, alturaCercado);
        _areasCercados["MT"] = new Rectangle(xEsq, yLinha2, larguraColuna, alturaCercado);
        _areasCercados["PA"] = new Rectangle(xEsq, yLinha3, larguraColuna, alturaCercado);
        _areasCercados["RS"] = new Rectangle(xDir, yLinha1, larguraColuna, alturaCercado);
        _areasCercados["CD"] = new Rectangle(xDir, yLinha2, larguraColuna, alturaCercado);
        _areasCercados["IS"] = new Rectangle(xDir, yLinha3, larguraColuna, alturaCercado);

        // Rio vertical (presente nos dois lados)
        _areasCercados["RI"] = new Rectangle(xRio, yLinha1, larguraRio, alturaCercado * 3 + margem * 2);
    }

    // Esta funcao faz alinhar as areas clicaveis com a imagem do tabuleiro.
    private void CalcularAreasImagemVerao()
    {
        // Areas em percentual do JPG assets/tabuleiro-verao.jpg.
        // Mantem o clique alinhado com a arte real sem complicar o desenho.
        DefinirArea("FI", AreaPct(0.04f, 0.03f, 0.34f, 0.24f));
        DefinirArea("MT", AreaPct(0.04f, 0.37f, 0.34f, 0.24f));
        DefinirArea("PA", AreaPct(0.05f, 0.69f, 0.34f, 0.23f));

        DefinirArea("RS", AreaPct(0.64f, 0.08f, 0.26f, 0.26f));
        DefinirArea("CD", AreaPct(0.60f, 0.40f, 0.36f, 0.23f));
        DefinirArea("IS", AreaPct(0.71f, 0.70f, 0.25f, 0.22f));

        var rioBounds = AreaPct(0.39f, 0.00f, 0.24f, 1.00f);
        DefinirArea("RI", rioBounds, CriarFormaRioVerao());
    }

    // Esta funcao cuida de registrar retangulo e formato clicavel de um cercado.
    private void DefinirArea(string codigo, Rectangle area, GraphicsPath? forma = null)
    {
        _areasCercados[codigo] = area;
        _formasCercados[codigo] = forma ?? CriarFormaRetangular(area);
    }

    // A funcao serve para criar uma area clicavel retangular.
    private static GraphicsPath CriarFormaRetangular(Rectangle area)
    {
        var path = new GraphicsPath();
        path.AddRectangle(area);
        return path;
    }

    // Esta funcao faz criar uma area clicavel parecida com o formato do rio.
    private GraphicsPath CriarFormaRioVerao()
    {
        // Poligono aproximado do rio na imagem real. E melhor para clique/hover
        // do que um retangulo, mas continua simples de explicar.
        var pontos = new[]
        {
            Pct(0.47f, 0.00f), Pct(0.57f, 0.00f), Pct(0.58f, 0.11f),
            Pct(0.56f, 0.22f), Pct(0.50f, 0.36f), Pct(0.47f, 0.48f),
            Pct(0.52f, 0.61f), Pct(0.59f, 0.77f), Pct(0.61f, 1.00f),
            Pct(0.50f, 1.00f), Pct(0.47f, 0.84f), Pct(0.42f, 0.68f),
            Pct(0.40f, 0.55f), Pct(0.43f, 0.42f), Pct(0.48f, 0.29f),
            Pct(0.50f, 0.16f)
        };

        var path = new GraphicsPath();
        path.AddPolygon(pontos);
        return path;
    }

    // Esta funcao cuida de converter porcentagem da imagem em coordenada da tela.
    private Point Pct(float x, float y) => new(
        _areaTabuleiro.Left + (int)(_areaTabuleiro.Width * x),
        _areaTabuleiro.Top + (int)(_areaTabuleiro.Height * y));

    // A funcao serve para criar retangulos usando porcentagem da imagem.
    private Rectangle AreaPct(float x, float y, float w, float h)
    {
        return new Rectangle(
            _areaTabuleiro.Left + (int)(_areaTabuleiro.Width * x),
            _areaTabuleiro.Top + (int)(_areaTabuleiro.Height * y),
            Math.Max(1, (int)(_areaTabuleiro.Width * w)),
            Math.Max(1, (int)(_areaTabuleiro.Height * h)));
    }

    // Esta funcao faz definir o espaco total onde o tabuleiro e desenhado.
    private Rectangle CalcularRetanguloTabuleiro()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return Rectangle.Empty;

        int padding = 8;
        return new Rectangle(
            padding,
            padding,
            Math.Max(1, ClientSize.Width - padding * 2),
            Math.Max(1, ClientSize.Height - padding * 2));
    }

    // Esta funcao cuida de desenhar o tabuleiro, dinossauros e instrucoes.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_areasCercados.Count == 0) CalcularAreas();

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var fora = new SolidBrush(Color.FromArgb(232, 226, 214)))
            g.FillRectangle(fora, ClientRectangle);

        bool usaImagem = _imagemTabuleiro != null;

        if (usaImagem)
            g.DrawImage(_imagemTabuleiro!, _areaTabuleiro);

        // Fundo do tabuleiro
        Color fundoCor = Color.FromArgb(180, 220, 180);
        if (!usaImagem)
        {
            using var fundo = new SolidBrush(fundoCor);
            g.FillRoundedRect(fundo, _areaTabuleiro, 10);
        }

        if (!usaImagem)
            DesenharOrientacao(g);

        // Cercados a desenha o Rio primeiro (atras)
        if (_areasCercados.TryGetValue("RI", out var areaRio))
            DesenharCercado(g, "RI", areaRio);

        foreach (var kv in _areasCercados)
        {
            if (kv.Key == "RI") continue;
            DesenharCercado(g, kv.Key, kv.Value);
        }

        // Animacao de colocacao por cima de tudo
        if (_animCercado != null && _animDino != null && _areasCercados.TryGetValue(_animCercado, out var aDest))
        {
            DesenharAnimacao(g, _animDino, aDest);
        }

        if (MostrarInstrucoes)
            DesenharInstrucoes(g);
    }

    // A funcao serve para desenhar textos de orientacao quando nao ha imagem.
    private void DesenharOrientacao(Graphics g)
    {
        using var fontePeq = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var brushTxt = new SolidBrush(Color.FromArgb(80, 50, 20));
        g.DrawString("< ALIMENTACAO", fontePeq, brushTxt, 4, 1);
        var medida = g.MeasureString("BANHEIROS >", fontePeq);
        g.DrawString("BANHEIROS >", fontePeq, brushTxt, ClientSize.Width - medida.Width - 4, 1);

        // Etiqueta do lado
        string etiqueta = "LADO VERAO";
        using var fonteLado = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var sz = g.MeasureString(etiqueta, fonteLado);
        g.DrawString(etiqueta, fonteLado, brushTxt, (ClientSize.Width - sz.Width) / 2, 1);
    }

    // Esta funcao desenha cada cercado e sua borda de validade.
    private void DesenharCercado(Graphics g, string cod, Rectangle area)
    {
        bool ehRio = cod == "RI";
        bool valido = CercadoValidoParaDado(cod);
        bool hover = _cercadoHover == cod && Interativo && valido;
        bool selecionado = CercadoSelecionado == cod;
        bool usarArteReal = _imagemTabuleiro != null;

        var info = ObterInfo(cod);

        // Fundo do cercado
        Color fundo;
        if (ehRio)
            fundo = Color.FromArgb(120, 180, 220);
        else if (info?.Lado == LadoTabuleiro.Floresta)
            fundo = Color.FromArgb(220, 240, 200);
        else
            fundo = Color.FromArgb(245, 220, 160);

        if (hover) fundo = ControlPaint.Light(fundo, 0.3f);

        if (!usarArteReal || hover || selecionado)
        {
            int alpha = usarArteReal ? 70 : 255;
            using var brushFundo = new SolidBrush(Color.FromArgb(alpha, fundo));
            if (usarArteReal && _formasCercados.TryGetValue(cod, out var forma))
                g.FillPath(brushFundo, forma);
            else
                g.FillRoundedRect(brushFundo, area, 8);
        }

        // Borda
        Color corBorda = selecionado ? Color.DarkOrange
                       : (Interativo && valido) ? Color.LimeGreen
                       : (Interativo && !valido) ? Color.LightGray
                       : (info?.Lado == LadoTabuleiro.Floresta ? Color.SaddleBrown : Color.MediumPurple);
        if (ehRio) corBorda = Color.SteelBlue;
        if (selecionado) corBorda = Color.DarkOrange;
        float larguraBorda = selecionado || (Interativo && valido) ? 3f : 2f;
        if (!usarArteReal || hover || selecionado)
        {
            using var pen = new Pen(corBorda, larguraBorda);
            if (usarArteReal && _formasCercados.TryGetValue(cod, out var forma))
                g.DrawPath(pen, forma);
            else
                g.DrawRoundedRect(pen, area, 8);
        }

        // Titulo
        if (!ehRio && !usarArteReal)
        {
            string nome = info?.Nome ?? cod;
            using var fonteTit = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var brushTit = new SolidBrush(Color.FromArgb(60, 30, 10));
            var sz = g.MeasureString(nome, fonteTit);
            g.DrawString(nome, fonteTit, brushTit,
                area.X + (area.Width - sz.Width) / 2, area.Y + 4);

            // Capacidade
            using var fontePeq = new Font("Segoe UI", 7f, FontStyle.Italic);
            using var brushPeq = new SolidBrush(Color.FromArgb(120, 80, 40));
            int qtd = _estado.TryGetValue(cod, out var ds) ? ds.Count : 0;
            string indicadorCap = info?.Capacidade > 1 ? $"{qtd}/{info.Capacidade}" : "";
            if (!string.IsNullOrEmpty(indicadorCap))
            {
                var sz2 = g.MeasureString(indicadorCap, fontePeq);
                g.DrawString(indicadorCap, fontePeq, brushPeq,
                    area.Right - sz2.Width - 6, area.Y + 4);
            }
        }
        else if (!usarArteReal)
        {
            using var fonteTit = new Font("Segoe UI", 11f, FontStyle.Bold | FontStyle.Italic);
            using var brushTit = new SolidBrush(Color.White);
            var sz = g.MeasureString("RIO", fonteTit);
            var st = g.Save();
            g.TranslateTransform(area.X + area.Width / 2, area.Y + 12);
            g.RotateTransform(90);
            g.DrawString("RIO", fonteTit, brushTit, -sz.Width / 2, -sz.Height / 2);
            g.Restore(st);
        }

        // Dinossauros dentro
        DesenharDinossauros(g, cod, area, info);
    }

    // Esta funcao cuida de pegar nome, capacidade e lado de um cercado.
    private CercadoInfo? ObterInfo(string cod)
    {
        var mapa = Cercado.CercadosPorLado(Lado);
        return mapa.TryGetValue(cod, out var i) ? i : null;
    }

    // A funcao serve para desenhar os dinossauros dentro do cercado correto.
    private void DesenharDinossauros(Graphics g, string cod, Rectangle area, CercadoInfo? info)
    {
        if (!_estado.TryGetValue(cod, out var dinos) || dinos.Count == 0) return;

        // Durante animacao, esconde o ULTIMO dino se ele bate com o que esta
        // sendo animado (caso contrario a animacao fica em cima do dino estatico).
        var lista = dinos;
        if (_animCercado == cod && _animProgresso < 1f && dinos.Count > 0
            && dinos[^1] == _animDino)
        {
            lista = dinos.Take(dinos.Count - 1).ToList();
        }

        if (cod == "RI")
        {
            DesenharNoRio(g, lista, area);
            return;
        }

        var tipo = info?.Tipo ?? TipoCercado.Linear;
        switch (tipo)
        {
            case TipoCercado.Unico:    DesenharUnico(g, lista, area); break;
            case TipoCercado.Linear:
            default:                   DesenharLinear(g, lista, area, info?.Capacidade ?? 6); break;
        }
    }

    // Esta funcao desenha dinossauros colocados no rio.
    private static void DesenharNoRio(Graphics g, List<string> dinos, Rectangle area)
    {
        int diametro = Math.Min(area.Width - 6, 22);
        int x = area.X + (area.Width - diametro) / 2;
        int y = area.Y + 40;
        int gap = 3;
        foreach (var d in dinos)
        {
            if (y + diametro > area.Bottom - 4) break;
            DinoRenderer.Desenhar(g, d, new Rectangle(x, y, diametro, diametro));
            y += diametro + gap;
        }
    }

    // Esta funcao cuida de desenhar dinossauros em cercados de varias posicoes.
    private static void DesenharLinear(Graphics g, List<string> dinos, Rectangle area, int capacidade)
    {
        int yBase = area.Y + 28;
        int alturaDisp = area.Height - 36;
        int diam = Math.Min(40, area.Width / Math.Max(capacidade, 3) - 4);
        if (diam < 18) diam = 18;
        int x = area.X + 6;
        int y = yBase + alturaDisp / 2 - diam / 2;
        int gap = 4;

        foreach (var d in dinos)
        {
            if (x + diam > area.Right - 4)
            {
                x = area.X + 6;
                y += diam + gap;
                if (y + diam > area.Bottom - 4) break;
            }
            DinoRenderer.Desenhar(g, d, new Rectangle(x, y, diam, diam));
            x += diam + gap;
        }
    }

    // A funcao serve para desenhar cercados que aceitam apenas um dinossauro.
    private static void DesenharUnico(Graphics g, List<string> dinos, Rectangle area)
    {
        if (dinos.Count == 0) return;
        int margem = 12;
        int diam = Math.Min(area.Width - margem * 2, area.Height - 36 - margem);
        if (diam < 24) diam = 24;
        int x = area.X + (area.Width - diam) / 2;
        int y = area.Y + 28 + ((area.Height - 28) - diam) / 2;
        DinoRenderer.Desenhar(g, dinos[0], new Rectangle(x, y, diam, diam));
    }

    // Esta funcao desenha as caixas explicativas do botao Instrucoes.
    private void DesenharInstrucoes(Graphics g)
    {
        DesenharInstrucao(g, AreaPct(0.02f, 0.02f, 0.36f, 0.13f),
            "Floresta da Igualdade",
            "Apenas dinossauros da mesma especie. Preencha da esquerda para a direita. Pontua 2/4/8/12/18/24.");

        DesenharInstrucao(g, AreaPct(0.03f, 0.33f, 0.34f, 0.13f),
            "Mata Tripla",
            "Pode ter ate 3 dinossauros de qualquer especie. Vale 7 pontos se tiver exatamente 3, senao vale 0.");

        DesenharInstrucao(g, AreaPct(0.03f, 0.66f, 0.36f, 0.14f),
            "Pradaria do Amor",
            "Aceita qualquer especie. Cada par da mesma especie vale 5 pontos. Pode ter mais de um par.");

        DesenharInstrucao(g, AreaPct(0.62f, 0.07f, 0.34f, 0.15f),
            "Rei da Selva",
            "Apenas 1 dinossauro. Vale 7 pontos se voce tiver mais dessa especie que cada oponente; empate nao conta.");

        DesenharInstrucao(g, AreaPct(0.55f, 0.36f, 0.42f, 0.14f),
            "Campina da Diferenca",
            "Apenas especies diferentes. Preencha da esquerda para a direita. Pontua 1/3/6/10/15/21.");

        DesenharInstrucao(g, AreaPct(0.57f, 0.64f, 0.39f, 0.14f),
            "Ilha Solitaria",
            "Apenas 1 dinossauro. Vale 7 pontos se ele for o unico da especie no seu zoologico; senao vale 0.");

        DesenharInstrucao(g, AreaPct(0.38f, 0.78f, 0.26f, 0.13f),
            "Rio",
            "Zona especial. Sempre pode receber dinossauro, independente do dado. Cada dinossauro no rio vale 1 ponto.");

        DesenharInstrucao(g, AreaPct(0.37f, 0.01f, 0.26f, 0.12f),
            "Dado",
            "O dado limita onde voce pode colocar: FL floresta, PR pradaria, AL esquerda, WC direita, VZ vazio, TI sem T-Rex. Quem esta com o dado ignora essa restricao.");
    }

    // Esta funcao cuida de desenhar uma unica caixa de explicacao.
    private static void DesenharInstrucao(Graphics g, Rectangle area, string titulo, string texto)
    {
        using var fundo = new SolidBrush(Color.FromArgb(238, 255, 255, 255));
        using var borda = new Pen(Color.FromArgb(90, 140, 190), 1.4f);
        using var fonteTitulo = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var fonteTexto = new Font("Segoe UI", 8.2f);
        using var brushTexto = new SolidBrush(Color.Black);

        g.FillRoundedRect(fundo, area, 5);
        g.DrawRoundedRect(borda, area, 5);
        var tituloRect = new Rectangle(area.X + 8, area.Y + 6, area.Width - 16, 20);
        var textoRect = new Rectangle(area.X + 8, area.Y + 28, area.Width - 16, area.Height - 34);
        g.DrawString(titulo, fonteTitulo, brushTexto, tituloRect);
        g.DrawString(texto, fonteTexto, brushTexto, textoRect);
    }

    // A funcao serve para desenhar o movimento de queda do dinossauro.
    private void DesenharAnimacao(Graphics g, string codDino, Rectangle areaDest)
    {
        // Calcula posicao final dentro do cercado (mesma logica do desenho linear/piramide simplificada)
        int diam = Math.Min(36, areaDest.Width / 6);
        if (diam < 22) diam = 22;
        int xFim = areaDest.X + areaDest.Width / 2 - diam / 2;
        int yFim = areaDest.Y + areaDest.Height / 2 - diam / 2;
        int yIni = -diam;

        // Easing (ease-out cubic)
        float t = _animProgresso;
        float e = 1f - (float)Math.Pow(1 - t, 3);
        int y = (int)(yIni + (yFim - yIni) * e);
        // Pequeno bounce no final
        if (t > 0.85f)
        {
            float b = (t - 0.85f) / 0.15f;
            int amp = (int)(8 * Math.Sin(b * Math.PI));
            y -= amp;
        }
        DinoRenderer.Desenhar(g, codDino, new Rectangle(xFim, y, diam, diam), destaque: true);
    }

    /// <summary>
    /// Validacao visual local. A validacao real A da DLL.
    /// </summary>
    private bool CercadoValidoParaDado(string cod)
    {
        if (cod == "RI") return true;
        if (IgnoraDado) return true;
        if (string.IsNullOrEmpty(FaceDadoAtual)) return true;

        var info = ObterInfo(cod);
        if (info == null) return true;

        switch (FaceDadoAtual)
        {
            case "FL": return info.Lado == LadoTabuleiro.Floresta;
            case "PR": return info.Lado == LadoTabuleiro.Pradaria;
            case "AL": return info.Lateral == LateralTabuleiro.Alimentacao;
            case "WC": return info.Lateral == LateralTabuleiro.Banheiros;
            case "VZ":
                return !_estado.TryGetValue(cod, out var ds) || ds.Count == 0;
            case "TI":
                return !_estado.TryGetValue(cod, out var ds2) || !ds2.Contains("Ti");
            default: return true;
        }
    }

    // Esta funcao faz mudar o cursor e destacar o cercado embaixo do mouse.
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!Interativo) return;
        string? hoverNovo = CercadoEm(e.Location);
        if (hoverNovo != _cercadoHover)
        {
            _cercadoHover = hoverNovo;
            Cursor = (hoverNovo != null && CercadoValidoParaDado(hoverNovo)) ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    // Esta funcao cuida de limpar o destaque quando o mouse sai do tabuleiro.
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _cercadoHover = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    // A funcao serve para transformar o clique no tabuleiro em uma selecao de cercado.
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!Interativo) return;
        var cercado = CercadoEm(e.Location);
        if (cercado != null && CercadoValidoParaDado(cercado))
            CercadoClicado?.Invoke(this, cercado);
    }

    // Esta funcao faz aceitar o arrastar de dinossauro para o tabuleiro.
    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        base.OnDragEnter(drgevent);
        drgevent.Effect = Interativo && drgevent.Data?.GetDataPresent(DataFormats.Text) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    // Esta funcao cuida de validar o cercado enquanto o dinossauro esta sendo arrastado.
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        var ponto = PointToClient(new Point(drgevent.X, drgevent.Y));
        var cercado = CercadoEm(ponto);
        bool valido = cercado != null && CercadoValidoParaDado(cercado);
        drgevent.Effect = Interativo && valido ? DragDropEffects.Copy : DragDropEffects.None;

        if (_cercadoHover != cercado)
        {
            _cercadoHover = cercado;
            Invalidate();
        }
    }

    // A funcao serve para soltar o dinossauro no cercado escolhido.
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (!Interativo) return;

        var ponto = PointToClient(new Point(drgevent.X, drgevent.Y));
        var cercado = CercadoEm(ponto);
        if (cercado != null && CercadoValidoParaDado(cercado))
            CercadoClicado?.Invoke(this, cercado);
    }

    // Esta funcao faz descobrir qual cercado existe em uma coordenada da tela.
    private string? CercadoEm(Point ponto)
    {
        foreach (var kv in _formasCercados.Where(kv => kv.Key != "RI"))
            if (kv.Value.IsVisible(ponto))
                return kv.Key;

        if (_formasCercados.TryGetValue("RI", out var rio) && rio.IsVisible(ponto))
            return "RI";

        foreach (var kv in _areasCercados)
            if (kv.Value.Contains(ponto))
                return kv.Key;
        return null;
    }
}

internal static class GraphicsExtensions
{
    // Esta funcao cuida de preencher retangulos com cantos arredondados.
    public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle r, int raio)
    {
        using var path = MontarRoundedPath(r, raio);
        g.FillPath(brush, path);
    }

    // A funcao serve para desenhar bordas arredondadas.
    public static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle r, int raio)
    {
        using var path = MontarRoundedPath(r, raio);
        g.DrawPath(pen, path);
    }

    // Esta funcao faz montar o caminho grafico de um retangulo arredondado.
    private static GraphicsPath MontarRoundedPath(Rectangle r, int raio)
    {
        var path = new GraphicsPath();
        int d = raio * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}


