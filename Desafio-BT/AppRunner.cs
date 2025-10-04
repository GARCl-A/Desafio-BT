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

    public async Task<int> RunAsync(string[] args)
    {
        _logger.LogInformation("Aplicação iniciada.");

        if (args.Length != 3)
        {
            _logger.LogWarning("Uso: <app> ATIVO PRECO_VENDA PRECO_COMPRA");
            return 1;
        }

        var ativo = SanitizeInput(args[0]);
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoVenda) ||
            !decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoCompra))
        {
            _logger.LogError("Preços devem ser valores numéricos válidos");
            return 2;
        }

        var destinationEmail = _config.GetValue<string>("DestinationEmail");
        if (string.IsNullOrEmpty(destinationEmail))
        {
            _logger.LogError("Email de destino não configurado");
            return 3;
        }

        _logger.LogDebug("Processando ativo: {Ativo}, Venda: {PrecoVenda}, Compra: {PrecoCompra}", LoggingUtils.SanitizeForLogging(ativo), precoVenda, precoCompra);

        try
        {
            _logger.LogInformation("Enviando e-mail...");
            await _emailService.SendEmailAsync(
                destinationEmail,
                 $"Alerta de Preço para o Ativo: {LoggingUtils.SanitizeForLogging(ativo)}",
                $"Uma operação foi sugerida para o ativo {LoggingUtils.SanitizeForLogging(ativo)} com preço de venda {precoVenda:C} e preço de compra {precoCompra:C}."
            );
            _logger.LogInformation("E-mail enviado com sucesso para {Email}", LoggingUtils.SanitizeForLogging(destinationEmail));
            return 0;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Erro de configuração: {Message}", LoggingUtils.SanitizeForLogging(ex.Message));
            return 4;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro de operação: {Message}", LoggingUtils.SanitizeForLogging(ex.Message));
            return 5;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar e-mail");
            return 6;
        }
    }

    private static string SanitizeInput(string input)
    {
        return string.IsNullOrWhiteSpace(input) ? "INVALID" : input.Trim().Replace("\n", "").Replace("\r", "");
    }
}