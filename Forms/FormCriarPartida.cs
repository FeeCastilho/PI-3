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
        Width = 420;
        Height = 260;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var l1 = new Label { Text = "Nome da partida (ate 15):", Location = new Point(15, 18), AutoSize = true };
        _txtNome = new TextBox { Location = new Point(180, 14), Width = 200, MaxLength = 15 };
        var l2 = new Label { Text = "Senha (ate 10):",         Location = new Point(15, 58), AutoSize = true };
        _txtSenha = new TextBox { Location = new Point(180, 54), Width = 200, MaxLength = 10 };
        var l3 = new Label { Text = "Nome do grupo:",           Location = new Point(15, 98), AutoSize = true };
        _txtGrupo = new TextBox { Location = new Point(180, 94), Width = 200 };

        var btnOk = new Button
        {
            Text = "Criar",
            Location = new Point(180, 150),
            Width = 95,
            BackColor = Color.LimeGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.Click += BtnOk_Click;
        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(285, 150),
            Width = 95,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] { l1, _txtNome, l2, _txtSenha, l3, _txtGrupo, btnOk, btnCancel });
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

