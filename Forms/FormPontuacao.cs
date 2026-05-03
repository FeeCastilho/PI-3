using DraftosaurusClient.Models;
using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

/// <summary>
/// Tela mostrada ao final da partida com a pontuação detalhada de
/// cada jogador (consome ListarPontuacao da DLL).
/// </summary>
public class FormPontuacao : Form
{
    private readonly DraftService _svc;
    private readonly int _idPartida;
    private readonly List<Jogador> _jogadores;

    public FormPontuacao(DraftService svc, int idPartida, List<Jogador> jogadores)
    {
        _svc = svc;
        _idPartida = idPartida;
        _jogadores = jogadores.OrderByDescending(j => j.Pontuacao).ToList();

        Text = "🏆 Pontuação Final — Partida " + idPartida;
        Width = 760;
        Height = 580;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 240, 230);
        Font = new Font("Segoe UI", 9.5f);

        var titulo = new Label
        {
            Text = "🏆 Resultado Final",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 60, 20),
            Location = new Point(20, 14),
            AutoSize = true
        };
        Controls.Add(titulo);

        // Ranking compacto no topo
        var pnlRanking = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(700, 60),
            BackColor = Color.FromArgb(255, 248, 220)
        };
        Controls.Add(pnlRanking);

        int xR = 10;
        for (int i = 0; i < _jogadores.Count; i++)
        {
            var j = _jogadores[i];
            string medalha = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"{i + 1}º" };
            var lbl = new Label
            {
                Text = $"{medalha} {j.Nome}\n{j.Pontuacao} pts",
                Location = new Point(xR, 8),
                Size = new Size(160, 44),
                Font = new Font("Segoe UI", 10f, i == 0 ? FontStyle.Bold : FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = i == 0 ? Color.LightYellow : Color.White
            };
            pnlRanking.Controls.Add(lbl);
            xR += 168;
        }

        // Tabs por jogador, com detalhamento
        var tabs = new TabControl
        {
            Location = new Point(20, 130),
            Size = new Size(700, 380)
        };
        Controls.Add(tabs);

        foreach (var j in _jogadores)
        {
            var page = new TabPage($"{j.Nome} ({j.Pontuacao})");
            var detalhes = _svc.ListarPontuacao(j.Id);

            var grid = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            grid.Columns.Add("Etapa do cálculo", 500);
            grid.Columns.Add("Pontos", 100);
            grid.Columns.Add("Acumulado", 90);

            int acum = 0;
            foreach (var (desc, pts) in detalhes)
            {
                acum = pts;  // a DLL pode retornar acumulado direto; trataremos as duas formas
                var item = new ListViewItem(desc);
                item.SubItems.Add(pts.ToString());
                item.SubItems.Add(acum.ToString());
                grid.Items.Add(item);
            }

            if (detalhes.Count == 0)
            {
                var item = new ListViewItem("(detalhamento não disponível)");
                item.SubItems.Add("-");
                item.SubItems.Add(j.Pontuacao.ToString());
                grid.Items.Add(item);
            }

            // Total
            var total = new ListViewItem("TOTAL")
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.LightYellow
            };
            total.SubItems.Add("");
            total.SubItems.Add(j.Pontuacao.ToString());
            grid.Items.Add(total);

            page.Controls.Add(grid);
            tabs.TabPages.Add(page);
        }

        var btnFechar = new Button
        {
            Text = "Fechar",
            Location = new Point(620, 520),
            Size = new Size(100, 32),
            DialogResult = DialogResult.OK
        };
        Controls.Add(btnFechar);
        AcceptButton = btnFechar;
    }
}
