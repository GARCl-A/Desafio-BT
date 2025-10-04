using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var host = CreateHost(args);
        return await RunApplication(host, args);
    }

    private static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        builder.ConfigureServices((hostContext, services) =>
        {
            ConfigureEmailSettings(services, hostContext.Configuration);
            services.AddSingleton<HttpClient>();
            services.AddSingleton<EmailService>();
            services.AddSingleton<TwelveDataService>();
            services.AddSingleton<AppRunner>();
        });

        return builder.Build();
    }

    private static async Task<int> RunApplication(IHost host, string[] args)
    {
        try
        {
            var appRunner = host.Services.GetRequiredService<AppRunner>();
            return await appRunner.RunAsync(args);
        }
        catch (Exception ex)
        {
            LogCriticalError(host, ex);
            return 98;
        }
    }

    private static void LogCriticalError(IHost host, Exception ex)
    {
        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Erro crítico na inicialização: {Message}", ex.Message);
        }
        catch
        {
            Console.WriteLine($"Erro crítico na inicialização: {ex.Message}");
        }
    }

    private static void ConfigureEmailSettings(IServiceCollection services, IConfiguration configuration)
    {
        ValidateDestinationEmail(configuration);
        ConfigureEmailOptions(services, configuration);
    }

    private static void ValidateDestinationEmail(IConfiguration configuration)
    {
        var destinationEmail = configuration.GetValue<string>("DestinationEmail");
        if (string.IsNullOrEmpty(destinationEmail))
        {
            throw new InvalidOperationException("Erro: 'DestinationEmail' não está configurado. Verifique seus appsettings ou user secrets.");
        }
    }

    private static void ConfigureEmailOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("EmailSettings"))
            .ValidateDataAnnotations()
            .Validate(ValidateEmailSettings, "Configurações de email inválidas")
            .ValidateOnStart();
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