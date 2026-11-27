using Microsoft.EntityFrameworkCore;
using MesaViva.Models;

namespace MesaViva.Data;

public class AppDbContext : DbContext
{
    public AppDbContext() { }

    internal AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Oferta> Ofertas { get; set; } = null!;
    public DbSet<Reserva> Reservas { get; set; } = null!;

    internal async Task<Oferta?> BuscarOfertaParaAtualizarAsync(int ofertaId)
    {
        // A linha da oferta coordena as alterações que afetam suas reservas.
        var ofertas = await Ofertas
            .FromSql($"SELECT * FROM `Ofertas` WHERE `Id` = {ofertaId} FOR UPDATE")
            .ToListAsync();

        return ofertas.SingleOrDefault();
    }

    internal async Task ComBloqueioTelefoneAsync(string digitos, Func<Task> salvar)
    {
        // O bloqueio por número fica na mesma conexão usada para consultar e gravar o cadastro.
        await Database.OpenConnectionAsync();
        try
        {
            int? obtido = await Database.SqlQuery<int?>(
                $"SELECT GET_LOCK(SHA2(CONCAT(DATABASE(), ':mesaviva:telefone:', {digitos}), 256), 10) AS Value")
                .SingleAsync();
            if (obtido != 1)
                throw new InvalidOperationException("Não foi possível validar o telefone agora. Tente novamente.");

            try
            {
                await salvar();
            }
            finally
            {
                await Database.SqlQuery<int?>(
                    $"SELECT RELEASE_LOCK(SHA2(CONCAT(DATABASE(), ':mesaviva:telefone:', {digitos}), 256)) AS Value")
                    .SingleAsync();
            }
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        string conexao = ConexaoMySql.LerConfiguracao()
            ?? throw new InvalidOperationException("Configure a variável MESAVIVA_DB para usar os comandos do EF Core.");
        optionsBuilder.UseMySQL(conexao);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Não permitir e-mails duplicados
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Uma oferta pertence a um estabelecimento
        modelBuilder.Entity<Oferta>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(o => o.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uma reserva pertence a uma oferta
        modelBuilder.Entity<Reserva>()
            .HasOne<Oferta>()
            .WithMany(oferta => oferta.Reservas)
            .HasForeignKey(r => r.OfertaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Uma reserva pertence a uma instituição
        modelBuilder.Entity<Reserva>()
            .HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.InstituicaoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Oferta>()
            .Property(o => o.QuantidadeTotal)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Reserva>()
            .Property(r => r.QuantidadeReservada)
            .HasPrecision(10, 2);
    }
}
