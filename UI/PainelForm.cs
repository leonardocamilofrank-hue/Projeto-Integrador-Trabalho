using MesaViva.Models;
using MesaViva.Services;

namespace MesaViva.UI;

internal sealed class PainelForm : Form
{
    private enum Pagina { MinhasOfertas, Recebidas, Disponiveis, Urgentes, MinhasReservas, Perfil }

    private const int LarguraConteudo = 1250;
    private const int LarguraCartaoSimples = 650;
    private readonly AuthService _auth;
    private Usuario _usuario;
    private readonly OfertaService _ofertas = new();
    private readonly ReservaService _reservas = new();
    private readonly Dictionary<Pagina, Button> _opcoes = new();
    private readonly TableLayoutPanel _raiz = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly TableLayoutPanel _lista = EstiloMesaViva.Coluna();
    private readonly Panel _conteudo;
    private readonly Label _titulo = EstiloMesaViva.Titulo("MesaViva", 22);
    private readonly Label _conta = EstiloMesaViva.Rotulo(string.Empty);
    private Pagina _pagina;
    private Button? _selecionado;

    public string Email => _usuario.Email;
    private bool EhEstabelecimento => _usuario.Tipo == Usuario.TipoEstabelecimento;

    public PainelForm(AuthService auth, Usuario usuario)
    {
        _auth = auth;
        _usuario = usuario;
        _pagina = EhEstabelecimento ? Pagina.MinhasOfertas : Pagina.Disponiveis;
        Text = "MesaViva";
        StartPosition = FormStartPosition.CenterScreen;
        EstiloMesaViva.PrepararJanela(this, new Size(1136, 730), new Size(660, 390));

        _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
        _raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _raiz.Controls.Add(CriarMenu(), 0, 0);
        DestacarPagina(_pagina);

        var area = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty };
        area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        area.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topo = new TableLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, RowCount = 1, BackColor = EstiloMesaViva.Superficie,
            Margin = Padding.Empty
        };
        topo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topo.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        TableLayoutPanel identificacao = EstiloMesaViva.Coluna();
        _conta.ForeColor = EstiloMesaViva.Verde;
        _conta.Margin = new Padding(0, 0, 0, 5);
        AtualizarConta();
        EstiloMesaViva.AdicionarLinha(identificacao, _conta);
        _titulo.Margin = Padding.Empty;
        EstiloMesaViva.AdicionarLinha(identificacao, _titulo);
        topo.Controls.Add(identificacao, 0, 0);
        Button atualizar = EstiloMesaViva.Botao("Atualizar", primario: false);
        atualizar.Anchor = AnchorStyles.Right;
        atualizar.Margin = new Padding(8, 0, 0, 0);
        atualizar.Click += async (_, _) => await AbrirPaginaAsync(_pagina);
        topo.Controls.Add(atualizar, 1, 0);

        TableLayoutPanel faixaTopo = EstiloMesaViva.LimitarLargura(topo, LarguraConteudo, centralizar: true);
        faixaTopo.BackColor = EstiloMesaViva.Superficie;
        faixaTopo.Padding = new Padding(EstiloMesaViva.EspacoExterno, 13, EstiloMesaViva.EspacoExterno, 13);
        TableLayoutPanel faixaLista = EstiloMesaViva.LimitarLargura(_lista, LarguraConteudo, centralizar: true);
        _conteudo = EstiloMesaViva.AreaRolavel(faixaLista, new Padding(EstiloMesaViva.EspacoExterno));

        void AlinharCabecalho()
        {
            if (_conteudo.ClientSize.Width <= 0)
                return;

            Padding margem = faixaTopo.Padding;
            int barra = Math.Max(0, faixaTopo.ClientSize.Width - _conteudo.ClientSize.Width);
            margem.Left = _conteudo.Padding.Left;
            margem.Right = _conteudo.Padding.Right + barra;
            if (faixaTopo.Padding != margem)
                faixaTopo.Padding = margem;
        }

        faixaTopo.ClientSizeChanged += (_, _) => AlinharCabecalho();
        _conteudo.ClientSizeChanged += (_, _) => AlinharCabecalho();
        _conteudo.PaddingChanged += (_, _) => AlinharCabecalho();
        area.Controls.Add(faixaTopo, 0, 0);
        area.Controls.Add(_conteudo, 0, 1);
        _raiz.Controls.Add(area, 1, 0);
        Controls.Add(_raiz);
        Shown += async (_, _) => await AbrirPaginaAsync(_pagina);
    }

    private Control CriarMenu()
    {
        var menu = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty,
            BackColor = EstiloMesaViva.VerdeEscuro, Padding = new Padding(EstiloMesaViva.EspacoInterno)
        };
        menu.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        menu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        menu.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        menu.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Label marca = EstiloMesaViva.Titulo("MesaViva", 19);
        marca.ForeColor = EstiloMesaViva.Superficie;
        marca.Margin = new Padding(8, 0, 0, 23);
        menu.Controls.Add(marca, 0, 0);

        TableLayoutPanel navegacao = EstiloMesaViva.Coluna();
        if (EhEstabelecimento)
        {
            AdicionarNavegacao(navegacao, Pagina.MinhasOfertas, "Minhas ofertas");
            AdicionarNavegacao(navegacao, Pagina.Recebidas, "Reservas recebidas");
        }
        else
        {
            AdicionarNavegacao(navegacao, Pagina.Disponiveis, "Alimentos disponíveis");
            AdicionarNavegacao(navegacao, Pagina.Urgentes, "Ofertas urgentes");
            AdicionarNavegacao(navegacao, Pagina.MinhasReservas, "Minhas reservas");
        }

        AdicionarNavegacao(navegacao, Pagina.Perfil, "Meus dados");
        Button sair = EstiloMesaViva.BotaoMenu("Sair da conta");
        sair.Click += (_, _) => Close();

        menu.Controls.Add(EstiloMesaViva.AreaRolavel(navegacao, Padding.Empty), 0, 1);
        menu.Controls.Add(sair, 0, 2);
        return menu;
    }

    private void AdicionarNavegacao(TableLayoutPanel pai, Pagina pagina, string texto)
    {
        Button botao = EstiloMesaViva.BotaoMenu(texto);
        botao.Click += async (_, _) => await AbrirPaginaAsync(pagina);
        _opcoes.Add(pagina, botao);
        EstiloMesaViva.AdicionarLinha(pai, botao);
    }

    private void DestacarPagina(Pagina pagina)
    {
        if (_selecionado is not null)
            EstiloMesaViva.DestacarMenu(_selecionado, false);
        _selecionado = _opcoes[pagina];
        EstiloMesaViva.DestacarMenu(_selecionado, true);
    }

    private Task<bool> ExecutarAsync(Func<Task> acao) => OperacaoNaTela.ExecutarAsync(this, _raiz, acao);

    private Task<bool> AbrirPaginaAsync(Pagina pagina)
    {
        return ExecutarAsync(async () =>
        {
            switch (pagina)
            {
                case Pagina.MinhasOfertas:
                    await MostrarMinhasOfertasAsync();
                    break;
                case Pagina.Recebidas:
                    await MostrarReservasAsync(recebidas: true);
                    break;
                case Pagina.Disponiveis:
                case Pagina.Urgentes:
                    await MostrarOfertasAsync(pagina == Pagina.Urgentes);
                    break;
                case Pagina.MinhasReservas:
                    await MostrarReservasAsync(recebidas: false);
                    break;
                case Pagina.Perfil:
                    MostrarPerfil();
                    break;
            }

            _pagina = pagina;
            DestacarPagina(pagina);
        });
    }

    private void ComecarPagina(string titulo, string? descricao = null)
    {
        _titulo.Text = titulo;
        _conteudo.AutoScrollPosition = Point.Empty;
        while (_lista.Controls.Count > 0)
            _lista.Controls[0].Dispose();

        _lista.RowStyles.Clear();
        _lista.RowCount = 0;
        if (descricao is not null)
            AdicionarNaLista(EstiloMesaViva.Legenda(descricao));
    }

    private void AdicionarNaLista(Control controle)
    {
        EstiloMesaViva.EscalarControleDinamico(controle, this);
        EstiloMesaViva.AdicionarLinha(_lista, controle);
    }

    private async Task MostrarMinhasOfertasAsync()
    {
        var itens = await _ofertas.ListarMinhasOfertasAsync(_usuario);
        ComecarPagina("Minhas ofertas", "Gerencie os alimentos publicados pela sua organização.");
        AdicionarResumoEstabelecimento(itens);

        Button cadastrar = EstiloMesaViva.Botao("Cadastrar alimento");
        cadastrar.Anchor = AnchorStyles.Left;
        cadastrar.Margin = new Padding(0, 0, 0, EstiloMesaViva.EspacoInterno);
        cadastrar.Click += async (_, _) => await AbrirOfertaAsync(null);
        AdicionarNaLista(cadastrar);

        if (itens.Count == 0)
            AdicionarEstadoVazio("Ainda não há ofertas cadastradas.");
        else
            AdicionarNaLista(CriarGradeOfertas(itens));
    }

    private async Task MostrarOfertasAsync(bool urgentes)
    {
        var itens = urgentes
            ? await _ofertas.ListarOfertasUrgentesAsync(_usuario)
            : await _ofertas.ListarOfertasDisponiveisAsync(_usuario);
        var reservas = await _reservas.ListarMinhasReservasAsync(_usuario);
        ComecarPagina(urgentes ? "Ofertas urgentes" : "Alimentos disponíveis",
            urgentes ? "A retirada destas ofertas termina nas próximas duas horas." : null);
        AdicionarResumoInstituicao(reservas, (urgentes ? "Ofertas urgentes" : "Ofertas disponíveis", itens.Count.ToString()));

        if (itens.Count == 0)
            AdicionarEstadoVazio(urgentes ? "Nenhuma oferta urgente no momento." : "Nenhum alimento disponível no momento.");
        else
            AdicionarNaLista(CriarGradeOfertas(itens));
    }

    private TableLayoutPanel CriarGradeOfertas(IEnumerable<OfertaComDisponibilidade> ofertas) =>
        CriarGrade(ofertas.Select(CriarCardOferta).ToArray(), maximoColunas: 3, larguraMinima: 365, larguraMaxima: 420);

    private Control CriarCardOferta(OfertaComDisponibilidade item)
    {
        Oferta oferta = item.Oferta;
        TableLayoutPanel cartao = CriarCartao(oferta.Alimento);
        AdicionarDetalhe(cartao, $"Disponível  ·  {Utils.FormatarQuantidade(item.QuantidadeDisponivel)}", destaque: true);
        AdicionarDetalhe(cartao, $"Quantidade total  ·  {Utils.FormatarQuantidade(oferta.QuantidadeTotal)}");
        if (item.QuantidadeDisponivel < 0)
            AdicionarDetalhe(cartao, "O total está abaixo da quantidade comprometida.");

        int percentual = oferta.QuantidadeTotal > 0
            ? (int)Math.Round(Math.Clamp(item.QuantidadeDisponivel / oferta.QuantidadeTotal, 0m, 1m) * 100)
            : 0;
        EstiloMesaViva.AdicionarLinha(cartao, new ProgressBar
        {
            Dock = DockStyle.Top, Height = 10, Value = percentual,
            Margin = new Padding(0, 3, 0, 10), AccessibleName = $"{percentual}% da quantidade total disponível"
        });
        AdicionarDetalhe(cartao, $"Validade  ·  {Utils.FormatarData(oferta.Validade)}");
        AdicionarDetalhe(cartao, $"Início da retirada  ·  {Utils.FormatarData(oferta.InicioRetirada)}");
        AdicionarDetalhe(cartao, $"Fim da retirada  ·  {Utils.FormatarData(oferta.FimRetirada)}");

        if (EhEstabelecimento)
        {
            Button editar = EstiloMesaViva.Botao("Editar", primario: false);
            editar.Click += async (_, _) => await AbrirOfertaAsync(oferta);
            Button excluir = EstiloMesaViva.Botao("Excluir", primario: false);
            excluir.ForeColor = EstiloMesaViva.Erro;
            excluir.Click += async (_, _) => await ExcluirOfertaAsync(oferta);
            AdicionarAcoes(cartao, editar, excluir);
        }
        else
        {
            Button reservar = EstiloMesaViva.Botao("Reservar alimento");
            reservar.Click += async (_, _) => await ReservarAsync(item);
            AdicionarAcoes(cartao, reservar);
        }

        return cartao;
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

        await AbrirPaginaAsync(_pagina);
        OperacaoNaTela.Informar(this, oferta is null ? "Oferta publicada com sucesso." : "Oferta atualizada.");
    }

    private async Task ExcluirOfertaAsync(Oferta oferta)
    {
        if (!Confirmar($"Excluir a oferta de {oferta.Alimento}?"))
            return;

        if (await ExecutarAsync(() => _ofertas.ExcluirOfertaAsync(_usuario, oferta.Id)))
        {
            await AbrirPaginaAsync(_pagina);
            OperacaoNaTela.Informar(this, "Oferta excluída.");
        }
    }

    private async Task ReservarAsync(OfertaComDisponibilidade item)
    {
        using var formulario = new QuantidadeForm(item.Oferta.Alimento, item.QuantidadeDisponivel, async quantidade =>
        {
            await _reservas.CriarReservaAsync(_usuario, item.Oferta.Id, quantidade);
        });
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        await AbrirPaginaAsync(_pagina);
        OperacaoNaTela.Informar(this, "Reserva realizada. Consulte o horário de retirada em Minhas reservas.");
    }

    private async Task MostrarReservasAsync(bool recebidas)
    {
        var itens = recebidas
            ? await _reservas.ListarReservasRecebidasAsync(_usuario)
            : await _reservas.ListarMinhasReservasAsync(_usuario);
        var ofertas = recebidas ? await _ofertas.ListarMinhasOfertasAsync(_usuario) : null;
        ComecarPagina(recebidas ? "Reservas recebidas" : "Minhas reservas", recebidas
            ? "Acompanhe as instituições que reservaram seus alimentos."
            : "Confira o horário de retirada e o status de suas reservas.");
        if (ofertas is not null)
            AdicionarResumoEstabelecimento(ofertas);
        else
            AdicionarResumoInstituicao(itens, ("Coletas confirmadas", itens.Count(item => item.Reserva.Status == Reserva.StatusColetada).ToString()));

        if (itens.Count == 0)
            AdicionarEstadoVazio("Nenhuma reserva encontrada.");
        else
            AdicionarNaLista(CriarGrade(itens.Select(item => CriarCardReserva(item, recebidas)).ToArray(),
                maximoColunas: 2, larguraMinima: 400, larguraMaxima: 520));
    }

    private Control CriarCardReserva(ReservaDetalhada item, bool recebida)
    {
        Reserva reserva = item.Reserva;
        TableLayoutPanel cartao = CriarCartao($"{item.Alimento} · Reserva #{reserva.Id}");
        Label status = EstiloMesaViva.Rotulo(reserva.Status);
        status.Padding = new Padding(8, 4, 8, 4);
        status.Margin = new Padding(0, 0, 0, 8);
        (status.BackColor, status.ForeColor) = reserva.Status switch
        {
            Reserva.StatusReservada => (EstiloMesaViva.VerdeSuave, EstiloMesaViva.VerdeEscuro),
            Reserva.StatusCancelada => (EstiloMesaViva.VermelhoSuave, EstiloMesaViva.Erro),
            Reserva.StatusColetada => (EstiloMesaViva.VerdeEscuro, EstiloMesaViva.Superficie),
            _ => (EstiloMesaViva.Fundo, EstiloMesaViva.Texto)
        };
        EstiloMesaViva.AdicionarLinha(cartao, status);
        AdicionarDetalhe(cartao, recebida ? $"Instituição  ·  {item.Instituicao}" : $"Estabelecimento  ·  {item.Estabelecimento}");
        AdicionarDetalhe(cartao, $"Quantidade  ·  {Utils.FormatarQuantidade(reserva.QuantidadeReservada)}", destaque: true);
        AdicionarDetalhe(cartao, $"Início da retirada  ·  {Utils.FormatarData(item.InicioRetirada)}");
        AdicionarDetalhe(cartao, $"Fim da retirada  ·  {Utils.FormatarData(item.FimRetirada)}");
        AdicionarDetalhe(cartao, $"Solicitada em  ·  {Utils.FormatarData(reserva.DataReserva)}");
        if (reserva.DataColeta is DateTime coleta)
            AdicionarDetalhe(cartao, $"Coleta confirmada em  ·  {Utils.FormatarData(coleta)}");

        if (!recebida && reserva.Status == Reserva.StatusReservada)
        {
            Button confirmar = EstiloMesaViva.Botao("Confirmar coleta");
            confirmar.Click += async (_, _) => await AlterarReservaAsync(reserva.Id, cancelar: false);
            Button cancelar = EstiloMesaViva.Botao("Cancelar reserva", primario: false);
            cancelar.ForeColor = EstiloMesaViva.Erro;
            cancelar.Click += async (_, _) => await AlterarReservaAsync(reserva.Id, cancelar: true);
            AdicionarAcoes(cartao, confirmar, cancelar);
        }

        return cartao;
    }

    private async Task AlterarReservaAsync(int reservaId, bool cancelar)
    {
        string pergunta = cancelar ? "Cancelar esta reserva?" : "Confirmar que a coleta foi realizada?";
        if (!Confirmar(pergunta))
            return;

        if (await ExecutarAsync(() => cancelar
            ? _reservas.CancelarReservaAsync(_usuario, reservaId)
            : _reservas.ConfirmarColetaAsync(_usuario, reservaId)))
        {
            await AbrirPaginaAsync(_pagina);
            OperacaoNaTela.Informar(this, cancelar ? "Reserva cancelada." : "Coleta confirmada.");
        }
    }

    private void MostrarPerfil()
    {
        ComecarPagina("Meus dados");
        TableLayoutPanel cartao = CriarCartao(_usuario.Nome);
        AdicionarDetalhe(cartao, $"E-mail  ·  {_usuario.Email}");
        AdicionarDetalhe(cartao, $"Telefone  ·  {_usuario.Telefone}");
        AdicionarDetalhe(cartao, $"Endereço  ·  {_usuario.Endereco}");
        Button editar = EstiloMesaViva.Botao("Editar dados");
        editar.Click += (_, _) => EditarPerfil();
        AdicionarAcoes(cartao, editar);
        AdicionarNaLista(EstiloMesaViva.LimitarLargura(cartao, LarguraCartaoSimples));
    }

    private void EditarPerfil()
    {
        using var formulario = new PerfilForm(_usuario, async (nome, email, telefone, endereco) =>
        {
            _usuario = await _auth.AtualizarDadosAsync(_usuario, nome, email, telefone, endereco);
        });
        if (formulario.ShowDialog(this) == DialogResult.OK)
        {
            AtualizarConta();
            MostrarPerfil();
            OperacaoNaTela.Informar(this, "Seus dados foram atualizados.");
        }
    }

    private void AtualizarConta()
    {
        _conta.Text = $"{_usuario.Nome} · {(EhEstabelecimento ? "Estabelecimento" : "Instituição")}";
    }

    private bool Confirmar(string pergunta)
    {
        return MessageBox.Show(this, pergunta, "MesaViva",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void AdicionarResumoEstabelecimento(List<OfertaComDisponibilidade> ofertas)
    {
        int reservasAtivas = ofertas.SelectMany(item => item.Oferta.Reservas)
            .Count(reserva => reserva.Status == Reserva.StatusReservada);
        DateTime agora = DateTime.Now;
        decimal disponivel = ofertas.Where(item => item.Oferta.PodeReservarEm(agora) && item.QuantidadeDisponivel > 0)
            .Sum(item => item.QuantidadeDisponivel);
        AdicionarIndicadores(("Ofertas publicadas", ofertas.Count.ToString()), ("Reservas ativas", reservasAtivas.ToString()),
            ("Alimentos disponíveis", Utils.FormatarQuantidade(disponivel)));
    }

    private void AdicionarResumoInstituicao(List<ReservaDetalhada> reservas, (string, string) primeiro)
    {
        int ativas = reservas.Count(item => item.Reserva.Status == Reserva.StatusReservada);
        decimal coletados = reservas.Where(item => item.Reserva.Status == Reserva.StatusColetada)
            .Sum(item => item.Reserva.QuantidadeReservada);
        AdicionarIndicadores(primeiro, ("Reservas ativas", ativas.ToString()), ("Alimentos coletados", Utils.FormatarQuantidade(coletados)));
    }

    private void AdicionarIndicadores(params (string Rotulo, string Valor)[] indicadores)
    {
        var cartoes = indicadores.Select(item =>
        {
            TableLayoutPanel cartao = EstiloMesaViva.Cartao();
            EstiloMesaViva.AdicionarLinha(cartao, EstiloMesaViva.Rotulo(item.Rotulo));
            Label valor = EstiloMesaViva.Titulo(item.Valor, 18);
            valor.ForeColor = EstiloMesaViva.Verde;
            valor.Margin = new Padding(0, 5, 0, 0);
            EstiloMesaViva.AdicionarLinha(cartao, valor);
            return cartao;
        }).ToArray();
        AdicionarNaLista(CriarGrade(cartoes, maximoColunas: 3, larguraMinima: 155, larguraMaxima: 325));
    }

    private void AdicionarEstadoVazio(string mensagem)
    {
        TableLayoutPanel cartao = EstiloMesaViva.Cartao(EstiloMesaViva.EspacoExterno);
        cartao.BackColor = EstiloMesaViva.VerdeSuave;
        Label aviso = EstiloMesaViva.Rotulo(mensagem);
        aviso.ForeColor = EstiloMesaViva.VerdeEscuro;
        EstiloMesaViva.AdicionarLinha(cartao, aviso);
        AdicionarNaLista(EstiloMesaViva.LimitarLargura(cartao, LarguraCartaoSimples));
    }

    private static TableLayoutPanel CriarGrade(IReadOnlyList<Control> cartoes, int maximoColunas, int larguraMinima, int larguraMaxima)
    {
        TableLayoutPanel grade = EstiloMesaViva.Coluna();
        (int Colunas, int Largura, int Espaco) medidasAtuais = default;
        void AjustarGrade()
        {
            int disponivel = grade.ClientSize.Width;
            if (!grade.IsHandleCreated || disponivel <= 0)
                return;

            int espaco = grade.LogicalToDeviceUnits(EstiloMesaViva.EspacoInterno);
            int colunas = Math.Clamp((disponivel + espaco) / (grade.LogicalToDeviceUnits(larguraMinima) + espaco),
                1, Math.Min(maximoColunas, cartoes.Count));
            int largura = Math.Min(grade.LogicalToDeviceUnits(larguraMaxima), (disponivel + espaco) / colunas - espaco);
            var medidas = (colunas, largura, espaco);
            if (medidas == medidasAtuais)
                return;

            bool reorganizar = colunas != medidasAtuais.Colunas || espaco != medidasAtuais.Espaco;
            medidasAtuais = medidas;
            grade.SuspendLayout();
            if (reorganizar)
            {
                grade.ColumnStyles.Clear();
                grade.RowStyles.Clear();
                grade.ColumnCount = colunas + 1;
                grade.RowCount = (cartoes.Count + colunas - 1) / colunas;
                for (int coluna = 0; coluna < colunas; coluna++)
                    grade.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute));
                grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int linha = 0; linha < grade.RowCount; linha++)
                    grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int i = 0; i < cartoes.Count; i++)
                {
                    int coluna = i % colunas;
                    cartoes[i].Margin = new Padding(0, 0, coluna == colunas - 1 ? 0 : espaco, espaco);
                    grade.SetCellPosition(cartoes[i], new TableLayoutPanelCellPosition(coluna, i / colunas));
                }
            }
            for (int coluna = 0; coluna < colunas; coluna++)
                grade.ColumnStyles[coluna].Width = largura + (coluna < colunas - 1 ? espaco : 0);
            grade.ResumeLayout();
        }

        grade.SuspendLayout();
        grade.Controls.AddRange(cartoes.ToArray());
        grade.HandleCreated += (_, _) => AjustarGrade();
        grade.ClientSizeChanged += (_, _) => AjustarGrade();
        grade.DpiChangedAfterParent += (_, _) =>
        {
            medidasAtuais = default;
            AjustarGrade();
        };
        grade.ResumeLayout();
        return grade;
    }

    private static TableLayoutPanel CriarCartao(string titulo)
    {
        TableLayoutPanel cartao = EstiloMesaViva.Cartao();
        cartao.Margin = new Padding(0, 0, 0, EstiloMesaViva.EspacoInterno);
        EstiloMesaViva.AdicionarLinha(cartao, EstiloMesaViva.Titulo(titulo, 15));
        return cartao;
    }

    private static void AdicionarDetalhe(TableLayoutPanel cartao, string texto, bool destaque = false)
    {
        Label detalhe = destaque ? EstiloMesaViva.Rotulo(texto) : EstiloMesaViva.Legenda(texto);
        detalhe.Margin = new Padding(0, 0, 0, 4);
        EstiloMesaViva.AdicionarLinha(cartao, detalhe);
    }

    private static void AdicionarAcoes(TableLayoutPanel cartao, params Button[] botoes)
    {
        var acoes = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            Margin = new Padding(0, EstiloMesaViva.Espaco, 0, 0)
        };
        acoes.Controls.AddRange(botoes);
        EstiloMesaViva.AdicionarLinha(cartao, acoes);
    }
}
