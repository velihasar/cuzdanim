using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Business.BusinessAspects;
using Business.Fakes.Handlers.Authorizations;
using Business.Fakes.Handlers.OperationClaims;
using Business.Fakes.Handlers.UserClaims;
using Business.Fakes.Handlers.Groups;
using Business.Fakes.Handlers.GroupClaims;
using Core.Utilities.IoC;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Helpers
{
    public static class OperationClaimCreatorMiddleware
    {
        public static async Task UseDbOperationClaimCreator(this IApplicationBuilder app)
        {
            var mediator = ServiceTool.ServiceProvider.GetService<IMediator>();
            foreach (var operationName in GetOperationNames())
            {
                await mediator.Send(new CreateOperationClaimInternalCommand
                {
                    ClaimName = operationName
                });
            }

            var operationClaims = (await mediator.Send(new GetOperationClaimsInternalQuery())).Data;
            
            // Default Group'u oluştur (yoksa)
            var defaultGroupResult = await mediator.Send(new GetGroupInternalQuery
            {
                GroupName = "Default Group"
            });
            
            // Eğer Default Group yoksa oluştur
            if (!defaultGroupResult.Success || defaultGroupResult.Data == null)
            {
                await mediator.Send(new CreateGroupInternalCommand
                {
                    GroupName = "Default Group"
                });
                
                // Tekrar sorgula
                defaultGroupResult = await mediator.Send(new GetGroupInternalQuery
                {
                    GroupName = "Default Group"
                });
            }
            
            // Add all operation claims to Default Group
            if (defaultGroupResult.Success && defaultGroupResult.Data != null && operationClaims != null && operationClaims.Any())
            {
                await mediator.Send(new CreateGroupClaimsInternalCommand
                {
                    GroupId = defaultGroupResult.Data.Id,
                    ClaimIds = operationClaims.Select(oc => oc.Id)
                });
            }
            
            // Admin kullanıcısı artık AdminUserCreatorMiddleware tarafından oluşturuluyor
            // Burada admin oluşturma kodu kaldırıldı
        }

        private static IEnumerable<string> GetOperationNames()
        {
            var assemblies = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x =>
                    // runtime generated anonmous type'larin assemblysi olmadigi icin null cek yap
                    x.Namespace != null && x.Namespace.StartsWith("Business.Handlers") &&
                    (x.Name.EndsWith("Command") || x.Name.EndsWith("Query")) &&
                    x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

            return assemblies.Select(x => x.Name).Distinct().ToList();
        }
    }
}
