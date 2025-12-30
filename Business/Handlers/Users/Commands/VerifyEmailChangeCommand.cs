using System;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.Users.Commands
{
    public class VerifyEmailChangeCommand : IRequest<IResult>
    {
        public string Token { get; set; }
    }

    public class VerifyEmailChangeCommandHandler : IRequestHandler<VerifyEmailChangeCommand, IResult>
    {
        private readonly IUserRepository _userRepository;

        public VerifyEmailChangeCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(VerifyEmailChangeCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return new ErrorResult("Geçersiz doğrulama linki.");
            }

            var user = await _userRepository.GetAsync(u => u.EmailChangeToken == request.Token);

            if (user == null)
            {
                return new ErrorResult("Geçersiz veya süresi dolmuş doğrulama linki.");
            }

            // Token expiry kontrolü
            if (user.EmailChangeTokenExpiry == null || user.EmailChangeTokenExpiry < DateTime.Now)
            {
                return new ErrorResult("Doğrulama linkinin süresi dolmuş. Lütfen yeni bir e-posta değişikliği isteyin.");
            }

            // PendingEmail kontrolü
            if (string.IsNullOrEmpty(user.PendingEmail))
            {
                return new ErrorResult("Bekleyen e-posta değişikliği bulunamadı.");
            }

            // Email değişikliğini onayla
            // PendingEmail'i Email'e taşı (eski email zaten Email'de, onu kaybediyoruz ama bu normal)
            user.Email = user.PendingEmail;
            user.PendingEmail = null;
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return new SuccessResult("E-posta adresiniz başarıyla güncellendi.");
        }
    }
}

