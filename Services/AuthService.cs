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
        var usuario = new Usuario();
        usuario.AtualizarDados(nome, email, telefone, endereco);
        usuario.Tipo = Usuario.NormalizarTipo(tipo)
            ?? throw new ArgumentException("O tipo deve ser Estabelecimento ou Instituição.", nameof(tipo));

        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new ArgumentException("A senha é obrigatória.", nameof(senha));
        }

        await VerificarEmailDisponivelAsync(usuario.Email);
        usuario.SenhaHash = GerarHashSenha(senha);
        _context.Usuarios.Add(usuario);

        try
        {
            await SalvarUsuarioAsync();
        }
        finally
        {
            _context.Entry(usuario).State = EntityState.Detached;
        }

        return usuario;
    }

    public async Task<Usuario> AtualizarDadosAsync(
        Usuario usuario, string nome, string email, string telefone, string endereco)
    {
        if (usuario.Id <= 0 || Usuario.NormalizarTipo(usuario.Tipo) is null)
        {
            throw new InvalidOperationException("Entre em uma conta para editar seus dados.");
        }

        Usuario cadastro = await _context.Usuarios.SingleOrDefaultAsync(item => item.Id == usuario.Id)
            ?? throw new InvalidOperationException("Conta não encontrada.");

        try
        {
            cadastro.AtualizarDados(nome, email, telefone, endereco);
            await VerificarEmailDisponivelAsync(cadastro.Email, cadastro.Id);
            await SalvarUsuarioAsync();
        }
        finally
        {
            _context.Entry(cadastro).State = EntityState.Detached;
        }

        cadastro.Tipo = Usuario.NormalizarTipo(cadastro.Tipo) ?? cadastro.Tipo;
        return cadastro;
    }

    private async Task VerificarEmailDisponivelAsync(string email, int? usuarioId = null)
    {
        bool emailJaCadastrado = await _context.Usuarios.AnyAsync(item =>
            item.Email == email && (usuarioId == null || item.Id != usuarioId));

        if (emailJaCadastrado)
        {
            throw new InvalidOperationException("Este e-mail já está cadastrado.");
        }
    }

    private async Task SalvarUsuarioAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (EhViolacaoDeEmailDuplicado(exception))
        {
            throw new InvalidOperationException("Este e-mail já está cadastrado.", exception);
        }
    }

    public async Task<Usuario?> FazerLoginAsync(string email, string senha)
    {
        if (!Usuario.TentarNormalizarEmail(email, out string emailNormalizado)
            || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        Usuario? usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Email == emailNormalizado);

        if (usuario is null || !VerificarSenha(senha, usuario.SenhaHash))
        {
            return null;
        }

        usuario.Tipo = Usuario.NormalizarTipo(usuario.Tipo) ?? usuario.Tipo;
        return usuario;
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
