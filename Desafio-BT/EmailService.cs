using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Sockets;
using MailKit;

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("EmailService inicializado - Servidor: {SmtpServer}, Porta: {Port}, Remetente: {SenderEmail}", 
            LoggingUtils.SanitizeForLogging(_settings.SmtpServer), _settings.Port, LoggingUtils.SanitizeForLogging(_settings.SenderEmail));
        _logger.LogDebug("Configurações de email validadas com sucesso");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Email de destino não pode ser vazio", nameof(toEmail));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Assunto não pode ser vazio", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Corpo do email não pode ser vazio", nameof(body));
        
        if (!toEmail.Contains('@') || toEmail.Length > 254)
            throw new ArgumentException("Formato de email inválido", nameof(toEmail));

        _logger.LogDebug("Preparando envio de email para {Email}", LoggingUtils.SanitizeForLogging(toEmail));

        MimeMessage message;
        try
        {
            message = new MimeMessage();
            message.From.Add(new MailboxAddress("Alerta de Ativos", _settings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar mensagem de email");
            throw new InvalidOperationException("Falha na criação da mensagem", ex);
        }

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.Password);
            await client.SendAsync(message);
            _logger.LogInformation("Email enviado com sucesso para {Email}", LoggingUtils.SanitizeForLogging(toEmail));
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Falha na autenticação SMTP para {Email}", LoggingUtils.SanitizeForLogging(toEmail));
            throw new InvalidOperationException("Credenciais SMTP inválidas", ex);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Erro de conexão de rede ao enviar email para {Email}", LoggingUtils.SanitizeForLogging(toEmail));
            throw new InvalidOperationException("Erro de conectividade de rede", ex);
        }
        catch (ProtocolException ex)
        {
            _logger.LogError(ex, "Erro de protocolo SMTP ao enviar email para {Email}", LoggingUtils.SanitizeForLogging(toEmail));
            throw new InvalidOperationException("Erro no protocolo SMTP", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar email para {Email}", LoggingUtils.SanitizeForLogging(toEmail));
            throw;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }
}