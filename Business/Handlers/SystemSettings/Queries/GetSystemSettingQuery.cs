using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Handlers.SystemSettings.Queries
{
    public class GetSystemSettingQuery : IRequest<IDataResult<SystemSetting>>
    {
        public int Id { get; set; }
        public string Key { get; set; } // Opsiyonel: Key'e göre de arama yapılabilir

        public class GetSystemSettingQueryHandler : IRequestHandler<GetSystemSettingQuery, IDataResult<SystemSetting>>
        {
            private readonly ISystemSettingRepository _systemSettingRepository;

            public GetSystemSettingQueryHandler(ISystemSettingRepository systemSettingRepository)
            {
                _systemSettingRepository = systemSettingRepository;
            }

            [AdminOnlyOperation]
            [CacheAspect(10)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<SystemSetting>> Handle(GetSystemSettingQuery request, CancellationToken cancellationToken)
            {
                SystemSetting setting = null;

                if (request.Id > 0)
                {
                    setting = await _systemSettingRepository.GetAsync(s => s.Id == request.Id);
                }
                else if (!string.IsNullOrWhiteSpace(request.Key))
                {
                    setting = await _systemSettingRepository.GetAsync(s => s.Key == request.Key);
                }

                if (setting == null)
                {
                    return new ErrorDataResult<SystemSetting>("System setting not found.");
                }

                return new SuccessDataResult<SystemSetting>(setting);
            }
        }
    }
}

