using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

public class AppRunner
{
    private readonly ILogger<AppRunner> _logger;
    private readonly EmailService _emailService;
    private readonly IConfiguration _config;

    public AppRunner(ILogger<AppRunner> logger, EmailService emailService, IConfiguration config)
    {
        _logger = logger;
        _emailService = emailService;
        _config = config;
    }

    public async Task RunAsync(string[] args)
    {
        _logger.LogInformation("Aplicação iniciada.");

        if (args.Length != 3)
        {
            _logger.LogWarning("Uso: <app> ATIVO PRECO_VENDA PRECO_COMPRA");
            return;
        }

        var ativo = SanitizeInput(args[0]);
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoVenda) ||
            !decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoCompra))
        {
            _logger.LogError("Preços devem ser valores numéricos válidos");
            return;
        }

        var destinationEmail = _config.GetValue<string>("DestinationEmail");

        _logger.LogDebug("Processando ativo: {Ativo}, Venda: {PrecoVenda}, Compra: {PrecoCompra}", ativo, precoVenda, precoCompra);

        try
        {
            _logger.LogInformation("Enviando e-mail...");
            await _emailService.SendEmailAsync(
                destinationEmail!,
                $"Alerta de Preço para o Ativo: {ativo}",
                $"Uma operação foi sugerida para o ativo {ativo} com preço de venda {precoVenda:C} e preço de compra {precoCompra:C}."
            );
            _logger.LogInformation("E-mail enviado com sucesso para {Email}", destinationEmail);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Erro de configuração: {Message}", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro de operação: {Message}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar e-mail");
        }
    }

    private static string SanitizeInput(string input)
    {
        return string.IsNullOrWhiteSpace(input) ? "INVALID" : input.Trim().Replace("\n", "").Replace("\r", "");
    }
}