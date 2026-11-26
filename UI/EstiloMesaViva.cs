using System.Globalization;
using MesaViva.Models;

namespace MesaViva.UI;

internal static class EstiloMesaViva
{
    public static readonly Color Verde = Color.FromArgb(39, 104, 73);
    public static readonly Color VerdeEscuro = Color.FromArgb(25, 60, 44);
    public static readonly Color VerdeSuave = Color.FromArgb(232, 241, 233);
    public static readonly Color Fundo = Color.FromArgb(244, 246, 243);
    public static readonly Color Superficie = Color.White;
    public static readonly Color Texto = Color.FromArgb(38, 56, 44);
    public static readonly Color TextoSuave = Color.FromArgb(78, 97, 84);
    public static readonly Color Borda = Color.FromArgb(223, 229, 223);
    public static readonly Color Erro = Color.FromArgb(140, 55, 49);
    public static readonly Color VermelhoSuave = Color.FromArgb(249, 235, 232);
    private static readonly Color VerdeHover = Color.FromArgb(32, 86, 61);

    public const int Espaco = 8;
    public const int EspacoInterno = 16;
    public const int EspacoExterno = 24;
    public const int AlturaBotao = 40;

    private static readonly Dictionary<(float, FontStyle), Font> Fontes = new();

    public static Font Fonte(float tamanho = 10, FontStyle estilo = FontStyle.Regular)
    {
        if (!Fontes.TryGetValue((tamanho, estilo), out Font? fonte))
        {
            fonte = new Font("Segoe UI", tamanho, estilo);
            Fontes.Add((tamanho, estilo), fonte);
        }
        return fonte;
    }

    public static void PrepararJanela(Form janela, Size tamanho, Size minimo)
    {
        // A escala inicial precisa incluir os controles adicionados pelo formulário.
        janela.SuspendLayout();
        janela.AutoScaleDimensions = new SizeF(96, 96);
        janela.AutoScaleMode = AutoScaleMode.Dpi;
        janela.Font = Fonte();
        janela.BackColor = Fundo;
        janela.ForeColor = Texto;
        janela.ClientSize = tamanho;
        janela.MinimumSize = minimo;
        janela.Load += (_, _) =>
        {
            janela.ResumeLayout(performLayout: true);
            Size area = Screen.FromControl(janela).WorkingArea.Size;
            janela.MinimumSize = new Size(Math.Min(janela.MinimumSize.Width, area.Width),
                Math.Min(janela.MinimumSize.Height, area.Height));
            janela.Size = new Size(Math.Min(janela.Width, area.Width), Math.Min(janela.Height, area.Height));
        };
    }

    public static Label Titulo(string texto, int tamanho = 24) => new()
    {
        Text = texto,
        AutoSize = true,
        UseMnemonic = false,
        Font = Fonte(tamanho, FontStyle.Bold),
        ForeColor = Texto,
        Margin = new Padding(0, 0, 0, Espaco)
    };

    public static Label Legenda(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        UseMnemonic = false,
        Font = Fonte(),
        ForeColor = TextoSuave,
        Margin = new Padding(0, 0, 0, 20)
    };

