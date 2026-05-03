using System.Drawing.Drawing2D;
using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;

namespace DraftosaurusClient.Controls;

/// <summary>
/// Controle que desenha o tabuleiro do jogador. Suporta lados Verão e Inverno.
///
/// Verão (3 linhas x 2 colunas + Rio vertical):
///   FI  | RS         FB  | VG       (inverno)
///   MT  | CD         PE  | PI
///   PA  | IS         PD  | QU
///
/// Eventos:
///   - CercadoClicado: dispara quando o usuário clica em um cercado.
///
/// Animações:
///   - AnimarColocacao(cercado, dino): faz uma "queda" do dino até o cercado.
/// </summary>
public class TabuleiroControl : Control
{
    private readonly Dictionary<string, Rectangle> _areasCercados = new();
    private readonly Dictionary<string, GraphicsPath> _formasCercados = new();
    private readonly Image? _imagemTabuleiro;
    private Dictionary<string, List<string>> _estado = new();
    private Rectangle _areaTabuleiro = Rectangle.Empty;
    private string? _cercadoHover;

    /// <summary>Lado atual do tabuleiro (Verão ou Inverno).</summary>
    public LadoMapa Lado { get; set; } = LadoMapa.Verao;

    public bool Interativo { get; set; } = false;
    public string? FaceDadoAtual { get; set; }
    public bool IgnoraDado { get; set; } = false;
    public string? CercadoSelecionado { get; set; }

    public event EventHandler<string>? CercadoClicado;

    // Animação
    private System.Windows.Forms.Timer? _animTimer;
    private string? _animCercado;
    private string? _animDino;
    private float _animProgresso; // 0..1

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

