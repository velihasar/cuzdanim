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
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUser = Environment.GetEnvironmentVariable("DB_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var hangfireDbName = Environment.GetEnvironmentVariable("HANGFIRE_DB_NAME") ?? "cuzdanim_hangfire";
            var hangfireConnectionString = Environment.GetEnvironmentVariable("HANGFIRE_CONNECTION_STRING");
            
            // Sadece environment variable'lar varsa override et, yoksa appsettings.json'daki değerleri kullan
            if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbPort) && 
                !string.IsNullOrEmpty(dbName) && !string.IsNullOrEmpty(dbUser) && 
                !string.IsNullOrEmpty(dbPassword))
            {
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
            }
            else if (!string.IsNullOrEmpty(hangfireConnectionString))
            {
                // Sadece Hangfire connection string varsa onu set et
                Configuration["TaskSchedulerOptions:ConnectionString"] = hangfireConnectionString;
            }

            // JWT TokenOptions için environment variable'ları replace et
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            var jwtSecurityKey = Environment.GetEnvironmentVariable("JWT_SECURITY_KEY");

            if (!string.IsNullOrEmpty(jwtIssuer))
            {
                Configuration["TokenOptions:Issuer"] = jwtIssuer;
            }
            if (!string.IsNullOrEmpty(jwtAudience))
            {
                Configuration["TokenOptions:Audience"] = jwtAudience;
            }
            if (!string.IsNullOrEmpty(jwtSecurityKey))
            {
                Configuration["TokenOptions:SecurityKey"] = jwtSecurityKey;
            }

            // AdminSettings için environment variable'ları replace et (connection string'ler gibi)
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var adminUserName = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            var adminFullName = Environment.GetEnvironmentVariable("ADMIN_FULL_NAME");

            if (!string.IsNullOrEmpty(adminEmail))
            {
                Configuration["AdminSettings:Email"] = adminEmail;
            }
            if (!string.IsNullOrEmpty(adminUserName))
            {
                Configuration["AdminSettings:UserName"] = adminUserName;
            }
            if (!string.IsNullOrEmpty(adminPassword))
            {
                Configuration["AdminSettings:Password"] = adminPassword;
            }
            if (!string.IsNullOrEmpty(adminFullName))
            {
                Configuration["AdminSettings:FullName"] = adminFullName;
            }

            // Google OAuth için environment variable'ı replace et
            var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            if (!string.IsNullOrEmpty(googleClientId))
            {
                Configuration["Google:ClientId"] = googleClientId;
            }

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
            // ✅ Auto Migration - Geliştirilmiş retry mekanizması + Environment variable debugging
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var maxRetries = 20;
                var retryDelay = 5000;
                var migrationSuccess = false;
                
                // Environment variable'ları kontrol et, yoksa Configuration'dan al
                var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
                var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
                var dbName = Environment.GetEnvironmentVariable("DB_NAME");
                var dbUser = Environment.GetEnvironmentVariable("DB_USER");
                var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
                
                // Environment variable yoksa Configuration'dan (appsettings.json) al
                if (string.IsNullOrEmpty(dbHost) || string.IsNullOrEmpty(dbPort) || 
                    string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(dbUser) || 
                    string.IsNullOrEmpty(dbPassword))
                {
                    // Configuration'dan connection string'i parse et
                    var configConnectionString = Configuration.GetConnectionString("DArchPgContext");
                    if (!string.IsNullOrEmpty(configConnectionString))
                    {
                        // Connection string'i parse et: "Host=localhost;Port=5432;Database=CuzdanimDb;Username=postgres;Password=1111;"
                        var parts = configConnectionString.Split(';');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
                                dbHost = dbHost ?? part.Substring(5).Trim();
                            else if (part.StartsWith("Port=", StringComparison.OrdinalIgnoreCase))
                                dbPort = dbPort ?? part.Substring(5).Trim();
                            else if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                                dbName = dbName ?? part.Substring(9).Trim();
                            else if (part.StartsWith("Username=", StringComparison.OrdinalIgnoreCase))
                                dbUser = dbUser ?? part.Substring(9).Trim();
                            else if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                                dbPassword = dbPassword ?? part.Substring(9).Trim();
                        }
                    }
                }
                
                // Tüm environment variable'ları logla (debugging için)
                Console.WriteLine("🔍 === Environment Variables Debug ===");
                Console.WriteLine($"DB_HOST: {dbHost ?? "null (using appsettings.json)"}");
                Console.WriteLine($"DB_PORT: {dbPort ?? "null (using appsettings.json)"}");
                Console.WriteLine($"DB_NAME: {dbName ?? "null (using appsettings.json)"}");
                Console.WriteLine($"DB_USER: {dbUser ?? "null (using appsettings.json)"}");
                
                // Coolify'ın sağladığı alternatif environment variable'ları kontrol et
                var possibleHosts = new List<string>();
                if (!string.IsNullOrEmpty(dbHost))
                {
                    possibleHosts.Add(dbHost);
                }
                // Eğer hala host yoksa, localhost'u ekle
                if (possibleHosts.Count == 0)
                {
                    possibleHosts.Add("localhost");
                }
                
                // Coolify'ın sağladığı özel değişkenleri kontrol et
                var coolifyDbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST");
                var coolifyDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                var postgresServiceName = Environment.GetEnvironmentVariable("POSTGRES_SERVICE_NAME");
                
                if (!string.IsNullOrEmpty(coolifyDbHost))
                {
                    Console.WriteLine($"🔍 POSTGRES_HOST bulundu: {coolifyDbHost}");
                    possibleHosts.Add(coolifyDbHost);
                }
                if (!string.IsNullOrEmpty(postgresServiceName))
                {
                    Console.WriteLine($"🔍 POSTGRES_SERVICE_NAME bulundu: {postgresServiceName}");
                    possibleHosts.Add(postgresServiceName);
                }
                if (!string.IsNullOrEmpty(coolifyDbUrl))
                {
                    Console.WriteLine($"🔍 DATABASE_URL bulundu: {coolifyDbUrl}");
                }
                
                // Tüm environment variable'ları listele (POSTGRES ile başlayanlar)
                Console.WriteLine("🔍 PostgreSQL ile ilgili tüm environment variable'lar:");
                foreach (var envVar in Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
                    .Where(e => e.Key.ToString().ToUpper().Contains("POSTGRES") || 
                               e.Key.ToString().ToUpper().Contains("DB_") ||
                               e.Key.ToString().ToUpper().Contains("DATABASE")))
                {
                    var key = envVar.Key.ToString();
                    var value = envVar.Value?.ToString() ?? "null";
                    // Şifreleri maskele
                    if (key.ToUpper().Contains("PASSWORD") || key.ToUpper().Contains("PASS"))
                    {
                        value = "***";
                    }
                    Console.WriteLine($"   {key} = {value}");
                }
                
                Console.WriteLine($"🔍 DB Connection Info: Host={dbHost}, Port={dbPort}, Database={dbName}, User={dbUser}");
                Console.WriteLine($"🔍 Denenecek hostname'ler: {string.Join(", ", possibleHosts.Distinct())}");
                
                // Her hostname için deneme yap
                foreach (var hostToTry in possibleHosts.Distinct())
                {
                    Console.WriteLine($"\n🔍 Hostname '{hostToTry}' deneniyor...");
                    
                    // Connection string'i oluştur
                    var testConnectionString = $"Host={hostToTry};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};Command Timeout=60;Timeout=60;Connection Lifetime=0;Pooling=true;MinPoolSize=1;MaxPoolSize=20;";
                    
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            // Connection string'i Configuration'a geçici olarak set et
                            Configuration["ConnectionStrings:DArchPgContext"] = testConnectionString;
                            
                            // Yeni scope oluştur ve DbContext'i al
                            using (var testScope = app.ApplicationServices.CreateScope())
                            {
                                var db = testScope.ServiceProvider.GetRequiredService<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();
                                
                                var connection = db.Database.GetDbConnection();
                                var maskedConnectionString = connection.ConnectionString?.Replace("Password=", "Password=***") ?? "null";
                                Console.WriteLine($"🔍 Connection String: {maskedConnectionString}");
                                
                                db.Database.Migrate();
                                Console.WriteLine($"✅ Migration başarılı! Hostname: {hostToTry}");
                                
                                // Başarılı olursa, connection string'i kalıcı olarak güncelle
                                Configuration["ConnectionStrings:DArchPgContext"] = testConnectionString;
                                Configuration["SeriLogConfigurations:PostgreConfiguration:ConnectionString"] = testConnectionString;
                                
                                migrationSuccess = true;
                                break;
                            }
                        }
                        catch (System.Net.Sockets.SocketException socketEx)
                        {
                            Console.WriteLine($"🔴 Bağlantı hatası (deneme {i + 1}/{maxRetries}, host: {hostToTry}): {socketEx.Message}");
                            Console.WriteLine($"   Socket Error Code: {socketEx.SocketErrorCode}");
                            
                            if (i < maxRetries - 1)
                            {
                                Console.WriteLine($"   ⏳ {retryDelay / 1000} saniye bekleniyor...");
                                System.Threading.Thread.Sleep(retryDelay);
                            }
                            else
                            {
                                Console.WriteLine($"   ❌ Hostname '{hostToTry}' başarısız, bir sonraki hostname deneniyor...");
                            }
                        }
                        catch (Npgsql.NpgsqlException npgsqlEx)
                        {
                            Console.WriteLine($"🔴 PostgreSQL hatası (deneme {i + 1}/{maxRetries}, host: {hostToTry}): {npgsqlEx.Message}");
                            
                            if (i < maxRetries - 1)
                            {
                                Console.WriteLine($"   ⏳ {retryDelay / 1000} saniye bekleniyor...");
                                System.Threading.Thread.Sleep(retryDelay);
                            }
                            else
                            {
                                Console.WriteLine($"   ❌ Hostname '{hostToTry}' başarısız, bir sonraki hostname deneniyor...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"🔴 Migration hatası (deneme {i + 1}/{maxRetries}, host: {hostToTry}): {ex.Message}");
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
                                Console.WriteLine($"   ❌ Hostname '{hostToTry}' başarısız, bir sonraki hostname deneniyor...");
                            }
                        }
                    }
                    
                    if (migrationSuccess)
                    {
                        break;
                    }
                }
                
                if (!migrationSuccess)
                {
                    Console.WriteLine("⚠ Migration başarısız, uygulama devam ediyor...");
                    Console.WriteLine("💡 İpucu: Coolify'da PostgreSQL servisinin gerçek hostname'ini kontrol edin.");
                    Console.WriteLine("💡 İpucu: Environment variable'ları yukarıdaki loglardan kontrol edin.");
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
            
            FileLogger logger = null;
            try
            {
                logger = app.ApplicationServices.GetService<FileLogger>();
            }
            catch
            {
                // Logger alınamazsa devam et
            }
            
            if (taskSchedulerConfig == null)
            {
                logger?.Error("TaskSchedulerConfig is null! Hangfire configuration not found.");
            }
            else
            {
                logger?.Info($"TaskSchedulerConfig found. Enabled: {taskSchedulerConfig.Enabled}");
                
                if (taskSchedulerConfig.Enabled)
                {
                    logger?.Info("Hangfire is enabled, setting up dashboard and jobs...");
                    
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
                        logger?.Info("Starting Hangfire recurring job registration...");
                    
                    var recurringJobManager = app.ApplicationServices.GetService<Hangfire.IRecurringJobManager>();
                    if (recurringJobManager != null)
                    {
                        logger?.Info("RecurringJobManager found, registering jobs...");
                        
                        // UpdateAssetTypePrices job'unu register et
                        // async Task method için Expression<Func<Task>> kullan
                        System.Linq.Expressions.Expression<Func<System.Threading.Tasks.Task>> updatePricesExpression = 
                            () => Business.Services.BuildinRecurringJobs.UpdateAssetTypePrices();
                        
                        recurringJobManager.AddOrUpdate(
                            "UpdateAssetTypePrices",
                            updatePricesExpression,
                            "*/30 * * * *"); // Her 30 dakikada bir çalışır
                        logger?.Info("UpdateAssetTypePrices job registered");

                        // DeleteOldTransactions job'unu register et
                        System.Linq.Expressions.Expression<Func<System.Threading.Tasks.Task>> deleteOldTransactionsExpression = 
                            () => Business.Services.BuildinRecurringJobs.DeleteOldTransactions();
                        
                        recurringJobManager.AddOrUpdate(
                            "DeleteOldTransactions",
                            deleteOldTransactionsExpression,
                            "0 3 * * *"); // Her gün saat 03:00'de çalışır
                        logger?.Info("DeleteOldTransactions job registered");

                        // CreateMonthlyRecurringTransactions job'unu register et
                        System.Linq.Expressions.Expression<Func<System.Threading.Tasks.Task>> createMonthlyRecurringExpression = 
                            () => Business.Services.BuildinRecurringJobs.CreateMonthlyRecurringTransactions();
                        
                        recurringJobManager.AddOrUpdate(
                            "CreateMonthlyRecurringTransactions",
                            createMonthlyRecurringExpression,
                            "*/2 * * * *"); // Her 2 dakikada bir çalışır
                        logger?.Info("CreateMonthlyRecurringTransactions job registered");

                        // Proje başladığında UpdateAssetTypePrices job'unu bir kere çalıştır
                        recurringJobManager.Trigger("UpdateAssetTypePrices");
                        
                        // Test için CreateMonthlyRecurringTransactions job'unu da bir kere çalıştır
                        recurringJobManager.Trigger("CreateMonthlyRecurringTransactions");
                        logger?.Info("CreateMonthlyRecurringTransactions job triggered manually for testing");
                        
                        logger?.Info("All recurring jobs registered successfully");
                    }
                    else
                    {
                        logger?.Error("RecurringJobManager is null! Hangfire may not be properly configured.");
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error($"Failed to register/trigger recurring jobs: {ex.Message}");
                    logger?.Error($"Stack trace: {ex.StackTrace}");
                }
                }
                else
                {
                    logger?.Warn("TaskSchedulerOptions.Enabled is false! Hangfire jobs will not be registered.");
                }
            }

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}