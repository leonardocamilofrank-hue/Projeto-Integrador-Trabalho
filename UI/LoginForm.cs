using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

internal sealed class LoginForm : Form
{
    private readonly AuthService _authService;
    private readonly TextBox _email = EstiloMesaViva.Campo();
    private readonly TextBox _senha = EstiloMesaViva.Campo(senha: true);
    private readonly TableLayoutPanel _conteudo = EstiloMesaViva.Cartao(32);

    public LoginForm(AuthService authService)
    {
        _authService = authService;
        Text = "MesaViva | Entrar";
        StartPosition = FormStartPosition.CenterScreen;
        EstiloMesaViva.PrepararJanela(this, new Size(600, 510), new Size(360, 300));

        Label marca = EstiloMesaViva.Titulo("MesaViva", 16);
        marca.ForeColor = EstiloMesaViva.Verde;
        marca.Margin = new Padding(0, 0, 0, 24);
        EstiloMesaViva.AdicionarLinha(_conteudo, marca);
        EstiloMesaViva.AdicionarLinha(_conteudo, EstiloMesaViva.Titulo("Entrar"));
        EstiloMesaViva.AdicionarLinha(_conteudo, EstiloMesaViva.Legenda(
            "Conecte sua organização à doação e à coleta de alimentos."));
        EstiloMesaViva.AdicionarCampo(_conteudo, "E-mail", _email);
        EstiloMesaViva.AdicionarCampo(_conteudo, "Senha", _senha, espacoAbaixo: false);
        EstiloMesaViva.AdicionarLinha(_conteudo, EstiloMesaViva.MostrarSenha(_senha));

        Button entrar = EstiloMesaViva.Botao("Entrar");
        entrar.Dock = DockStyle.Top;
        entrar.Margin = Padding.Empty;
        entrar.Click += async (_, _) => await EntrarAsync();
        EstiloMesaViva.AdicionarLinha(_conteudo, entrar);

        var cadastrar = new LinkLabel
        {
            Text = "Criar uma conta MesaViva", AutoSize = true,
            LinkColor = EstiloMesaViva.Verde, ActiveLinkColor = EstiloMesaViva.VerdeEscuro,
            VisitedLinkColor = EstiloMesaViva.Verde, Font = EstiloMesaViva.Fonte(10, FontStyle.Bold),
            Margin = new Padding(0, 16, 0, 0)
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
        EstiloMesaViva.AdicionarLinha(_conteudo, cadastrar);

        var centralizar = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 1,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var larguraCartao = new ColumnStyle(SizeType.Absolute, 420);
        centralizar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centralizar.ColumnStyles.Add(larguraCartao);
        centralizar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        centralizar.Controls.Add(_conteudo, 1, 0);
        var area = EstiloMesaViva.AreaRolavel(centralizar, new Padding(20, 26, 20, 26));
        centralizar.SizeChanged += (_, _) =>
        {
            int largura = Math.Min(LogicalToDeviceUnits(420), centralizar.ClientSize.Width);
            if (largura > 0 && larguraCartao.Width != largura)
                larguraCartao.Width = largura;
        };
        Controls.Add(area);
        ActiveControl = _email;
        AcceptButton = entrar;
    }

    private async Task EntrarAsync()
    {
        Usuario? usuario = null;
        string email = _email.Text;
        string senha = _senha.Text;

        if (!await OperacaoNaTela.ExecutarAsync(this, _conteudo, async () =>
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
                throw new ArgumentException("Informe o e-mail e a senha.");

            // A verificação PBKDF2 usa CPU depois da consulta ao banco.
            usuario = await Task.Run(() => _authService.FazerLoginAsync(email, senha));
            if (usuario is null)
                throw new InvalidOperationException("E-mail ou senha inválidos.");
        }) || usuario is null)
        {
            (string.IsNullOrWhiteSpace(email) ? _email : _senha).Focus();
            return;
        }

        _senha.Clear();
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
        : base("Criar conta", "Escolha o perfil que representa sua organização.", 600)
    {
        _authService = authService;
        AdicionarCampo("Nome da organização", _nome);
        AdicionarCampo("E-mail", _email);
        AdicionarCampo("Senha", _senha, espacoAbaixo: false);
        AdicionarLinha(EstiloMesaViva.MostrarSenha(_senha));
        AdicionarCampo($"Telefone (ex.: {Usuario.ExemploTelefone})", _telefone);
        AdicionarCampo("Endereço", _endereco);
        _tipo.Items.AddRange(["Estabelecimento", "Instituição"]);
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
        string tipo = _tipo.SelectedIndex switch
        {
            0 => Usuario.TipoEstabelecimento,
            1 => Usuario.TipoInstituicao,
            _ => throw new ArgumentException("Selecione o tipo de conta.")
        };
        Usuario cadastro = await Task.Run(() =>
            _authService.CadastrarUsuarioAsync(nome, email, senha, telefone, endereco, tipo));
        Email = cadastro.Email;
        _senha.Clear();
        OperacaoNaTela.Informar(this, "Conta criada com sucesso. Agora você já pode entrar.");
    }
}
