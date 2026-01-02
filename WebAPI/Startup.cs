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
            Console.WriteLine("PostgreSQL bağlantısı ve migration işlemi başlatılıyor...");
            Console.WriteLine("========================================");
            
            var maxRetries = 30; // 30 kez deneme (DNS çözümleme için daha fazla zaman)
            var migrationSuccess = false;
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"\n[Deneme {attempt}/{maxRetries}] PostgreSQL'e bağlanılmaya çalışılıyor...");
                    Console.WriteLine($"Bağlantı String: Host=postgres;Port=5432;Database=CuzdanimDb");
                    
                    // DNS çözümlemesini önce test et
                    try
                    {
                        Console.WriteLine("Adım 0: DNS çözümlemesi test ediliyor (postgres)...");
                        var addresses = System.Net.Dns.GetHostAddresses("postgres");
                        Console.WriteLine($"  ✓ DNS çözümlemesi başarılı! IP adresleri: {string.Join(", ", addresses.Select(a => a.ToString()))}");
                    }
                    catch (System.Net.Sockets.SocketException dnsEx)
                    {
                        Console.WriteLine($"  ⚠ DNS çözümleme hatası: {dnsEx.Message}");
                        Console.WriteLine($"  ⚠ Bu normal olabilir - PostgreSQL servisi henüz hazır olmayabilir");
                        // DNS hatası olsa bile bağlantıyı denemeye devam et
                    }
                    catch (Exception dnsEx)
                    {
                        Console.WriteLine($"  ⚠ DNS test hatası: {dnsEx.Message}");
                    }
                    
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<DataAccess.Concrete.EntityFramework.Contexts.ProjectDbContext>();
                        
                        if (db == null)
                        {
                            throw new Exception("Database context null. ServiceProvider sorunu.");
                        }

                        Console.WriteLine("Adım 1: Veritabanı bağlantısı test ediliyor...");
                        var connection = db.Database.GetDbConnection();
                        Console.WriteLine($"  Bağlantı tipi: {connection.GetType().Name}");
                        Console.WriteLine($"  Bağlantı string (maskelenmiş): {connection.ConnectionString?.Replace("Password=Adana.14531989", "Password=***") ?? "null"}");
                        
                        connection.Open();
                        Console.WriteLine($"  ✓ Bağlantı başarıyla açıldı!");
                        Console.WriteLine($"  Veritabanı: {connection.Database}");
                        Console.WriteLine($"  Sunucu Versiyonu: {connection.ServerVersion}");
                        connection.Close();
                        Console.WriteLine($"  ✓ Bağlantı başarıyla kapatıldı!");
                        
                        Console.WriteLine($"✓ PostgreSQL bağlantısı {attempt}. denemede başarılı!");
                        Console.WriteLine("Adım 2: Veritabanı migration'ları çalıştırılıyor...");
                        
                        db.Database.Migrate(); // deploy sırasında migrationları uygular
                        
                        migrationSuccess = true;
                        Console.WriteLine("✓ Migration'lar başarıyla tamamlandı!");
                        Console.WriteLine("========================================");
                        break;
                    }
                }
                catch (System.Net.Sockets.SocketException socketEx)
                {
                    lastException = socketEx;
                    var isDnsError = socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TryAgain || 
                                    socketEx.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound ||
                                    socketEx.Message.Contains("Resource temporarily unavailable") ||
                                    socketEx.StackTrace?.Contains("Dns.GetHostEntryOrAddressesCore") == true;
                    
                    Console.WriteLine($"✗ Socket Hatası - Deneme {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Hata Kodu: {socketEx.ErrorCode}");
                    Console.WriteLine($"  Socket Hatası: {socketEx.SocketErrorCode}");
                    Console.WriteLine($"  Mesaj: {socketEx.Message}");
                    
                    if (isDnsError)
                    {
                        Console.WriteLine($"  ⚠ DNS Çözümleme Hatası: 'postgres' hostname'i çözülemiyor");
                        Console.WriteLine($"  ⚠ Olası nedenler:");
                        Console.WriteLine($"     - PostgreSQL container'ı henüz başlamadı");
                        Console.WriteLine($"     - Container'lar farklı network'lerde");
                        Console.WriteLine($"     - Docker network yapılandırması eksik");
                    }
                    
                    if (attempt < maxRetries)
                    {
                        // Exponential backoff: İlk denemelerde kısa, sonra daha uzun bekleme
                        var baseDelay = isDnsError ? 3 : 2; // DNS hataları için daha uzun bekleme
                        var delaySeconds = Math.Min(baseDelay + (attempt / 3), 10); // Maksimum 10 saniye
                        Console.WriteLine($"  {delaySeconds} saniye bekleniyor...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
                catch (Npgsql.NpgsqlException npgsqlEx)
                {
                    lastException = npgsqlEx;
                    Console.WriteLine($"✗ PostgreSQL Hatası - Deneme {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Hata Kodu: {npgsqlEx.SqlState}");
                    Console.WriteLine($"  Mesaj: {npgsqlEx.Message}");
                    Console.WriteLine($"  İç Hata: {npgsqlEx.InnerException?.Message ?? "Yok"}");
                    
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = Math.Min(2 + (attempt / 3), 8);
                        Console.WriteLine($"  {delaySeconds} saniye bekleniyor...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"✗ Hata - Deneme {attempt}/{maxRetries}:");
                    Console.WriteLine($"  Tip: {ex.GetType().Name}");
                    Console.WriteLine($"  Mesaj: {ex.Message}");
                    Console.WriteLine($"  İç Hata: {ex.InnerException?.Message ?? "Yok"}");
                    
                    if (attempt < maxRetries)
                    {
                        var delaySeconds = Math.Min(2 + (attempt / 3), 8);
                        Console.WriteLine($"  {delaySeconds} saniye bekleniyor...");
                        System.Threading.Thread.Sleep(delaySeconds * 1000);
                    }
                }
            }

            if (!migrationSuccess)
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("❌ VERİTABANI MİGRATİON BAŞARISIZ");
                Console.WriteLine("========================================");
                Console.WriteLine($"{maxRetries} denemeden sonra başarısız oldu.");
                Console.WriteLine($"Son Hata Tipi: {lastException?.GetType().Name ?? "Bilinmiyor"}");
                Console.WriteLine($"Son Hata Mesajı: {lastException?.Message ?? "Bilinmeyen hata"}");
                Console.WriteLine($"İç Hata: {lastException?.InnerException?.Message ?? "Yok"}");
                Console.WriteLine("\nOlası nedenler:");
                Console.WriteLine("1. PostgreSQL servisi çalışmıyor");
                Console.WriteLine("2. PostgreSQL servis adı yanlış: postgres");
                Console.WriteLine("3. Network sorunu - container'lar farklı network'lerde");
                Console.WriteLine("4. PostgreSQL henüz hazır değil (zamanlama sorunu)");
                Console.WriteLine("5. Bağlantı string parametreleri yanlış");
                Console.WriteLine("6. docker-compose.yaml'da PostgreSQL servisi tanımlı değil");
                Console.WriteLine("\nÇözüm önerileri:");
                Console.WriteLine("- docker-compose.yaml dosyasında PostgreSQL servisinin tanımlı olduğundan emin olun");
                Console.WriteLine("- Container'ların aynı network'te olduğunu kontrol edin");
                Console.WriteLine("- 'docker-compose ps' ile servislerin çalıştığını kontrol edin");
                Console.WriteLine("- 'docker-compose logs postgres' ile PostgreSQL loglarını kontrol edin");
                Console.WriteLine("========================================");
                
                throw new Exception(
                    $"Veritabanı migration'ı {maxRetries} denemeden sonra başarısız oldu. " +
                    $"Son hata: {lastException?.GetType().Name} - {lastException?.Message}. " +
                    $"İç hata: {lastException?.InnerException?.Message ?? "Yok"}. " +
                    $"Veritabanı bağlantısı olmadan uygulama başlatılamaz.", 
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