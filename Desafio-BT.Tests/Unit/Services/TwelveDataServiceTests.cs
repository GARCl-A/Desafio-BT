using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Desafio_BT.Services;
using System.Net;

namespace Desafio_BT.Tests.Unit.Services;

public class TwelveDataServiceTests
{
    [Fact]
    public void Constructor_MissingApiKey_ThrowsException()
    {
        var logger = new Mock<ILogger<TwelveDataService>>().Object;
        var config = new ConfigurationBuilder().Build();
        var httpClient = new HttpClient();

        Assert.Throws<InvalidOperationException>(() => 
            new TwelveDataService(httpClient, logger, config));
    }

    [Fact]
    public void Constructor_ValidApiKey_CreatesInstance()
    {
        var logger = new Mock<ILogger<TwelveDataService>>().Object;
        var config = CreateConfig("test-key");
        var httpClient = new HttpClient();

        var service = new TwelveDataService(httpClient, logger, config);
        
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ValidResponse_ReturnsPrice()
    {
        var logger = new Mock<ILogger<TwelveDataService>>();
        var config = CreateConfig("test-key");
        var httpClient = CreateMockHttpClient("{\"price\":\"25.50\"}");
        var service = new TwelveDataService(httpClient, logger.Object, config);

        var result = await service.GetCurrentPriceAsync("PETR4");

        Assert.Equal(25.50m, result);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_InvalidPriceFormat_ThrowsException()
    {
        var logger = new Mock<ILogger<TwelveDataService>>();
        var config = CreateConfig("test-key");
        var httpClient = CreateMockHttpClient("{\"price\":\"invalid\"}");
        var service = new TwelveDataService(httpClient, logger.Object, config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentPriceAsync("PETR4"));
        
        Assert.Contains("Preço inválido", exception.Message);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_EmptyPrice_ThrowsException()
    {
        var logger = new Mock<ILogger<TwelveDataService>>();
        var config = CreateConfig("test-key");
        var httpClient = CreateMockHttpClient("{\"price\":\"\"}");
        var service = new TwelveDataService(httpClient, logger.Object, config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentPriceAsync("PETR4"));
        
        Assert.Contains("Preço inválido", exception.Message);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_HttpRequestException_ThrowsInvalidOperationException()
    {
        var logger = new Mock<ILogger<TwelveDataService>>();
        var config = CreateConfig("test-key");
        var httpClient = CreateMockHttpClientWithException();
        var service = new TwelveDataService(httpClient, logger.Object, config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentPriceAsync("PETR4"));
        
        Assert.Contains("Falha na consulta da API", exception.Message);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_NullResponse_ThrowsException()
    {
        var logger = new Mock<ILogger<TwelveDataService>>();
        var config = CreateConfig("test-key");
        var httpClient = CreateMockHttpClient("null");
        var service = new TwelveDataService(httpClient, logger.Object, config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentPriceAsync("PETR4"));
        
        Assert.Contains("Preço inválido", exception.Message);
    }

    private static IConfiguration CreateConfig(string apiKey)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKey", apiKey) })
            .Build();
    }

    private static HttpClient CreateMockHttpClient(string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(mockHandler.Object);
    }

    private static HttpClient CreateMockHttpClientWithException()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.IsAny<HttpRequestMessage>(), 
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        return new HttpClient(mockHandler.Object);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_NullSymbol_ThrowsArgumentException()
    {
        var mockHttpClient = new Mock<HttpClient>();
        var mockLogger = new Mock<ILogger<TwelveDataService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKey", "test-key") })
            .Build();
        
        var service = new TwelveDataService(mockHttpClient.Object, mockLogger.Object, config);
        
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCurrentPriceAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentPriceAsync_InvalidSymbol_ThrowsArgumentException(string symbol)
    {
        var mockHttpClient = new Mock<HttpClient>();
        var mockLogger = new Mock<ILogger<TwelveDataService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKey", "test-key") })
            .Build();
        
        var service = new TwelveDataService(mockHttpClient.Object, mockLogger.Object, config);
        
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCurrentPriceAsync(symbol));
    }

    [Fact]
    public async Task GetCurrentPriceAsync_SymbolTooLong_ThrowsArgumentException()
    {
        var mockHttpClient = new Mock<HttpClient>();
        var mockLogger = new Mock<ILogger<TwelveDataService>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKey", "test-key") })
            .Build();
        
        var service = new TwelveDataService(mockHttpClient.Object, mockLogger.Object, config);
        var longSymbol = new string('A', 11); // > 10 characters
        
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCurrentPriceAsync(longSymbol));
    }
}