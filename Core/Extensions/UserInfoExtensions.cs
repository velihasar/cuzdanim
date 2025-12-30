using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Core.Utilities.IoC;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Extensions
{
    public static class UserInfoExtensions
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static int GetUserId()
        {
            // SecuredOperation ile aynı mantık: constructor'da bir kez al ve cache'le
            if (_httpContextAccessor == null)
            {
                if (ServiceTool.ServiceProvider == null)
                {
                    return 0;
                }
                _httpContextAccessor = ServiceTool.ServiceProvider.GetService<IHttpContextAccessor>();
            }

            if (_httpContextAccessor?.HttpContext == null || _httpContextAccessor.HttpContext.User == null)
            {
                return 0;
            }

            var result = _httpContextAccessor.HttpContext.User.Claims
                ?.FirstOrDefault(x => x.Type.EndsWith("nameidentifier"))?.Value;
            
            return result != null ? Convert.ToInt32(result) : 0;
        }

    }
}
