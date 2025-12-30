using System;
using System.Linq;
using System.Text.RegularExpressions;
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
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Jwt;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Authorizations.Queries
{
    public class LoginUserQuery : IRequest<IDataResult<AccessToken>>
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public class LoginUserQueryHandler : IRequestHandler<LoginUserQuery, IDataResult<AccessToken>>
        {
            private readonly IUserRepository _userRepository;
            private readonly ITokenHelper _tokenHelper;
            private readonly IMediator _mediator;
            private readonly ICacheManager _cacheManager;
            private readonly IConfiguration _configuration;

            public LoginUserQueryHandler(
                IUserRepository userRepository, 
                ITokenHelper tokenHelper, 
                IMediator mediator, 
                ICacheManager cacheManager,
                IConfiguration configuration)
            {
                _userRepository = userRepository;
                _tokenHelper = tokenHelper;
                _mediator = mediator;
                _cacheManager = cacheManager;
                _configuration = configuration;
            }

            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<AccessToken>> Handle(LoginUserQuery request, CancellationToken cancellationToken)
            {
                User user = null;
                var loginValue = request.UserName?.Trim();

                if (string.IsNullOrWhiteSpace(loginValue))
                {
                    return new ErrorDataResult<AccessToken>(Messages.UserNotFound);
                }

                // Email formatı kontrolü
                var isEmail = Regex.IsMatch(loginValue, @"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase);

                if (isEmail)
                {
                    // Email ile giriş - önce deterministik encryption ile direkt arama yap
                    var normalizedEmail = loginValue.Trim().ToLowerInvariant();
                    var encryptedEmail = EmailEncryptionHelper.EncryptEmailDeterministic(normalizedEmail, _configuration);
                    
                    // Direkt veritabanında arama (performans için)
                    var allUsers = await _userRepository.GetListAsync(u => u.Status);
                    user = allUsers.FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) && u.Email == encryptedEmail);
                    
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

                                if (decryptedEmail.Trim().ToLowerInvariant() == normalizedEmail)
                                {
                                    user = userInList;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Username ile giriş
                    user = await _userRepository.GetAsync(u => u.UserName == loginValue && u.Status);
                }

                if (user == null)
                {
                    return new ErrorDataResult<AccessToken>(Messages.UserNotFound);
                }

                if (!HashingHelper.VerifyPasswordHash(request.Password, user.PasswordSalt, user.PasswordHash))
                {
                    return new ErrorDataResult<AccessToken>(Messages.PasswordError);
                }

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
}