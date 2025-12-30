using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Users.Commands
{
    public class CreateUserCommand : IRequest<IResult>
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobilePhones { get; set; }
        public bool Status { get; set; }
        public DateTime BirthDate { get; set; }
        public int Gender { get; set; }
        public DateTime RecordDate { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public DateTime UpdateContactDate { get; set; }
        public string Password { get; set; }


        public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, IResult>
        {
            private readonly IUserRepository _userRepository;
            private readonly IConfiguration _configuration;

            public CreateUserCommandHandler(IUserRepository userRepository, IConfiguration configuration)
            {
                _userRepository = userRepository;
                _configuration = configuration;
            }

            [SecuredOperation(Priority = 1)]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
            {
                // Email unique kontrolü - tüm email'leri decrypt edip kontrol et
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var allUsers = await _userRepository.GetListAsync();
                    var normalizedRequestEmail = request.Email.Trim().ToLowerInvariant();
                    
                    foreach (var existingUser in allUsers)
                    {
                        if (!string.IsNullOrEmpty(existingUser.Email))
                        {
                            var decryptedEmail = EmailEncryptionHelper.DecryptEmail(existingUser.Email, _configuration);
                            if (string.IsNullOrEmpty(decryptedEmail))
                            {
                                decryptedEmail = existingUser.Email;
                            }
                            if (decryptedEmail.Trim().ToLowerInvariant() == normalizedRequestEmail)
                            {
                                return new ErrorResult(Messages.NameAlreadyExist);
                            }
                        }
                    }
                }

                // Email'i deterministik olarak şifrele (arama performansı için)
                var encryptedEmail = !string.IsNullOrWhiteSpace(request.Email) 
                    ? EmailEncryptionHelper.EncryptEmailDeterministic(request.Email, _configuration) 
                    : null;

                var user = new User
                {
                    Email = encryptedEmail, // Şifrelenmiş email'i kaydet
                    FullName = request.FullName,
                    Status = true,
                    Address = request.Address,
                    BirthDate = request.BirthDate,
                    Gender = request.Gender,
                    Notes = request.Notes,
                    MobilePhones = request.MobilePhones
                };

                _userRepository.Add(user);
                await _userRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}