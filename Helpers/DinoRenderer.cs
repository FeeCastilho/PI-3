using System.Drawing.Drawing2D;
using DraftosaurusClient.Models;

namespace DraftosaurusClient.Helpers;

/// <summary>
/// Renderiza dinossauros como silhuetas vetoriais distintas por espécie.
///
/// Cada espécie tem uma silhueta característica:
///   Br (Braquiossauro)   — pescoço longo, corpo redondo
///   Ep (Espinossauro)    — vela nas costas, focinho longo
///   Et (Estegossauro)    — placas dorsais
///   Pa (Parasaurolófo)   — crista para trás
///   Ti (Tiranossauro)    — bípede grande, braços curtos
///   Tr (Tricerátops)     — chifres + babado craniano
///
/// Se houver um PNG em Resources/dinos/{Codigo}.png ao lado do .exe,
/// ele é usado em vez da silhueta. Cache em memória.
/// </summary>
public static class DinoRenderer
{
    private static readonly Dictionary<string, Image?> _pngCache = new();
    private static bool _pngTentativaFeita;

    /// <summary>Desenha um dinossauro dentro de um retângulo.</summary>
    public static void Desenhar(Graphics g, string codigo, Rectangle area, bool destaque = false)
    {
        // 1. Tenta PNG customizado
        var png = TentarCarregarPng(codigo);
        if (png != null)
        {
            DesenharPng(g, png, area, destaque);
            return;
        }

        // 2. Fallback — silhueta vetorial
        DesenharSilhueta(g, codigo, area, destaque);
    }

    private static void DesenharPng(Graphics g, Image png, Rectangle area, bool destaque)
    {
        if (destaque)
        {
            using var glow = new SolidBrush(Color.FromArgb(120, 255, 255, 80));
            g.FillEllipse(glow, area.X - 3, area.Y - 3, area.Width + 6, area.Height + 6);
        }
        g.DrawImage(png, area);
    }

