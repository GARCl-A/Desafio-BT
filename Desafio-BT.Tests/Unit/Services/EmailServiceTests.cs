using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desafio_BT.Services;
using Desafio_BT.Models;

namespace Desafio_BT.Tests.Unit.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IOptions<EmailSettings>> _mockOptions;
    private readonly EmailSettings _emailSettings;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockOptions = new Mock<IOptions<EmailSettings>>();
        
        _emailSettings = new EmailSettings
        {
            SmtpServer = "smtp.test.com",
            Port = 587,
            SenderEmail = "sender@test.com",
            SmtpUsername = "username",
            Password = "password"
        };
        
        _mockOptions.Setup(o => o.Value).Returns(_emailSettings);
    }

    [Theory]
    [InlineData("", "Subject", "Body")]
    [InlineData("test@test.com", "", "Body")]
    [InlineData("test@test.com", "Subject", "")]
    public async Task SendEmailAsync_InvalidParameters_ThrowsException(string toEmail, string subject, string body)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SendEmailAsync(toEmail, subject, body));
    }

    [Fact]
    public void Constructor_ValidSettings_CreatesInstance()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        
        Assert.NotNull(service);
    }
}