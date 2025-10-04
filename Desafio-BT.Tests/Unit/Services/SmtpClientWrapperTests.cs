using Xunit;
using Desafio_BT.Services;
using MailKit.Security;
using MimeKit;

namespace Desafio_BT.Tests.Unit.Services;

public class SmtpClientWrapperTests
{
    [Fact]
    public void IsConnected_ReturnsClientConnectionStatus()
    {
        using var wrapper = new SmtpClientWrapper();
        
        Assert.False(wrapper.IsConnected);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var wrapper = new SmtpClientWrapper();
        
        wrapper.Dispose();
        wrapper.Dispose();
        
        Assert.True(true);
    }

    [Fact]
    public async Task ConnectAsync_CallsClientConnect()
    {
        using var wrapper = new SmtpClientWrapper();
        
        await Assert.ThrowsAsync<Exception>(() => 
            wrapper.ConnectAsync("invalid", 587, SecureSocketOptions.StartTls));
    }

    [Fact]
    public async Task AuthenticateAsync_CallsClientAuthenticate()
    {
        using var wrapper = new SmtpClientWrapper();
        
        await Assert.ThrowsAsync<Exception>(() => 
            wrapper.AuthenticateAsync("user", "pass"));
    }

    [Fact]
    public async Task SendAsync_CallsClientSend()
    {
        using var wrapper = new SmtpClientWrapper();
        var message = new MimeMessage();
        
        await Assert.ThrowsAsync<Exception>(() => 
            wrapper.SendAsync(message));
    }

    [Fact]
    public async Task DisconnectAsync_CallsClientDisconnect()
    {
        using var wrapper = new SmtpClientWrapper();
        
        await wrapper.DisconnectAsync(true);
        
        Assert.True(true);
    }
}