using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Desafio_BT.Services;
using Desafio_BT.Utils;

public class AppRunner
{
    private readonly ILogger<AppRunner> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ITwelveDataService _twelveDataService;

    public AppRunner(ILogger<AppRunner> logger, IEmailService emailService, IConfiguration config, ITwelveDataService twelveDataService)
    {
        _logger = logger;
        _emailService = emailService;
        _config = config;
        _twelveDataService = twelveDataService;
    }

    public async Task<int> RunAsync(string[] args)
    {
        _logger.LogInformation("Aplicação iniciada.");

        var validationResult = ValidateArguments(args);
        if (validationResult != 0) return validationResult;

        var (ativo, precoVenda, precoCompra) = ParseArguments(args);
        var destinationEmail = GetDestinationEmail();
        if (destinationEmail == null) return 3;

        await StartMonitoring(ativo, precoVenda, precoCompra, destinationEmail);
        return 0;
    }

    private int ValidateArguments(string[] args)
    {
        if (args.Length != 3)
        {
            _logger.LogWarning("Uso: <app> ATIVO PRECO_VENDA  PRECO_COMPRA");
            return 1;
        }
        return 0;
    }

    private (string ativo, decimal precoVenda, decimal precoCompra) ParseArguments(string[] args)
    {
        var ativo = SanitizeInput(args[0]);
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoVenda) ||
            !decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var precoCompra))
        {
            _logger.LogError("Preços devem ser valores numéricos válidos");
            throw new ArgumentException("Preços inválidos");
        }
        return (ativo, precoVenda, precoCompra);
    }

    private string? GetDestinationEmail()
    {
        var destinationEmail = _config.GetValue<string>("DestinationEmail");
        if (string.IsNullOrEmpty(destinationEmail))
        {
            _logger.LogError("Email de destino não configurado");
            return null;
        }
        return destinationEmail;
    }

    private async Task StartMonitoring(string ativo, decimal precoVenda, decimal precoCompra, string destinationEmail)
    {
        _logger.LogInformation("Iniciando monitoramento do ativo {Ativo}. Pressione Ctrl+C para parar.", LoggingUtils.SanitizeForLogging(ativo));
        
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        
        using var timer = new Timer(async _ => await MonitorAsset(ativo, precoVenda, precoCompra, destinationEmail, cts.Token), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        
        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Monitoramento interrompido pelo usuário");
        }
    }

    private async Task MonitorAsset(string ativo, decimal precoVenda, decimal precoCompra, string destinationEmail, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;
        
        var precoAtual = await GetPriceWithRetry(ativo);
        if (precoAtual.HasValue)
        {
            await ProcessPriceAlert(ativo, precoAtual.Value, precoVenda, precoCompra, destinationEmail);
        }
    }

    private async Task<decimal?> GetPriceWithRetry(string ativo)
    {
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _twelveDataService.GetCurrentPriceAsync(ativo);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Tentativa {Attempt}/{MaxRetries} falhou para {Ativo}. Tentando novamente...", attempt, maxRetries, LoggingUtils.SanitizeForLogging(ativo));
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no monitoramento do ativo {Ativo} após {MaxRetries} tentativas", LoggingUtils.SanitizeForLogging(ativo), maxRetries);
                return null;
            }
        }
        return null;
    }

    private async Task ProcessPriceAlert(string ativo, decimal precoAtual, decimal precoVenda, decimal precoCompra, string destinationEmail)
    {
        var acao = GetAlertAction(precoAtual, precoVenda, precoCompra);
        if (acao != null)
        {
            await SendPriceAlert(ativo, precoAtual, precoVenda, precoCompra, destinationEmail, acao);
        }
        else
        {
            _logger.LogInformation("Preço monitorado - {Ativo}: {Preco} (sem alerta)", LoggingUtils.SanitizeForLogging(ativo), precoAtual);
        }
    }

    private static string? GetAlertAction(decimal precoAtual, decimal precoVenda, decimal precoCompra)
    {
        if (precoAtual <= precoCompra) return "COMPRAR";
        if (precoAtual >= precoVenda) return "VENDER";
        return null;
    }

    private async Task SendPriceAlert(string ativo, decimal precoAtual, decimal precoVenda, decimal precoCompra, string destinationEmail, string acao)
    {
        await _emailService.SendEmailAsync(
            destinationEmail,
            $"Alerta {acao} - {LoggingUtils.SanitizeForLogging(ativo)}",
            $"Ação: {acao}\nPreço atual: {precoAtual:C}\nPreço de venda: {precoVenda:C}\nPreço de compra: {precoCompra:C}\nHorário: {DateTime.Now:HH:mm:ss}"
        );
        _logger.LogInformation("Email enviado - {Ativo}: {Preco} - {Acao}", LoggingUtils.SanitizeForLogging(ativo), precoAtual, acao);
    }

    private static string SanitizeInput(string input)
    {
        return string.IsNullOrWhiteSpace(input) ? "INVALID" : input.Trim().Replace("\n", "").Replace("\r", "");
    }
}