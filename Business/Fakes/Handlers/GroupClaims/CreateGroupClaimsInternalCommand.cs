using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Business.Constants;
using Core.Entities.Concrete;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;

namespace Business.Fakes.Handlers.GroupClaims
{
    public class CreateGroupClaimsInternalCommand : IRequest<IResult>
    {
        public int GroupId { get; set; }
        public IEnumerable<int> ClaimIds { get; set; }

        public class CreateGroupClaimsInternalCommandHandler : IRequestHandler<CreateGroupClaimsInternalCommand, IResult>
        {
            private readonly IGroupClaimRepository _groupClaimRepository;

            public CreateGroupClaimsInternalCommandHandler(IGroupClaimRepository groupClaimRepository)
            {
                _groupClaimRepository = groupClaimRepository;
            }

            public async Task<IResult> Handle(CreateGroupClaimsInternalCommand request, CancellationToken cancellationToken)
            {
                if (request.ClaimIds == null || !request.ClaimIds.Any())
                {
                    return new SuccessResult("No claims to add");
                }

                var groupClaims = request.ClaimIds.Select(claimId => new GroupClaim
                {
                    GroupId = request.GroupId,
                    ClaimId = claimId
                });

                await _groupClaimRepository.BulkInsert(request.GroupId, groupClaims);
                await _groupClaimRepository.SaveChangesAsync();

                return new SuccessResult(Messages.Added);
            }
        }
    }
}

