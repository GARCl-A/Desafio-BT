using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Desafio_BT.Services;

namespace Desafio_BT.Tests.Unit;

public class AppRunnerTests
{
    [Fact]
    public async Task RunAsync_InvalidArgumentCount_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, 
            config, mockTwelveDataService.Object);
        
        var result = await appRunner.RunAsync(new[] { "PETR4" });
        
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task RunAsync_MissingDestinationEmail_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        Assert.Null(config.GetValue<string>("DestinationEmail"));
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, 
            config, mockTwelveDataService.Object);
        
        var result = await appRunner.RunAsync(new[] { "PETR4", "25.50", "20.00" });
        
        Assert.Equal(3, result);
    }

    [Theory]
    [InlineData("PETR4", "invalid", "20.00")]
    [InlineData("PETR4", "25.50", "invalid")]
    [InlineData("PETR4", "20.00", "25.00")]
    public async Task RunAsync_InvalidPrices_ThrowsException(string ativo, string precoVenda, string precoCompra)
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("DestinationEmail", "test@test.com") })
            .Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, 
            config, mockTwelveDataService.Object);
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            appRunner.RunAsync(new[] { ativo, precoVenda, precoCompra }));
    }
}