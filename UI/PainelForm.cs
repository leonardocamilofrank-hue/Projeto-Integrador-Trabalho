using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

internal sealed class PainelForm : Form
{
    private readonly AuthService _auth;
    private Usuario _usuario;
    private readonly OfertaService _ofertas = new();
    private readonly ReservaService _reservas = new();
    private readonly TableLayoutPanel _raiz = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Panel _conteudo = new() { Dock = DockStyle.Fill, Padding = new Padding(24), AutoScroll = true };
    private readonly TableLayoutPanel _lista = new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1
    };
    private readonly Label _titulo = EstiloMesaViva.Titulo("MesaViva", 20);
    private readonly Label _conta = new() { Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false };

    public string Email => _usuario.Email;
    private bool EhEstabelecimento => _usuario.Tipo == Usuario.TipoEstabelecimento;

    public PainelForm(AuthService auth, Usuario usuario)
    {
        _auth = auth;
        _usuario = usuario;
        Text = "MesaViva";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 470);
        Size = new Size(1080, 710);
        BackColor = EstiloMesaViva.Fundo;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 222));
        _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _raiz.Controls.Add(CriarMenu(), 0, 0);

        var area = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White,
            Padding = new Padding(24, 15, 24, 12), Margin = Padding.Empty
        };
        topo.RowStyles.Add(new RowStyle(SizeType.Percent, 66));
        topo.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        _titulo.Margin = Padding.Empty;
        _conta.ForeColor = EstiloMesaViva.Verde;
        _conta.Margin = Padding.Empty;
        AtualizarConta();
        topo.Controls.Add(_titulo, 0, 0);
        topo.Controls.Add(_conta, 0, 1);

        _lista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _conteudo.Controls.Add(_lista);
        area.Controls.Add(topo, 0, 0);
        area.Controls.Add(_conteudo, 0, 1);
        _raiz.Controls.Add(area, 1, 0);
        Controls.Add(_raiz);
        Shown += async (_, _) => await CarregarAsync(EhEstabelecimento
            ? MostrarMinhasOfertasAsync
            : () => MostrarOfertasAsync(urgentes: false));
    }

    private Control CriarMenu()
    {
        var menu = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty,
            BackColor = EstiloMesaViva.VerdeEscuro, Padding = new Padding(16)
        };
        menu.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        menu.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        menu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        menu.Controls.Add(new Label
        {
            Text = "●  MesaViva", Dock = DockStyle.Fill, ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var navegacao = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true
        };
        if (EhEstabelecimento)
        {
            AdicionarNavegacao(navegacao, "Minhas ofertas", () => CarregarAsync(MostrarMinhasOfertasAsync));
            AdicionarNavegacao(navegacao, "Reservas recebidas", () => CarregarAsync(() => MostrarReservasAsync(recebidas: true)));
        }
        else
        {
            AdicionarNavegacao(navegacao, "Alimentos disponíveis", () => CarregarAsync(() => MostrarOfertasAsync(urgentes: false)));
            AdicionarNavegacao(navegacao, "Ofertas urgentes", () => CarregarAsync(() => MostrarOfertasAsync(urgentes: true)));
            AdicionarNavegacao(navegacao, "Minhas reservas", () => CarregarAsync(() => MostrarReservasAsync(recebidas: false)));
        }

        var rodape = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill };
        AdicionarNavegacao(rodape, "Meus dados", MostrarPerfil);
        Button sair = CriarBotaoMenu("Sair da conta");
        sair.Click += (_, _) => Close();
        rodape.Controls.Add(sair);

        menu.Controls.Add(navegacao, 0, 1);
        menu.Controls.Add(rodape, 0, 2);
        return menu;
    }

    private static void AdicionarNavegacao(Control pai, string texto, Func<Task> acao)
    {
        Button botao = CriarBotaoMenu(texto);
        botao.Click += async (_, _) => await acao();
        pai.Controls.Add(botao);
    }

    private static Button CriarBotaoMenu(string texto)
    {
        var botao = new Button
        {
            Text = texto, Width = 180, Height = 43, FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White, BackColor = EstiloMesaViva.VerdeEscuro,
            TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 3)
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.FlatAppearance.MouseOverBackColor = EstiloMesaViva.Verde;
        return botao;
    }

    private Task<bool> CarregarAsync(Func<Task> carregar)
    {
        return OperacaoNaTela.ExecutarAsync(this, _raiz, carregar);
    }

    private void ComecarPagina(string titulo, string descricao)
    {
        _titulo.Text = titulo;
        _conteudo.AutoScrollPosition = Point.Empty;
        while (_lista.Controls.Count > 0)
            _lista.Controls[0].Dispose();

        _lista.RowStyles.Clear();
        _lista.RowCount = 0;
        AdicionarNaLista(EstiloMesaViva.Legenda(descricao));
    }

    private void AdicionarNaLista(Control controle)
    {
        int linha = _lista.RowCount++;
        _lista.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lista.Controls.Add(controle, 0, linha);
    }

    private async Task MostrarMinhasOfertasAsync()
    {
        var itens = await _ofertas.ListarMinhasOfertasAsync(_usuario);
        ComecarPagina("Minhas ofertas", "Gerencie os alimentos publicados pela sua organização.");
        Button cadastrar = EstiloMesaViva.Botao("Cadastrar alimento");
        cadastrar.Anchor = AnchorStyles.Left;
        cadastrar.Margin = new Padding(0, 0, 0, 14);
        cadastrar.Click += async (_, _) => await AbrirOfertaAsync(null);
        AdicionarNaLista(cadastrar);

        if (itens.Count == 0)
            AdicionarNaLista(EstiloMesaViva.Legenda("Ainda não há ofertas cadastradas."));
        foreach (OfertaComDisponibilidade item in itens)
            AdicionarNaLista(CriarCardOferta(item, urgentes: false));
    }

    private async Task MostrarOfertasAsync(bool urgentes)
    {
        var itens = urgentes
            ? await _ofertas.ListarOfertasUrgentesAsync(_usuario)
            : await _ofertas.ListarOfertasDisponiveisAsync(_usuario);
        ComecarPagina(urgentes ? "Ofertas urgentes" : "Alimentos disponíveis", urgentes
            ? "A retirada destas ofertas termina nas próximas duas horas."
            : "Escolha um alimento para reservar.");

        if (itens.Count == 0)
            AdicionarNaLista(EstiloMesaViva.Legenda("Nenhum alimento disponível no momento."));
        foreach (OfertaComDisponibilidade item in itens)
            AdicionarNaLista(CriarCardOferta(item, urgentes));
    }

    private Control CriarCardOferta(OfertaComDisponibilidade item, bool urgentes)
    {
        Oferta oferta = item.Oferta;
        string disponivel = item.QuantidadeDisponivel < 0
            ? "Saldo inconsistente: as reservas superam o total."
            : $"Disponível: {Utils.FormatarQuantidade(item.QuantidadeDisponivel)} de {Utils.FormatarQuantidade(oferta.QuantidadeTotal)}";
        string[] detalhes =
        [
            disponivel,
            $"Validade: {oferta.Validade:dd/MM/yyyy HH:mm}",
            $"Retirada: {oferta.InicioRetirada:dd/MM/yyyy HH:mm} até {oferta.FimRetirada:dd/MM/yyyy HH:mm}"
        ];
        if (EhEstabelecimento)
        {
            Button editar = EstiloMesaViva.Botao("Editar", primario: false);
            editar.Click += async (_, _) => await AbrirOfertaAsync(oferta);
            Button excluir = EstiloMesaViva.Botao("Excluir", primario: false);
            excluir.ForeColor = Color.FromArgb(160, 55, 48);
            excluir.Click += async (_, _) => await ExcluirOfertaAsync(oferta);
            return CriarCard(oferta.Alimento, detalhes, editar, excluir);
        }

        Button reservar = EstiloMesaViva.Botao("Reservar alimento");
        reservar.Click += async (_, _) => await ReservarAsync(item, urgentes);
        return CriarCard(oferta.Alimento, detalhes, reservar);
    }

    private async Task AbrirOfertaAsync(Oferta? oferta)
    {
        using var formulario = new OfertaForm(oferta, async (alimento, quantidade, validade, inicio, fim) =>
        {
            if (oferta is null)
                await _ofertas.CadastrarOfertaAsync(_usuario, alimento, quantidade, validade, inicio, fim);
            else
                await _ofertas.EditarOfertaAsync(_usuario, oferta.Id, alimento, quantidade, validade, inicio, fim);
        });
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        OperacaoNaTela.Informar(this, oferta is null ? "Oferta publicada com sucesso." : "Oferta atualizada.");
        await CarregarAsync(MostrarMinhasOfertasAsync);
    }

    private async Task ExcluirOfertaAsync(Oferta oferta)
    {
        if (!Confirmar($"Excluir a oferta de {oferta.Alimento}?"))
            return;

        if (await CarregarAsync(() => _ofertas.ExcluirOfertaAsync(_usuario, oferta.Id)))
        {
            OperacaoNaTela.Informar(this, "Oferta excluída.");
            await CarregarAsync(MostrarMinhasOfertasAsync);
        }
    }

    private async Task ReservarAsync(OfertaComDisponibilidade item, bool urgentes)
    {
        using var formulario = new QuantidadeForm(item.Oferta.Alimento, item.QuantidadeDisponivel, async quantidade =>
        {
            await _reservas.CriarReservaAsync(_usuario, item.Oferta.Id, quantidade);
        });
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        OperacaoNaTela.Informar(this, "Reserva realizada. Consulte o horário de retirada em Minhas reservas.");
        await CarregarAsync(() => MostrarOfertasAsync(urgentes));
    }

    private async Task MostrarReservasAsync(bool recebidas)
    {
        var itens = recebidas
            ? await _reservas.ListarReservasRecebidasAsync(_usuario)
            : await _reservas.ListarMinhasReservasAsync(_usuario);
        ComecarPagina(recebidas ? "Reservas recebidas" : "Minhas reservas", recebidas
            ? "Acompanhe as instituições que reservaram seus alimentos."
            : "Confira o horário de retirada e o status de suas reservas.");
        if (itens.Count == 0)
            AdicionarNaLista(EstiloMesaViva.Legenda("Nenhuma reserva encontrada."));
        foreach (ReservaDetalhada item in itens)
            AdicionarNaLista(CriarCardReserva(item, recebidas));
    }

    private Control CriarCardReserva(ReservaDetalhada item, bool recebida)
    {
        Reserva reserva = item.Reserva;
        string status = reserva.Status switch
        {
            Reserva.StatusReservada => "Reservada",
            Reserva.StatusCancelada => "Cancelada",
            Reserva.StatusColetada => "Coletada",
            _ => reserva.Status
        };
        var detalhes = new List<string>
        {
            recebida ? $"Instituição: {item.Instituicao}" : $"Estabelecimento: {item.Estabelecimento}",
            $"{Utils.FormatarQuantidade(reserva.QuantidadeReservada)} · {status}",
            $"Retirada: {item.InicioRetirada:dd/MM/yyyy HH:mm} até {item.FimRetirada:dd/MM/yyyy HH:mm}",
            $"Solicitada em: {reserva.DataReserva:dd/MM/yyyy HH:mm}"
        };
        if (reserva.DataColeta is DateTime coleta)
            detalhes.Add($"Coleta confirmada em: {coleta:dd/MM/yyyy HH:mm}");

        string titulo = $"{item.Alimento} · Reserva #{reserva.Id}";
        if (recebida || reserva.Status != Reserva.StatusReservada)
            return CriarCard(titulo, detalhes);

        Button cancelar = EstiloMesaViva.Botao("Cancelar reserva", primario: false);
        cancelar.Click += async (_, _) => await AlterarReservaAsync(reserva.Id, cancelar: true);
        Button confirmar = EstiloMesaViva.Botao("Confirmar coleta");
        confirmar.Click += async (_, _) => await AlterarReservaAsync(reserva.Id, cancelar: false);
        return CriarCard(titulo, detalhes, cancelar, confirmar);
    }

    private async Task AlterarReservaAsync(int reservaId, bool cancelar)
    {
        string pergunta = cancelar ? "Cancelar esta reserva?" : "Confirmar que a coleta foi realizada?";
        if (!Confirmar(pergunta))
            return;

        if (await CarregarAsync(() => cancelar
            ? _reservas.CancelarReservaAsync(_usuario, reservaId)
            : _reservas.ConfirmarColetaAsync(_usuario, reservaId)))
        {
            OperacaoNaTela.Informar(this, cancelar ? "Reserva cancelada." : "Coleta confirmada.");
            await CarregarAsync(() => MostrarReservasAsync(recebidas: false));
        }
    }

    private Task MostrarPerfil()
    {
        using var formulario = new PerfilForm(_usuario, async (nome, email, telefone, endereco) =>
        {
            _usuario = await _auth.AtualizarDadosAsync(_usuario, nome, email, telefone, endereco);
        });
        if (formulario.ShowDialog(this) == DialogResult.OK)
        {
            AtualizarConta();
            OperacaoNaTela.Informar(this, "Seus dados foram atualizados.");
        }
        return Task.CompletedTask;
    }

    private void AtualizarConta()
    {
        _conta.Text = $"{(EhEstabelecimento ? "Estabelecimento" : "Instituição")} · {_usuario.Nome}";
    }

    private bool Confirmar(string pergunta)
    {
        return MessageBox.Show(this, pergunta, "MesaViva",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private static Control CriarCard(string titulo, IReadOnlyList<string> detalhes, params Button[] botoes)
    {
        var card = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            ColumnCount = 1, RowCount = 2 + detalhes.Count, BackColor = Color.White,
            Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12)
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Label nome = EstiloMesaViva.Titulo(titulo, 15);
        nome.AutoSize = false;
        nome.AutoEllipsis = true;
        nome.Dock = DockStyle.Fill;
        nome.Margin = Padding.Empty;
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        card.Controls.Add(nome, 0, 0);
        for (int i = 0; i < detalhes.Count; i++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            card.Controls.Add(new Label
            {
                Text = detalhes[i], Dock = DockStyle.Fill, AutoEllipsis = true, UseMnemonic = false,
                ForeColor = EstiloMesaViva.TextoSuave, Margin = Padding.Empty
            }, 0, i + 1);
        }
        if (botoes.Length > 0)
        {
            var acoes = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 0) };
            acoes.Controls.AddRange(botoes);
            card.Controls.Add(acoes, 0, detalhes.Count + 1);
        }
        return card;
    }
}
