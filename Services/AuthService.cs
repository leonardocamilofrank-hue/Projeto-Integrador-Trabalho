using System.Net.Mail;
using System.Security.Cryptography;
using MesaViva.Data;
using MesaViva.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace MesaViva.Services;

public class AuthService
{
    private const int IteracoesHash = 600_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const string PrefixoHash = "PBKDF2-SHA256";
    private const string IndiceEmail = "IX_Usuarios_Email";

    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario> CadastrarUsuarioAsync(
        string nome,
        string email,
        string senha,
        string telefone,
        string endereco,
        string tipo)
    {
        ValidarCadastro(nome, email, senha, telefone, endereco, tipo);
        _ = TentarNormalizarEmail(email, out string emailNormalizado);

        bool emailJaCadastrado = await _context.Usuarios
            .AnyAsync(usuario => usuario.Email == emailNormalizado);

        if (emailJaCadastrado)
        {
            throw new InvalidOperationException("Este e-mail já está cadastrado.");
        }

        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = GerarHashSenha(senha),
            Telefone = telefone.Trim(),
            Endereco = endereco.Trim(),
            Tipo = NormalizarTipo(tipo)
        };

        _context.Usuarios.Add(usuario);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (EhViolacaoDeEmailDuplicado(exception))
        {
            _context.Entry(usuario).State = EntityState.Detached;
            throw new InvalidOperationException(
                "Este e-mail já está cadastrado.",
                exception);
        }

        return usuario;
    }

    public async Task<Usuario?> FazerLoginAsync(string email, string senha)
    {
        if (!TentarNormalizarEmail(email, out string emailNormalizado)
            || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        Usuario? usuario = await _context.Usuarios
            .FirstOrDefaultAsync(item => item.Email == emailNormalizado);

        if (usuario is null || !VerificarSenha(senha, usuario.SenhaHash))
        {
            return null;
        }

        return usuario;
    }

    private static void ValidarCadastro(
        string nome,
        string email,
        string senha,
        string telefone,
        string endereco,
        string tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome é obrigatório.", nameof(nome));
        }

        if (!TentarNormalizarEmail(email, out _))
        {
            throw new ArgumentException("Informe um e-mail válido.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new ArgumentException("A senha é obrigatória.", nameof(senha));
        }

        if (string.IsNullOrWhiteSpace(telefone))
        {
            throw new ArgumentException("O telefone é obrigatório.", nameof(telefone));
        }

        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw new ArgumentException("O endereço é obrigatório.", nameof(endereco));
        }

        _ = NormalizarTipo(tipo);
    }

    private static bool TentarNormalizarEmail(string? email, out string emailNormalizado)
    {
        emailNormalizado = string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        string emailAparado = email.Trim();
        if (!MailAddress.TryCreate(emailAparado, out MailAddress? endereco)
            || !string.IsNullOrEmpty(endereco.DisplayName)
            || !string.Equals(endereco.Address, emailAparado, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        emailNormalizado = endereco.Address.ToLowerInvariant();
        return true;
    }

    private static string NormalizarTipo(string tipo)
    {
        if (string.Equals(tipo?.Trim(), "Estabelecimento", StringComparison.OrdinalIgnoreCase))
        {
            return "Estabelecimento";
        }

        if (string.Equals(tipo?.Trim(), "Instituição", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tipo?.Trim(), "Instituicao", StringComparison.OrdinalIgnoreCase))
        {
            return "Instituição";
        }

        throw new ArgumentException(
            "O tipo deve ser Estabelecimento ou Instituição.",
            nameof(tipo));
    }

    private static bool EhViolacaoDeEmailDuplicado(DbUpdateException exception)
    {
        for (Exception? erro = exception; erro is not null; erro = erro.InnerException)
        {
            if (erro is MySqlException erroMySql
                && erroMySql.Number == 1062
                && erroMySql.Message.Contains(IndiceEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GerarHashSenha(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            IteracoesHash,
            HashAlgorithmName.SHA256,
            TamanhoHash);

        return string.Join(
            '$',
            PrefixoHash,
            IteracoesHash,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    private static bool VerificarSenha(string? senha, string? senhaHash)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(senhaHash))
        {
            return false;
        }

        string[] partes = senhaHash.Split('$');
        if (partes.Length != 4
            || partes[0] != PrefixoHash
            || !int.TryParse(partes[1], out int iteracoes)
            || iteracoes != IteracoesHash)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashArmazenado = Convert.FromBase64String(partes[3]);
            if (salt.Length != TamanhoSalt || hashArmazenado.Length != TamanhoHash)
            {
                return false;
            }

            byte[] hashInformado = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                salt,
                iteracoes,
                HashAlgorithmName.SHA256,
                TamanhoHash);

            return CryptographicOperations.FixedTimeEquals(hashArmazenado, hashInformado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
