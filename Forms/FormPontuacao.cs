using DraftosaurusClient.Models;
using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

public class FormPontuacao : Form
{
    private readonly DraftService _svc;
    private readonly int _idPartida;
    private readonly List<Jogador> _jogadores;

    // Esta funcao cuida de iniciar 'FormPontuacao' do programa.
    public FormPontuacao(DraftService svc, int idPartida, List<Jogador> jogadores)
    {
        _svc = svc;
        _idPartida = idPartida;
        _jogadores = jogadores.OrderByDescending(j => j.Pontuacao).ToList();

        Text = "Pontuacao Final - Partida " + idPartida;
        Width = 760;
        Height = 580;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 240, 230);
        Font = new Font("Segoe UI", 9.5f);

        var titulo = new Label
        {
            Text = "Resultado Final",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 60, 20),
            Location = new Point(20, 14),
            AutoSize = true
        };
        Controls.Add(titulo);

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
            string posicao = $"{i + 1}o";
            var lbl = new Label
            {
                Text = $"{posicao} {j.Nome}\n{j.Pontuacao} pts",
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

        var tabs = new TabControl
        {
            Location = new Point(20, 130),
            Size = new Size(700, 380)
        };
        Controls.Add(tabs);

        var tabuleiros = CarregarTabuleiros();
        foreach (var j in _jogadores)
        {
            var page = new TabPage($"{j.Nome} ({j.Pontuacao})");
            var detalhes = _svc.ListarPontuacao(j.Id);
            if (detalhes.Count == 0)
                detalhes = CalcularDetalhamentoLocal(j, tabuleiros);

            var grid = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            grid.Columns.Add("Etapa do calculo", 500);
            grid.Columns.Add("Pontos", 100);
            grid.Columns.Add("Acumulado", 90);

            int acumulado = 0;
            foreach (var (desc, pts) in detalhes)
            {
                acumulado += pts;
                var item = new ListViewItem(desc);
                item.SubItems.Add(pts.ToString());
                item.SubItems.Add(acumulado.ToString());
                grid.Items.Add(item);
            }

            if (detalhes.Count == 0)
            {
                var item = new ListViewItem("(detalhamento nao disponivel)");
                item.SubItems.Add("-");
                item.SubItems.Add(j.Pontuacao.ToString());
                grid.Items.Add(item);
            }

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

    // A funcao serve para iniciar 'CarregarTabuleiros' do programa.
    private Dictionary<int, Dictionary<string, List<string>>> CarregarTabuleiros()
    {
        var tabuleiros = new Dictionary<int, Dictionary<string, List<string>>>();
        foreach (var jogador in _jogadores)
        {
            try
            {
                tabuleiros[jogador.Id] = _svc.ExibirTabuleiro(jogador.Id);
            }
            catch
            {
                tabuleiros[jogador.Id] = new Dictionary<string, List<string>>();
            }
        }
        return tabuleiros;
    }

    // Esta funcao executa a etapa 'CalcularDetalhamentoLocal' do programa.
    private List<(string descricao, int pontos)> CalcularDetalhamentoLocal(
        Jogador jogador,
        Dictionary<int, Dictionary<string, List<string>>> tabuleiros)
    {
        var detalhes = new List<(string, int)>();
        if (!tabuleiros.TryGetValue(jogador.Id, out var tabuleiro))
        {
            detalhes.Add(("Detalhamento local indisponivel", jogador.Pontuacao));
            return detalhes;
        }

        int totalLocal = 0;
        Adicionar("Floresta da Igualdade", PontuarFloresta(tabuleiro));
        Adicionar("Mata Tripla", PontuarMataTripla(tabuleiro));
        Adicionar("Pradaria do Amor", PontuarPradariaAmor(tabuleiro));
        Adicionar("Campina da Diferenca", PontuarCampinaDiferenca(tabuleiro));
        Adicionar("Rei da Selva", PontuarReiDaSelva(jogador.Id, tabuleiros));
        Adicionar("Ilha Solitaria", PontuarIlhaSolitaria(tabuleiro));
        Adicionar("Rio", Qtd(tabuleiro, "RI"));
        Adicionar("Bonus T-Rex", PontuarTRex(tabuleiro));

        int ajuste = jogador.Pontuacao - totalLocal;
        if (ajuste != 0)
            detalhes.Add(("Ajuste da pontuacao oficial da DLL", ajuste));

        return detalhes;

        void Adicionar(string nome, int pontos)
        {
            totalLocal += pontos;
            detalhes.Add((nome, pontos));
        }
    }

    // Esta funcao cuida de iniciar 'PontuarFloresta' do programa.
    private static int PontuarFloresta(Dictionary<string, List<string>> tabuleiro)
    {
        var dinos = Dinos(tabuleiro, "FI");
        int[] pontos = { 0, 2, 4, 8, 12, 18, 24 };
        if (dinos.Count == 0) return 0;
        return dinos.Distinct().Count() == 1 ? pontos[Math.Min(dinos.Count, 6)] : 0;
    }

    // A funcao serve para iniciar 'PontuarMataTripla' do programa.
    private static int PontuarMataTripla(Dictionary<string, List<string>> tabuleiro)
    {
        return Qtd(tabuleiro, "MT") == 3 ? 7 : 0;
    }

    // Esta funcao executa a etapa 'PontuarPradariaAmor' do programa.
    private static int PontuarPradariaAmor(Dictionary<string, List<string>> tabuleiro)
    {
        return Dinos(tabuleiro, "PA")
            .GroupBy(d => d)
            .Sum(g => (g.Count() / 2) * 5);
    }

    // Esta funcao cuida de avaliar se uma jogada combina com a Campina da Diferenca.
    private static int PontuarCampinaDiferenca(Dictionary<string, List<string>> tabuleiro)
    {
        var dinos = Dinos(tabuleiro, "CD");
        int[] pontos = { 0, 1, 3, 6, 10, 15, 21 };
        if (dinos.Count != dinos.Distinct().Count()) return 0;
        return pontos[Math.Min(dinos.Count, 6)];
    }

    // A funcao serve para iniciar 'PontuarReiDaSelva' do programa.
    private static int PontuarReiDaSelva(
        int idJogador,
        Dictionary<int, Dictionary<string, List<string>>> tabuleiros)
    {
        if (!tabuleiros.TryGetValue(idJogador, out var meuTabuleiro)) return 0;
        var rei = Dinos(meuTabuleiro, "RS").FirstOrDefault();
        if (string.IsNullOrEmpty(rei)) return 0;

        int meuTotal = TodasEspecies(meuTabuleiro).Count(d => d == rei);
        foreach (var kv in tabuleiros)
        {
            if (kv.Key == idJogador) continue;
            int totalOutro = TodasEspecies(kv.Value).Count(d => d == rei);
            if (meuTotal <= totalOutro) return 0;
        }
        return 7;
    }

    // Esta funcao faz avaliar se uma jogada e boa para a Ilha Solitaria.
    private static int PontuarIlhaSolitaria(Dictionary<string, List<string>> tabuleiro)
    {
        var ilha = Dinos(tabuleiro, "IS").FirstOrDefault();
        if (string.IsNullOrEmpty(ilha)) return 0;
        return TodasEspecies(tabuleiro).Count(d => d == ilha) == 1 ? 7 : 0;
    }

    // Esta funcao cuida de iniciar 'PontuarTRex' do programa.
    private static int PontuarTRex(Dictionary<string, List<string>> tabuleiro)
    {
        string[] cercados = { "FI", "MT", "PA", "RS", "CD", "IS" };
        return cercados.Count(c => Dinos(tabuleiro, c).Contains("Ti"));
    }

    // A funcao serve para iniciar 'Qtd' do programa.
    private static int Qtd(Dictionary<string, List<string>> tabuleiro, string cercado) =>
        Dinos(tabuleiro, cercado).Count;

    // Esta funcao executa a etapa 'Dinos' do programa.
    private static List<string> Dinos(Dictionary<string, List<string>> tabuleiro, string cercado) =>
        tabuleiro.TryGetValue(cercado, out var dinos) ? dinos : new List<string>();

    // Esta funcao cuida de iniciar 'TodasEspecies' do programa.
    private static List<string> TodasEspecies(Dictionary<string, List<string>> tabuleiro) =>
        tabuleiro.Values.SelectMany(x => x).Where(d => d != "XX").ToList();
}

