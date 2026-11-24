using MesaViva.Data;
using MesaViva.Services;
using MesaViva.UI;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MESAVIVA_DB")))
{
    Console.WriteLine("Configure a variável de ambiente MESAVIVA_DB antes de iniciar o programa.");
    return;
}

await using var context = new AppDbContext();
var authService = new AuthService(context);
var ofertaService = new OfertaService();
var reservaService = new ReservaService();

try
{
    await MenuInicial.ExibirAsync(authService, ofertaService, reservaService);
}
catch (EndOfStreamException)
{
    Console.WriteLine("Entrada encerrada.");
}
