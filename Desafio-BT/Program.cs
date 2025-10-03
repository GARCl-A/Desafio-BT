using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
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
            var configuration = hostContext.Configuration;

            var destinationEmail = configuration.GetValue<string>("DestinationEmail");
            if (string.IsNullOrEmpty(destinationEmail))
            {
                throw new InvalidOperationException("Erro: 'DestinationEmail' não está configurado. Verifique seus appsettings ou user secrets.");
            }

            services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("EmailSettings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
                
            services.AddSingleton<EmailService>();
            services.AddSingleton<AppRunner>();
        });

        var host = builder.Build();

        try
        {
            var appRunner = host.Services.GetRequiredService<AppRunner>();
            await appRunner.RunAsync(args);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetService<ILogger<Program>>();
            logger?.LogCritical(ex, "Erro crítico na aplicação");
            throw;
        }
    }
}