using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Desafio_BT.Models;
using Desafio_BT.Services;

namespace Desafio_BT.Tests.Unit;

public class ProgramTests
{
    private static readonly string[] InvalidArgs = ["invalid"];

    [Fact]
    public async Task Main_WithInvalidArguments_ReturnsErrorCode()
    {
        using var env = new EnvironmentScope()
            .WithDestinationEmail("test@test.com")
            .WithApiKey("test-key")
            .WithValidEmailSettings();

        var result = await Program.Main(InvalidArgs);
        
        Assert.Equal(1, result);
    }

    [Fact]
    public void Configuration_WithValidEmailSettings_ConfiguresCorrectly()
    {
        var config = ConfigBuilder.New()
            .WithDestinationEmail("test@test.com")
            .WithApiKey("test-key")
            .WithValidEmailSettings()
            .Build();

        using var host = CreateTestHost(config);
        var options = host.Services.GetRequiredService<IOptions<EmailSettings>>();
        
        Assert.Equal("smtp.gmail.com", options.Value.SmtpServer);
        Assert.Equal(587, options.Value.Port);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Configuration_WithInvalidDestinationEmail_ThrowsException(string? email)
    {
        var config = ConfigBuilder.New()
            .WithDestinationEmail(email)
            .WithApiKey("test-key")
            .WithValidEmailSettings()
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => CreateTestHost(config));
        Assert.Contains("DestinationEmail", exception.Message);
    }

    [Fact]
    public void Configuration_WithMissingApiKey_ThrowsException()
    {
        var config = ConfigBuilder.New()
            .WithDestinationEmail("test@test.com")
            .WithValidEmailSettings()
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => CreateTestHost(config));
        Assert.Contains("ApiKey", exception.Message);
    }

    [Theory]
    [InlineData("", "587", "test@test.com", "user", "pass")]
    [InlineData("smtp.gmail.com", "0", "test@test.com", "user", "pass")]
    [InlineData("smtp.gmail.com", "587", "", "user", "pass")]
    [InlineData("smtp.gmail.com", "587", "test@test.com", "", "pass")]
    [InlineData("smtp.gmail.com", "587", "test@test.com", "user", "")]
    public void Configuration_WithInvalidEmailSettings_FailsValidation(
        string server, string port, string email, string username, string password)
    {
        var config = ConfigBuilder.New()
            .WithDestinationEmail("test@test.com")
            .WithApiKey("test-key")
            .WithEmailSettings(server, port, email, username, password)
            .Build();

        using var host = CreateTestHost(config);
        
        var exception = Assert.Throws<OptionsValidationException>(() => 
            host.Services.GetRequiredService<IOptions<EmailSettings>>().Value);
        
        Assert.True(exception.Message.Contains("Port") || 
                   exception.Message.Contains("email") || 
                   exception.Message.Contains("Configurações"));
    }

    [Fact]
    public void Configuration_WithValidSettings_RegistersAllServices()
    {
        var config = ConfigBuilder.New()
            .WithDestinationEmail("test@test.com")
            .WithApiKey("test-key")
            .WithValidEmailSettings()
            .Build();

        using var host = CreateTestHost(config);
        
        Assert.NotNull(host.Services.GetService<IEmailService>());
        Assert.NotNull(host.Services.GetService<ITwelveDataService>());
        Assert.NotNull(host.Services.GetService<IOptions<EmailSettings>>());
    }

    private static IHost CreateTestHost(IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.Sources.Clear();
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                var destinationEmail = context.Configuration.GetValue<string>("DestinationEmail");
                if (string.IsNullOrEmpty(destinationEmail))
                    throw new InvalidOperationException("DestinationEmail não configurado");

                var apiKey = context.Configuration.GetValue<string>("ApiKey");
                if (string.IsNullOrEmpty(apiKey))
                    throw new InvalidOperationException("ApiKey não configurada");

                services.AddOptions<EmailSettings>()
                    .Bind(context.Configuration.GetSection("EmailSettings"))
                    .ValidateDataAnnotations()
                    .Validate(settings => ValidateEmailSettings(settings), "Configurações de email inválidas")
                    .ValidateOnStart();

                services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
                services.AddSingleton<IEmailService, EmailService>();
                services.AddSingleton<ITwelveDataService, TwelveDataService>();
            })
            .Build();
    }

    private static bool ValidateEmailSettings(EmailSettings settings)
    {
        return settings != null &&
               !string.IsNullOrEmpty(settings.SmtpServer) &&
               settings.Port > 0 && settings.Port <= 65535 &&
               !string.IsNullOrEmpty(settings.SenderEmail) &&
               !string.IsNullOrEmpty(settings.SmtpUsername) &&
               !string.IsNullOrEmpty(settings.Password);
    }
}

public static class ConfigBuilder
{
    public static ConfigurationFluentBuilder New() => new();
}

public class ConfigurationFluentBuilder
{
    private readonly Dictionary<string, string?> _config = [];

    public ConfigurationFluentBuilder WithDestinationEmail(string? email)
    {
        if (email != null)
            _config["DestinationEmail"] = email;
        return this;
    }

    public ConfigurationFluentBuilder WithApiKey(string apiKey)
    {
        _config["ApiKey"] = apiKey;
        return this;
    }

    public ConfigurationFluentBuilder WithValidEmailSettings()
    {
        return WithEmailSettings("smtp.gmail.com", "587", "test@test.com", "test", "password");
    }

    public ConfigurationFluentBuilder WithEmailSettings(string server, string port, string email, string username, string password)
    {
        _config["EmailSettings:SmtpServer"] = server;
        _config["EmailSettings:Port"] = port;
        _config["EmailSettings:SenderEmail"] = email;
        _config["EmailSettings:SmtpUsername"] = username;
        _config["EmailSettings:Password"] = password;
        return this;
    }

    public IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(_config)
            .Build();
    }
}

public class EnvironmentScope : IDisposable
{
    private readonly List<string> _keysToCleanup = [];

    public EnvironmentScope WithDestinationEmail(string email)
    {
        return Set("DestinationEmail", email);
    }

    public EnvironmentScope WithApiKey(string apiKey)
    {
        return Set("ApiKey", apiKey);
    }

    public EnvironmentScope WithValidEmailSettings()
    {
        return Set("EmailSettings__SmtpServer", "smtp.gmail.com")
               .Set("EmailSettings__Port", "587")
               .Set("EmailSettings__SenderEmail", "test@test.com")
               .Set("EmailSettings__SmtpUsername", "test")
               .Set("EmailSettings__Password", "password");
    }

    private EnvironmentScope Set(string key, string? value)
    {
        Environment.SetEnvironmentVariable(key, value);
        _keysToCleanup.Add(key);
        return this;
    }

    public void Dispose()
    {
        foreach (var key in _keysToCleanup)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
        GC.SuppressFinalize(this);
    }
}