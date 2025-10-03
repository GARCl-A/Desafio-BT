using System.ComponentModel.DataAnnotations;

public class EmailSettings
{
    [Required]
    public string SmtpServer { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    [Required]
    public string SmtpUsername { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}