using System.Globalization;
using MesaViva.Models;

namespace MesaViva.UI;

internal static class OperacaoNaTela
{
    public static async Task<bool> ExecutarAsync(Form janela, Control conteudo, Func<Task> operacao)
    {
        if (!conteudo.Enabled || janela.IsDisposed)
            return false;

        void ImpedirFechamento(object? _, FormClosingEventArgs evento)
        {
            evento.Cancel = true;
        }

        conteudo.Enabled = false;
        janela.UseWaitCursor = true;
        janela.FormClosing += ImpedirFechamento;
        try
        {
            await operacao();
            return true;
        }
        catch (Exception erro)
        {
            janela.UseWaitCursor = false;
            string mensagem = erro is (ArgumentException or InvalidOperationException)
                && erro.TargetSite?.DeclaringType?.Assembly == typeof(OperacaoNaTela).Assembly
                ? erro.Message
                : "Não foi possível acessar os dados do MesaViva. Confira a conexão e as permissões no MySQL e tente novamente.";
            MessageBox.Show(janela, mensagem, "MesaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        finally
        {
            janela.FormClosing -= ImpedirFechamento;
            janela.UseWaitCursor = false;
            conteudo.Enabled = true;
        }
    }

    public static void Informar(IWin32Window janela, string mensagem)
    {
        MessageBox.Show(janela, mensagem, "MesaViva", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

internal abstract class DialogoForm : Form
{
    private readonly TableLayoutPanel _conteudo = EstiloMesaViva.Cartao(24);
    private readonly TableLayoutPanel _estrutura = new()
    {
        Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty
    };

    protected DialogoForm(string titulo, string legenda, int altura)
    {
        Text = $"MesaViva | {titulo}";
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        EstiloMesaViva.PrepararJanela(this, new Size(510, altura), new Size(380, 270));

        AdicionarLinha(EstiloMesaViva.Titulo(titulo, 20));
        AdicionarLinha(EstiloMesaViva.Legenda(legenda));
        _estrutura.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _estrutura.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _estrutura.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var area = EstiloMesaViva.AreaRolavel(_conteudo, new Padding(16, 16, 16, 0));
        _estrutura.Controls.Add(area, 0, 0);
        Controls.Add(_estrutura);
    }

    protected void AdicionarLinha(Control controle)
    {
        EstiloMesaViva.AdicionarLinha(_conteudo, controle);
    }

    protected void AdicionarCampo(string rotulo, Control campo, bool espacoAbaixo = true)
    {
        EstiloMesaViva.AdicionarCampo(_conteudo, rotulo, campo, espacoAbaixo);
    }

    protected void AdicionarAcoes(string texto, Func<Task> confirmar)
    {
        var acoes = new FlowLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Top, Margin = Padding.Empty, Padding = new Padding(16)
        };
        Button principal = EstiloMesaViva.Botao(texto);
        principal.Click += async (_, _) =>
        {
            if (await OperacaoNaTela.ExecutarAsync(this, _estrutura, confirmar))
                DialogResult = DialogResult.OK;
        };

        Button cancelar = EstiloMesaViva.Botao("Cancelar", primario: false);
        cancelar.DialogResult = DialogResult.Cancel;
        acoes.Controls.AddRange([principal, cancelar]);
        _estrutura.Controls.Add(acoes, 0, 1);
        AcceptButton = principal;
        CancelButton = cancelar;
    }
}

internal sealed class OfertaForm : DialogoForm
{
    private const string ExemploData = "30/11/2026 14:30";
    private readonly TextBox _alimento = EstiloMesaViva.Campo();
    private readonly CampoQuantidade _quantidade = new();

    public OfertaForm(Oferta? oferta, Func<string, decimal, DateTime, DateTime, DateTime, Task> salvar)
        : base(oferta is null ? "Cadastrar alimento" : "Editar oferta",
            "Marque os campos de data e ajuste o dia e a hora de cada etapa.", 515)
    {
        _alimento.Text = oferta?.Alimento ?? string.Empty;
        _quantidade.Text = oferta?.QuantidadeTotal.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        AdicionarCampo("Alimento", _alimento);
        AdicionarCampo("Quantidade total (kg)", _quantidade);
        Func<DateTime> validade = AdicionarData("Validade", oferta?.Validade);
        Func<DateTime> inicio = AdicionarData("Início da retirada", oferta?.InicioRetirada);
        Func<DateTime> fim = AdicionarData("Fim da retirada", oferta?.FimRetirada);
        AdicionarAcoes("Salvar oferta", () =>
            salvar(_alimento.Text, _quantidade.LerQuantidade(), validade(), inicio(), fim()));
    }

    private Func<DateTime> AdicionarData(string rotulo, DateTime? valor)
    {
        if (valor is DateTime antiga
            && (antiga < DateTimePicker.MinimumDateTime || antiga > DateTimePicker.MaximumDateTime))
        {
            // O MySQL aceita datas fora do intervalo do controle nativo do Windows.
            return AdicionarDataComMascara(rotulo, antiga);
        }

        var campo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom, CustomFormat = Utils.FormatoData,
            ShowUpDown = true, ShowCheckBox = true, Checked = valor.HasValue
        };
        if (valor.HasValue)
            campo.Value = valor.Value;
        AdicionarCampo(rotulo, campo);

        return () =>
        {
            if (!campo.Checked)
                throw new ArgumentException($"Informe a data e a hora do campo de {rotulo.ToLowerInvariant()}.");

            DateTime data = campo.Value.AddTicks(-(campo.Value.Ticks % TimeSpan.TicksPerMinute));
            // Preserve a precisão já armazenada quando a data e a hora exibidas não mudarem.
            if (valor is DateTime original && original.Ticks / TimeSpan.TicksPerMinute == data.Ticks / TimeSpan.TicksPerMinute)
                return original;
            return data;
        };
    }

    private Func<DateTime> AdicionarDataComMascara(string rotulo, DateTime valor)
    {
        MaskedTextBox campo = EstiloMesaViva.CampoComMascara("00/00/0000 00:00");
        string original = Utils.FormatarData(valor);
        campo.Text = original;
        AdicionarCampo(rotulo, campo);
        return () =>
        {
            if (campo.Text == original)
                return valor;
            if (campo.MaskCompleted && DateTime.TryParseExact(campo.Text, Utils.FormatoData,
                CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out DateTime data))
                return data;
            throw new ArgumentException($"Informe uma data e hora válidas para {rotulo} (ex.: {ExemploData}).");
        };
    }
}

internal sealed class CampoQuantidade : NumericUpDown
{
    private const NumberStyles Formato = NumberStyles.Number & ~NumberStyles.AllowThousands;

    public CampoQuantidade()
    {
        DecimalPlaces = QuantidadeEmKg.CasasDecimais;
        Maximum = QuantidadeEmKg.Maximo;
        Increment = 0.01m;
        Font = EstiloMesaViva.Fonte(11);
        Text = string.Empty;
    }

    public decimal LerQuantidade()
    {
        if (!decimal.TryParse(Text, Formato, CultureInfo.CurrentCulture, out decimal quantidade))
            throw new ArgumentException("Informe uma quantidade em kg no formato numérico (ex.: 2,50).");

        return quantidade;
    }

    protected override void ValidateEditText()
    {
        if (PodeFormatarEntrada())
            base.ValidateEditText();
    }

    protected override void UpdateEditText()
    {
        // O controle também tenta formatar ao perder o foco. Preserve a entrada para a validação.
        if (!UserEdit || PodeFormatarEntrada())
            base.UpdateEditText();
    }

    private bool PodeFormatarEntrada()
    {
        return decimal.TryParse(Text, Formato, CultureInfo.CurrentCulture, out decimal quantidade)
            && quantidade >= Minimum && quantidade <= Maximum
            && decimal.Round(quantidade, DecimalPlaces) == quantidade;
    }
}

internal sealed class QuantidadeForm : DialogoForm
{
    public QuantidadeForm(string alimento, decimal disponivel, Func<decimal, Task> reservar)
        : base("Reservar alimento", $"{alimento} · {Utils.FormatarQuantidade(disponivel)} disponíveis", 300)
    {
        var quantidade = new CampoQuantidade();
        AdicionarCampo("Quantidade desejada (kg)", quantidade);
        AdicionarAcoes("Confirmar reserva", () => reservar(quantidade.LerQuantidade()));
    }
}

internal sealed class PerfilForm : DialogoForm
{
    public PerfilForm(Usuario usuario, Func<string, string, string, string, Task> salvar)
        : base("Meus dados", "Atualize as informações da sua conta.", 455)
    {
        TextBox nome = EstiloMesaViva.Campo();
        TextBox email = EstiloMesaViva.Campo();
        MaskedTextBox telefone = EstiloMesaViva.CampoTelefone(usuario.Telefone);
        TextBox endereco = EstiloMesaViva.Campo();
        nome.Text = usuario.Nome;
        email.Text = usuario.Email;
        endereco.Text = usuario.Endereco;

        AdicionarCampo("Nome da organização", nome);
        AdicionarCampo("E-mail", email);
        AdicionarCampo($"Telefone (ex.: {Usuario.ExemploTelefone})", telefone);
        AdicionarCampo("Endereço", endereco);
        AdicionarAcoes("Salvar alterações", () => salvar(nome.Text, email.Text, telefone.Text, endereco.Text));
    }
}
