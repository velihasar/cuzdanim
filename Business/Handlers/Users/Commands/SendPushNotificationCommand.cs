using Business.BusinessAspects;
using Business.Constants;
using Business.Services;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Users.Commands
{
    public class SendPushNotificationCommand : IRequest<IResult>
    {
        public int? UserId { get; set; } // Belirli bir kullanıcıya göndermek için (opsiyonel)
        public string Title { get; set; }
        public string Body { get; set; }
        public object Data { get; set; } // Ek data payload

        public class SendPushNotificationCommandHandler : IRequestHandler<SendPushNotificationCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IFirebaseNotificationService _firebaseNotificationService;

            public SendPushNotificationCommandHandler(
                IUserRepository userRepository,
                IFirebaseNotificationService firebaseNotificationService)
            {
                _userRepository = userRepository;
                _firebaseNotificationService = firebaseNotificationService;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(SendPushNotificationCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                {
                    return new ErrorResult("Başlık ve mesaj gereklidir.");
                }

                if (request.UserId.HasValue)
                {
                    // Belirli bir kullanıcıya gönder
                    var user = await _userRepository.GetAsync(u => u.UserId == request.UserId.Value);
                    if (user == null)
                    {
                        return new ErrorResult(Messages.UserNotFound);
                    }

                    if (string.IsNullOrWhiteSpace(user.FcmToken))
                    {
                        return new ErrorResult("Kullanıcının FCM token'ı kayıtlı değil.");
                    }

                    var success = await _firebaseNotificationService.SendNotificationAsync(
                        user.FcmToken,
                        request.Title,
                        request.Body,
                        request.Data
                    );

                    if (success)
                    {
                        return new SuccessResult("Push notification başarıyla gönderildi.");
                    }
                    else
                    {
                        return new ErrorResult("Push notification gönderilemedi.");
                    }
                }
                else
                {
                    // Tüm kullanıcılara gönder (FCM token'ı olanlar)
                    var users = await _userRepository.GetListAsync(u => !string.IsNullOrWhiteSpace(u.FcmToken) && u.Status);
                    var tokens = users.Select(u => u.FcmToken).ToArray();

                    if (tokens.Length == 0)
                    {
                        return new ErrorResult("FCM token'ı olan kullanıcı bulunamadı.");
                    }

                    var success = await _firebaseNotificationService.SendNotificationToMultipleAsync(
                        tokens,
                        request.Title,
                        request.Body,
                        request.Data
                    );

                    if (success)
                    {
                        return new SuccessResult($"{tokens.Length} kullanıcıya push notification gönderildi.");
                    }
                    else
                    {
                        return new ErrorResult("Push notification gönderilemedi.");
                    }
                }
            }
        }
    }
}

