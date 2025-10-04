using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Desafio_BT.Services;
using System.Reflection;

namespace Desafio_BT.Tests.Unit;

public class AppRunnerTests
{
    private static readonly string[] SingleArgument = { "PETR4" };
    private static readonly string[] ValidArguments = { "PETR4", "25.50", "20.00" };

    [Fact]
    public async Task RunAsync_InvalidArgumentCount_ReturnsError()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, 
            config, mockTwelveDataService.Object);
        
        var result = await appRunner.RunAsync(SingleArgument);
        
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
        
        var result = await appRunner.RunAsync(ValidArguments);
        
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

    [Fact]
    public void GetDestinationEmail_ValidEmail_ReturnsEmail()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("DestinationEmail", "test@test.com") })
            .Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("GetDestinationEmail", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = method?.Invoke(appRunner, null) as string;
        
        Assert.Equal("test@test.com", result);
    }

    [Fact]
    public void GetDestinationEmail_EmptyEmail_ReturnsNull()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("GetDestinationEmail", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = method?.Invoke(appRunner, null) as string;
        
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPriceWithRetry_Success_ReturnsPrice()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        mockTwelveDataService.Setup(x => x.GetCurrentPriceAsync("PETR4")).ReturnsAsync(25.50m);
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("GetPriceWithRetry", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = await (Task<decimal?>)method!.Invoke(appRunner, new object[] { "PETR4" })!;
        
        Assert.Equal(25.50m, result);
    }

    [Fact]
    public async Task GetPriceWithRetry_AllRetriesFail_ReturnsNull()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        mockTwelveDataService.Setup(x => x.GetCurrentPriceAsync("PETR4")).ThrowsAsync(new Exception("API Error"));
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("GetPriceWithRetry", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = await (Task<decimal?>)method!.Invoke(appRunner, new object[] { "PETR4" })!;
        
        Assert.Null(result);
    }

    [Theory]
    [InlineData(20.00, 30.00, 25.00, "COMPRAR")]
    [InlineData(30.00, 30.00, 25.00, "VENDER")]
    [InlineData(27.00, 30.00, 25.00, null)]
    public void GetAlertAction_ReturnsCorrectAction(decimal precoAtual, decimal precoVenda, decimal precoCompra, string? expectedAction)
    {
        var method = typeof(AppRunner).GetMethod("GetAlertAction", BindingFlags.NonPublic | BindingFlags.Static);
        
        var result = method?.Invoke(null, new object[] { precoAtual, precoVenda, precoCompra }) as string;
        
        Assert.Equal(expectedAction, result);
    }

    [Fact]
    public async Task ProcessPriceAlert_WithAlert_SendsEmail()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ProcessPriceAlert", BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(appRunner, new object[] { "PETR4", 20.00m, 30.00m, 25.00m, "test@test.com" })!;
        
        mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPriceAlert_NoAlert_DoesNotSendEmail()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ProcessPriceAlert", BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(appRunner, new object[] { "PETR4", 27.00m, 30.00m, 25.00m, "test@test.com" })!;
        
        mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendPriceAlert_SendsEmailWithCorrectParameters()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("SendPriceAlert", BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(appRunner, new object[] { "PETR4", 20.00m, 30.00m, 25.00m, "test@test.com", "COMPRAR" })!;
        
        mockEmailService.Verify(x => x.SendEmailAsync(
            "test@test.com",
            It.Is<string>(s => s.Contains("COMPRAR") && s.Contains("PETR4")),
            It.Is<string>(s => s.Contains("COMPRAR") && s.Contains("20"))
        ), Times.Once);
    }

    [Fact]
    public async Task MonitorAsset_CancellationRequested_ReturnsEarly()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("MonitorAsset", BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)method!.Invoke(appRunner, new object[] { "PETR4", 30.00m, 25.00m, "test@test.com", cts.Token })!;
        
        mockTwelveDataService.Verify(x => x.GetCurrentPriceAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ValidArguments_ReturnsZero()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("DestinationEmail", "test@test.com") })
            .Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        
        try
        {
            var task = appRunner.RunAsync(ValidArguments);
            await task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando monitoramento")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateArgumentsArray_NullArray_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { null! }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_EmptyArray_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        var emptyArray = Array.Empty<string>();
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { emptyArray }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_EmptyFirstArgument_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        string[] args = ["", "25.50", "20.00"];
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { args }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_EmptySecondArgument_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        string[] args = ["PETR4", "", "20.00"];
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { args }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_EmptyThirdArgument_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        string[] args = ["PETR4", "25.50", ""];
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { args }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_WhitespaceArgument_ThrowsArgumentException()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        string[] args = ["   ", "25.50", "20.00"];
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(null, new object[] { args }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateArgumentsArray_ValidArguments_DoesNotThrow()
    {
        var method = typeof(AppRunner).GetMethod("ValidateArgumentsArray", BindingFlags.NonPublic | BindingFlags.Static);
        
        var exception = Record.Exception(() => method?.Invoke(null, new object[] { ValidArguments }));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ParsePricesAsync_ValidPrices_ReturnsParsedValues()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ParsePricesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = await (Task<(decimal, decimal)>)method!.Invoke(appRunner, new object[] { "25.50", "20.00" })!;
        
        Assert.Equal(25.50m, result.Item1);
        Assert.Equal(20.00m, result.Item2);
    }

    [Theory]
    [InlineData("invalid", "20.00")]
    [InlineData("25.50", "invalid")]
    [InlineData("", "20.00")]
    [InlineData("25.50", "")]
    public async Task ParsePricesAsync_InvalidPrices_ThrowsArgumentException(string precoVenda, string precoCompra)
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ParsePricesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await (Task)method!.Invoke(appRunner, new object[] { precoVenda, precoCompra })!);
    }

    [Fact]
    public void ValidatePriceLogic_ValidPrices_DoesNotThrow()
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ValidatePriceLogic", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var exception = Record.Exception(() => method?.Invoke(appRunner, new object[] { 20.00m, 25.50m }));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(25.50, 20.00)]
    [InlineData(25.00, 25.00)]
    public void ValidatePriceLogic_InvalidPriceLogic_ThrowsArgumentException(decimal precoCompra, decimal precoVenda)
    {
        var mockLogger = new Mock<ILogger<AppRunner>>();
        var mockEmailService = new Mock<IEmailService>();
        var mockTwelveDataService = new Mock<ITwelveDataService>();
        var config = new ConfigurationBuilder().Build();
        
        var appRunner = new AppRunner(mockLogger.Object, mockEmailService.Object, config, mockTwelveDataService.Object);
        var method = typeof(AppRunner).GetMethod("ValidatePriceLogic", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var ex = Assert.Throws<TargetInvocationException>(() => method?.Invoke(appRunner, new object[] { precoCompra, precoVenda }));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }
}