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
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MESAVIVA_DB")))
        {
            MessageBox.Show(
                "Configure a variável de ambiente MESAVIVA_DB com a conexão do MySQL antes de iniciar o MesaViva.",
                "Configuração necessária", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var context = new AppDbContext();
        using var login = new LoginForm(new AuthService(context));
        Application.Run(login);
    }
}
