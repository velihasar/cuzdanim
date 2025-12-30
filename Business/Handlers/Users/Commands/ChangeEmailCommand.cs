using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.IoC;
using Core.Utilities.Mail;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.Users.Commands
{
    public class ChangeEmailCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string NewEmail { get; set; }
    }

    public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, IResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ICacheManager _cacheManager;
        private readonly IMailService _mailService;

        public ChangeEmailCommandHandler(
            IUserRepository userRepository, 
            IConfiguration configuration,
            ICacheManager cacheManager,
            IMailService mailService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _cacheManager = cacheManager;
            _mailService = mailService;
        }

        [SecuredOperation(Priority = 1)]
        [CacheRemoveAspect()]
        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                return new ErrorResult(Messages.UserNotFound);
            }

            // Mevcut email'i decrypt et
            var currentDecryptedEmail = !string.IsNullOrEmpty(user.Email) 
                ? EmailEncryptionHelper.DecryptEmail(user.Email, _configuration) ?? user.Email
                : null;

            var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();
            var normalizedCurrentEmail = currentDecryptedEmail?.Trim().ToLowerInvariant();

            // Yeni email mevcut email ile aynı mı?
            if (normalizedNewEmail == normalizedCurrentEmail)
            {
                return new ErrorResult("Yeni e-posta adresi mevcut e-posta adresi ile aynı olamaz.");
            }

            // Zaten bekleyen bir email değişikliği var mı?
            if (!string.IsNullOrEmpty(user.PendingEmail))
            {
                var pendingDecrypted = EmailEncryptionHelper.DecryptEmail(user.PendingEmail, _configuration);
                if (pendingDecrypted?.Trim().ToLowerInvariant() == normalizedNewEmail)
                {
                    return new ErrorResult("Bu e-posta adresi için zaten bir doğrulama linki gönderildi. Lütfen e-postanızı kontrol edin.");
                }
            }

            // Rate limiting kontrolü (3 dakika içinde aynı email'e tekrar gönderme engelle)
            var rateLimitKey = $"EmailChange_{normalizedNewEmail}";
            if (_cacheManager.IsAdd(rateLimitKey))
            {
                return new ErrorResult("E-posta değişikliği doğrulama kodu çok kısa süre önce gönderildi. Lütfen 3 dakika sonra tekrar deneyin.");
            }

            // Kullanıcı başına rate limiting (farklı email'lerle spam'ı önlemek için)
            var userRateLimitKey = $"EmailChange_User_{request.UserId}";
            if (_cacheManager.IsAdd(userRateLimitKey))
            {
                return new ErrorResult("Çok fazla e-posta değişikliği isteği gönderdiniz. Lütfen 3 dakika sonra tekrar deneyin.");
            }

            // Tüm user'ları çekip email'lerini kontrol et (unique kontrolü)
            var allUsers = await _userRepository.GetListAsync();
            foreach (var existingUser in allUsers)
            {
                if (existingUser.UserId != request.UserId && !string.IsNullOrEmpty(existingUser.Email))
                {
                    var decryptedEmail = EmailEncryptionHelper.DecryptEmail(existingUser.Email, _configuration);
                    if (string.IsNullOrEmpty(decryptedEmail))
                    {
                        decryptedEmail = existingUser.Email;
                    }
                    if (decryptedEmail.Trim().ToLowerInvariant() == normalizedNewEmail)
                    {
                        return new ErrorResult("Bu e-posta adresi zaten kullanılıyor.");
                    }
                }
            }

            // Email change token oluştur (6 haneli sayı)
            var random = new Random();
            var changeToken = random.Next(100000, 999999).ToString();
            var tokenExpiry = DateTime.Now.AddHours(24);

            // Yeni email'i deterministik olarak şifrele ve PendingEmail'e kaydet
            user.PendingEmail = EmailEncryptionHelper.EncryptEmailDeterministic(request.NewEmail, _configuration);
            user.EmailChangeToken = changeToken;
            user.EmailChangeTokenExpiry = tokenExpiry;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Yeni email'e doğrulama maili gönder
            try
            {
                var frontendUrl = _configuration.GetSection("AppSettings").GetSection("FrontendUrl").Value 
                    ?? "http://localhost:8081";
                
                var verificationLink = $"{frontendUrl}/verify-email-change?token={changeToken}";
                
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
        .button {{ display: inline-block; padding: 12px 30px; background-color: #0a7ea4; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .code-box {{ background-color: #fff; border: 2px solid #0a7ea4; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center; }}
        .code {{ font-family: 'Courier New', monospace; font-size: 18px; font-weight: bold; color: #0a7ea4; letter-spacing: 2px; word-break: break-all; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>E-posta Değişikliği</h1>
        </div>
        <div class='content'>
            <p>Merhaba {user.UserName},</p>
            <p>E-posta adresinizi değiştirmek için aşağıdaki doğrulama kodunu uygulamaya giriniz:</p>
            
            <div class='code-box'>
                <p style='margin: 0 0 10px 0; font-weight: bold;'>Doğrulama Kodu:</p>
                <p class='code'>{changeToken}</p>
            </div>
            
            <p><strong>Nasıl kullanılır?</strong></p>
            <ol style='text-align: left; padding-left: 20px;'>
                <li>Uygulamadaki ""E-posta Değişikliği Doğrulama"" ekranını açın</li>
                <li>Yukarıdaki doğrulama kodunu kopyalayın</li>
                <li>Kodu uygulamaya yapıştırın ve ""Doğrula"" butonuna tıklayın</li>
            </ol>
            
            <p><strong>Önemli:</strong> Bu kod 24 saat geçerlidir. 24 saat içinde doğrulama yapmazsanız, e-posta değişikliği iptal edilecektir.</p>
            <p>Eğer bu değişikliği siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz. E-posta adresiniz değişmeyecektir.</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
        </div>
    </div>
</body>
</html>";

                var senderEmail = _configuration.GetSection("EmailConfiguration").GetSection("SenderEmail").Value;
                var senderName = _configuration.GetSection("EmailConfiguration").GetSection("SenderName").Value;
                
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
                    ToAddresses = new List<EmailAddress> { new EmailAddress { Name = user.UserName, Address = request.NewEmail } },
                    FromAddresses = new List<EmailAddress> { new EmailAddress { Name = senderName, Address = senderEmail } },
                    Subject = "Cüzdanım - E-posta Değişikliği Doğrulama",
                    Content = emailContent
                };

                await _mailService.SendAsync(emailMessage);

                // Rate limiting için cache'e kaydet (3 dakika)
                _cacheManager.Add(rateLimitKey, DateTime.Now, 3);
                // Kullanıcı başına rate limiting (3 dakika)
                _cacheManager.Add(userRateLimitKey, DateTime.Now, 3);

                return new SuccessResult("E-posta değişikliği için doğrulama linki yeni e-posta adresinize gönderildi. Lütfen e-postanızı kontrol edin.");
            }
            catch (Exception ex)
            {
                // Mail gönderilemezse değişiklikleri geri al
                user.PendingEmail = null;
                user.EmailChangeToken = null;
                user.EmailChangeTokenExpiry = null;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
                
                var logger = ServiceTool.ServiceProvider.GetService<FileLogger>();
                if (logger != null)
                {
                    var errorDetails = $"Email değişikliği mail gönderme hatası - NewEmail: {request.NewEmail}, UserId: {request.UserId}, " +
                                     $"Exception Type: {ex.GetType().Name}, Message: {ex.Message}, " +
                                     $"InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "Yok")}";
                    logger.Error(errorDetails);
                }
                
                return new ErrorResult($"E-posta gönderilemedi: {ex.Message}");
            }
        }
    }
}

