using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Users.Commands
{
    public class RegisterFcmTokenCommand : IRequest<Core.Utilities.Results.IResult>
    {
        public string FcmToken { get; set; }

        public class RegisterFcmTokenCommandHandler : IRequestHandler<RegisterFcmTokenCommand, Core.Utilities.Results.IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IHttpContextAccessor _httpContextAccessor;

            public RegisterFcmTokenCommandHandler(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
            {
                _userRepository = userRepository;
                _httpContextAccessor = httpContextAccessor;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<Core.Utilities.Results.IResult> Handle(RegisterFcmTokenCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.FcmToken))
                {
                    return new ErrorResult("FCM token gereklidir.");
                }

                // UserId'yi token'dan al
                var userIdClaim = _httpContextAccessor?.HttpContext?.User?.Claims
                    ?.FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return new ErrorResult(Messages.AuthorizationsDenied);
                }

                var user = await _userRepository.GetByIdWithTrackingAsync(userId);
                if (user == null)
                {
                    return new ErrorResult(Messages.UserNotFound);
                }

                user.FcmToken = request.FcmToken;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                return new SuccessResult("FCM token başarıyla kaydedildi.");
            }
        }
    }
}

