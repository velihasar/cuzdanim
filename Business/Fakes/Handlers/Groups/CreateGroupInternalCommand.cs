using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Fakes.Handlers.Groups
{
    public class CreateGroupInternalCommand : IRequest<IResult>
    {
        public string GroupName { get; set; }

        public class CreateGroupInternalCommandHandler : IRequestHandler<CreateGroupInternalCommand, IResult>
        {
            private readonly IGroupRepository _groupRepository;

            public CreateGroupInternalCommandHandler(IGroupRepository groupRepository)
            {
                _groupRepository = groupRepository;
            }

            public async Task<IResult> Handle(CreateGroupInternalCommand request, CancellationToken cancellationToken)
            {
                // Zaten varsa oluşturma
                var existingGroup = await _groupRepository.GetAsync(g => g.GroupName == request.GroupName);
                if (existingGroup != null)
                {
                    return new SuccessResult("Group already exists");
                }

                var group = new Group
                {
                    GroupName = request.GroupName
                };
                _groupRepository.Add(group);
                await _groupRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}

