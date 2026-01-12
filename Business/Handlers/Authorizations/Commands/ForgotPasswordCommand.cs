using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.IoC;
using Core.Utilities.Mail;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Handlers.Authorizations.Commands
{
    public class ForgotPasswordCommand : IRequest<IResult>
    {
        public string Email { get; set; }
    }

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, IResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMailService _mailService;
        private readonly ICacheManager _cacheManager;
        private readonly IConfiguration _configuration;

        public ForgotPasswordCommandHandler(
            IUserRepository userRepository, 
            IMailService mailService,
            ICacheManager cacheManager,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _mailService = mailService;
            _cacheManager = cacheManager;
            _configuration = configuration;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new ErrorResult("E-posta adresi gereklidir.");
            }

            // Email ile kullanıcı bulma - önce deterministik encryption ile direkt arama
            var normalizedRequestEmail = request.Email.Trim().ToLowerInvariant();
            var encryptedEmail = EmailEncryptionHelper.EncryptEmailDeterministic(normalizedRequestEmail, _configuration);
            
            // Direkt veritabanında arama (performans için)
            var allUsers = await _userRepository.GetListAsync();
            User user = allUsers.FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) && u.Email == encryptedEmail);
            
            // Eğer deterministik encryption ile bulunamazsa, eski yöntemle devam et (geriye dönük uyumluluk)
            if (user == null)
            {
                foreach (var userInList in allUsers)
                {
                    if (!string.IsNullOrEmpty(userInList.Email))
                    {
                        var decryptedEmail = EmailEncryptionHelper.DecryptEmail(userInList.Email, _configuration);
                        if (string.IsNullOrEmpty(decryptedEmail))
                        {
                            decryptedEmail = userInList.Email;
                        }
                        
                        if (decryptedEmail.Trim().ToLowerInvariant() == normalizedRequestEmail)
                        {
                            user = userInList;
                            break;
                        }
                    }
                }
            }

            // Kullanıcı bulunamadıysa bile güvenlik için başarılı mesaj döndür (email enumeration saldırısını önlemek için)
            if (user == null)
            {
                return new SuccessResult("Eğer bu e-posta adresine kayıtlı bir hesap varsa, şifre sıfırlama kodu gönderildi.");
            }

            // Rate limiting kontrolü (3 dakika içinde aynı email'e tekrar gönderme engelle)
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var rateLimitKey = $"PasswordReset_{normalizedEmail}";
            if (_cacheManager.IsAdd(rateLimitKey))
            {
                return new ErrorResult("Şifre sıfırlama kodu çok kısa süre önce gönderildi. Lütfen 3 dakika sonra tekrar deneyin.");
            }

            // 6 haneli reset token oluştur
            var random = new Random();
            var resetToken = random.Next(100000, 999999).ToString();
            var tokenExpiry = DateTime.Now.AddHours(1); // 1 saat geçerli

            // Kullanıcıyı tracking ile çek
            var userToUpdate = await _userRepository.GetByIdWithTrackingAsync(user.UserId);
            if (userToUpdate == null)
            {
                return new ErrorResult("Kullanıcı bulunamadı.");
            }

            userToUpdate.PasswordResetToken = resetToken;
            userToUpdate.PasswordResetTokenExpiry = tokenExpiry;

            _userRepository.Update(userToUpdate);
            await _userRepository.SaveChangesAsync();

            // Email gönder
            try
            {
                var decryptedEmail = EmailEncryptionHelper.DecryptEmail(user.Email, _configuration) ?? user.Email;
                
                var emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0a7ea4; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .code-box {{ background-color: #fff; border: 2px solid #0a7ea4; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center; }}
        .code {{ font-family: 'Courier New', monospace; font-size: 18px; font-weight: bold; color: #0a7ea4; letter-spacing: 2px; word-break: break-all; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Şifre Sıfırlama</h1>
        </div>
        <div class='content'>
            <p>Merhaba {user.UserName},</p>
            <p>Şifrenizi sıfırlamak için aşağıdaki doğrulama kodunu uygulamaya giriniz:</p>
            
            <div class='code-box'>
                <p style='margin: 0 0 10px 0; font-weight: bold;'>Doğrulama Kodu:</p>
                <p class='code'>{resetToken}</p>
            </div>
            
            <p><strong>Nasıl kullanılır?</strong></p>
            <ol style='text-align: left; padding-left: 20px;'>
                <li>Uygulamadaki ""Şifre Sıfırlama"" ekranını açın</li>
                <li>Yukarıdaki doğrulama kodunu giriniz</li>
                <li>Yeni şifrenizi belirleyiniz</li>
            </ol>
            
            <p><strong>Önemli:</strong> Bu kod 1 saat geçerlidir. Eğer bu isteği siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz. Şifreniz değişmeyecektir.</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
        </div>
    </div>
</body>
</html>";

                // Environment variable resolution için MailManager'ın static metodunu kullan
                var senderEmail = Core.Utilities.Mail.MailManager.ResolveConfigurationValue(
                    _configuration.GetSection("EmailConfiguration").GetSection("SenderEmail").Value);
                var senderName = Core.Utilities.Mail.MailManager.ResolveConfigurationValue(
                    _configuration.GetSection("EmailConfiguration").GetSection("SenderName").Value);
                
                if (string.IsNullOrEmpty(senderEmail))
                {
                    senderEmail = "noreply@cuzdanim.com";
                }
                
                if (string.IsNullOrEmpty(senderName))
                {
                    senderName = "Cüzdanım";
                }

                var emailMessage = new EmailMessage
                {
                    ToAddresses = new List<EmailAddress> { new EmailAddress { Name = user.UserName, Address = decryptedEmail } },
                    FromAddresses = new List<EmailAddress> { new EmailAddress { Name = senderName, Address = senderEmail } },
                    Subject = "Cüzdanım - Şifre Sıfırlama",
                    Content = emailContent
                };

                await _mailService.SendAsync(emailMessage);

                // Rate limiting için cache'e kaydet (3 dakika)
                _cacheManager.Add(rateLimitKey, DateTime.Now, 3);
            }
            catch (Exception ex)
            {
                // Email gönderme hatası - logla
                var logger = ServiceTool.ServiceProvider.GetService<FileLogger>();
                if (logger != null)
                {
                    var errorDetails = $"Şifre sıfırlama mail gönderme hatası - Email: {request.Email}, " +
                                     $"Exception Type: {ex.GetType().Name}, Message: {ex.Message}";
                    logger.Error(errorDetails);
                }
                
                return new ErrorResult("E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin.");
            }

            return new SuccessResult("Eğer bu e-posta adresine kayıtlı bir hesap varsa, şifre sıfırlama kodu gönderildi.");
        }
    }
}