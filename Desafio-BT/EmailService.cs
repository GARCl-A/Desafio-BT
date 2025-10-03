using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _smtpServer;
    private readonly int _port;
    private readonly string _senderEmail;
    private readonly string _password;

    public EmailService(string smtpServer, int port, string senderEmail, string password)
    {
        _smtpServer = smtpServer;
        _port = port;
        _senderEmail = senderEmail;
        _password = password;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_senderEmail),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        mailMessage.To.Add(toEmail);

        using (var smtpClient = new SmtpClient(_smtpServer, _port))
        {
            smtpClient.Credentials = new NetworkCredential(_senderEmail, _password);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}