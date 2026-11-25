using System.Globalization;
using MesaViva.Models;

namespace MesaViva.UI;

internal static class EstiloMesaViva
{
    public static readonly Color Verde = Color.FromArgb(39, 104, 73);
    public static readonly Color VerdeEscuro = Color.FromArgb(27, 75, 53);
    public static readonly Color Fundo = Color.FromArgb(246, 248, 245);
    public static readonly Color Texto = Color.FromArgb(39, 49, 43);
    public static readonly Color TextoSuave = Color.FromArgb(111, 122, 114);
    public static readonly Color Borda = Color.FromArgb(224, 231, 225);

    public static Label Titulo(string texto, int tamanho = 24) => new()
    {
        Text = texto,
        AutoSize = true,
        UseMnemonic = false,
        Font = new Font("Segoe UI", tamanho, FontStyle.Bold),
        ForeColor = Texto,
        Margin = new Padding(0, 0, 0, 8)
    };

    public static Label Legenda(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        UseMnemonic = false,
        Font = new Font("Segoe UI", 10),
        ForeColor = TextoSuave,
        Margin = new Padding(0, 0, 0, 20)
    };

    public static Button Botao(string texto, bool primario = true)
    {
        var botao = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(110, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Verde : Color.White,
            ForeColor = primario ? Color.White : VerdeEscuro,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 0)
        };
        botao.FlatAppearance.BorderColor = primario ? Verde : Borda;
        return botao;
    }

    public static TextBox Campo(bool senha = false) => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 11),
        UseSystemPasswordChar = senha
    };

    public static MaskedTextBox CampoComMascara(string mascara) => new()
    {
        Culture = CultureInfo.InvariantCulture,
        Mask = mascara,
        AsciiOnly = true,
        TextMaskFormat = MaskFormat.IncludeLiterals,
        RejectInputOnFirstFailure = true,
        InsertKeyMode = InsertKeyMode.Overwrite,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 11)
    };

    public static MaskedTextBox CampoTelefone(string? telefone = null)
    {
        MaskedTextBox campo = CampoComMascara("(00) 00000-0000");
        if (Usuario.TentarFormatarTelefone(telefone, out string formatado))
            campo.Text = formatado;
        return campo;
    }

    public static Label Rotulo(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Texto,
        Margin = new Padding(0, 0, 0, 0)
    };

    public static void AdicionarCampo(FlowLayoutPanel painel, string rotulo, Control campo, int largura)
    {
        campo.Width = largura;
        campo.AccessibleName = rotulo;
        campo.Margin = new Padding(0, 5, 0, 14);
        painel.Controls.Add(Rotulo(rotulo));
        painel.Controls.Add(campo);
    }
}
