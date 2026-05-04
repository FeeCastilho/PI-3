using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

public class FormSalaEspera : Form
{
    private readonly DraftService _svc;
    private readonly int _idPartida;
    private readonly int _idJogador;
    private readonly string _senhaJogador;
    private readonly string _nomeJogador;
    private readonly ListBox _lstJogadores;
    private readonly Label _lblInfo;
    private readonly System.Windows.Forms.Timer _timer;

    // A funcao serve para iniciar 'FormSalaEspera' do programa.
    public FormSalaEspera(DraftService svc, int idPartida, int idJogador, string senhaJogador, string nomeJogador)
    {
        _svc = svc;
        _idPartida = idPartida;
        _idJogador = idJogador;
        _senhaJogador = senhaJogador;
        _nomeJogador = nomeJogador;

        Text = $"Sala de espera - Partida {idPartida}";
        Width = 520;
        Height = 460;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 240, 230);
        Font = new Font("Segoe UI", 9.5f);

        _lblInfo = new Label
        {
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f),
            Text = $"Voce entrou como: {nomeJogador} (Id {idJogador})\n" +
                   $"Sua senha de jogador: {senhaJogador}\n" +
                   "Anote a senha; ela e necessaria para jogar."
        };
        Controls.Add(_lblInfo);

        var btnCopiar = new Button
        {
            Text = "Copiar (Id, Senha)",
            Location = new Point(20, 90),
            Size = new Size(170, 28)
        };
        btnCopiar.Click += (_, _) =>
        {
            Clipboard.SetText($"Id: {idJogador}, Senha: {senhaJogador}");
            MessageBox.Show("Copiado!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        Controls.Add(btnCopiar);

        var lblLista = new Label
        {
            Text = "Jogadores na partida:",
            Location = new Point(20, 130),
            AutoSize = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        Controls.Add(lblLista);

        _lstJogadores = new ListBox
        {
            Location = new Point(20, 155),
            Size = new Size(470, 180)
        };
        Controls.Add(_lstJogadores);

        var btnIniciar = new Button
        {
            Text = "Iniciar partida",
            Location = new Point(20, 350),
            Size = new Size(180, 36),
            BackColor = Color.LimeGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        btnIniciar.Click += (_, _) => Iniciar();
        Controls.Add(btnIniciar);

        var btnSair = new Button
        {
            Text = "Sair",
            Location = new Point(410, 350),
            Size = new Size(80, 36),
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnSair);

        var lblObs = new Label
        {
            Text = "Qualquer jogador pode iniciar a partida.\n" +
                   "Depois de iniciar, ninguem mais pode entrar.",
            Location = new Point(210, 355),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8f, FontStyle.Italic)
        };
        Controls.Add(lblObs);

        _timer = new System.Windows.Forms.Timer { Interval = 2500 };
        _timer.Tick += (_, _) => AtualizarSala();
        _timer.Start();

        Load += (_, _) => AtualizarSala();
        FormClosing += (_, _) => _timer.Stop();
    }

    // Esta funcao executa a etapa 'AtualizarSala' do programa.
    private void AtualizarSala()
    {
        try
        {
            var estado = _svc.VerificarPartida(_idPartida);
            if (estado.Status == 'J')
            {
                _timer.Stop();
                BeginInvoke((Action)AbrirJogo);
                return;
            }

            var jogs = _svc.ListarJogadores(_idPartida);
            _lstJogadores.Items.Clear();
            foreach (var j in jogs)
            {
                string marca = j.Id == _idJogador ? "  <- voce" : "";
                _lstJogadores.Items.Add($"#{j.Id} - {j.Nome}{marca}");
            }
        }
        catch
        {
        }
    }

    // Esta funcao cuida de iniciar a partida e descobrir quem ficou com o dado.
    private void Iniciar()
    {
        try
        {
            var (idDado, face) = _svc.Iniciar(_idJogador, _senhaJogador);
            MessageBox.Show($"Partida iniciada!\nJogador #{idDado} comeca com o dado.\nFace: {face}",
                "Iniciado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AbrirJogo();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao iniciar:\n" + ex.Message, "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // A funcao serve para iniciar 'AbrirJogo' do programa.
    private void AbrirJogo()
    {
        _timer.Stop();
        Hide();
        using var jogo = new FormJogo(_svc, _idPartida, _idJogador, _senhaJogador, _nomeJogador);
        jogo.ShowDialog(this);
        DialogResult = DialogResult.OK;
        Close();
    }
}

