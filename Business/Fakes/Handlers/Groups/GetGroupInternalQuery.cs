using System.Threading;
using System.Threading.Tasks;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Fakes.Handlers.Groups
{
    public class GetGroupInternalQuery : IRequest<IDataResult<Group>>
    {
        public string GroupName { get; set; }

        public class GetGroupInternalQueryHandler : IRequestHandler<GetGroupInternalQuery, IDataResult<Group>>
        {
            private readonly IGroupRepository _groupRepository;

            public GetGroupInternalQueryHandler(IGroupRepository groupRepository)
            {
                _groupRepository = groupRepository;
            }

            public async Task<IDataResult<Group>> Handle(GetGroupInternalQuery request, CancellationToken cancellationToken)
            {
                var group = await _groupRepository.GetAsync(g => g.GroupName == request.GroupName);
                if (group == null)
                {
                    return new ErrorDataResult<Group>("Group not found");
                }
                return new SuccessDataResult<Group>(group);
            }
        }
    }
}

