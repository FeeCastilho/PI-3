using DraftosaurusClient.Models;
using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

/// <summary>
/// Tela inicial: o jogador escolhe entre criar uma nova partida
/// ou entrar em uma já aberta.
/// </summary>
public class FormLobby : Form
{
    private readonly DraftService _svc = new();
    private readonly ListView _lv;
    private readonly TextBox _txtNomeJogador;
    private readonly TextBox _txtSenhaPartida;
    private readonly Button _btnEntrar, _btnCriar, _btnAtualizar;

    public FormLobby()
    {
        Text = "Draftosaurus — Lobby";
        Width = 720;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 240, 230);
        Font = new Font("Segoe UI", 9.5f);

        var lblTitulo = new Label
        {
            Text = "🦕 Draftosaurus",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 60, 20),
            Location = new Point(20, 14),
            AutoSize = true
        };
        Controls.Add(lblTitulo);

        var lblVersao = new Label
        {
            Text = "v" + _svc.Versao,
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.Gray,
            Location = new Point(28, 60),
            AutoSize = true
        };
        Controls.Add(lblVersao);

        // ListView de partidas
        var lblPart = new Label
        {
            Text = "Partidas disponíveis:",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(20, 90),
            AutoSize = true
        };
        Controls.Add(lblPart);

        _lv = new ListView
        {
            Location = new Point(20, 115),
            Size = new Size(680, 240),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        _lv.Columns.Add("Id", 50);
        _lv.Columns.Add("Nome", 200);
        _lv.Columns.Add("Criada em", 160);
        _lv.Columns.Add("Status", 100);
        Controls.Add(_lv);

        _btnAtualizar = new Button
        {
            Text = "🔄 Atualizar lista",
            Location = new Point(20, 365),
            Size = new Size(140, 32)
        };
        _btnAtualizar.Click += (_, _) => CarregarPartidas();
        Controls.Add(_btnAtualizar);

        _btnCriar = new Button
        {
            Text = "➕ Criar nova partida",
            Location = new Point(170, 365),
            Size = new Size(160, 32)
        };
        _btnCriar.Click += (_, _) => AbrirCriarPartida();
        Controls.Add(_btnCriar);

        var btnDiag = new Button
        {
            Text = "🔧 Diagnóstico DLL",
            Location = new Point(340, 365),
            Size = new Size(140, 32),
            ForeColor = Color.Gray
        };
        btnDiag.Click += (_, _) => AbrirDiagnostico();
        Controls.Add(btnDiag);

        // Inputs para entrar
        var lblNome = new Label { Text = "Seu nome:", Location = new Point(20, 415), AutoSize = true };
        Controls.Add(lblNome);
        _txtNomeJogador = new TextBox { Location = new Point(95, 412), Width = 180, MaxLength = 20 };
        Controls.Add(_txtNomeJogador);

        var lblSenha = new Label { Text = "Senha:", Location = new Point(290, 415), AutoSize = true };
        Controls.Add(lblSenha);
        _txtSenhaPartida = new TextBox
        {
            Location = new Point(335, 412),
            Width = 130,
            MaxLength = 10,
            UseSystemPasswordChar = true
        };
        Controls.Add(_txtSenhaPartida);

        _btnEntrar = new Button
        {
            Text = "▶ Entrar na partida selecionada",
            Location = new Point(475, 410),
            Size = new Size(225, 32),
            BackColor = Color.LimeGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _btnEntrar.Click += (_, _) => EntrarPartida();
        Controls.Add(_btnEntrar);

        Load += (_, _) => CarregarPartidas();
    }

    private void CarregarPartidas()
    {
        _lv.Items.Clear();
        try
        {
            // Busca todas no backend e filtra localmente para nao esconder partidas ja criadas.
            var partidas = _svc.ListarPartidas('T')
                .Where(p => p.Status != 'E')
                .OrderByDescending(p => p.DataCriacao)
                .ThenByDescending(p => p.Id)
                .ToList();
            foreach (var p in partidas)
            {
                var item = new ListViewItem(p.Id.ToString());
                item.SubItems.Add(p.Nome);
                item.SubItems.Add(p.DataCriacao == DateTime.MinValue ? "-" : p.DataCriacao.ToString("dd/MM HH:mm"));
                item.SubItems.Add(StatusTexto(p.Status));
                item.Tag = p;
                _lv.Items.Add(item);
            }
            if (partidas.Count == 0)
            {
                var vazio = new ListViewItem("");
                vazio.SubItems.Add("(nenhuma partida aberta — crie uma!)");
                _lv.Items.Add(vazio);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao listar partidas:\n" + ex.Message, "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string StatusTexto(char s) => s switch
    {
        'A' => "Aberta",
        'J' => "Jogando",
        'E' => "Encerrada",
        _ => s.ToString()
    };

    private void AbrirCriarPartida()
    {
        using var dlg = new FormCriarPartida(_svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            CarregarPartidas();
            // Pré-seleciona a partida criada
            foreach (ListViewItem it in _lv.Items)
            {
                if (it.Tag is Partida p && p.Id == dlg.IdCriado)
                {
                    it.Selected = true;
                    it.Focused = true;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Janela de diagnóstico — chama métodos da DLL e mostra o que vem.
    /// Útil para verificar se a integração está funcionando.
    /// </summary>
    private void AbrirDiagnostico()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Versão DLL: {_svc.Versao}");
        sb.AppendLine();

        try
        {
            sb.AppendLine("=== ListarFacesDado() ===");
            var faces = _svc.ListarFacesDado();
            foreach (var f in faces) sb.AppendLine($"  {f.Codigo,-3} {f.Nome,-20} {f.Descricao}");
        }
        catch (Exception ex) { sb.AppendLine("ERRO: " + ex.Message); }

        sb.AppendLine();
        try
        {
            sb.AppendLine("=== ListarCercados() ===");
            var cercs = _svc.ListarCercados();
            foreach (var c in cercs) sb.AppendLine($"  {c.Codigo,-3} {c.Nome,-25} {c.Descricao}");
        }
        catch (Exception ex) { sb.AppendLine("ERRO: " + ex.Message); }

        sb.AppendLine();
        try
        {
            sb.AppendLine("=== ListarDinossauros() ===");
            var dinos = _svc.ListarDinossauros();
            foreach (var d in dinos) sb.AppendLine($"  {d.Codigo,-3} {d.Nome,-15} cor:{d.Cor.Name}");
        }
        catch (Exception ex) { sb.AppendLine("ERRO: " + ex.Message); }

        sb.AppendLine();
        try
        {
            sb.AppendLine("=== ListarPartidas('T') ===");
            var parts = _svc.ListarPartidas('T');
            sb.AppendLine($"  {parts.Count} partida(s) encontrada(s)");
            foreach (var p in parts.Take(5))
                sb.AppendLine($"  Id={p.Id,-4} Status={p.Status}  {p.Nome}");
        }
        catch (Exception ex) { sb.AppendLine("ERRO: " + ex.Message); }

        var dlg = new Form
        {
            Text = "Diagnóstico DLL",
            Width = 700,
            Height = 500,
            StartPosition = FormStartPosition.CenterParent
        };
        var txt = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Text = sb.ToString()
        };
        dlg.Controls.Add(txt);
        dlg.ShowDialog(this);
    }

    private void EntrarPartida()
    {
        if (_lv.SelectedItems.Count == 0 || _lv.SelectedItems[0].Tag is not Partida partida)
        {
            MessageBox.Show("Selecione uma partida na lista.", "Atenção",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtNomeJogador.Text))
        {
            MessageBox.Show("Informe seu nome.", "Atenção",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var nome = _txtNomeJogador.Text.Trim();
        var sessao = SessionStore.Buscar(partida.Id, nome);

        if (partida.Status != 'A' && sessao == null)
        {
            if (TentarReentrarComSenhaJogador(partida, nome, _txtSenhaPartida.Text))
                return;

            MessageBox.Show(
                "Essa partida ja foi iniciada, entao a DLL nao permite criar um novo jogador nela.\n\n" +
                "Para voltar nessa partida, use o mesmo nome de jogador que voce ja tinha usado e informe a senha de jogador gerada pela sala de espera.",
                "Partida em andamento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            int idJ;
            string senhaJ;

            if (sessao != null)
            {
                idJ = sessao.IdJogador;
                senhaJ = sessao.SenhaJogador;
                if (!SenhaJogadorFunciona(idJ, senhaJ))
                {
                    SessionStore.Remover(partida.Id, nome);
                    MessageBox.Show(
                        "A senha salva para esse jogador nao funcionou mais.\n\n" +
                        "Entre novamente usando a senha de jogador que apareceu na sala de espera.",
                        "Sessao local invalida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                (idJ, senhaJ) = _svc.Entrar(partida.Id, nome, _txtSenhaPartida.Text);
                SessionStore.Salvar(new JogadorSessao
                {
                    IdPartida = partida.Id,
                    NomeJogador = nome,
                    IdJogador = idJ,
                    SenhaJogador = senhaJ
                });
            }
            // Avança para sala de espera
            AbrirSala(partida.Id, idJ, senhaJ, nome);
        }
        catch (Exception ex)
        {
            if (TentarReentrarComSenhaJogador(partida, nome, _txtSenhaPartida.Text))
                return;

            MessageBox.Show("Erro ao entrar:\n" + ex.Message, "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool TentarReentrarComSenhaJogador(Partida partida, string nome, string senhaJogador)
    {
        if (string.IsNullOrWhiteSpace(senhaJogador)) return false;

        try
        {
            var jogador = _svc.ListarJogadores(partida.Id)
                .FirstOrDefault(j => string.Equals(j.Nome, nome, StringComparison.OrdinalIgnoreCase));
            if (jogador == null) return false;
            if (!SenhaJogadorFunciona(jogador.Id, senhaJogador)) return false;

            SessionStore.Salvar(new JogadorSessao
            {
                IdPartida = partida.Id,
                NomeJogador = nome,
                IdJogador = jogador.Id,
                SenhaJogador = senhaJogador
            });

            AbrirSala(partida.Id, jogador.Id, senhaJogador, nome);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AbrirSala(int idPartida, int idJogador, string senhaJogador, string nome)
    {
        Hide();
        using var sala = new FormSalaEspera(_svc, idPartida, idJogador, senhaJogador, nome);
        sala.ShowDialog(this);
        Show();
        CarregarPartidas();
    }

    private bool SenhaJogadorFunciona(int idJogador, string senhaJogador)
    {
        try
        {
            _svc.ExibirMao(idJogador, senhaJogador);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
