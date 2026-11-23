using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(
            IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                throw new Exception(
                    "Chưa cấu hình Email:SenderEmail.");
            }

            if (string.IsNullOrWhiteSpace(_settings.AppPassword))
            {
                throw new Exception(
                    "Chưa cấu hình Email:AppPassword.");
            }

            using var message = new MailMessage();

            message.From = new MailAddress(
                _settings.SenderEmail,
                _settings.SenderName);

            message.To.Add(toEmail);

            message.Subject = subject;

            message.Body = htmlBody;

            message.IsBodyHtml = true;

            using var smtp = new SmtpClient(
                _settings.SmtpServer,
                _settings.SmtpPort);

            smtp.EnableSsl = true;

            smtp.Credentials = new NetworkCredential(
                _settings.SenderEmail,
                _settings.AppPassword);

            await smtp.SendMailAsync(message);
        }
    }


    public class EmailSettings
    {
        public string SmtpServer { get; set; }
            = "smtp.gmail.com";

        public int SmtpPort { get; set; }
            = 587;

        public string SenderName { get; set; }
            = "RetailFlow";

        public string SenderEmail { get; set; }
            = "";

        public string AppPassword { get; set; }
            = "";
    }
}