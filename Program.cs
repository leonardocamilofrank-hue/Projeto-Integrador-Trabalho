using MesaViva.Data;
using MesaViva.Services;
using MesaViva.UI;

namespace Projeto_Integrador_Trabalho;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using (var conexao = new ConexaoForm(ConexaoMySql.LerConfiguracao()))
        {
            if (conexao.ShowDialog() != DialogResult.OK)
                return;

            Environment.SetEnvironmentVariable(ConexaoMySql.Variavel, conexao.ConfiguracaoValidada,
                EnvironmentVariableTarget.Process);
        }

        using var context = new AppDbContext();
        using var login = new LoginForm(new AuthService(context));
        Application.Run(login);
    }
}
