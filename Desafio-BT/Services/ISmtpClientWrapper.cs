using MailKit.Security;
using MimeKit;

namespace Desafio_BT.Services;

public interface ISmtpClientWrapper : IDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions options);
    Task AuthenticateAsync(string userName, string password);
    Task SendAsync(MimeMessage message);
    Task DisconnectAsync(bool quit);
    bool IsConnected { get; }
}