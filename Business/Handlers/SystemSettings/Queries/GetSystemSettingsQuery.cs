using System.Collections.Generic;
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
    public class GetSystemSettingsQuery : IRequest<IDataResult<IEnumerable<SystemSetting>>>
    {
        public string Category { get; set; } // Opsiyonel: Kategoriye göre filtreleme

        public class GetSystemSettingsQueryHandler : IRequestHandler<GetSystemSettingsQuery, IDataResult<IEnumerable<SystemSetting>>>
        {
            private readonly ISystemSettingRepository _systemSettingRepository;

            public GetSystemSettingsQueryHandler(ISystemSettingRepository systemSettingRepository)
            {
                _systemSettingRepository = systemSettingRepository;
            }

            [AdminOnlyOperation]
            [CacheAspect(10)]
            [LogAspect(typeof(FileLogger))]
            public async Task<IDataResult<IEnumerable<SystemSetting>>> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
            {
                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    var settings = await _systemSettingRepository.GetListAsync(s => s.Category == request.Category);
                    return new SuccessDataResult<IEnumerable<SystemSetting>>(settings);
                }

                var allSettings = await _systemSettingRepository.GetListAsync();
                return new SuccessDataResult<IEnumerable<SystemSetting>>(allSettings);
            }
        }
    }
}

