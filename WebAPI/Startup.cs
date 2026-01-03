using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using Business;
using Business.Helpers;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.IoC;
using Core.Utilities.Security.Encyption;
using Core.Utilities.Security.Jwt;
using Core.Utilities.TaskScheduler.Hangfire.Models;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerUI;
using ConfigurationManager = Business.ConfigurationManager;

namespace WebAPI
{
    /// <summary>
    ///
    /// </summary>
    public partial class Startup : BusinessStartup
    {
        /// <summary>
        /// Constructor of <c>Startup</c>
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="hostEnvironment"></param>
        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
            : base(configuration, hostEnvironment)
        {
        }


        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <remarks>
        /// It is common to all configurations and must be called. Aspnet core does not call this method because there are other methods.
        /// </remarks>
        /// <param name="services"></param>
        public override void ConfigureServices(IServiceCollection services)
        {
            // Business katmanında olan dependency tanımlarının bir metot üzerinden buraya implemente edilmesi.

            // Environment variable'lardan connection string oluştur
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "postgres";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "CuzdanimDb";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
            var hangfireDbName = Environment.GetEnvironmentVariable("HANGFIRE_DB_NAME") ?? "cuzdanim_hangfire";
            var hangfireConnectionString = Environment.GetEnvironmentVariable("HANGFIRE_CONNECTION_STRING");
            
            // Hangfire connection string yoksa oluştur
            if (string.IsNullOrEmpty(hangfireConnectionString))
            {
                hangfireConnectionString = $"Host={dbHost};Port={dbPort};Database={hangfireDbName};Username={dbUser};Password={dbPassword};Command Timeout=30;Timeout=30;";
            }

            // Connection string'leri oluştur - Timeout değerlerini artır ve pooling ekle
            var pgConnectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};Command Timeout=60;Timeout=60;Connection Lifetime=0;Pooling=true;MinPoolSize=1;MaxPoolSize=20;";