    private static Image? TentarCarregarPng(string codigo)
    {
        if (_pngCache.TryGetValue(codigo, out var cache)) return cache;

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, "Resources", "dinos", $"{codigo}.png");
            if (File.Exists(path))
            {
                var img = Image.FromFile(path);
                _pngCache[codigo] = img;
                return img;
            }
        }
        catch { }

        _pngCache[codigo] = null;
        return null;
    }

    // ============================================================
    // SILHUETAS VETORIAIS
    // ============================================================

    private static void DesenharSilhueta(Graphics g, string codigo, Rectangle area, bool destaque)
    {
        var saved = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Glow se destacado
        if (destaque)
        {
            using var glow = new SolidBrush(Color.FromArgb(140, 255, 240, 100));
            g.FillEllipse(glow, area.X - 4, area.Y - 4, area.Width + 8, area.Height + 8);
        }

        // Fundo circular pastel (ajuda a destacar a silhueta)
        Color cor = Dinossauro.CorPorCodigo(codigo);
        Color fundoClaro = Color.FromArgb(255,
            Math.Min(255, cor.R + 60),
            Math.Min(255, cor.G + 60),
            Math.Min(255, cor.B + 60));
        using (var bg = new SolidBrush(fundoClaro))
            g.FillEllipse(bg, area);
        using (var bordap = new Pen(Color.FromArgb(80, 0, 0, 0), 1.2f))
            g.DrawEllipse(bordap, area);

        // Silhueta central
        using var brush = new SolidBrush(Color.FromArgb(230, cor));
        using var pen = new Pen(Color.FromArgb(200, 30, 20, 10), 1.2f);

        Action<Graphics, Rectangle, Brush, Pen> desenhar = codigo switch
        {
            "Br" => DesenharBraquiossauro,
            "Ep" => DesenharEspinossauro,
            "Et" => DesenharEstegossauro,
            "Pa" => DesenharParasaurolofo,
            "Ti" => DesenharTiranossauro,
            "Tr" => DesenharTriceratops,
            _    => DesenharGenerico
        };
        desenhar(g, area, brush, pen);

        g.Restore(saved);
    }

    // -- Cada silhueta é desenhada relativa ao retângulo, normalizada 0..1 --

    private static void DesenharBraquiossauro(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Pescoço longo + corpo redondo + 4 patas
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.65, 0.10), (0.78, 0.18), (0.82, 0.32), (0.78, 0.42),
            (0.70, 0.50), (0.85, 0.55), (0.90, 0.68), (0.85, 0.78),
            (0.70, 0.80), (0.55, 0.78), (0.40, 0.80), (0.22, 0.80),
            (0.12, 0.70), (0.18, 0.55), (0.30, 0.50), (0.50, 0.48),
            (0.60, 0.40), (0.62, 0.25), (0.60, 0.15));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        // Patas (retângulos)
        DesenharPatas(g, r, b, p, new[] { 0.25, 0.40, 0.65, 0.80 }, 0.78, 0.12, 0.10);
        // Olho
        DesenharOlho(g, r, 0.74, 0.16);
    }

    private static void DesenharEspinossauro(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Corpo bípede com vela alta nas costas
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.20, 0.55), (0.18, 0.45),
            (0.30, 0.42), (0.40, 0.30), (0.50, 0.20), (0.60, 0.30), (0.70, 0.42),
            (0.78, 0.40), (0.85, 0.45),
            (0.92, 0.55), (0.85, 0.58),
            (0.78, 0.62), (0.70, 0.78), (0.62, 0.78), (0.62, 0.66),
            (0.50, 0.66), (0.45, 0.78), (0.38, 0.78), (0.40, 0.62),
            (0.30, 0.60), (0.22, 0.58));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        DesenharOlho(g, r, 0.86, 0.50);
    }

    private static void DesenharEstegossauro(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Corpo baixinho com placas no dorso
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.15, 0.65), (0.20, 0.55), (0.30, 0.55), (0.40, 0.50),
            (0.55, 0.50), (0.70, 0.55), (0.78, 0.55), (0.85, 0.62),
            (0.88, 0.70), (0.82, 0.78), (0.72, 0.78), (0.72, 0.70),
            (0.30, 0.70), (0.30, 0.78), (0.20, 0.78), (0.14, 0.72));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        // Placas triangulares no dorso
        var placas = new GraphicsPath();
        var pl1 = Norm(r, (0.32, 0.50), (0.40, 0.32), (0.46, 0.50));
        var pl2 = Norm(r, (0.46, 0.50), (0.55, 0.30), (0.62, 0.50));
        var pl3 = Norm(r, (0.62, 0.50), (0.70, 0.34), (0.76, 0.52));
        placas.AddPolygon(pl1);
        placas.StartFigure();
        placas.AddPolygon(pl2);
        placas.StartFigure();
        placas.AddPolygon(pl3);
        g.FillPath(b, placas);
        g.DrawPath(p, placas);
        DesenharOlho(g, r, 0.20, 0.62);
    }

    private static void DesenharParasaurolofo(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Bípede com crista para trás
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.22, 0.55), (0.18, 0.42),
            (0.30, 0.40), (0.45, 0.30),
            (0.30, 0.18), (0.20, 0.20),  // crista para trás
            (0.30, 0.30), (0.50, 0.28),
            (0.65, 0.30), (0.78, 0.42), (0.85, 0.50),
            (0.92, 0.60),
            (0.78, 0.65),
            (0.72, 0.78), (0.62, 0.78), (0.62, 0.66),
            (0.46, 0.66), (0.42, 0.78), (0.34, 0.78), (0.36, 0.62),
            (0.28, 0.60), (0.22, 0.58));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        DesenharOlho(g, r, 0.86, 0.55);
    }

    private static void DesenharTiranossauro(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // T-Rex: cabeça grande, braços curtos, pernas grossas, cauda
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.12, 0.55),
            (0.20, 0.50),
            (0.32, 0.45),
            (0.42, 0.30), (0.55, 0.25), (0.70, 0.30), (0.82, 0.38),  // cabeça
            (0.94, 0.42),
            (0.92, 0.50), (0.78, 0.50),
            (0.70, 0.55),
            (0.62, 0.62),
            (0.66, 0.70), (0.70, 0.80), (0.62, 0.80), (0.56, 0.66),
            (0.46, 0.66),
            (0.42, 0.80), (0.34, 0.80), (0.36, 0.62),
            (0.28, 0.60),
            (0.22, 0.58));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        // Bracinhos pequenos
        var bracos = Norm(r, (0.50, 0.45), (0.55, 0.50), (0.52, 0.55), (0.48, 0.50));
        g.FillPolygon(b, bracos);
        g.DrawPolygon(p, bracos);
        // Boca aberta — uma fenda
        var boca = Norm(r, (0.78, 0.42), (0.92, 0.44), (0.88, 0.48), (0.78, 0.46));
        using (var bocaB = new SolidBrush(Color.FromArgb(180, 60, 0, 0)))
            g.FillPolygon(bocaB, boca);
        DesenharOlho(g, r, 0.80, 0.36);
    }

    private static void DesenharTriceratops(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Quadrúpede com babado e 3 chifres
        var path = new GraphicsPath();
        var pts = Norm(r,
            (0.15, 0.60), (0.20, 0.55), (0.30, 0.55),
            // babado craniano (para cima)
            (0.32, 0.40), (0.42, 0.28), (0.55, 0.22), (0.68, 0.25), (0.74, 0.40),
            // Chifre maior — frente
            (0.85, 0.45), (0.90, 0.40), (0.88, 0.50),
            (0.82, 0.55),
            // Chifres pequenos sobre os olhos
            (0.62, 0.45), (0.60, 0.30), (0.58, 0.45),
            (0.50, 0.45), (0.48, 0.30), (0.46, 0.45),
            (0.42, 0.50),
            // Corpo de volta
            (0.78, 0.62), (0.86, 0.68), (0.82, 0.78), (0.72, 0.78),
            (0.68, 0.70), (0.30, 0.70), (0.28, 0.78), (0.20, 0.78),
            (0.14, 0.70));
        path.AddPolygon(pts);
        g.FillPath(b, path);
        g.DrawPath(p, path);
        DesenharOlho(g, r, 0.66, 0.42);
    }

    private static void DesenharGenerico(Graphics g, Rectangle r, Brush b, Pen p)
    {
        // Bolinha simples como fallback
        g.FillEllipse(b, Inset(r, 0.20));
        g.DrawEllipse(p, Inset(r, 0.20));
    }

    // ============================================================
    // HELPERS GEOMÉTRICOS
    // ============================================================

    private static PointF[] Norm(Rectangle r, params (double x, double y)[] pts)
    {
        var arr = new PointF[pts.Length];
        for (int i = 0; i < pts.Length; i++)
            arr[i] = new PointF(
                r.X + (float)(pts[i].x * r.Width),
                r.Y + (float)(pts[i].y * r.Height));
        return arr;
    }

    private static Rectangle Inset(Rectangle r, double frac)
    {
        int dx = (int)(r.Width * frac / 2);
        int dy = (int)(r.Height * frac / 2);
        return new Rectangle(r.X + dx, r.Y + dy, r.Width - 2 * dx, r.Height - 2 * dy);
    }

    private static void DesenharPatas(Graphics g, Rectangle r, Brush b, Pen p,
                                      double[] xs, double yTopo, double larg, double alt)
    {
        foreach (var x in xs)
        {
            var pata = new RectangleF(
                r.X + (float)((x - larg / 2) * r.Width),
                r.Y + (float)(yTopo * r.Height),
                (float)(larg * r.Width),
                (float)(alt * r.Height));
            g.FillRectangle(b, pata);
            g.DrawRectangle(p, pata.X, pata.Y, pata.Width, pata.Height);
        }
    }

    private static void DesenharOlho(Graphics g, Rectangle r, double x, double y)
    {
        float d = Math.Max(2f, r.Width * 0.06f);
        var olho = new RectangleF(
            r.X + (float)(x * r.Width) - d / 2,
            r.Y + (float)(y * r.Height) - d / 2,
            d, d);
        g.FillEllipse(Brushes.White, olho);
        var pupila = new RectangleF(olho.X + d * 0.25f, olho.Y + d * 0.25f, d * 0.5f, d * 0.5f);
        g.FillEllipse(Brushes.Black, pupila);
    }
}