    private static Image? CarregarImagemTabuleiro()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "assets", "tabuleiro-verao.jpg");
        if (!File.Exists(caminho)) return null;

        using var fs = new FileStream(caminho, FileMode.Open, FileAccess.Read);
        using var img = Image.FromStream(fs);
        return new Bitmap(img);
    }

    public void AtualizarEstado(Dictionary<string, List<string>> estado)
    {
        var novo = estado ?? new();
        if (EstadosIguais(_estado, novo)) return;
        _estado = novo;
        Invalidate();
    }

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
    /// Anima a colocação de um dinossauro em um cercado. O dino "cai"
    /// do topo da tela até a posição final no cercado.
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

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        CalcularAreas();
        Invalidate();
    }

    private void CalcularAreas()
    {
        foreach (var forma in _formasCercados.Values)
            forma.Dispose();

        _areasCercados.Clear();
        _formasCercados.Clear();
        _areaTabuleiro = CalcularRetanguloTabuleiro();

        if (_imagemTabuleiro != null && Lado == LadoMapa.Verao)
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

        if (Lado == LadoMapa.Verao)
        {
            // Coluna esquerda: FI, MT, PA
            _areasCercados["FI"] = new Rectangle(xEsq, yLinha1, larguraColuna, alturaCercado);
            _areasCercados["MT"] = new Rectangle(xEsq, yLinha2, larguraColuna, alturaCercado);
            _areasCercados["PA"] = new Rectangle(xEsq, yLinha3, larguraColuna, alturaCercado);
            // Coluna direita: RS, CD, IS
            _areasCercados["RS"] = new Rectangle(xDir, yLinha1, larguraColuna, alturaCercado);
            _areasCercados["CD"] = new Rectangle(xDir, yLinha2, larguraColuna, alturaCercado);
            _areasCercados["IS"] = new Rectangle(xDir, yLinha3, larguraColuna, alturaCercado);
        }
        else
        {
            // Inverno — layout análogo
            // Coluna esquerda: FB, PE, PD
            _areasCercados["FB"] = new Rectangle(xEsq, yLinha1, larguraColuna, alturaCercado);
            _areasCercados["PE"] = new Rectangle(xEsq, yLinha2, larguraColuna, alturaCercado);
            _areasCercados["PD"] = new Rectangle(xEsq, yLinha3, larguraColuna, alturaCercado);
            // Coluna direita: VG, PI, QU
            _areasCercados["VG"] = new Rectangle(xDir, yLinha1, larguraColuna, alturaCercado);
            _areasCercados["PI"] = new Rectangle(xDir, yLinha2, larguraColuna, alturaCercado);
            _areasCercados["QU"] = new Rectangle(xDir, yLinha3, larguraColuna, alturaCercado);
        }

        // Rio vertical (presente nos dois lados)
        _areasCercados["RI"] = new Rectangle(xRio, yLinha1, larguraRio, alturaCercado * 3 + margem * 2);
    }

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

    private void DefinirArea(string codigo, Rectangle area, GraphicsPath? forma = null)
    {
        _areasCercados[codigo] = area;
        _formasCercados[codigo] = forma ?? CriarFormaRetangular(area);
    }

    private static GraphicsPath CriarFormaRetangular(Rectangle area)
    {
        var path = new GraphicsPath();
        path.AddRectangle(area);
        return path;
    }

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

    private Point Pct(float x, float y) => new(
        _areaTabuleiro.Left + (int)(_areaTabuleiro.Width * x),
        _areaTabuleiro.Top + (int)(_areaTabuleiro.Height * y));

    private Rectangle AreaPct(float x, float y, float w, float h)
    {
        return new Rectangle(
            _areaTabuleiro.Left + (int)(_areaTabuleiro.Width * x),
            _areaTabuleiro.Top + (int)(_areaTabuleiro.Height * y),
            Math.Max(1, (int)(_areaTabuleiro.Width * w)),
            Math.Max(1, (int)(_areaTabuleiro.Height * h)));
    }

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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_areasCercados.Count == 0) CalcularAreas();

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var fora = new SolidBrush(Color.FromArgb(232, 226, 214)))
            g.FillRectangle(fora, ClientRectangle);

        bool usaImagem = _imagemTabuleiro != null && Lado == LadoMapa.Verao;

        if (usaImagem)
            g.DrawImage(_imagemTabuleiro!, _areaTabuleiro);

        // Fundo do tabuleiro
        Color fundoCor = Lado == LadoMapa.Verao
            ? Color.FromArgb(180, 220, 180)
            : Color.FromArgb(195, 215, 230);
        if (!usaImagem)
        {
            using var fundo = new SolidBrush(fundoCor);
            g.FillRoundedRect(fundo, _areaTabuleiro, 10);
        }

        if (!usaImagem)
            DesenharOrientacao(g);

        // Cercados — desenha o Rio primeiro (atrás)
        if (_areasCercados.TryGetValue("RI", out var areaRio))
            DesenharCercado(g, "RI", areaRio);

        foreach (var kv in _areasCercados)
        {
            if (kv.Key == "RI") continue;
            DesenharCercado(g, kv.Key, kv.Value);
        }

        // Animação de colocação por cima de tudo
        if (_animCercado != null && _animDino != null && _areasCercados.TryGetValue(_animCercado, out var aDest))
        {
            DesenharAnimacao(g, _animDino, aDest);
        }
    }

    private void DesenharOrientacao(Graphics g)
    {
        using var fontePeq = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var brushTxt = new SolidBrush(Color.FromArgb(80, 50, 20));
        g.DrawString("◀ ALIMENTAÇÃO", fontePeq, brushTxt, 4, 1);
        var medida = g.MeasureString("BANHEIROS ▶", fontePeq);
        g.DrawString("BANHEIROS ▶", fontePeq, brushTxt, ClientSize.Width - medida.Width - 4, 1);

        // Etiqueta do lado
        string etiqueta = Lado == LadoMapa.Verao ? "LADO VERÃO" : "LADO INVERNO";
        using var fonteLado = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var sz = g.MeasureString(etiqueta, fonteLado);
        g.DrawString(etiqueta, fonteLado, brushTxt, (ClientSize.Width - sz.Width) / 2, 1);
    }

    private void DesenharCercado(Graphics g, string cod, Rectangle area)
    {
        bool ehRio = cod == "RI";
        bool valido = CercadoValidoParaDado(cod);
        bool hover = _cercadoHover == cod && Interativo && valido;
        bool selecionado = CercadoSelecionado == cod;
        bool usarArteReal = _imagemTabuleiro != null && Lado == LadoMapa.Verao;

        var info = ObterInfo(cod);

        // Fundo do cercado
        Color fundo;
        if (ehRio)
            fundo = Color.FromArgb(120, 180, 220);
        else if (info?.Lado == LadoTabuleiro.Floresta)
            fundo = Lado == LadoMapa.Verao
                ? Color.FromArgb(220, 240, 200)
                : Color.FromArgb(220, 230, 240);
        else
            fundo = Lado == LadoMapa.Verao
                ? Color.FromArgb(245, 220, 160)
                : Color.FromArgb(225, 220, 200);

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

        // Título
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

    private CercadoInfo? ObterInfo(string cod)
    {
        var mapa = Cercado.CercadosPorLado(Lado);
        return mapa.TryGetValue(cod, out var i) ? i : null;
    }

    private void DesenharDinossauros(Graphics g, string cod, Rectangle area, CercadoInfo? info)
    {
        if (!_estado.TryGetValue(cod, out var dinos) || dinos.Count == 0) return;

        // Durante animação, esconde o ÚLTIMO dino se ele bate com o que está
        // sendo animado (caso contrário a animação fica em cima do dino estático).
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
            case TipoCercado.Piramide: DesenharPiramide(g, lista, area); break;
            case TipoCercado.Unico:    DesenharUnico(g, lista, area); break;
            case TipoCercado.Alternada:
            case TipoCercado.Linear:
            default:                   DesenharLinear(g, lista, area, info?.Capacidade ?? 6); break;
        }
    }

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

    /// <summary>
    /// Pirâmide: 3 dinos na base, 2 no meio, 1 no topo.
    /// Cuidado especial: dinos da mesma espécie não podem ser adjacentes
    /// horizontal/verticalmente (regra do manual). Mostramos a estrutura
    /// real mesmo que a DLL aceite invalidamente.
    /// </summary>
    private static void DesenharPiramide(Graphics g, List<string> dinos, Rectangle area)
    {
        int yBase = area.Y + 30;
        int alturaDisp = area.Height - 38;
        int diam = Math.Min(28, alturaDisp / 3 - 2);
        int gap = 3;

        // 3 níveis: base (3), meio (2), topo (1)
        int xCentro = area.X + area.Width / 2;
        int yLinha1 = yBase + alturaDisp - diam;                    // base
        int yLinha2 = yLinha1 - diam - gap;                         // meio
        int yLinha3 = yLinha2 - diam - gap;                         // topo

        // Posicões em ordem: 0,1,2 (base), 3,4 (meio), 5 (topo)
        var posicoes = new (int x, int y)[]
        {
            (xCentro - diam - gap, yLinha1),
            (xCentro,              yLinha1),
            (xCentro + diam + gap, yLinha1),
            (xCentro - diam / 2 - gap / 2, yLinha2),
            (xCentro + diam / 2 + gap / 2, yLinha2),
            (xCentro,              yLinha3)
        };

        // Desenha slots vazios sempre
        using (var penSlot = new Pen(Color.FromArgb(80, 80, 80, 80), 1f) { DashStyle = DashStyle.Dot })
        {
            foreach (var (px, py) in posicoes)
                g.DrawEllipse(penSlot, px - diam / 2, py, diam, diam);
        }

        for (int i = 0; i < dinos.Count && i < posicoes.Length; i++)
        {
            var (px, py) = posicoes[i];
            DinoRenderer.Desenhar(g, dinos[i], new Rectangle(px - diam / 2, py, diam, diam));
        }
    }

    private void DesenharAnimacao(Graphics g, string codDino, Rectangle areaDest)
    {
        // Calcula posição final dentro do cercado (mesma lógica do desenho linear/piramide simplificada)
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
    /// Validação visual local. A validação real é da DLL.
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

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _cercadoHover = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!Interativo) return;
        var cercado = CercadoEm(e.Location);
        if (cercado != null && CercadoValidoParaDado(cercado))
            CercadoClicado?.Invoke(this, cercado);
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        base.OnDragEnter(drgevent);
        drgevent.Effect = Interativo && drgevent.Data?.GetDataPresent(DataFormats.Text) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

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

    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (!Interativo) return;

        var ponto = PointToClient(new Point(drgevent.X, drgevent.Y));
        var cercado = CercadoEm(ponto);
        if (cercado != null && CercadoValidoParaDado(cercado))
            CercadoClicado?.Invoke(this, cercado);
    }

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
    public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle r, int raio)
    {
        using var path = MontarRoundedPath(r, raio);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle r, int raio)
    {
        using var path = MontarRoundedPath(r, raio);
        g.DrawPath(pen, path);
    }

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
