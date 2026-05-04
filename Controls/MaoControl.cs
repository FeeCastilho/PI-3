using DraftosaurusClient.Helpers;
using DraftosaurusClient.Models;

namespace DraftosaurusClient.Controls;

/// <summary>
/// Mao do jogador: linha de cards de dinossauros clicaveis com silhueta
/// vetorial e contador de quantidade.
/// </summary>
public class MaoControl : FlowLayoutPanel
{
    private string? _selecionado;
    public event EventHandler<string?>? DinossauroSelecionado;

    public string? CodigoSelecionado
    {
        get => _selecionado;
        private set
        {
            _selecionado = value;
            DinossauroSelecionado?.Invoke(this, value);
        }
    }

    // A funcao serve para iniciar 'MaoControl' do programa.
    public MaoControl()
    {
        AutoScroll = true;
        WrapContents = true;
        FlowDirection = FlowDirection.LeftToRight;
        BackColor = Color.FromArgb(60, 50, 40);
        Padding = new Padding(8);
    }

    private Dictionary<string, int> _maoAtual = new();

    // Esta funcao recarrega os dinossauros disponiveis na mao.
    public void AtualizarMao(Dictionary<string, int> mao)
    {
        // Otimizacao: so rebuilda se a mao realmente mudou
        if (MaosIguais(_maoAtual, mao)) return;
        _maoAtual = new Dictionary<string, int>(mao);

        SuspendLayout();
        foreach (Control c in Controls.OfType<Control>().ToList())
        {
            c.Click -= Item_Click;
            Controls.Remove(c);
            c.Dispose();
        }

        // Um card por especie (nao um por dino) - com badge de quantidade
        foreach (var kv in mao.Where(k => k.Value > 0).OrderBy(k => k.Key))
        {
            var card = new DinoCard(kv.Key, kv.Value)
            {
                Margin = new Padding(4),
                Cursor = Cursors.Hand
            };
            card.Click += Item_Click;
            Controls.Add(card);
        }

        if (_selecionado != null && (!mao.TryGetValue(_selecionado, out var q) || q == 0))
            CodigoSelecionado = null;

        AtualizarSelecaoVisual();
        ResumeLayout();
    }

    // Esta funcao cuida de iniciar 'MaosIguais' do programa.
    private static bool MaosIguais(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
            if (!b.TryGetValue(kv.Key, out var v) || v != kv.Value) return false;
        return true;
    }

    // A funcao serve para iniciar 'Item_Click' do programa.
    private void Item_Click(object? sender, EventArgs e)
    {
        if (sender is not DinoCard card) return;
        CodigoSelecionado = card.Codigo;
        AtualizarSelecaoVisual();
    }

    // Esta funcao executa a etapa 'OnControlAdded' do programa.
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is DinoCard card)
            card.MouseDown += DinoCard_MouseDown;
    }

    // Esta funcao cuida de iniciar 'OnControlRemoved' do programa.
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is DinoCard card)
            card.MouseDown -= DinoCard_MouseDown;
        base.OnControlRemoved(e);
    }

    // A funcao serve para iniciar 'DinoCard_MouseDown' do programa.
    private void DinoCard_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not DinoCard card) return;

        CodigoSelecionado = card.Codigo;
        AtualizarSelecaoVisual();
        card.DoDragDrop(card.Codigo, DragDropEffects.Copy);
    }

    // Esta funcao executa a etapa 'AtualizarSelecaoVisual' do programa.
    private void AtualizarSelecaoVisual()
    {
        foreach (Control c in Controls)
            if (c is DinoCard dc)
                dc.Selecionado = (dc.Codigo == _selecionado);
    }

    // Esta funcao cuida de iniciar 'LimparSelecao' do programa.
    public void LimparSelecao()
    {
        CodigoSelecionado = null;
        AtualizarSelecaoVisual();
    }
}

/// <summary>Card visual de um dinossauro com silhueta + nome + badge de qtd.</summary>
internal class DinoCard : Control
{
    public string Codigo { get; }
    public int Quantidade { get; private set; }

    private bool _selecionado;
    public bool Selecionado
    {
        get => _selecionado;
        set { if (_selecionado != value) { _selecionado = value; Invalidate(); } }
    }

    // A funcao serve para iniciar 'DinoCard' do programa.
    public DinoCard(string codigo, int quantidade)
    {
        Codigo = codigo;
        Quantidade = quantidade;
        Width = 80;
        Height = 100;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
               | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        var tt = new ToolTip();
        tt.SetToolTip(this, $"{Dinossauro.NomePorCodigo(codigo)} (x{quantidade})");
    }

    // Esta funcao desenha o tabuleiro, dinossauros e instrucoes.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Fundo do card
        Color cor = Dinossauro.CorPorCodigo(Codigo);
        Color fundo = _selecionado
            ? Color.FromArgb(255, 240, 220)
            : Color.FromArgb(245, 240, 235);

        var areaCard = new Rectangle(2, 2, Width - 4, Height - 4);
        using (var b = new SolidBrush(fundo))
            g.FillRoundedRect(b, areaCard, 6);

        // Borda
        Color borda = _selecionado ? Color.Gold : cor;
        float esp = _selecionado ? 3f : 2f;
        using (var p = new Pen(borda, esp))
            g.DrawRoundedRect(p, areaCard, 6);

        // Silhueta
        var areaDino = new Rectangle(8, 8, Width - 16, Height - 36);
        DinoRenderer.Desenhar(g, Codigo, areaDino);

        // Nome embaixo
        using var fonte = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var brushNome = new SolidBrush(Color.FromArgb(40, 30, 20));
        string nome = Dinossauro.NomePorCodigo(Codigo);
        if (nome.Length > 12) nome = nome.Substring(0, 11) + "a";
        var sz = g.MeasureString(nome, fonte);
        g.DrawString(nome, fonte, brushNome, (Width - sz.Width) / 2, Height - 22);

        // Badge de quantidade (canto superior direito)
        if (Quantidade > 1)
        {
            var badgeRect = new Rectangle(Width - 26, 4, 22, 20);
            using (var b = new SolidBrush(Color.Crimson))
                g.FillEllipse(b, badgeRect);
            using (var p = new Pen(Color.White, 1.5f))
                g.DrawEllipse(p, badgeRect);
            using var fb = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var brushQ = new SolidBrush(Color.White);
            string txt = Quantidade.ToString();
            var szq = g.MeasureString(txt, fb);
            g.DrawString(txt, fb, brushQ,
                badgeRect.X + (badgeRect.Width - szq.Width) / 2 + 1,
                badgeRect.Y + (badgeRect.Height - szq.Height) / 2);
        }
    }
}

