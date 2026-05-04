using DraftosaurusClient.Services;

namespace DraftosaurusClient.Forms;

/// <summary>Dialogo simples para criar uma nova partida.</summary>
public class FormCriarPartida : Form
{
    private readonly DraftService _svc;
    private readonly TextBox _txtNome;
    private readonly TextBox _txtSenha;
    private readonly TextBox _txtGrupo;

    public int IdCriado { get; private set; }

    // Esta funcao cuida de iniciar 'FormCriarPartida' do programa.
    public FormCriarPartida(DraftService svc)
    {
        _svc = svc;
        Text = "Criar nova partida";
        Width = 520;
        Height = 290;
        MinimumSize = new Size(500, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var l1 = new Label { Text = "Nome da partida (ate 15):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _txtNome = new TextBox { Dock = DockStyle.Fill, MaxLength = 15, Margin = new Padding(0, 7, 0, 0) };
        var l2 = new Label { Text = "Senha (ate 10):", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _txtSenha = new TextBox { Dock = DockStyle.Fill, MaxLength = 10, Margin = new Padding(0, 7, 0, 0) };
        var l3 = new Label { Text = "Nome do grupo:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        _txtGrupo = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 7, 0, 0) };

        var btnOk = new Button
        {
            Text = "Criar",
            Width = 110,
            Height = 34,
            BackColor = Color.LimeGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.Click += BtnOk_Click;
        var btnCancel = new Button
        {
            Text = "Cancelar",
            Width = 110,
            Height = 34,
            DialogResult = DialogResult.Cancel
        };

        var botoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        botoes.Controls.Add(btnCancel);
        botoes.Controls.Add(btnOk);

        layout.Controls.Add(l1, 0, 0);
        layout.Controls.Add(_txtNome, 1, 0);
        layout.Controls.Add(l2, 0, 1);
        layout.Controls.Add(_txtSenha, 1, 1);
        layout.Controls.Add(l3, 0, 2);
        layout.Controls.Add(_txtGrupo, 1, 2);
        layout.Controls.Add(botoes, 0, 4);
        layout.SetColumnSpan(botoes, 2);
        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    // A funcao serve para iniciar 'BtnOk_Click' do programa.
    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtNome.Text) || string.IsNullOrWhiteSpace(_txtGrupo.Text))
        {
            MessageBox.Show("Preencha nome e grupo.", "Atencao",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            IdCriado = _svc.CriarPartida(_txtNome.Text.Trim(), _txtSenha.Text, _txtGrupo.Text.Trim());
            MessageBox.Show($"Partida criada com Id {IdCriado}.\nCompartilhe esse Id e a senha com seus colegas.",
                "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao criar partida:\n" + ex.Message, "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

