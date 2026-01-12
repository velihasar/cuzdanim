using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.Authorizations.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.IoC;
using Core.Utilities.Mail;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.Authorizations.Commands
{
    public class RegisterUserCommand : IRequest<IResult>
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        
        [JsonPropertyName("password")]
        public string Password { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        
        [JsonPropertyName("kvkkAccepted")]
        public bool KvkkAccepted { get; set; }


        public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IUserGroupRepository _userGroupRepository;
            private readonly IGroupRepository _groupRepository;
            private readonly ICacheManager _cacheManager;
            private readonly IMailService _mailService;
            private readonly IConfiguration _configuration;
            private readonly FileLogger _logger;

            public RegisterUserCommandHandler(
                IUserRepository userRepository, 
                IUserGroupRepository userGroupRepository, 
                IGroupRepository groupRepository,
                ICacheManager cacheManager,
                IMailService mailService,
                IConfiguration configuration,
                FileLogger logger)
            {
                _userRepository = userRepository;
                _userGroupRepository = userGroupRepository;
                _groupRepository = groupRepository;
                _cacheManager = cacheManager;
                _mailService = mailService;
                _configuration = configuration;
                _logger = logger;
            }


            [ValidationAspect(typeof(RegisterUserValidator), Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    // Null check'ler
                    if (request == null)
                    {
                        _logger?.Error("[RegisterUserCommand] Request is null");
                        return new ErrorResult("Geçersiz istek. Lütfen tüm alanları doldurun.");
                    }

                    _logger?.Info($"[RegisterUserCommand] Handle started - UserName: {request.UserName}, Email: {request.Email}, KvkkAccepted: {request.KvkkAccepted}");

                    if (string.IsNullOrWhiteSpace(request.UserName))
                    {
                        return new ErrorResult("Kullanıcı adı zorunludur.");
                    }

                    if (string.IsNullOrWhiteSpace(request.Email))
                    {
                        return new ErrorResult("E-posta adresi zorunludur.");
                    }

                    if (string.IsNullOrWhiteSpace(request.Password))
                    {
                        return new ErrorResult("Şifre zorunludur.");
                    }

                    if (!request.KvkkAccepted)
                    {
                        return new ErrorResult("KVKK Aydınlatma Metni'ni kabul etmelisiniz.");
                    }

                    // Configuration kontrolü
                    if (_configuration == null)
                    {
                        _logger?.Error("[RegisterUserCommand] Configuration is null");
                        throw new Exception("Configuration is null");
                    }

                    // 1. Username kontrolü
                    var existingUserByUsername = await _userRepository.GetAsync(u => u.UserName == request.UserName);
                
                User existingUser = null;
                bool isUpdating = false;
                
                if (existingUserByUsername != null)
                {
                    if (existingUserByUsername.Status == true)
                    {
                        // Doğrulanmış kullanıcı - hata ver
                        return new ErrorResult(Messages.NameAlreadyExist);
                    }
                    // Doğrulanmamış kullanıcı - güncelleyeceğiz
                    existingUser = existingUserByUsername;
                    isUpdating = true;
                }

                // 2. Email unique kontrolü - tüm email'leri decrypt edip kontrol et
                var allUsers = await _userRepository.GetListAsync();
                var normalizedRequestEmail = request.Email.Trim().ToLowerInvariant();
                
                foreach (var userInList in allUsers)
                {
                    if (!string.IsNullOrEmpty(userInList.Email))
                    {
                        string decryptedEmail = null;
                        try
                        {
                            decryptedEmail = EmailEncryptionHelper.DecryptEmail(userInList.Email, _configuration);
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error($"[RegisterUserCommand] Email decryption error for user {userInList.UserId}: {ex.Message}");
                            // Decrypt edilemezse eski format olarak kabul et
                            decryptedEmail = null;
                        }
                        
                        // Eğer decrypt edilemezse (eski format), direkt karşılaştır
                        if (string.IsNullOrEmpty(decryptedEmail))
                        {
                            decryptedEmail = userInList.Email;
                        }
                        
                        if (decryptedEmail.Trim().ToLowerInvariant() == normalizedRequestEmail)
                        {
                            if (userInList.Status == true)
                            {
                                // Doğrulanmış kullanıcı - hata ver
                                return new ErrorResult("Bu e-posta adresi zaten kullanılıyor.");
                            }
                            
                            // Doğrulanmamış kullanıcı
                            if (existingUser == null)
                            {
                                // Eğer existingUserByUsername varsa ve aynı kullanıcıysa, onu kullan
                                // Aksi halde allUsers listesinden gelen entity'yi kullan
                                if (existingUserByUsername != null && existingUserByUsername.UserId == userInList.UserId)
                                {
                                    existingUser = existingUserByUsername;
                                }
                                else
                                {
                                    existingUser = userInList;
                                }
                                isUpdating = true;
                            }
                            else if (existingUser.UserId != userInList.UserId)
                            {
                                // Farklı kullanıcılar - username ve email çakışıyor
                                return new ErrorResult("Bu kullanıcı adı ve e-posta adresi farklı hesaplara ait. Lütfen farklı bilgiler kullanın.");
                            }
                            // Eğer existingUser zaten varsa ve aynı kullanıcıysa, existingUserByUsername'i kullan (tracking sorununu önlemek için)
                            else if (existingUserByUsername != null && existingUserByUsername.UserId == userInList.UserId)
                            {
                                // existingUserByUsername'i kullan (zaten set edilmiş)
                            }
                        }
                    }
                }

                // Rate limiting kontrolü (3 dakika içinde aynı email'e tekrar gönderme engelle)
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var rateLimitKey = $"EmailVerification_{normalizedEmail}";
                if (_cacheManager.IsAdd(rateLimitKey))
                {
                    return new ErrorResult("E-posta doğrulama kodu çok kısa süre önce gönderildi. Lütfen 3 dakika sonra tekrar deneyin.");
                }

                // 3. Email verification token oluştur (6 haneli sayı)
                var random = new Random();
                var verificationToken = random.Next(100000, 999999).ToString();
                var tokenExpiry = DateTime.Now.AddHours(24);

                HashingHelper.CreatePasswordHash(request.Password, out var passwordSalt, out var passwordHash);
                
                // Email'i deterministik olarak şifrele (arama performansı için)
                string encryptedEmail;
                try
                {
                    encryptedEmail = EmailEncryptionHelper.EncryptEmailDeterministic(request.Email, _configuration);
                }
                catch (Exception ex)
                {
                    _logger?.Error($"[RegisterUserCommand] Email encryption error: {ex.Message}, StackTrace: {ex.StackTrace}");
                    throw new Exception($"E-posta şifreleme hatası: {ex.Message}", ex);
                }
                
                User user;
                
                if (isUpdating && existingUser != null)
                {
                    // Mevcut doğrulanmamış kullanıcıyı güncelle
                    // AsNoTracking ile geldiği için, tracking ile tekrar çekmeliyiz
                    var userToUpdate = await _userRepository.GetByIdWithTrackingAsync(existingUser.UserId);
                    
                    if (userToUpdate == null)
                    {
                        return new ErrorResult("Kullanıcı bulunamadı.");
                    }
                    
                    userToUpdate.Email = encryptedEmail;
                    userToUpdate.FullName = string.IsNullOrWhiteSpace(request.FullName) ? userToUpdate.FullName : request.FullName;
                    userToUpdate.PasswordHash = passwordHash;
                    userToUpdate.PasswordSalt = passwordSalt;
                    userToUpdate.EmailVerificationToken = verificationToken;
                    userToUpdate.EmailVerificationTokenExpiry = tokenExpiry;
                    userToUpdate.Status = false; // Hala doğrulanmamış
                    
                    _userRepository.Update(userToUpdate);
                    await _userRepository.SaveChangesAsync();
                    
                    user = userToUpdate;
                }
                else
                {
                    // Yeni kullanıcı oluştur
                    user = new User
                    {
                        UserName = request.UserName,
                        Email = encryptedEmail, // Şifrelenmiş email'i kaydet
                        FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                        Status = false, // Email doğrulanana kadar false
                        EmailVerificationToken = verificationToken,
                        EmailVerificationTokenExpiry = tokenExpiry
                    };

                    _userRepository.Add(user);
                    await _userRepository.SaveChangesAsync();

                    // Add user to Default Group
                    var defaultGroup = await _groupRepository.GetAsync(g => g.GroupName == "Default Group");
                    if (defaultGroup != null)
                    {
                        var userGroup = new UserGroup
                        {
                            UserId = user.UserId,
                            GroupId = defaultGroup.Id
                        };
                        _userGroupRepository.Add(userGroup);
                        await _userGroupRepository.SaveChangesAsync();
                    }
                }

                // Email gönder
                try
                {
                    var baseUrl = _configuration.GetSection("AppSettings").GetSection("BaseUrl").Value;
                    var frontendUrl = _configuration.GetSection("AppSettings").GetSection("FrontendUrl").Value;
                    
                    if (string.IsNullOrEmpty(frontendUrl))
                    {
                        frontendUrl = "http://localhost:8081"; // Expo default port
                    }
                    
                    if (string.IsNullOrEmpty(baseUrl))
                    {
                        baseUrl = "http://localhost:5000"; // Fallback
                    }

                    // Frontend URL'ine yönlendir - mobil uygulama deep link
                    var verificationLink = $"{frontendUrl}/verify-email?token={verificationToken}";
                    
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
            <h1>Cüzdanım'a Hoş Geldiniz!</h1>
        </div>
        <div class='content'>
            <p>Merhaba {request.UserName},</p>
            <p>Hesabınızı oluşturduğunuz için teşekkür ederiz. Hesabınızı aktifleştirmek için aşağıdaki doğrulama kodunu uygulamaya giriniz:</p>
            
            <div class='code-box'>
                <p style='margin: 0 0 10px 0; font-weight: bold;'>Doğrulama Kodu:</p>
                <p class='code'>{verificationToken}</p>
            </div>
            
            <p><strong>Nasıl kullanılır?</strong></p>
            <ol style='text-align: left; padding-left: 20px;'>
                <li>Uygulamadaki ""E-posta Doğrulama"" ekranını açın</li>
                <li>Yukarıdaki doğrulama kodunu kopyalayın</li>
                <li>Kodu uygulamaya yapıştırın ve ""Doğrula"" butonuna tıklayın</li>
            </ol>
            
            <p><strong>Önemli:</strong> Bu kod 24 saat geçerlidir. 24 saat içinde doğrulama yapmazsanız, hesabınız aktif olmayacaktır.</p>
            <p>Eğer bu hesabı siz oluşturmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
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
                        senderEmail = "noreply@cuzdanim.com"; // Fallback
                    }
                    
                    if (string.IsNullOrEmpty(senderName))
                    {
                        senderName = "Cüzdanım";
                    }

                    var emailMessage = new EmailMessage
                    {
                        ToAddresses = new List<EmailAddress> { new EmailAddress { Name = request.UserName, Address = request.Email } },
                        FromAddresses = new List<EmailAddress> { new EmailAddress { Name = senderName, Address = senderEmail } },
                        Subject = "Cüzdanım - E-posta Doğrulama",
                        Content = emailContent
                    };

                    await _mailService.SendAsync(emailMessage);

                    // Email gönderme başarılı - logla
                    var successLog = $"[RegisterUserCommand] Email gönderildi - Email: {request.Email}, UserName: {request.UserName}, VerificationToken: {verificationToken}";
                    _logger?.Info(successLog);
                    Console.WriteLine(successLog);

                    // Rate limiting için cache'e kaydet (3 dakika)
                    _cacheManager.Add(rateLimitKey, DateTime.Now, 3);
                }
                catch (Exception ex)
                {
                    // Email gönderme hatası - hem FileLogger hem Console'a logla
                    var errorDetails = $"[RegisterUserCommand] Mail gönderme hatası - Email: {request.Email}, UserName: {request.UserName}, " +
                                     $"Exception Type: {ex.GetType().Name}, Message: {ex.Message}, " +
                                     $"StackTrace: {ex.StackTrace}, " +
                                     $"InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "Yok")}";
                    
                    _logger?.Error(errorDetails);
                    Console.WriteLine(errorDetails);
                    Console.Error.WriteLine(errorDetails);
                    
                    // Email gönderme hatası olsa bile kullanıcı kaydedildi
                    // Kullanıcıya mail gönderilemediği bilgisini ver
                    var userFriendlyMessage = ex.Message.Contains("SMTP") 
                        ? "E-posta sunucu ayarlarında bir sorun var. Lütfen yönetici ile iletişime geçin." 
                        : $"Doğrulama e-postası gönderilemedi. Lütfen yönetici ile iletişime geçin. (Hata: {ex.Message})";
                    
                    return new SuccessResult($"Kayıt işlemi tamamlandı ancak {userFriendlyMessage}");
                }

                if (isUpdating)
                {
                    return new SuccessResult("Yeni doğrulama kodu e-posta adresinize gönderildi. Lütfen e-postanızı kontrol edin.");
                }
                
                return new SuccessResult("Kayıt işlemi başarılı. E-posta adresinize gönderilen doğrulama kodunu giriniz.");
                }
                catch (Exception ex)
                {
                    // Genel exception handling
                    _logger?.Error($"[RegisterUserCommand] Unhandled exception - " +
                                $"Message: {ex.Message}, " +
                                $"StackTrace: {ex.StackTrace}, " +
                                $"InnerException: {(ex.InnerException != null ? ex.InnerException.Message : "None")}, " +
                                $"UserName: {request?.UserName}, " +
                                $"Email: {request?.Email}");
                    
                    // Exception'ı tekrar fırlat - ExceptionMiddleware yakalayacak
                    throw;
                }
            }
        }
    }
}