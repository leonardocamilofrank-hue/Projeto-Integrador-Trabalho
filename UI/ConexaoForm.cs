using MesaViva.Data;

namespace MesaViva.UI;

internal sealed class ConexaoForm : Form
{
    private readonly TextBox _senha = EstiloMesaViva.Campo(senha: true);
    private readonly TextBox _servidor = EstiloMesaViva.Campo();
    private readonly NumericUpDown _porta = new() { Minimum = 1, Maximum = 65535, Value = ConexaoMySql.PortaPadrao };
    private readonly TextBox _banco = EstiloMesaViva.Campo();
    private readonly TextBox _usuario = EstiloMesaViva.Campo();
    private readonly CheckBox _avancadas = new() { Text = "Configurações avançadas", AutoSize = true };
    private readonly TableLayoutPanel _campos = EstiloMesaViva.Coluna();
    private readonly TableLayoutPanel _detalhes = EstiloMesaViva.Coluna();
    private readonly Label _mensagem = EstiloMesaViva.Legenda(string.Empty);
    private readonly Button _conectar = EstiloMesaViva.Botao("Conectar");
    private CancellationTokenSource? _tentativa;

    public string? ConfiguracaoValidada { get; private set; }

    public ConexaoForm(string? configuracao)
    {
        Text = "MesaViva | Conexão com MySQL";
        StartPosition = FormStartPosition.CenterScreen;
        EstiloMesaViva.PrepararJanela(this, new Size(510, 385), new Size(380, 270));

        TableLayoutPanel conteudo = EstiloMesaViva.Cartao(EstiloMesaViva.EspacoExterno);
        Label marca = EstiloMesaViva.Rotulo("MesaViva");
        marca.ForeColor = EstiloMesaViva.Verde;
        marca.Margin = new Padding(0, 0, 0, EstiloMesaViva.Espaco);
        EstiloMesaViva.AdicionarLinha(conteudo, marca);
        EstiloMesaViva.AdicionarLinha(conteudo, EstiloMesaViva.Titulo("Conectar ao MySQL", 20));
        EstiloMesaViva.AdicionarLinha(conteudo, _mensagem);

        _servidor.Text = ConexaoMySql.ServidorPadrao;
        _banco.Text = ConexaoMySql.BancoPadrao;
        _usuario.Text = ConexaoMySql.UsuarioPadrao;
        EstiloMesaViva.AdicionarCampo(_campos, "Senha do MySQL", _senha, espacoAbaixo: false);
        EstiloMesaViva.AdicionarLinha(_campos, EstiloMesaViva.MostrarSenha(_senha));
        _avancadas.Margin = new Padding(0, 0, 0, EstiloMesaViva.Espaco);
        EstiloMesaViva.AdicionarLinha(_campos, _avancadas);
        EstiloMesaViva.AdicionarCampo(_detalhes, "Servidor", _servidor);
        EstiloMesaViva.AdicionarCampo(_detalhes, "Porta", _porta);
        EstiloMesaViva.AdicionarCampo(_detalhes, "Banco de dados", _banco);
        EstiloMesaViva.AdicionarCampo(_detalhes, "Usuário do MySQL", _usuario);
        _detalhes.Visible = false;
        EstiloMesaViva.AdicionarLinha(_campos, _detalhes);
        _avancadas.CheckedChanged += (_, _) =>
        {
            _detalhes.Visible = _avancadas.Checked;
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int altura = Math.Min(LogicalToDeviceUnits(_avancadas.Checked ? 635 : 425), area.Height);
            Height = altura;
            Top = Math.Max(area.Top, Math.Min(Top, area.Bottom - altura));
        };
        EstiloMesaViva.AdicionarLinha(conteudo, _campos);

        var cancelar = EstiloMesaViva.Botao("Cancelar", primario: false);
        cancelar.DialogResult = DialogResult.Cancel;
        var acoes = new FlowLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(EstiloMesaViva.EspacoInterno), Margin = Padding.Empty
        };
        acoes.Controls.AddRange([_conectar, cancelar]);
        var estrutura = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        estrutura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        estrutura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        estrutura.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        estrutura.Controls.Add(EstiloMesaViva.AreaRolavel(conteudo, new Padding(16, 16, 16, 0)), 0, 0);
        estrutura.Controls.Add(acoes, 0, 1);
        Controls.Add(estrutura);
        AcceptButton = _conectar;
        CancelButton = cancelar;
        _conectar.Click += async (_, _) => await ConectarAsync();
        FormClosing += (_, _) =>
        {
            if (DialogResult != DialogResult.OK)
                _tentativa?.Cancel();
        };

        if (configuracao is null)
        {
            _mensagem.Text = "Informe a senha usada na sua instalação do MySQL.";
            ActiveControl = _senha;
        }
        else
        {
            _campos.Visible = _conectar.Visible = false;
            Shown += async (_, _) => await ConectarAsync(configuracao);
        }
    }

    private async Task ConectarAsync(string? configuracao = null)
    {
        if (_tentativa is not null)
            return;

        if (configuracao is null)
        {
            if (string.IsNullOrWhiteSpace(_servidor.Text) || string.IsNullOrWhiteSpace(_banco.Text)
                || string.IsNullOrWhiteSpace(_usuario.Text))
            {
                ExibirErro("Preencha o servidor, o banco e o usuário nas configurações avançadas.");
                return;
            }

            configuracao = ConexaoMySql.Montar(_servidor.Text.Trim(), (uint)_porta.Value,
                _banco.Text.Trim(), _usuario.Text.Trim(), _senha.Text);
        }

        using var tentativa = new CancellationTokenSource();
        _tentativa = tentativa;
        _campos.Enabled = _conectar.Enabled = false;
        _mensagem.ForeColor = EstiloMesaViva.TextoSuave;
        _mensagem.Text = "Verificando a conexão…";
        UseWaitCursor = true;
        try
        {
            string? erro = await Task.Run(() => ConexaoMySql.VerificarAsync(configuracao, tentativa.Token));
            if (tentativa.IsCancellationRequested || IsDisposed)
                return;

            if (erro is null)
            {
                ConfiguracaoValidada = configuracao;
                _senha.Clear();
                DialogResult = DialogResult.OK;
                return;
            }

            ExibirErro(erro);
            _campos.Visible = _conectar.Visible = true;
        }
        catch (OperationCanceledException) when (tentativa.IsCancellationRequested) { }
        finally
        {
            _tentativa = null;
            if (!IsDisposed && !tentativa.IsCancellationRequested)
            {
                _campos.Enabled = _conectar.Enabled = true;
                UseWaitCursor = false;
                _senha.Focus();
            }
        }
    }

    private void ExibirErro(string mensagem)
    {
        _mensagem.ForeColor = EstiloMesaViva.Erro;
        _mensagem.Text = mensagem;
    }
}
