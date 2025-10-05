using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Desafio_BT.Utils;

namespace Desafio_BT.Services;

public class TwelveDataService : ITwelveDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwelveDataService> _logger;
    private readonly string _apiKey;

    public TwelveDataService(HttpClient httpClient, ILogger<TwelveDataService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config.GetValue<string>("ApiKey") ?? throw new InvalidOperationException("ApiKey não configurada");
    }

    public async Task<decimal> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            ValidateSymbol(symbol);
            var response = await FetchPriceDataAsync(symbol);
            var priceData = DeserializePriceResponse(response);
            return ParsePrice(priceData, symbol);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao consultar API para {Symbol}", LoggingUtils.SanitizeForLogging(symbol));
            throw new InvalidOperationException($"Falha na consulta da API para {symbol}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout na consulta da API para {Symbol}", LoggingUtils.SanitizeForLogging(symbol));
            throw new InvalidOperationException($"Timeout na consulta da API para {symbol}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da API para {Symbol}", LoggingUtils.SanitizeForLogging(symbol));
            throw new InvalidOperationException($"Resposta inválida da API para {symbol}", ex);
        }
        catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Erro inesperado ao obter preço para {Symbol}", LoggingUtils.SanitizeForLogging(symbol));
            throw new InvalidOperationException($"Erro inesperado ao consultar preço para {symbol}", ex);
        }
    }

    private static void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Símbolo da ação não pode ser vazio", nameof(symbol));
        if (symbol.Length > 10)
            throw new ArgumentException("Símbolo da ação muito longo", nameof(symbol));
    }

    private async Task<string> FetchPriceDataAsync(string symbol)
    {
        var url = $"https://api.twelvedata.com/price?symbol={symbol}&apikey={_apiKey}";
        var response = await _httpClient.GetStringAsync(url);
        return response;
    }

    private static PriceResponse DeserializePriceResponse(string response)
    {
        var priceData = JsonSerializer.Deserialize<PriceResponse>(response);
        return priceData ?? new PriceResponse();
    }

    private static decimal ParsePrice(PriceResponse priceData, string symbol)
    {
        if (decimal.TryParse(priceData.Price, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            return price;
        }
        
        throw new InvalidOperationException($"Preço inválido retornado pela API para {LoggingUtils.SanitizeForLogging(symbol)}. Price: '{LoggingUtils.SanitizeForLogging(priceData.Price)}'");
    }

    private sealed class PriceResponse
    {
        [JsonPropertyName("price")]
        public string Price { get; set; } = string.Empty;
    }
}