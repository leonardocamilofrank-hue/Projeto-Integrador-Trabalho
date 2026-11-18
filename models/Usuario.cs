using System.Net.Mail;

namespace MesaViva.Models;

public class Usuario
{
    public const string TipoEstabelecimento = "ESTABELECIMENTO";
    public const string TipoInstituicao = "INSTITUICAO";

    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;

    public void AtualizarDados(string nome, string email, string telefone, string endereco)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome é obrigatório.", nameof(nome));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("O e-mail é obrigatório.", nameof(email));
        }

        var emailSemEspacos = email.Trim();
        if (!MailAddress.TryCreate(emailSemEspacos, out var enderecoEmail)
            || enderecoEmail.Address != emailSemEspacos
            || !enderecoEmail.Host.Contains('.'))
        {
            throw new ArgumentException("Informe um e-mail válido, como nome@exemplo.com.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new ArgumentException("O telefone é obrigatório.", nameof(telefone));
        }

        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw new ArgumentException("O endereço é obrigatório.", nameof(endereco));
        }

        Nome = nome.Trim();
        Email = emailSemEspacos;
        Telefone = telefone.Trim();
        Endereco = endereco.Trim();
    }
}
