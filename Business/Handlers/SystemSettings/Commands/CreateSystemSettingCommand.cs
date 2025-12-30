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
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.SystemSettings.Commands
{
    public class CreateSystemSettingCommand : IRequest<IResult>
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public class CreateSystemSettingCommandHandler : IRequestHandler<CreateSystemSettingCommand, IResult>
        {
            private readonly ISystemSettingRepository _systemSettingRepository;

            public CreateSystemSettingCommandHandler(ISystemSettingRepository systemSettingRepository)
            {
                _systemSettingRepository = systemSettingRepository;
            }

            [AdminOnlyOperation]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(CreateSystemSettingCommand request, CancellationToken cancellationToken)
            {
                // Key unique kontrolü
                var existingSetting = await _systemSettingRepository.GetAsync(s => s.Key == request.Key);
                if (existingSetting != null)
                {
                    return new ErrorResult("Bu key zaten kullanılıyor.");
                }

                var systemSetting = new SystemSetting
                {
                    Key = request.Key,
                    Value = request.Value,
                    Description = request.Description,
                    Category = request.Category
                };

                _systemSettingRepository.Add(systemSetting);
                await _systemSettingRepository.SaveChangesAsync();

                return new SuccessResult(Messages.Added);
            }
        }
    }
}

