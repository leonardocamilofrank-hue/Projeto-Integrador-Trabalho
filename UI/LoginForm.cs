using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

internal sealed class LoginForm : Form
{
    private readonly AuthService _authService;
    private readonly TextBox _email = EstiloMesaViva.Campo();
    private readonly TextBox _senha = EstiloMesaViva.Campo(senha: true);
    private readonly FlowLayoutPanel _conteudo = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true
    };

    public LoginForm(AuthService authService)
    {
        _authService = authService;
        Text = "MesaViva | Entrar";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(500, 590);
        Size = new Size(620, 690);
        BackColor = EstiloMesaViva.Fundo;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        var centralizar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(24)
        };
        centralizar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centralizar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
        centralizar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centralizar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        centralizar.RowStyles.Add(new RowStyle(SizeType.Absolute, 440));
        centralizar.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var cartao = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(34),
            BorderStyle = BorderStyle.FixedSingle
        };
        var marca = new Label
        {
            Text = "●  MESAVIVA",
            AutoSize = true,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = EstiloMesaViva.Verde,
            Margin = new Padding(0, 2, 0, 24)
        };
        _conteudo.Controls.Add(marca);
        _conteudo.Controls.Add(EstiloMesaViva.Titulo("Entrar", 22));
        _conteudo.Controls.Add(EstiloMesaViva.Legenda("Conectando alimentos a quem precisa."));
        EstiloMesaViva.AdicionarCampo(_conteudo, "E-mail", _email, 308);
        EstiloMesaViva.AdicionarCampo(_conteudo, "Senha", _senha, 308);

        var entrar = EstiloMesaViva.Botao("Entrar");
        entrar.MinimumSize = new Size(308, 40);
        entrar.Margin = Padding.Empty;
        entrar.Click += async (_, _) => await EntrarAsync();
        _conteudo.Controls.Add(entrar);

        var cadastrar = new LinkLabel
        {
            Text = "Criar uma conta MesaViva",
            AutoSize = true,
            LinkColor = EstiloMesaViva.Verde,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 20, 0, 0)
        };
        cadastrar.LinkClicked += (_, _) =>
        {
            using var formulario = new CadastroForm(_authService);
            if (formulario.ShowDialog(this) == DialogResult.OK)
            {
                _email.Text = formulario.Email;
                _senha.Clear();
                _senha.Focus();
            }
        };
        _conteudo.Controls.Add(cadastrar);
        cartao.Controls.Add(_conteudo);
        centralizar.Controls.Add(cartao, 1, 1);
        Controls.Add(centralizar);
        AcceptButton = entrar;
    }

    private async Task EntrarAsync()
    {
        Usuario? usuario = null;
        string email = _email.Text;
        string senha = _senha.Text;
        _senha.Clear();

        if (!await OperacaoNaTela.ExecutarAsync(this, _conteudo, async () =>
        {
            // A verificação PBKDF2 também usa CPU após a consulta ao banco.
            usuario = await Task.Run(() => _authService.FazerLoginAsync(email, senha));
            if (usuario is null)
            {
                throw new InvalidOperationException("E-mail ou senha inválidos.");
            }

            if (usuario.Tipo is not (Usuario.TipoEstabelecimento or Usuario.TipoInstituicao))
            {
                throw new InvalidOperationException("Tipo de conta não reconhecido.");
            }
        }) || usuario is null)
        {
            _senha.Focus();
            return;
        }

        using var painel = new PainelForm(_authService, usuario);
        Hide();
        try
        {
            painel.ShowDialog();
            _email.Text = painel.Email;
        }
        finally
        {
            Show();
            Activate();
            _senha.Focus();
        }
    }
}

internal sealed class CadastroForm : DialogoForm
{
    private readonly AuthService _authService;
    private readonly TextBox _nome = EstiloMesaViva.Campo();
    private readonly TextBox _email = EstiloMesaViva.Campo();
    private readonly TextBox _senha = EstiloMesaViva.Campo(senha: true);
    private readonly MaskedTextBox _telefone = EstiloMesaViva.CampoTelefone();
    private readonly TextBox _endereco = EstiloMesaViva.Campo();
    private readonly ComboBox _tipo = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public string Email { get; private set; } = string.Empty;

    public CadastroForm(AuthService authService)
        : base("Criar conta", "Escolha o perfil que representa sua organização.", 530)
    {
        _authService = authService;
        AdicionarCampo("Nome", _nome);
        AdicionarCampo("E-mail", _email);
        AdicionarCampo("Senha", _senha);
        AdicionarCampo($"Telefone (ex.: {Usuario.ExemploTelefone})", _telefone);
        AdicionarCampo("Endereço", _endereco);
        _tipo.Items.AddRange(["Estabelecimento", "Instituição"]);
        _tipo.SelectedIndex = 0;
        AdicionarCampo("Tipo de conta", _tipo);
        AdicionarAcoes("Cadastrar conta", CadastrarAsync);
    }

    private async Task CadastrarAsync()
    {
        string nome = _nome.Text;
        string email = _email.Text;
        string senha = _senha.Text;
        string telefone = _telefone.Text;
        string endereco = _endereco.Text;
        string tipo = _tipo.SelectedIndex == 0 ? Usuario.TipoEstabelecimento : Usuario.TipoInstituicao;
        Usuario cadastro = await Task.Run(() =>
            _authService.CadastrarUsuarioAsync(nome, email, senha, telefone, endereco, tipo));
        Email = cadastro.Email;
        _senha.Clear();
        OperacaoNaTela.Informar(this, "Conta criada com sucesso. Agora você já pode entrar.");
    }
}
