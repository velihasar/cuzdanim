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

namespace Business.Handlers.Authorizations.Commands
{
    public class VerifyEmailCommand : IRequest<IResult>
    {
        public string Token { get; set; }
    }

    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, IResult>
    {
        private readonly IUserRepository _userRepository;

        public VerifyEmailCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return new ErrorResult("Geçersiz doğrulama linki.");
            }

            var user = await _userRepository.GetAsync(u => u.EmailVerificationToken == request.Token);

            if (user == null)
            {
                return new ErrorResult("Geçersiz veya süresi dolmuş doğrulama linki.");
            }

            // Token expiry kontrolü
            if (user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.Now)
            {
                return new ErrorResult("Doğrulama linkinin süresi dolmuş. Lütfen yeni bir doğrulama linki isteyin.");
            }

            // Zaten doğrulanmış mı kontrol et
            if (user.Status)
            {
                return new ErrorResult("Bu e-posta adresi zaten doğrulanmış.");
            }

            // Email doğrula
            user.Status = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return new SuccessResult("E-posta adresiniz başarıyla doğrulandı. Artık giriş yapabilirsiniz.");
        }
    }
}

