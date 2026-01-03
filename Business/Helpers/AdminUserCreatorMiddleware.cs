using System;
using System.Linq;
using System.Threading.Tasks;
using Business.Fakes.Handlers.OperationClaims;
using Business.Fakes.Handlers.UserClaims;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Utilities.IoC;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Helpers
{
    public static class AdminUserCreatorMiddleware
    {
        public static async Task UseAdminUserCreator(this IApplicationBuilder app)
        {
            var mediator = ServiceTool.ServiceProvider.GetService<IMediator>();
            var configuration = ServiceTool.ServiceProvider.GetService<IConfiguration>();
            var userRepository = ServiceTool.ServiceProvider.GetService<IUserRepository>();
            var dbContext = ServiceTool.ServiceProvider.GetService<ProjectDbContext>();

            // appsettings.json'dan admin bilgilerini al
            var adminEmail = configuration["AdminSettings:Email"];
            var adminUserName = configuration["AdminSettings:UserName"];
            var adminPassword = configuration["AdminSettings:Password"];
            var adminFullName = configuration["AdminSettings:FullName"] ?? "System Administrator";

            // Environment variable'dan gelen değerleri kontrol et (${VAR} formatından geliyorsa null olabilir)
            var isPasswordFromEnv = !string.IsNullOrWhiteSpace(adminPassword) && !adminPassword.StartsWith("${");
            var isUserNameFromEnv = !string.IsNullOrWhiteSpace(adminUserName) && !adminUserName.StartsWith("${");
            var isEmailFromEnv = !string.IsNullOrWhiteSpace(adminEmail) && !adminEmail.StartsWith("${");
            var isFullNameFromEnv = !string.IsNullOrWhiteSpace(adminFullName) && !adminFullName.StartsWith("${");

            // Eğer appsettings'te admin ayarları yoksa, default değerleri kullan
            if (string.IsNullOrWhiteSpace(adminEmail) || !isEmailFromEnv)
            {
                adminEmail = "admin@adminmail.com";
            }
            if (string.IsNullOrWhiteSpace(adminUserName) || !isUserNameFromEnv)
            {
                adminUserName = "admin";
            }
            if (string.IsNullOrWhiteSpace(adminPassword) || !isPasswordFromEnv)
            {
                adminPassword = "Q1w212*_*"; // Production'da mutlaka değiştirin!
            }

            // UserId = 1 olan kullanıcı var mı kontrol et
            var existingAdmin = await userRepository.GetAsync(u => u.UserId == 1);
            
            if (existingAdmin == null)
            {
                // Email unique kontrolü
                var isThereAnyEmail = await userRepository.GetAsync(u => u.Email == adminEmail);
                if (isThereAnyEmail != null)
                {
                    // Email zaten kullanılıyor, admin oluşturulamıyor
                    return;
                }
                
                // Password hash oluştur
                HashingHelper.CreatePasswordHash(adminPassword, out var passwordSalt, out var passwordHash);

                // Yeni admin kullanıcısı oluştur
                var adminUser = new User
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    FullName = adminFullName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Status = true,
                    RecordDate = System.DateTime.Now,
                    UpdateContactDate = System.DateTime.Now
                };

                userRepository.Add(adminUser);
                await userRepository.SaveChangesAsync();

                // Eğer UserId 1 değilse, sequence'i reset et ve UserId'yi 1 yap
                if (adminUser.UserId != 1)
                {
                    try
                    {
                        var oldUserId = adminUser.UserId;
                        
                        // Önce mevcut UserId'yi 1 yap
                        await dbContext.Database.ExecuteSqlRawAsync(
                            $"UPDATE \"Users\" SET \"UserId\" = 1 WHERE \"UserId\" = {oldUserId};");
                        
                        // Sequence'i reset et (1'den başlat)
                        await dbContext.Database.ExecuteSqlRawAsync(
                            $"SELECT setval('\"Users_UserId_seq\"', 1, false);");
                        
                        // Değişiklikleri kaydet
                        await dbContext.SaveChangesAsync();
                        
                        // Admin kullanıcısının UserId'si başarıyla 1 yapıldı
                        adminUser.UserId = 1;
                    }
                    catch (Exception ex)
                    {
                        // Sequence reset başarısız olursa, log kaydet
                        var logger = ServiceTool.ServiceProvider.GetService<Core.CrossCuttingConcerns.Logging.Serilog.Loggers.FileLogger>();
                        logger?.Error($"Admin kullanıcısının UserId'si 1 yapılamadı! Mevcut UserId = {adminUser.UserId}. Hata: {ex.Message}");
                    }
                }

                // Tüm operation claim'lerini admin kullanıcısına ata
                var operationClaimsResult = await mediator.Send(new GetOperationClaimsInternalQuery());
                var operationClaims = operationClaimsResult?.Data;
                
                if (operationClaims != null && operationClaims.Any())
                {
                    await mediator.Send(new CreateUserClaimsInternalCommand
                    {
                        UserId = adminUser.UserId, // UserId artık 1 olmalı
                        OperationClaims = operationClaims
                    });
                    
                    var logger = ServiceTool.ServiceProvider.GetService<FileLogger>();
                    logger?.Info($"Admin kullanıcısı başarıyla oluşturuldu: UserId = {adminUser.UserId}, UserName = {adminUserName}, Email = {adminEmail}");
                }
            }
            else
            {
                // Admin kullanıcısı zaten var
                // İsteğe bağlı: Email ve password'ü güncelle (sadece appsettings'te farklı değerler varsa)
                var shouldUpdate = false;
                
                // Email kontrolü ve güncelleme
                if (existingAdmin.Email != adminEmail && !string.IsNullOrWhiteSpace(adminEmail))
                {
                    // Yeni email'in başka bir kullanıcıda olup olmadığını kontrol et
                    var emailExists = await userRepository.GetAsync(u => u.Email == adminEmail && u.UserId != 1);
                    if (emailExists == null)
                    {
                        existingAdmin.Email = adminEmail;
                        shouldUpdate = true;
                    }
                }

                // UserName güncelleme
                if (!string.IsNullOrWhiteSpace(adminUserName) && existingAdmin.UserName != adminUserName)
                {
                    // UserName unique kontrolü
                    var userNameExists = await userRepository.GetAsync(u => u.UserName == adminUserName && u.UserId != 1);
                    if (userNameExists == null)
                    {
                        existingAdmin.UserName = adminUserName;
                        shouldUpdate = true;
                    }
                }

                // FullName güncelleme
                if (!string.IsNullOrWhiteSpace(adminFullName) && existingAdmin.FullName != adminFullName)
                {
                    existingAdmin.FullName = adminFullName;
                    shouldUpdate = true;
                }

                // Password güncelleme (environment variable'dan geliyorsa veya default değilse)
                if (isPasswordFromEnv && adminPassword != "Q1w212*_*")
                {
                    HashingHelper.CreatePasswordHash(adminPassword, out var passwordSalt, out var passwordHash);
                    existingAdmin.PasswordHash = passwordHash;
                    existingAdmin.PasswordSalt = passwordSalt;
                    shouldUpdate = true;
                }

                if (shouldUpdate)
                {
                    existingAdmin.UpdateContactDate = System.DateTime.Now;
                    userRepository.Update(existingAdmin);
                    await userRepository.SaveChangesAsync();
                }

                // Operation claim'lerini kontrol et ve eksik olanları ekle
                var operationClaimsResult = await mediator.Send(new GetOperationClaimsInternalQuery());
                var operationClaims = operationClaimsResult?.Data;
                
                if (operationClaims != null && operationClaims.Any())
                {
                    await mediator.Send(new CreateUserClaimsInternalCommand
                    {
                        UserId = 1,
                        OperationClaims = operationClaims
                    });
                }
            }
        }
    }
}

