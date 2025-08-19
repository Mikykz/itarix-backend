using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlMessage);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SmtpEmailSender(IConfiguration config)
    {
        _smtpHost = config["SMTP:Host"];
        _smtpPort = int.Parse(config["SMTP:Port"]);
        _smtpUser = config["SMTP:Username"];
        _smtpPass = config["SMTP:Password"];
        _fromEmail = config["SMTP:FromEmail"];
        _fromName = config["SMTP:FromName"];
    }

    public async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        using (var client = new SmtpClient(_smtpHost, _smtpPort))
        {
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);

            var mail = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            await client.SendMailAsync(mail);
        }
    }
}
