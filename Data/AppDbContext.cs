using Microsoft.EntityFrameworkCore;
using MesaViva.Models;

namespace MesaViva.Data;

public class AppDbContext : DbContext
{
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Oferta> Ofertas { get; set; } = null!;
    public DbSet<Reserva> Reservas { get; set; } = null!;

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        string conexao = Environment.GetEnvironmentVariable("MESAVIVA_DB")
            ?? throw new InvalidOperationException(
                "Configure a variável MESAVIVA_DB.");

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
            .WithMany()
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
