using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.Authorizations.Commands
{
    public class ResetPasswordCommand : IRequest<IResult>
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, IResult>
    {
        private readonly IUserRepository _userRepository;

        public ResetPasswordCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return new ErrorResult("Geçersiz doğrulama kodu.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new ErrorResult("Yeni şifre gereklidir.");
            }

            // Şifre validasyonu (en az 8 karakter, büyük harf, küçük harf, rakam, özel karakter)
            if (request.NewPassword.Length < 8)
            {
                return new ErrorResult("Şifre en az 8 karakter olmalıdır.");
            }

            var user = await _userRepository.GetAsync(u => u.PasswordResetToken == request.Token);

            if (user == null)
            {
                return new ErrorResult("Geçersiz veya süresi dolmuş doğrulama kodu.");
            }

            // Token expiry kontrolü
            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.Now)
            {
                return new ErrorResult("Doğrulama kodunun süresi dolmuş. Lütfen yeni bir şifre sıfırlama isteği gönderin.");
            }

            // Kullanıcıyı tracking ile çek
            var userToUpdate = await _userRepository.GetByIdWithTrackingAsync(user.UserId);
            if (userToUpdate == null)
            {
                return new ErrorResult("Kullanıcı bulunamadı.");
            }

            // Yeni şifreyi hash'le
            HashingHelper.CreatePasswordHash(request.NewPassword, out var passwordSalt, out var passwordHash);

            // Şifreyi güncelle
            userToUpdate.PasswordHash = passwordHash;
            userToUpdate.PasswordSalt = passwordSalt;
            userToUpdate.PasswordResetToken = null;
            userToUpdate.PasswordResetTokenExpiry = null;

            _userRepository.Update(userToUpdate);
            await _userRepository.SaveChangesAsync();

            return new SuccessResult("Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.");
        }
    }
}

