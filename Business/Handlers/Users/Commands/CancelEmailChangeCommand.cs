using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IResult = Core.Utilities.Results.IResult;

namespace Business.Handlers.Users.Commands
{
    public class CancelEmailChangeCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
    }

    public class CancelEmailChangeCommandHandler : IRequestHandler<CancelEmailChangeCommand, IResult>
    {
        private readonly IUserRepository _userRepository;

        public CancelEmailChangeCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [SecuredOperation(Priority = 1)]
        [CacheRemoveAspect()]
        [LogAspect(typeof(FileLogger))]
        public async Task<IResult> Handle(CancelEmailChangeCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                return new ErrorResult(Messages.UserNotFound);
            }

            // Bekleyen email değişikliği var mı?
            if (string.IsNullOrEmpty(user.PendingEmail))
            {
                return new ErrorResult("İptal edilecek e-posta değişikliği bulunamadı.");
            }

            // PendingEmail'i temizle (Email zaten eski email'i tutuyor, değişiklik yapılmadı)
            user.PendingEmail = null;
            user.EmailChangeToken = null;
            user.EmailChangeTokenExpiry = null;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return new SuccessResult("E-posta değişikliği iptal edildi.");
        }
    }
}