            // Configuration'a ekle (override) - appsettings.json'daki ${VAR} formatını replace et
            Configuration["ConnectionStrings:DArchPgContext"] = pgConnectionString;
            Configuration["TaskSchedulerOptions:ConnectionString"] = hangfireConnectionString;
            Configuration["SeriLogConfigurations:PostgreConfiguration:ConnectionString"] = pgConnectionString;

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            services.AddApiVersioning(v =>
            {
                v.DefaultApiVersion = new ApiVersion(1, 0);
                v.AssumeDefaultVersionWhenUnspecified = true;
                v.ReportApiVersions = true;
                v.ApiVersionReader = new HeaderApiVersionReader("x-dev-arch-version");
            });

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllowOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            var tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Claim mapping'i devre dışı bırak - tam URI formatında tut
                    options.MapInboundClaims = false;
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = tokenOptions.Issuer,
                        ValidAudience = tokenOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
                        ClockSkew = TimeSpan.Zero
                    };
                });
            services.AddSwaggerGen(c =>
            {
                c.IncludeXmlComments(Path.ChangeExtension(typeof(Startup).Assembly.Location, ".xml"));
            });

            services.AddTransient<FileLogger>();
            services.AddTransient<PostgreSqlLogger>();
            services.AddTransient<MsSqlLogger>();
            services.AddScoped<IpControlAttribute>();

            base.ConfigureServices(services);
        }


        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ✅ Auto Migration - Geliştirilmiş retry mekanizması (DNS testi kaldırıldı)
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var maxRetries = 20; // Retry sayısını artır (DNS/Network hazır olana kadar)
                var retryDelay = 5000; // 5 saniye bekleme
                var migrationSuccess = false;
                
                // Environment variable'ları logla
                var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "postgres";
                var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
                var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "CuzdanimDb";
                var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
                Console.WriteLine($"🔍 DB Connection Info: Host={dbHost}, Port={dbPort}, Database={dbName}, User={dbUser}");
                
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        var db = scope.ServiceProvider.GetRequiredService<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();
                        
                        // Connection string'i logla
                        var connection = db.Database.GetDbConnection();
                        var maskedConnectionString = connection.ConnectionString?.Replace("Password=", "Password=***") ?? "null";
                        Console.WriteLine($"🔍 Connection String: {maskedConnectionString}");
                        
                        // DNS testini kaldırdık - Npgsql kendi DNS çözümlemesini yapar
                        // Direkt migration denemesi yapıyoruz, Npgsql daha iyi hata yönetimi yapar
                        
                        db.Database.Migrate();
                        Console.WriteLine("✅ Migration başarılı!");
                        migrationSuccess = true;
                        break;
                    }
                    catch (System.Net.Sockets.SocketException socketEx)
                    {
                        // Socket/DNS hataları için özel mesaj
                        Console.WriteLine($"🔴 Bağlantı hatası (deneme {i + 1}/{maxRetries}): {socketEx.Message}");
                        Console.WriteLine($"   Exception Type: {socketEx.GetType().Name}");
                        Console.WriteLine($"   Socket Error Code: {socketEx.SocketErrorCode}");
                        
                        if (i < maxRetries - 1)
                        {
                            Console.WriteLine($"   ⏳ {retryDelay / 1000} saniye bekleniyor (DNS/Network hazır olana kadar)...");
                            System.Threading.Thread.Sleep(retryDelay);
                        }
                        else
                        {
                            Console.WriteLine("⚠ Migration başarısız, uygulama devam ediyor...");
                            Console.WriteLine("💡 İpucu: PostgreSQL servisinin çalıştığından ve aynı network'te olduğundan emin olun.");
                        }
                    }
                    catch (Npgsql.NpgsqlException npgsqlEx)
                    {
                        // PostgreSQL özel hataları
                        Console.WriteLine($"🔴 PostgreSQL hatası (deneme {i + 1}/{maxRetries}): {npgsqlEx.Message}");
                        Console.WriteLine($"   Exception Type: {npgsqlEx.GetType().Name}");
                        
                        if (i < maxRetries - 1)
                        {
                            Console.WriteLine($"   ⏳ {retryDelay / 1000} saniye bekleniyor...");
                            System.Threading.Thread.Sleep(retryDelay);
                        }
                        else
                        {
                            Console.WriteLine("⚠ Migration başarısız, uygulama devam ediyor...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"🔴 Migration hatası (deneme {i + 1}/{maxRetries}): {ex.Message}");
                        Console.WriteLine($"   Exception Type: {ex.GetType().Name}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"   Inner Exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                        }
                        if (i < maxRetries - 1)
                        {
                            Console.WriteLine($"   ⏳ {retryDelay / 1000} saniye bekleniyor...");
                            System.Threading.Thread.Sleep(retryDelay);
                        }
                        else
                        {
                            Console.WriteLine("⚠ Migration başarısız, uygulama devam ediyor...");
                        }
                    }
                }
            }

            // VERY IMPORTANT. Since we removed the build from AddDependencyResolvers, let's set the Service provider manually.
            // By the way, we can construct with DI by taking type to avoid calling static methods in aspects.
            ServiceTool.ServiceProvider = app.ApplicationServices;

            var configurationManager = app.ApplicationServices.GetService<ConfigurationManager>();
            switch (configurationManager.Mode)
            {
                case ApplicationMode.Development:
                    _ = app.UseDbFakeDataCreator();
                    break;

                case ApplicationMode.Profiling:
                case ApplicationMode.Staging:
                case ApplicationMode.Production:
                    // Bu modlarda admin kullanıcısı OperationClaimCreatorMiddleware'den sonra oluşturulacak
                    break;
            }

            app.UseDeveloperExceptionPage();

            app.ConfigureCustomExceptionMiddleware();

            // Önce operation claim'leri oluştur (ve Default Group'u oluşturur)
            // Migration başarılı olduktan sonra çalışacak (database bağlantısı hazır)
            _ = app.UseDbOperationClaimCreator();
            
            // Sonra admin kullanıcısını oluştur (operation claim'ler hazır olmalı)
            // Development modunda da çalıştır (test için)
            _ = app.UseAdminUserCreator();
            
            // Swagger'ı tüm ortamlarda aç (Production dahil)
                app.UseSwagger();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.json", "Cuzdanim");
                    c.DocExpansion(DocExpansion.None);
                });

            app.UseCors("AllowOrigin");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            // Make Turkish your default language. It shouldn't change according to the server.
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("tr-TR"),
            });

            var cultureInfo = new CultureInfo("tr-TR");
            cultureInfo.DateTimeFormat.ShortTimePattern = "HH:mm";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            app.UseStaticFiles();

            var taskSchedulerConfig = Configuration.GetSection("TaskSchedulerOptions").Get<TaskSchedulerConfig>();
            
            if (taskSchedulerConfig.Enabled)
            {
                app.UseHangfireDashboard(taskSchedulerConfig.Path, new DashboardOptions
                {
                    DashboardTitle = taskSchedulerConfig.Title,
                    Authorization = new[]
                    {
                        new HangfireCustomBasicAuthenticationFilter
                        {
                            User = taskSchedulerConfig.Username,
                            Pass = taskSchedulerConfig.Password
                        }
                    }
                });

                // Recurring job'ları manuel olarak register et
                try
                {
                    var recurringJobManager = app.ApplicationServices.GetService<Hangfire.IRecurringJobManager>();
                    if (recurringJobManager != null)
                    {
                        // UpdateAssetTypePrices job'unu register et
                        // async Task method için Expression<Func<Task>> kullan
                        System.Linq.Expressions.Expression<Func<System.Threading.Tasks.Task>> jobExpression = 
                            () => Business.Services.BuildinRecurringJobs.UpdateAssetTypePrices();
                        
                        recurringJobManager.AddOrUpdate(
                            "UpdateAssetTypePrices",
                            jobExpression,
                            "* * * * *"); // Her dakika çalışır

                        // Proje başladığında job'u bir kere çalıştır
                        recurringJobManager.Trigger("UpdateAssetTypePrices");
                    }
                }
                catch (Exception ex)
                {
                    var logger = app.ApplicationServices.GetService<FileLogger>();
                    logger?.Error($"Failed to register/trigger UpdateAssetTypePrices job: {ex.Message}");
                }
            }

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}