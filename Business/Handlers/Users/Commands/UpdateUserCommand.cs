using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Users.Commands
{
    public class UpdateUserCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string MobilePhones { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }

        public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IConfiguration _configuration;

            public UpdateUserCommandHandler(IUserRepository userRepository, IConfiguration configuration)
            {
                _userRepository = userRepository;
                _configuration = configuration;
            }


            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
            {
                // GetAsync AsNoTracking kullanıyor, bu yüzden Query() ile track edilmiş entity alıyoruz
                var isThereAnyUser = await _userRepository.Query()
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId);

                if (isThereAnyUser == null)
                {
                    return new ErrorResult(Messages.UserNotFound);
                }

                // Kullanıcı adı artık email'den otomatik oluşturuluyor, güncellenemez
                if (!string.IsNullOrWhiteSpace(request.UserName))
                {
                    return new ErrorResult("Kullanıcı adı değiştirilemez. Kullanıcı adı e-posta adresinizden otomatik oluşturulmaktadır.");
                }

                // Email değişikliği için ChangeEmailCommand kullanılmalı
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    return new ErrorResult("E-posta değişikliği için lütfen e-posta değişikliği endpoint'ini kullanın.");
                }

                // Diğer alanları güncelle (null gönderilirse null yap, boş string gönderilirse boş string yap)
                if (request.FullName != null)
                {
                    isThereAnyUser.FullName = request.FullName;
                }
                if (request.MobilePhones != null)
                {
                    isThereAnyUser.MobilePhones = request.MobilePhones;
                }
                if (request.Address != null)
                {
                    isThereAnyUser.Address = request.Address;
                }
                if (request.Notes != null)
                {
                    isThereAnyUser.Notes = request.Notes;
                }

                try
                {
                    _userRepository.Update(isThereAnyUser);
                    await _userRepository.SaveChangesAsync();
                    return new SuccessResult(Messages.Updated);
                }
                catch (DbUpdateException ex)
                {
                    // Veritabanı hatası (örneğin: email uzunluğu, unique constraint vb.)
                    return new ErrorResult($"Güncelleme sırasında bir hata oluştu: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (Exception ex)
                {
                    // Genel hata
                    return new ErrorResult($"Güncelleme sırasında bir hata oluştu: {ex.Message}");
                }
            }
        }
    }
}