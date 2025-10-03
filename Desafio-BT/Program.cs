using Microsoft.Extensions.Configuration;

if (args.Length != 3)
{
    Console.WriteLine("Uso: <app> ATIVO PRECO_VENDA PRECO_COMPRA");
    return;
}

var ativo = args[0];
var precoVenda = args[1];
var precoCompra = args[2];

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>();

if (emailSettings == null || string.IsNullOrEmpty(emailSettings.SenderEmail))
{
    Console.WriteLine("Erro: As configurações de e-mail não foram encontradas no appsettings.json.");
    return;
}

var destinationEmail = configuration.GetSection("DestinationEmail").Get<string>();

if (string.IsNullOrEmpty(destinationEmail))
{
    Console.WriteLine("Erro: O e-mail de destino não foi encontrado no appsettings.json.");
    return;
}

var emailService = new EmailService(
    emailSettings.SmtpServer,
    emailSettings.Port,
    emailSettings.SenderEmail,
    emailSettings.Password
);

try
{
    Console.WriteLine("Enviando e-mail...");
    await emailService.SendEmailAsync(
        destinationEmail,
        $"Alerta de Preço para o Ativo: {ativo}",
        $"Uma operação foi sugerida para o ativo {ativo} com preço de venda {precoVenda} e preço de compra {precoCompra}."
    );
    Console.WriteLine("E-mail enviado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"Ocorreu um erro ao enviar o e-mail: {ex.Message}");
}
