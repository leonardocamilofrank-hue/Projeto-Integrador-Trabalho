using System.Net.Mail;

namespace MesaViva.Models;

public class Usuario
{
    public const string TipoEstabelecimento = "ESTABELECIMENTO";
    public const string TipoInstituicao = "INSTITUICAO";
    internal const string ExemploTelefone = "(48) 99624-7660";

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

        if (!TentarFormatarTelefone(telefone, out string telefoneFormatado))
        {
            throw new ArgumentException(
                $"Informe um telefone com 11 dígitos, incluindo o DDD (ex.: {ExemploTelefone}).",
                nameof(telefone));
        }

        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw new ArgumentException("O endereço é obrigatório.", nameof(endereco));
        }

        Nome = nome.Trim();
        Email = emailNormalizado;
        Telefone = telefoneFormatado;
        Endereco = endereco.Trim();
    }

    internal static bool TentarFormatarTelefone(string? telefone, out string formatado)
    {
        formatado = string.Empty;
        string digitos = new((telefone ?? string.Empty)
            .Where(caractere => caractere is not (' ' or '(' or ')' or '-')).ToArray());

        if (digitos.Length != 11 || !digitos.All(char.IsAsciiDigit))
            return false;

        formatado = $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}";
        return true;
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
