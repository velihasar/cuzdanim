using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Business.BusinessAspects;
using Business.Constants;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Dtos;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Users.Queries
{
    public class GetUserQuery : IRequest<IDataResult<UserDto>>
    {
        public int UserId { get; set; }

        public class GetUserQueryHandler : IRequestHandler<GetUserQuery, IDataResult<UserDto>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMapper _mapper;
            private readonly IConfiguration _configuration;

            public GetUserQueryHandler(IUserRepository userRepository, IMapper mapper, IConfiguration configuration)
            {
                _userRepository = userRepository;
                _mapper = mapper;
                _configuration = configuration;
            }

            [SecuredOperation(Priority = 1)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
            {
                var user = await _userRepository.GetAsync(p => p.UserId == request.UserId);
                if (user == null)
                {
                    return new ErrorDataResult<UserDto>(Messages.UserNotFound);
                }
                var userDto = _mapper.Map<UserDto>(user);
                
                // Email'i decrypt et
                if (!string.IsNullOrEmpty(user.Email))
                {
                    userDto.Email = EmailEncryptionHelper.DecryptEmail(user.Email, _configuration) ?? user.Email;
                }
                
                return new SuccessDataResult<UserDto>(userDto);
            }
        }
    }
}