using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.Users.Commands
{
    public class UserChangePasswordCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string Password { get; set; }

        public class UserChangePasswordCommandHandler : IRequestHandler<UserChangePasswordCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMediator _mediator;

            public UserChangePasswordCommandHandler(IUserRepository userRepository, IMediator mediator)
            {
                _userRepository = userRepository;
                _mediator = mediator;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UserChangePasswordCommand request, CancellationToken cancellationToken)
            {
                // Kullanıcı kontrolü - tracking ile çek (güncelleme için)
                var isThereAnyUser = await _userRepository.GetByIdWithTrackingAsync(request.UserId);
                if (isThereAnyUser == null)
                {
                    return new ErrorResult(Messages.UserNotFound);
                }

                // Google ile giriş yapan kullanıcılar için kontrol (PasswordHash ve PasswordSalt null ise)
                // Bu kullanıcılar için şifre değiştirme işlemi doğrudan yapılabilir
                bool isGoogleUser = (isThereAnyUser.PasswordHash == null || isThereAnyUser.PasswordHash.Length == 0) && 
                                   (isThereAnyUser.PasswordSalt == null || isThereAnyUser.PasswordSalt.Length == 0) && 
                                   !string.IsNullOrEmpty(isThereAnyUser.GoogleId);

                // Şifre null/boş kontrolü
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return new ErrorResult("Şifre gereklidir.");
                }

                // Şifre validasyonu (en az 8 karakter, büyük harf, küçük harf, rakam, özel karakter)
                if (request.Password.Length < 8)
                {
                    return new ErrorResult("Şifre en az 8 karakter olmalıdır.");
                }

                if (!Regex.IsMatch(request.Password, @"[A-Z]"))
                {
                    return new ErrorResult("Şifre en az 1 büyük harf içermelidir.");
                }

                if (!Regex.IsMatch(request.Password, @"[a-z]"))
                {
                    return new ErrorResult("Şifre en az 1 küçük harf içermelidir.");
                }

                if (!Regex.IsMatch(request.Password, @"[0-9]"))
                {
                    return new ErrorResult("Şifre en az 1 rakam içermelidir.");
                }

                if (!Regex.IsMatch(request.Password, @"[^a-zA-Z0-9]"))
                {
                    return new ErrorResult("Şifre en az 1 özel karakter içermelidir.");
                }

                // Şifreyi hash'le
                HashingHelper.CreatePasswordHash(request.Password, out var passwordSalt, out var passwordHash);

                // Şifreyi güncelle (Google ile giriş yapan kullanıcılar için de çalışır)
                isThereAnyUser.PasswordHash = passwordHash;
                isThereAnyUser.PasswordSalt = passwordSalt;

                _userRepository.Update(isThereAnyUser);
                await _userRepository.SaveChangesAsync();
                
                var successMessage = isGoogleUser 
                    ? "Şifreniz başarıyla oluşturuldu. Artık şifrenizle de giriş yapabilirsiniz." 
                    : "Şifreniz başarıyla güncellendi.";
                    
                return new SuccessResult(successMessage);
            }
        }
    }
}