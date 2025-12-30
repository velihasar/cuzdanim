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

namespace Business.Handlers.SystemSettings.Commands
{
    public class UpdateSystemSettingCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public class UpdateSystemSettingCommandHandler : IRequestHandler<UpdateSystemSettingCommand, IResult>
        {
            private readonly ISystemSettingRepository _systemSettingRepository;

            public UpdateSystemSettingCommandHandler(ISystemSettingRepository systemSettingRepository)
            {
                _systemSettingRepository = systemSettingRepository;
            }

            [AdminOnlyOperation]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(UpdateSystemSettingCommand request, CancellationToken cancellationToken)
            {
                var setting = await _systemSettingRepository.GetAsync(s => s.Id == request.Id);
                if (setting == null)
                {
                    return new ErrorResult("System setting not found.");
                }

                // Key değişiyorsa unique kontrolü yap
                if (!string.IsNullOrWhiteSpace(request.Key) && setting.Key != request.Key)
                {
                    var existingSetting = await _systemSettingRepository.GetAsync(s => s.Key == request.Key && s.Id != request.Id);
                    if (existingSetting != null)
                    {
                        return new ErrorResult("Bu key zaten kullanılıyor.");
                    }
                    setting.Key = request.Key;
                }

                if (!string.IsNullOrWhiteSpace(request.Value))
                {
                    setting.Value = request.Value;
                }

                if (request.Description != null)
                {
                    setting.Description = request.Description;
                }

                if (request.Category != null)
                {
                    setting.Category = request.Category;
                }

                _systemSettingRepository.Update(setting);
                await _systemSettingRepository.SaveChangesAsync();

                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

