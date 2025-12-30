using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Dtos;
using Core.Utilities.Results;
using Core.Utilities.Security.Encyption;
using DataAccess.Abstract;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Business.Handlers.Users.Queries
{
    public class GetUsersQuery : IRequest<IDataResult<IEnumerable<UserDto>>>
    {
        public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IDataResult<IEnumerable<UserDto>>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IMapper _mapper;
            private readonly IConfiguration _configuration;

            public GetUsersQueryHandler(IUserRepository userRepository, IMapper mapper, IConfiguration configuration)
            {
                _userRepository = userRepository;
                _mapper = mapper;
                _configuration = configuration;
            }

            [SecuredOperation(Priority = 1)]
            [PerformanceAspect(5)]
            [CacheAspect(10)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
            {
                var userList = await _userRepository.GetListAsync();
                var userDtoList = _mapper.Map<IEnumerable<UserDto>>(userList);
                
                // Her kullanıcının email'ini decrypt et
                foreach (var userDto in userDtoList)
                {
                    if (!string.IsNullOrEmpty(userDto.Email))
                    {
                        var user = userList.FirstOrDefault(u => u.UserId == userDto.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            userDto.Email = EmailEncryptionHelper.DecryptEmail(user.Email, _configuration) ?? user.Email;
                        }
                    }
                }
                
                return new SuccessDataResult<IEnumerable<UserDto>>(userDtoList);
            }
        }
    }
}