    public static Button Botao(string texto, bool primario = true)
    {
        var botao = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(0, AlturaBotao),
            FlatStyle = FlatStyle.Flat,
            BackColor = primario ? Verde : Superficie,
            ForeColor = primario ? Superficie : VerdeEscuro,
            Font = Fonte(9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 0)
        };
        botao.FlatAppearance.BorderColor = primario ? Verde : Borda;
        botao.FlatAppearance.MouseOverBackColor = primario ? VerdeHover : VerdeSuave;
        botao.FlatAppearance.MouseDownBackColor = primario ? VerdeEscuro : Borda;
        return botao;
    }

    public static Button BotaoMenu(string texto)
    {
        Button botao = Botao(texto, primario: false);
        botao.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        botao.Dock = DockStyle.Fill;
        botao.TextAlign = ContentAlignment.MiddleLeft;
        botao.Margin = new Padding(0, 0, 0, 4);
        botao.FlatAppearance.BorderSize = 0;
        botao.FlatAppearance.MouseOverBackColor = VerdeHover;
        botao.FlatAppearance.MouseDownBackColor = Verde;
        DestacarMenu(botao, false);
        return botao;
    }

    public static void DestacarMenu(Button botao, bool selecionado)
    {
        botao.BackColor = selecionado ? VerdeSuave : VerdeEscuro;
        botao.ForeColor = selecionado ? VerdeEscuro : Superficie;
        botao.FlatAppearance.MouseOverBackColor = selecionado ? Borda : VerdeHover;
        botao.FlatAppearance.MouseDownBackColor = selecionado ? Borda : Verde;
    }

    public static TextBox Campo(bool senha = false) => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        Font = Fonte(11),
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
        Font = Fonte(11)
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
        Font = Fonte(9, FontStyle.Bold),
        ForeColor = Texto,
        Margin = new Padding(0, 0, 0, 0)
    };

    public static TableLayoutPanel Coluna()
    {
        var coluna = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            ColumnCount = 1, RowCount = 0, Margin = Padding.Empty
        };
        coluna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        coluna.ClientSizeChanged += (_, _) => AjustarRotulos(coluna);
        return coluna;
    }

    public static void AdicionarLinha(TableLayoutPanel coluna, Control controle)
    {
        int linha = coluna.RowCount++;
        coluna.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        coluna.Controls.Add(controle, 0, linha);
        if (controle is Label rotulo)
            AjustarRotulo(coluna, rotulo);
    }

    public static void AdicionarCampo(TableLayoutPanel coluna, string rotulo, Control campo, bool espacoAbaixo = true)
    {
        campo.Dock = DockStyle.Top;
        campo.AccessibleName = rotulo;
        campo.Margin = new Padding(0, 4, 0, espacoAbaixo ? 12 : 4);
        AdicionarLinha(coluna, Rotulo(rotulo));
        AdicionarLinha(coluna, campo);
    }

    public static CheckBox MostrarSenha(TextBox senha)
    {
        var mostrar = new CheckBox { Text = "Mostrar senha", AutoSize = true, Margin = new Padding(0, 0, 0, 14) };
        mostrar.CheckedChanged += (_, _) => senha.UseSystemPasswordChar = !mostrar.Checked;
        return mostrar;
    }

    public static TableLayoutPanel Cartao(int espacoInterno = EspacoInterno)
    {
        TableLayoutPanel cartao = Coluna();
        cartao.BackColor = Superficie;
        cartao.Padding = new Padding(espacoInterno);
        cartao.Paint += (_, evento) =>
        {
            using var caneta = new Pen(Borda);
            if (cartao.ClientSize.Width > 0 && cartao.ClientSize.Height > 0)
                evento.Graphics.DrawRectangle(caneta, 0, 0, cartao.ClientSize.Width - 1, cartao.ClientSize.Height - 1);
        };
        return cartao;
    }

    public static Panel AreaRolavel(Control conteudo, Padding margem)
    {
        var area = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = margem, Margin = Padding.Empty };
        area.Controls.Add(conteudo);

        int alturaAnterior = conteudo.Height;
        bool recalculoPendente = false;
        conteudo.SizeChanged += (_, _) =>
        {
            if (alturaAnterior == conteudo.Height)
                return;

            alturaAnterior = conteudo.Height;
            if (!area.IsHandleCreated || recalculoPendente)
                return;

            // A grade pode mudar de altura enquanto a área rolável ainda está se redimensionando.
            recalculoPendente = true;
            area.BeginInvoke(() =>
            {
                recalculoPendente = false;
                if (!area.IsDisposed)
                    area.PerformLayout();
            });
        };
        return area;
    }

    public static TableLayoutPanel LimitarLargura(Control conteudo, int larguraMaxima, bool centralizar = false)
    {
        var faixa = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            ColumnCount = centralizar ? 3 : 2, RowCount = 1, Margin = Padding.Empty
        };
        var larguraConteudo = new ColumnStyle(SizeType.Absolute, larguraMaxima);
        if (centralizar)
            faixa.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        faixa.ColumnStyles.Add(larguraConteudo);
        faixa.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, centralizar ? 50 : 100));
        faixa.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        faixa.Controls.Add(conteudo, centralizar ? 1 : 0, 0);

        void AjustarLargura()
        {
            int disponivel = Math.Max(0, faixa.ClientSize.Width - faixa.Padding.Horizontal);
            int largura = Math.Min(faixa.LogicalToDeviceUnits(larguraMaxima), disponivel);
            if (larguraConteudo.Width != largura)
                larguraConteudo.Width = largura;
        }

        faixa.ClientSizeChanged += (_, _) => AjustarLargura();
        faixa.PaddingChanged += (_, _) => AjustarLargura();
        faixa.DpiChangedAfterParent += (_, _) => AjustarLargura();
        return faixa;
    }

    public static void EscalarControleDinamico(Control controle, Control referencia)
    {
        float escala = referencia.DeviceDpi / 96f;
        if (escala != 1)
            controle.Scale(new SizeF(escala, escala));
    }

    private static void AjustarRotulos(TableLayoutPanel coluna)
    {
        foreach (Label rotulo in coluna.Controls.OfType<Label>())
            AjustarRotulo(coluna, rotulo);
    }

    private static void AjustarRotulo(TableLayoutPanel coluna, Label rotulo)
    {
        int largura = coluna.ClientSize.Width - coluna.Padding.Horizontal - rotulo.Margin.Horizontal;
        if (largura > 0)
            rotulo.MaximumSize = new Size(largura, 0);
    }
}
