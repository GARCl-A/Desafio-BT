using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Desafio_BT.Services;

public class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _client = new();

    public bool IsConnected => _client.IsConnected;

    public Task ConnectAsync(string host, int port, SecureSocketOptions options) =>
        _client.ConnectAsync(host, port, options);

    public Task AuthenticateAsync(string userName, string password) =>
        _client.AuthenticateAsync(userName, password);

    public Task SendAsync(MimeMessage message) =>
        _client.SendAsync(message);

    public Task DisconnectAsync(bool quit) =>
        _client.DisconnectAsync(quit);

    public void Dispose() => _client.Dispose();
}