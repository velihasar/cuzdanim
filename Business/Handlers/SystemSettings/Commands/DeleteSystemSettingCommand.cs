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
    public class DeleteSystemSettingCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteSystemSettingCommandHandler : IRequestHandler<DeleteSystemSettingCommand, IResult>
        {
            private readonly ISystemSettingRepository _systemSettingRepository;

            public DeleteSystemSettingCommandHandler(ISystemSettingRepository systemSettingRepository)
            {
                _systemSettingRepository = systemSettingRepository;
            }

            [AdminOnlyOperation]
            [CacheRemoveAspect()]
            [LogAspect(typeof(FileLogger))]
            public async Task<IResult> Handle(DeleteSystemSettingCommand request, CancellationToken cancellationToken)
            {
                var setting = await _systemSettingRepository.GetAsync(s => s.Id == request.Id);
                if (setting == null)
                {
                    return new ErrorResult("System setting not found.");
                }

                _systemSettingRepository.Delete(setting);
                await _systemSettingRepository.SaveChangesAsync();

                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

