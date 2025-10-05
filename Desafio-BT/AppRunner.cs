using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Desafio_BT.Services;
using Desafio_BT.Utils;

namespace Desafio_BT;

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


        var validationResult = ValidateArguments(args);
        if (validationResult != 0) return validationResult;

        var (ativo, precoVenda, precoCompra) = await ParseArgumentsAsync(args);
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

    private async Task<(string ativo, decimal precoVenda, decimal precoCompra)> ParseArgumentsAsync(string[] args)
    {
        try
        {
            ValidateArgumentsArray(args);
            var ativo = SanitizeInput(args[0]);
            var (precoVenda, precoCompra) = await ParsePricesAsync(args[1], args[2]);
            ValidatePriceLogic(precoCompra, precoVenda);
            return (ativo, precoVenda, precoCompra);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar argumentos");
            throw new ArgumentException("Erro ao processar argumentos", ex);
        }
    }

    private static void ValidateArgumentsArray(string[] args)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentException("Argumentos não fornecidos");
        
        for (int i = 0; i < args.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(args[i]))
                throw new ArgumentException($"Argumento {i + 1} está vazio");
        }
    }

    private async Task<(decimal precoVenda, decimal precoCompra)> ParsePricesAsync(string precoVendaStr, string precoCompraStr)
    {
        await Task.Yield();
        
        if (!decimal.TryParse(precoVendaStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var precoVenda) ||
            !decimal.TryParse(precoCompraStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var precoCompra))
        {
            _logger.LogError("Preços devem ser valores numéricos válidos. Entrada1: {Input1}, Entrada2: {Input2}", 
                LoggingUtils.SanitizeForLogging(precoVendaStr), LoggingUtils.SanitizeForLogging(precoCompraStr));
            throw new ArgumentException("Preços inválidos");
        }
        
        return (precoVenda, precoCompra);
    }

    private void ValidatePriceLogic(decimal precoCompra, decimal precoVenda)
    {
        if (precoCompra >= precoVenda)
        {
            _logger.LogError("Preço de compra deve ser menor que preço de venda. Compra: {PrecoCompra}, Venda: {PrecoVenda}", 
                precoCompra, precoVenda);
            throw new ArgumentException("Preço de compra deve ser menor que preço de venda");
        }
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
        Console.WriteLine($"\n📊 Monitorando {ativo} - Venda: {precoVenda:C} | Compra: {precoCompra:C}");
        Console.WriteLine("🔄 Verificando preços a cada 15 segundos... (Ctrl+C para parar)\n");
        
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        
        try
        {
            await MonitorAsset(ativo, precoVenda, precoCompra, destinationEmail, cts.Token);
            
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                await MonitorAsset(ativo, precoVenda, precoCompra, destinationEmail, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n👋 Monitoramento encerrado. Até logo!");
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
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - {ativo}: {precoAtual:C}");
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
        var emoji = acao == "COMPRAR" ? "🟢" : "🔴";
        var subject = $"{emoji} ALERTA {LoggingUtils.SanitizeForLogging(acao)} - {LoggingUtils.SanitizeForLogging(ativo)}";
        var body = $@"📊 OPORTUNIDADE DETECTADA!

{emoji} AÇÃO RECOMENDADA: {LoggingUtils.SanitizeForLogging(acao)}
💰 Ativo: {LoggingUtils.SanitizeForLogging(ativo)}
💵 Preço Atual: {precoAtual:C}

📈 SEUS LIMITES:
• Venda: {precoVenda:C}
• Compra: {precoCompra:C}

⏰ Detectado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}

---
Monitor de Ações - Sistema Automatizado";

        await _emailService.SendEmailAsync(destinationEmail, subject, body);
        Console.WriteLine($"\n📧 ALERTA ENVIADO! {acao} {ativo} por {precoAtual:C}\n");
    }

    private static string SanitizeInput(string input)
    {
        return string.IsNullOrWhiteSpace(input) ? "INVALID" : input.Trim().Replace("\n", "").Replace("\r", "");
    }
}