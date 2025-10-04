using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Desafio_BT.Services;

public class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _client = new();
    private bool _disposed;

    public bool IsConnected => _client.IsConnected;

    public Task ConnectAsync(string host, int port, SecureSocketOptions options) =>
        _client.ConnectAsync(host, port, options);

    public Task AuthenticateAsync(string userName, string password) =>
        _client.AuthenticateAsync(userName, password);

    public Task SendAsync(MimeMessage message) =>
        _client.SendAsync(message);

    public Task DisconnectAsync(bool quit) =>
        _client.DisconnectAsync(quit);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}