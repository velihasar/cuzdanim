using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Business.Constants;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using DataAccess.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Business.BusinessAspects
{
    /// <summary>
    /// This Aspect controls admin-only operations.
    /// Only users with UserId = 1 (System Admin) or users in "Admin" group can access.
    /// </summary>
    public class AdminOnlyOperation : MethodInterception
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheManager _cacheManager;

        public AdminOnlyOperation()
        {
            _httpContextAccessor = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>();
            _cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
        }

        protected override void OnBefore(IInvocation invocation)
        {
            // HttpContext ve User kontrolü
            if (_httpContextAccessor?.HttpContext == null || _httpContextAccessor.HttpContext.User == null)
            {
                throw new SecurityException(Messages.AuthorizationsDenied);
            }

            var userId = _httpContextAccessor.HttpContext.User.Claims
                ?.FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new SecurityException(Messages.AuthorizationsDenied);
            }

            // System Admin kontrolü (UserId = 1)
            if (!int.TryParse(userId, out var userIdInt))
            {
                throw new SecurityException(Messages.AuthorizationsDenied);
            }

            if (userIdInt == 1)
            {
                return; // System Admin (UserId = 1) - izin ver
            }

            // Admin grubu kontrolü - önce cache'den kontrol et
            var userClaims = _cacheManager.Get<System.Collections.Generic.IEnumerable<string>>($"{Core.CrossCuttingConcerns.Caching.CacheKeys.UserIdForClaim}={userId}");
            if (userClaims != null)
            {
                // Admin ile başlayan veya Admin içeren claim'leri kontrol et
                var hasAdminClaim = userClaims.Any(claim => 
                    claim.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    claim.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                    claim.StartsWith("Admin", StringComparison.OrdinalIgnoreCase));
                
                if (hasAdminClaim)
                {
                    return; // Admin grubunda - izin ver
                }
            }
            else
            {
                // Cache boşsa, veritabanından kontrol et
                if (userIdInt > 0)
                {
                    var userGroupRepository = ServiceTool.ServiceProvider.GetService<IUserGroupRepository>();
                    var groupRepository = ServiceTool.ServiceProvider.GetService<IGroupRepository>();
                    
                    if (userGroupRepository != null && groupRepository != null)
                    {
                        try
                        {
                            // Admin grubunu bul
                            var adminGroupsTask = groupRepository.GetListAsync(g => g.GroupName.Equals("Admin", StringComparison.OrdinalIgnoreCase));
                            var adminGroups = adminGroupsTask?.Result;
                            var adminGroup = adminGroups?.FirstOrDefault();
                            
                            if (adminGroup != null)
                            {
                                // Kullanıcının Admin grubunda olup olmadığını kontrol et
                                var userGroupsTask = userGroupRepository.GetListAsync(ug => ug.UserId == userIdInt);
                                var userGroups = userGroupsTask?.Result;
                                
                                if (userGroups != null)
                                {
                                    var isInAdminGroup = userGroups.Any(ug => ug.GroupId == adminGroup.Id);
                                    
                                    if (isInAdminGroup)
                                    {
                                        return; // Admin grubunda - izin ver
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Veritabanı hatası durumunda sessizce devam et
                            // Exception detayları log'lanabilir ama şu an için sessizce devam ediyoruz
                        }
                    }
                }
            }

            // Admin değilse erişim reddedildi
            throw new SecurityException(Messages.AuthorizationsDenied);
        }
    }
}

