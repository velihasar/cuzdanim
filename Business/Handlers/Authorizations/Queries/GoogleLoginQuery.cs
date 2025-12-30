using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Business.Services.Authentication;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Caching;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Google;
using Core.Utilities.Security.Jwt;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Authorizations.Queries
{
    public class GoogleLoginQuery : IRequest<IDataResult<AccessToken>>
    {
        public string IdToken { get; set; } // Google'dan gelen ID token
    }

    public class GoogleLoginQueryHandler : IRequestHandler<GoogleLoginQuery, IDataResult<AccessToken>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenHelper _tokenHelper;
        private readonly ICacheManager _cacheManager;
        private readonly IConfiguration _configuration;

        public GoogleLoginQueryHandler(
            IUserRepository userRepository,
            ITokenHelper tokenHelper,
            ICacheManager cacheManager,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenHelper = tokenHelper;
            _cacheManager = cacheManager;
            _configuration = configuration;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IDataResult<AccessToken>> Handle(GoogleLoginQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                return new ErrorDataResult<AccessToken>("Google token gereklidir.");
            }

            // Google token'ı doğrula
            GoogleUserInfo googleUser;
            try
            {
                googleUser = await GoogleTokenValidator.ValidateTokenAsync(request.IdToken, _configuration);
            }
            catch (UnauthorizedAccessException ex)
            {
                // UnauthorizedAccessException zaten açıklayıcı mesaj içeriyor
                return new ErrorDataResult<AccessToken>(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Yapılandırma hatası
                return new ErrorDataResult<AccessToken>($"Google giriş yapılandırma hatası: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Genel hatalar için daha açıklayıcı mesaj
                return new ErrorDataResult<AccessToken>($"Google ile giriş yapılırken bir hata oluştu: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(googleUser.Email))
            {
                return new ErrorDataResult<AccessToken>("Google hesabınızda e-posta adresi bulunamadı.");
            }

            // Kullanıcıyı GoogleId veya Email ile bul
            User user = null;
            
            // Önce GoogleId ile ara
            if (!string.IsNullOrWhiteSpace(googleUser.GoogleId))
            {
                user = await _userRepository.GetAsync(u => u.GoogleId == googleUser.GoogleId && u.Status);
            }

            // GoogleId ile bulunamazsa, email ile ara
            if (user == null)
            {
                var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
                var encryptedEmail = EmailEncryptionHelper.EncryptEmailDeterministic(normalizedEmail, _configuration);
                
                var allUsers = await _userRepository.GetListAsync(u => u.Status);
                user = allUsers.FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) && u.Email == encryptedEmail);
                
                // Eğer deterministik encryption ile bulunamazsa, eski yöntemle devam et
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

                            if (decryptedEmail.Trim().ToLowerInvariant() == normalizedEmail)
                            {
                                user = userInList;
                                break;
                            }
                        }
                    }
                }
            }

            // Kullanıcı yoksa, yeni kullanıcı oluştur
            if (user == null)
            {
                // Username oluştur: email'in @ öncesi kısmı
                var emailPrefix = googleUser.Email.Split('@')[0].ToLowerInvariant();
                var baseUsername = emailPrefix;
                
                // Username unique kontrolü ve çakışma çözümü
                var username = baseUsername;
                var counter = 1;
                while (await _userRepository.GetAsync(u => u.UserName == username) != null)
                {
                    username = $"{baseUsername}{counter}";
                    counter++;
                    
                    // Güvenlik için maksimum deneme sayısı
                    if (counter > 1000)
                    {
                        return new ErrorDataResult<AccessToken>("Kullanıcı adı oluşturulamadı. Lütfen destek ile iletişime geçin.");
                    }
                }

                // Email'i deterministik olarak şifrele
                var encryptedEmail = EmailEncryptionHelper.EncryptEmailDeterministic(googleUser.Email, _configuration);
                
                // Yeni kullanıcı oluştur (Google ile giriş yapan kullanıcılar otomatik doğrulanmış)
                user = new User
                {
                    UserName = username,
                    Email = encryptedEmail,
                    FullName = googleUser.Name ?? $"{googleUser.GivenName} {googleUser.FamilyName}".Trim(),
                    GoogleId = googleUser.GoogleId,
                    Status = true, // Google ile giriş yapan kullanıcılar otomatik doğrulanmış
                    PasswordHash = null, // Google ile giriş yapan kullanıcıların şifresi yok
                    PasswordSalt = null
                };

                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();
            }
            else
            {
                // Mevcut kullanıcıyı güncelle (GoogleId yoksa ekle, email güncellenmiş olabilir)
                if (string.IsNullOrEmpty(user.GoogleId) && !string.IsNullOrEmpty(googleUser.GoogleId))
                {
                    user.GoogleId = googleUser.GoogleId;
                }
                
                // FullName güncellenmiş olabilir
                if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(googleUser.Name))
                {
                    user.FullName = googleUser.Name;
                }
                
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }

            // JWT token oluştur
            var claims = _userRepository.GetClaims(user.UserId);
            var accessToken = _tokenHelper.CreateToken<DArchToken>(user);
            accessToken.Claims = claims.Select(x => x.Name).ToList();

            user.RefreshToken = accessToken.RefreshToken;
            user.LastLoginDate = DateTime.Now;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _cacheManager.Add($"{CacheKeys.UserIdForClaim}={user.UserId}", claims.Select(x => x.Name));

            return new SuccessDataResult<AccessToken>(accessToken, Messages.SuccessfulLogin);
        }
    }
}

