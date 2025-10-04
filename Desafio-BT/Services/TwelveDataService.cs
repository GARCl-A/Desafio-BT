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
            var response = await FetchPriceDataAsync(symbol);
            var priceData = DeserializePriceResponse(response);
            return ParsePrice(priceData, symbol);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao consultar API para {Symbol}", LoggingUtils.SanitizeForLogging(symbol));
            throw new InvalidOperationException($"Falha na consulta da API para {symbol}", ex);
        }
    }

    private async Task<string> FetchPriceDataAsync(string symbol)
    {
        var url = $"https://api.twelvedata.com/price?symbol={symbol}&apikey={_apiKey}";
        var response = await _httpClient.GetStringAsync(url);
        _logger.LogInformation("Resposta da API para {Symbol}: {Response}", LoggingUtils.SanitizeForLogging(symbol), response);
        return response;
    }

    private PriceResponse DeserializePriceResponse(string response)
    {
        var priceData = JsonSerializer.Deserialize<PriceResponse>(response);
        _logger.LogInformation("Price deserializado: '{Price}'", priceData?.Price ?? "null");
        return priceData ?? new PriceResponse();
    }

    private decimal ParsePrice(PriceResponse priceData, string symbol)
    {
        if (decimal.TryParse(priceData.Price, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var price))
        {
            _logger.LogInformation("Preço obtido para {Symbol}: {Price}", LoggingUtils.SanitizeForLogging(symbol), price);
            return price;
        }
        
        throw new InvalidOperationException($"Preço inválido retornado pela API para {symbol}. Price: '{priceData.Price}'");
    }

    private sealed class PriceResponse
    {
        [JsonPropertyName("price")]
        public string Price { get; set; } = string.Empty;
    }
}