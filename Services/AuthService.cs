using System.Net.Mail;
using System.Security.Cryptography;
using MesaViva.Data;
using MesaViva.Models;
using Microsoft.EntityFrameworkCore;

namespace MesaViva.Services;

public class AuthService
{
    private const int IteracoesHash = 600_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const string PrefixoHash = "PBKDF2-SHA256";

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

        string emailNormalizado = email.Trim().ToLowerInvariant();
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

        // O índice único já definido no AppDbContext protege contra cadastros simultâneos.
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<Usuario?> FazerLoginAsync(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        string emailNormalizado = email.Trim().ToLowerInvariant();
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

        if (string.IsNullOrWhiteSpace(email)
            || !MailAddress.TryCreate(email.Trim(), out MailAddress? enderecoEmail)
            || !string.Equals(enderecoEmail.Address, email.Trim(), StringComparison.Ordinal))
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

    private static bool VerificarSenha(string senha, string senhaHash)
    {
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
