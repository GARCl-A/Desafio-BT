using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

public class AppRunner
{
    private readonly ILogger<AppRunner> _logger;
    private readonly EmailService _emailService;
    private readonly IConfiguration _config;
    private readonly TwelveDataService _twelveDataService;

    public AppRunner(ILogger<AppRunner> logger, EmailService emailService, IConfiguration config, TwelveDataService twelveDataService)
    {
        _logger = logger;
        _emailService = emailService;
        _config = config;
        _twelveDataService = twelveDataService;
    }

    public async Task<int> RunAsync(string[] args)
    {
        _logger.LogInformation("Aplicação iniciada.");

        if (args.Length != 3)
        {
            _logger.LogWarning("Uso: <app> ATIVO PRECO_VENDA  PRECO_COMPRA");
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

        _logger.LogInformation("Iniciando monitoramento do ativo {Ativo}. Pressione Ctrl+C para parar.", LoggingUtils.SanitizeForLogging(ativo));
        
        using var timer = new Timer(async _ => await MonitorAsset(ativo, precoVenda, precoCompra, destinationEmail), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        
        await Task.Run(() => Console.ReadKey());
        return 0;
    }

    private async Task MonitorAsset(string ativo, decimal precoVenda, decimal precoCompra, string destinationEmail)
    {
        try
        {
            var precoAtual = await _twelveDataService.GetCurrentPriceAsync(ativo);
            
            if (precoAtual <= precoCompra || precoAtual >= precoVenda)
            {
                var acao = precoAtual <= precoCompra ? "COMPRAR" : "VENDER";
                await _emailService.SendEmailAsync(
                    destinationEmail,
                    $"Alerta {acao} - {LoggingUtils.SanitizeForLogging(ativo)}",
                    $"Ação: {acao}\nPreço atual: {precoAtual:C}\nPreço de venda: {precoVenda:C}\nPreço de compra: {precoCompra:C}\nHorário: {DateTime.Now:HH:mm:ss}"
                );
                _logger.LogInformation("Email enviado - {Ativo}: {Preco} - {Acao}", LoggingUtils.SanitizeForLogging(ativo), precoAtual, acao);
            }
            else
            {
                _logger.LogInformation("Preço monitorado - {Ativo}: {Preco} (sem alerta)", LoggingUtils.SanitizeForLogging(ativo), precoAtual);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no monitoramento do ativo {Ativo}", LoggingUtils.SanitizeForLogging(ativo));
        }
    }

    private static string SanitizeInput(string input)
    {
        return string.IsNullOrWhiteSpace(input) ? "INVALID" : input.Trim().Replace("\n", "").Replace("\r", "");
    }
}