using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Desafio_BT.Services;

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
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ApiKey", "test-key") })
            .Build();
        var httpClient = new HttpClient();

        var service = new TwelveDataService(httpClient, logger, config);
        
        Assert.NotNull(service);
    }
}