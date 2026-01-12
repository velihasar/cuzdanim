using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Core.Utilities.Mail
{
    public class MailManager : IMailService
    {
        private readonly IConfiguration _configuration;

        public MailManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(EmailMessage emailMessage)
        {
            var smtpServer = _configuration.GetSection("EmailConfiguration").GetSection("SmtpServer").Value;
            var smtpPortStr = _configuration.GetSection("EmailConfiguration").GetSection("SmtpPort").Value;
            var userName = _configuration.GetSection("EmailConfiguration").GetSection("UserName").Value;
            var password = _configuration.GetSection("EmailConfiguration").GetSection("Password").Value;

            if (string.IsNullOrEmpty(smtpServer))
            {
                throw new Exception("SMTP Server ayarı bulunamadı. EmailConfiguration:SmtpServer ayarını kontrol edin.");
            }

            if (string.IsNullOrEmpty(smtpPortStr) || !int.TryParse(smtpPortStr, out var smtpPort))
            {
                throw new Exception($"SMTP Port ayarı geçersiz. EmailConfiguration:SmtpPort ayarını kontrol edin. Değer: {smtpPortStr}");
            }

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                throw new Exception("SMTP kullanıcı adı veya şifre ayarı bulunamadı. EmailConfiguration:UserName ve EmailConfiguration:Password ayarlarını kontrol edin.");
            }

            var message = new MimeMessage();
            message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            message.From.AddRange(emailMessage.FromAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            
            if (emailMessage.ReplyToAddresses != null && emailMessage.ReplyToAddresses.Any())
            {
                message.ReplyTo.AddRange(emailMessage.ReplyToAddresses.Select(x => new MailboxAddress(x.Name, x.Address)));
            }

            message.Subject = emailMessage.Subject;

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = emailMessage.Content
            };
            
            using var emailClient = new SmtpClient();
            
            // Timeout ayarla (30 saniye)
            emailClient.Timeout = 30000;
            
            try
            {
                // ConnectAsync'e CancellationToken ekle ve timeout ayarla
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                Console.WriteLine($"[MailManager] SMTP bağlantısı başlatılıyor - Server: {smtpServer}, Port: {smtpPort}, To: {string.Join(", ", emailMessage.ToAddresses.Select(x => x.Address))}");
                
                await emailClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls, cts.Token);
                Console.WriteLine($"[MailManager] SMTP bağlantısı başarılı");
                
                await emailClient.AuthenticateAsync(userName, password, cts.Token);
                Console.WriteLine($"[MailManager] SMTP kimlik doğrulama başarılı");
                
                await emailClient.SendAsync(message, cts.Token);
                Console.WriteLine($"[MailManager] Email gönderildi - To: {string.Join(", ", emailMessage.ToAddresses.Select(x => x.Address))}, Subject: {emailMessage.Subject}");
                
                await emailClient.DisconnectAsync(true, cts.Token);
                Console.WriteLine($"[MailManager] SMTP bağlantısı kapatıldı");
            }
            catch (Exception ex)
            {
                // Detaylı hata mesajı ile exception fırlat
                var errorMessage = $"Mail gönderme hatası - SMTP Server: {smtpServer}, Port: {smtpPort}, To: {string.Join(", ", emailMessage.ToAddresses.Select(x => x.Address))}, Hata: {ex.Message}";
                Console.WriteLine($"[MailManager] {errorMessage}");
                Console.Error.WriteLine($"[MailManager] {errorMessage}");
                Console.Error.WriteLine($"[MailManager] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"[MailManager] InnerException: {ex.InnerException.Message}");
                }
                throw new Exception(errorMessage, ex);
            }
        }
    }
}