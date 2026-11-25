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
                : "Não foi possível acessar os dados do MesaViva. Verifique a conexão com o banco de dados e tente novamente.";
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
    private readonly FlowLayoutPanel _conteudo = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(26)
    };

    protected DialogoForm(string titulo, string legenda, int altura)
    {
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(480, altura);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        Label subtitulo = EstiloMesaViva.Legenda(legenda);
        subtitulo.MaximumSize = new Size(400, 0);
        _conteudo.Controls.Add(EstiloMesaViva.Titulo(titulo, 20));
        _conteudo.Controls.Add(subtitulo);
        Controls.Add(_conteudo);
    }

    protected void AdicionarCampo(string rotulo, Control campo)
    {
        EstiloMesaViva.AdicionarCampo(_conteudo, rotulo, campo, 400);
    }

    protected void AdicionarAcoes(string texto, Func<Task> confirmar)
    {
        var acoes = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        Button principal = EstiloMesaViva.Botao(texto);
        principal.Click += async (_, _) =>
        {
            if (await OperacaoNaTela.ExecutarAsync(this, _conteudo, confirmar))
                DialogResult = DialogResult.OK;
        };

        Button cancelar = EstiloMesaViva.Botao("Cancelar", primario: false);
        cancelar.DialogResult = DialogResult.Cancel;
        acoes.Controls.Add(principal);
        acoes.Controls.Add(cancelar);
        _conteudo.Controls.Add(acoes);
        AcceptButton = principal;
        CancelButton = cancelar;
    }
}

internal sealed class OfertaForm : DialogoForm
{
    private const string FormatoData = "dd/MM/yyyy HH:mm";
    private const string ExemploData = "30/11/2026 14:30";
    private readonly TextBox _alimento = EstiloMesaViva.Campo();
    private readonly NumericUpDown _quantidade = new()
    {
        DecimalPlaces = 2, Minimum = 0.01m, Maximum = 99_999_999.99m, Increment = 0.01m
    };
    public OfertaForm(Oferta? oferta, Func<string, decimal, DateTime, DateTime, DateTime, Task> salvar)
        : base(oferta is null ? "Cadastrar alimento" : "Editar oferta",
            $"Informe a quantidade em kg. Digite as datas e horas no formato dd/MM/aaaa HH:mm (ex.: {ExemploData}).", 480)
    {
        _alimento.Text = oferta?.Alimento ?? string.Empty;
        if (oferta is not null)
            _quantidade.Value = Math.Clamp(oferta.QuantidadeTotal, _quantidade.Minimum, _quantidade.Maximum);
        AdicionarCampo("Alimento", _alimento);
        AdicionarCampo("Quantidade total (kg)", _quantidade);
        Func<DateTime> validade = AdicionarData("Validade", oferta?.Validade);
        Func<DateTime> inicio = AdicionarData("Início da retirada", oferta?.InicioRetirada);
        Func<DateTime> fim = AdicionarData("Fim da retirada", oferta?.FimRetirada);
        AdicionarAcoes("Salvar oferta", () =>
            salvar(_alimento.Text, _quantidade.Value, validade(), inicio(), fim()));
    }

    private Func<DateTime> AdicionarData(string rotulo, DateTime? valor)
    {
        MaskedTextBox campo = EstiloMesaViva.CampoComMascara("00/00/0000 00:00");
        string original = valor?.ToString(FormatoData, CultureInfo.InvariantCulture) ?? string.Empty;
        campo.Text = original;
        AdicionarCampo(rotulo, campo);
        return () =>
        {
            // Preserve os segundos já gravados quando a data da oferta não mudar.
            if (valor is DateTime dataOriginal && campo.Text == original)
                return dataOriginal;
            if (DateTime.TryParseExact(campo.Text, FormatoData, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime data) && campo.MaskCompleted)
                return data;
            throw new ArgumentException(
                $"Preencha o campo {rotulo} com uma data e hora válidas no formato dd/MM/aaaa HH:mm (ex.: {ExemploData}).");
        };
    }
}

internal sealed class QuantidadeForm : DialogoForm
{
    public QuantidadeForm(string alimento, decimal disponivel, Func<decimal, Task> reservar)
        : base("Reservar alimento", $"{alimento} · {Utils.FormatarQuantidade(disponivel)} disponíveis", 245)
    {
        var quantidade = new NumericUpDown
        {
            Minimum = 0.01m, Maximum = disponivel, DecimalPlaces = 2, Increment = 0.01m
        };
        AdicionarCampo("Quantidade desejada (kg)", quantidade);
        AdicionarAcoes("Confirmar reserva", () => reservar(quantidade.Value));
    }
}

internal sealed class PerfilForm : DialogoForm
{
    public PerfilForm(Usuario usuario, Func<string, string, string, string, Task> salvar)
        : base("Meus dados", "Atualize as informações da sua conta.", 435)
    {
        TextBox nome = EstiloMesaViva.Campo();
        TextBox email = EstiloMesaViva.Campo();
        MaskedTextBox telefone = EstiloMesaViva.CampoTelefone(usuario.Telefone);
        TextBox endereco = EstiloMesaViva.Campo();
        nome.Text = usuario.Nome;
        email.Text = usuario.Email;
        endereco.Text = usuario.Endereco;

        AdicionarCampo("Nome", nome);
        AdicionarCampo("E-mail", email);
        AdicionarCampo($"Telefone (ex.: {Usuario.ExemploTelefone})", telefone);
        AdicionarCampo("Endereço", endereco);
        AdicionarAcoes("Salvar alterações", () => salvar(nome.Text, email.Text, telefone.Text, endereco.Text));
    }
}
