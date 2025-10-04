#pragma warning disable xUnit1012
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Desafio_BT.Services;
using Desafio_BT.Models;
using System.Reflection;
using MimeKit;
using MailKit;
using MailKit.Security;
using System.Net.Sockets;

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
    [InlineData(null, "Subject", "Body")]
    [InlineData("test@test.com", null, "Body")]
    [InlineData("test@test.com", "Subject", null)]
    [InlineData("   ", "Subject", "Body")]
    [InlineData("test@test.com", "   ", "Body")]
    [InlineData("test@test.com", "Subject", "   ")]
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

    [Fact]
    public void Constructor_NullSettings_ThrowsNullReferenceException()
    {
        Assert.Throws<NullReferenceException>(() => 
            new EmailService(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new EmailService(_mockOptions.Object, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateEmailParameters_InvalidToEmail_ThrowsArgumentException(string toEmail)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            method.Invoke(null, [toEmail, "Subject", "Body"]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateEmailParameters_InvalidSubject_ThrowsArgumentException(string subject)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            method.Invoke(null, ["test@test.com", subject, "Body"]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateEmailParameters_InvalidBody_ThrowsArgumentException(string body)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            method.Invoke(null, ["test@test.com", "Subject", body]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test.com")]
    public void ValidateEmailParameters_InvalidEmailFormat_ThrowsArgumentException(string toEmail)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            method.Invoke(null, [toEmail, "Subject", "Body"]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateEmailParameters_EmailTooLong_ThrowsArgumentException()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        var longEmail = new string('a', 250) + "@test.com"; // > 254 characters
        
        var ex = Assert.Throws<TargetInvocationException>(() => 
            method.Invoke(null, [longEmail, "Subject", "Body"]));
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    [Fact]
    public void ValidateEmailParameters_ValidParameters_DoesNotThrow()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var exception = Record.Exception(() => 
            method.Invoke(null, ["test@test.com", "Subject", "Body"]));
        
        Assert.Null(exception);
    }

    [Fact]
    public void CreateEmailMessage_ValidParameters_ReturnsMessage()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("CreateEmailMessage");
        
        var result = method.Invoke(service, ["test@test.com", "Test Subject", "Test Body"]);
        
        Assert.NotNull(result);
        var message = result as MimeMessage;
        Assert.NotNull(message);
        Assert.Equal("Test Subject", message.Subject);
        Assert.Single(message.To);
        Assert.Single(message.From);
    }

    [Fact]
    public void CreateEmailMessage_InvalidSenderEmail_CreatesMessageAnyway()
    {
        _emailSettings.SenderEmail = "invalid-email";
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("CreateEmailMessage");
        
        var result = method.Invoke(service, ["test@test.com", "Subject", "Body"]);
        var message = result as MimeMessage;
        
        Assert.NotNull(message);
        Assert.Equal("Subject", message.Subject);
    }

    [Fact]
    public void CreateEmailMessage_ValidParameters_SetsCorrectProperties()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("CreateEmailMessage");
        
        var result = method.Invoke(service, ["recipient@test.com", "Test Subject", "Test Body"]);
        var message = result as MimeMessage;
        
        Assert.NotNull(message);
        Assert.Equal("Test Subject", message.Subject);
        Assert.Equal("recipient@test.com", message.To[0].ToString());
        Assert.Contains("sender@test.com", message.From[0].ToString());
        Assert.Equal("Test Body", ((TextPart)message.Body).Text);
    }

    [Theory]
    [InlineData("valid@email.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.org")]
    public void ValidateEmailParameters_ValidEmails_DoesNotThrow(string email)
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        var method = GetPrivateMethod("ValidateEmailParameters");
        
        var exception = Record.Exception(() => 
            method.Invoke(null, [email, "Subject", "Body"]));
        
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_LogsInitializationInfo()
    {
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EmailService inicializado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_Success_CallsSmtpClientCorrectly()
    {
        var mockSmtpClient = new Mock<ISmtpClientWrapper>();
        mockSmtpClient.Setup(x => x.IsConnected).Returns(true);
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object, () => mockSmtpClient.Object);
        
        await service.SendEmailAsync("test@test.com", "Subject", "Body");
        
        mockSmtpClient.Verify(x => x.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls), Times.Once);
        mockSmtpClient.Verify(x => x.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.Password), Times.Once);
        mockSmtpClient.Verify(x => x.SendAsync(It.IsAny<MimeMessage>()), Times.Once);
        mockSmtpClient.Verify(x => x.DisconnectAsync(true), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_AuthenticationException_ThrowsInvalidOperationException()
    {
        var mockSmtpClient = new Mock<ISmtpClientWrapper>();
        mockSmtpClient.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new AuthenticationException());
        
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object, () => mockSmtpClient.Object);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.SendEmailAsync("test@test.com", "subject", "body"));
        
        Assert.Equal("Credenciais SMTP inválidas", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_SocketException_ThrowsInvalidOperationException()
    {
        var mockSmtpClient = new Mock<ISmtpClientWrapper>();
        mockSmtpClient.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>()))
            .ThrowsAsync(new SocketException());
        
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object, () => mockSmtpClient.Object);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.SendEmailAsync("test@test.com", "subject", "body"));
        
        Assert.Equal("Erro de conectividade de rede", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_ProtocolException_ThrowsInvalidOperationException()
    {
        var mockSmtpClient = new Mock<ISmtpClientWrapper>();
        mockSmtpClient.Setup(x => x.SendAsync(It.IsAny<MimeMessage>()))
            .ThrowsAsync(new Exception("Protocol error"));
        
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object, () => mockSmtpClient.Object);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.SendEmailAsync("test@test.com", "subject", "body"));
        
        Assert.Contains("test@test.com", ex.Message);
    }

    [Fact]
    public async Task SendEmailAsync_GenericException_ThrowsInvalidOperationExceptionWithContext()
    {
        var mockSmtpClient = new Mock<ISmtpClientWrapper>();
        var toEmail = "test@test.com";
        mockSmtpClient.Setup(x => x.SendAsync(It.IsAny<MimeMessage>()))
            .ThrowsAsync(new Exception("Unexpected error"));
        
        var service = new EmailService(_mockOptions.Object, _mockLogger.Object, () => mockSmtpClient.Object);
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.SendEmailAsync(toEmail, "subject", "body"));
        
        Assert.Contains(toEmail, ex.Message);
    }

    private static MethodInfo GetPrivateMethod(string methodName)
    {
        return typeof(EmailService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method {methodName} not found");
    }
}