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

    public static string? NormalizarTipo(string? tipo)
    {
        return tipo?.Trim().ToUpperInvariant() switch
        {
            TipoEstabelecimento => TipoEstabelecimento,
            TipoInstituicao or "INSTITUIÇÃO" => TipoInstituicao,
            _ => null
        };
    }

    internal void ExigirTipo(string tipo)
    {
        if (Id <= 0 || NormalizarTipo(Tipo) != tipo)
        {
            throw new InvalidOperationException("Seu tipo de conta não permite esta operação.");
        }
    }

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

        if (!TentarNormalizarEmail(email, out string emailNormalizado))
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
        Email = emailNormalizado;
        Telefone = telefone.Trim();
        Endereco = endereco.Trim();
    }

    internal static bool TentarNormalizarEmail(string? email, out string emailNormalizado)
    {
        emailNormalizado = string.Empty;
        string? emailAparado = email?.Trim();
        if (string.IsNullOrEmpty(emailAparado) || emailAparado.Length > 255
            || !MailAddress.TryCreate(emailAparado, out MailAddress? endereco)
            || !string.IsNullOrEmpty(endereco.DisplayName)
            || !string.Equals(endereco.Address, emailAparado, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        emailNormalizado = endereco.Address.ToLowerInvariant();
        return true;
    }
}
