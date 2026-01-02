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
            // ✅ Auto Migration - PostgreSQL hazır olana kadar bekleyip retry mekanizması ile çalıştır
            Console.WriteLine("========================================");
            Console.WriteLine("Starting database connection and migration process...");
            Console.WriteLine("========================================");
            
            var maxRetries = 15; // 15 kez deneme
            var migrationSuccess = false;
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"\n[Attempt {attempt}/{maxRetries}] Trying to connect to PostgreSQL...");
                    Console.WriteLine($"Connection String: Host=postgresql-database-x0okocg48g0o8g4ooc08kkoo;Port=5432;Database=CuzdanimDb");
                    
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();
                        
                        if (db == null)
                        {
                            throw new Exception("Database context is null. ServiceProvider issue.");
                        }

                        Console.WriteLine("Step 1: Testing database connection...");
                        // Önce connection test et
                        var canConnect = db.Database.CanConnect();
                        
                        if (canConnect)
                        {
                            Console.WriteLine($"✓ PostgreSQL connection successful on attempt {attempt}!");
                            Console.WriteLine("Step 2: Running database migrations...");
                            
                            db.Database.Migrate(); // deploy sırasında migrationları uygular
                            
                            migrationSuccess = true;
                            Console.WriteLine("✓ Migrations completed successfully!");
                            Console.WriteLine("========================================");
                            break;
                        }
                        else
                        {
                            throw new Exception("CanConnect() returned false. Database is not accessible.");
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException socketEx)
                {
                    lastException = socketEx;
                    Console.WriteLine($"✗ Socket Exception on attempt {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Error Code: {socketEx.ErrorCode}");
                    Console.WriteLine($"  Socket Error: {socketEx.SocketErrorCode}");
                    Console.WriteLine($"  Message: {socketEx.Message}");
                    Console.WriteLine($"  Inner Exception: {socketEx.InnerException?.Message ?? "None"}");
                    Console.WriteLine($"  Stack Trace: {socketEx.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = attempt <= 5 ? 2 : 3; // İlk 5 denemede 2 saniye, sonra 3 saniye
                        Console.WriteLine($"  Waiting {delaySeconds} seconds before retry...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
                catch (Npgsql.NpgsqlException npgsqlEx)
                {
                    lastException = npgsqlEx;
                    Console.WriteLine($"✗ PostgreSQL Exception on attempt {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Error Code: {npgsqlEx.SqlState}");
                    Console.WriteLine($"  Message: {npgsqlEx.Message}");
                    Console.WriteLine($"  Inner Exception: {npgsqlEx.InnerException?.Message ?? "None"}");
                    Console.WriteLine($"  Stack Trace: {npgsqlEx.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = attempt <= 5 ? 2 : 3;
                        Console.WriteLine($"  Waiting {delaySeconds} seconds before retry...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"✗ Exception on attempt {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Type: {ex.GetType().Name}");
                    Console.WriteLine($"  Message: {ex.Message}");
                    Console.WriteLine($"  Inner Exception: {ex.InnerException?.Message ?? "None"}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"  Inner Exception Type: {ex.InnerException.GetType().Name}");
                        Console.WriteLine($"  Inner Exception Stack: {ex.InnerException.StackTrace}");
                    }
                    Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = attempt <= 5 ? 2 : 3;
                        Console.WriteLine($"  Waiting {delaySeconds} seconds before retry...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
            }

            if (!migrationSuccess)
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("❌ DATABASE MIGRATION FAILED");
                Console.WriteLine("========================================");
                Console.WriteLine($"Failed after {maxRetries} attempts.");
                Console.WriteLine($"Last Exception Type: {lastException?.GetType().Name ?? "Unknown"}");
                Console.WriteLine($"Last Error Message: {lastException?.Message ?? "Unknown error"}");
                Console.WriteLine($"Last Inner Exception: {lastException?.InnerException?.Message ?? "None"}");
                Console.WriteLine("\nPossible causes:");
                Console.WriteLine("1. PostgreSQL service is not running");
                Console.WriteLine("2. PostgreSQL service name is incorrect: postgresql-database-x0okocg48g0o8g4ooc08kkoo");
                Console.WriteLine("3. Network issue - containers are in different networks");
                Console.WriteLine("4. PostgreSQL is not ready yet (timing issue)");
                Console.WriteLine("5. Connection string parameters are incorrect");
                Console.WriteLine("========================================");
                
                throw new Exception(
                    $"Database migration failed after {maxRetries} attempts. " +
                    $"Last error: {lastException?.GetType().Name} - {lastException?.Message}. " +
                    $"Inner: {lastException?.InnerException?.Message ?? "None"}. " +
                    $"Application cannot start without database connection.", 
                    lastException);
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