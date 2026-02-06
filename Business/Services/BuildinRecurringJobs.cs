using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Utilities.IoC;
using DataAccess.Abstract;
using Hangfire;
using Hangfire.RecurringJobExtensions;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Core.Entities.Concrete;
using Business.Services;

namespace Business.Services;

public class BuildinRecurringJobs
{
    // Yarım saatte bir çalışır (her 30 dakikada bir)
    // Cron expression: "*/30 * * * *" = Her 30 dakikada bir
    [RecurringJob("*/30 * * * *", RecurringJobId = "UpdateAssetTypePrices")]
    public static async Task UpdateAssetTypePrices()
    {
        // ServiceProvider'dan dependency'leri resolve et
        var serviceProvider = ServiceTool.ServiceProvider;
        
        if (serviceProvider == null)
        {
            Console.WriteLine("ERROR: ServiceProvider is null!");
            return;
        }
        
        // Her job için yeni bir scope oluştur (DbContext concurrency sorununu çözmek için)
        using (var scope = serviceProvider.CreateScope())
        {
            var scopedServiceProvider = scope.ServiceProvider;
            var assetTypeRepository = scopedServiceProvider?.GetService<IAssetTypeRepository>();
            var assetTypePriceService = scopedServiceProvider?.GetService<IAssetTypePriceService>();
            var logger = scopedServiceProvider?.GetService<FileLogger>();
            
            try
            {
            if (assetTypeRepository == null || assetTypePriceService == null)
            {
                logger?.Error("UpdateAssetTypePrices: Required services not found in DI container");
                return;
            }

            // ConvertedAmountType != 0 olan (yani dolu olan) AssetType'ları bul
            var assetTypes = await assetTypeRepository.GetListAsync(x => x.ConvertedAmountType != 0);

            if (assetTypes == null || !assetTypes.Any())
            {
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var assetType in assetTypes)
            {
                try
                {
                    // TL için güncelleme yapma (zaten 1.0)
                    if (assetType.ConvertedAmountType == AssetConvertType.Tl)
                    {
                        continue;
                    }

                    // Fiyatı API'den çek (AssetType'a göre - ApiUrlKey varsa onu kullanır)
                    var price = await assetTypePriceService.GetPriceForAssetTypeAsync(assetType);

                    if (price.HasValue && price.Value > 0)
                    {
                        assetType.TlValue = price.Value;
                        assetType.UpdatedDate = DateTime.Now;
                        
                        assetTypeRepository.Update(assetType);
                        await assetTypeRepository.SaveChangesAsync();

                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error($"UpdateAssetTypePrices: Error updating {assetType.Name}. {ex.Message}");
                    failCount++;
                }
            }
        }
            catch (Exception ex)
            {
                logger?.Error($"UpdateAssetTypePrices job error: {ex.Message}");
                throw;
            }
        }
    }

    // Her gün saat 03:00'de çalışır
    // Cron expression: "0 3 * * *" = Her gün saat 03:00
    // Son bir yıldan eski olan transaction'ları siler (son bir yılın verileri kalır)
    [RecurringJob("0 3 * * *", RecurringJobId = "DeleteOldTransactions")]
    public static async Task DeleteOldTransactions()
    {
        // ServiceProvider'dan dependency'leri resolve et
        var serviceProvider = ServiceTool.ServiceProvider;
        
        if (serviceProvider == null)
        {
            Console.WriteLine("ERROR: ServiceProvider is null!");
            return;
        }
        
        // Her job için yeni bir scope oluştur (DbContext concurrency sorununu çözmek için)
        using (var scope = serviceProvider.CreateScope())
        {
            var scopedServiceProvider = scope.ServiceProvider;
            var transactionRepository = scopedServiceProvider?.GetService<ITransactionRepository>();
            var logger = scopedServiceProvider?.GetService<FileLogger>();
            
            try
            {
            if (transactionRepository == null)
            {
                logger?.Error("DeleteOldTransactions: ITransactionRepository not found in DI container");
                return;
            }

            // Son bir yıl öncesinin tarihini hesapla
            var oneYearAgo = DateTime.UtcNow.AddYears(-1);
            
            logger?.Info($"DeleteOldTransactions: Starting deletion of transactions older than {oneYearAgo:yyyy-MM-dd}");

            // Son bir yıldan eski olan transaction'ları bul (son bir yılın verileri kalacak)
            var oldTransactions = await transactionRepository.GetListAsync(
                x => x.Date < oneYearAgo && x.IsActive != false
            );

            if (oldTransactions == null || !oldTransactions.Any())
            {
                logger?.Info("DeleteOldTransactions: No old transactions found to delete");
                return;
            }

            int totalCount = oldTransactions.Count();
            int deletedCount = 0;
            int errorCount = 0;

            // Transaction'ları sil
            foreach (var transaction in oldTransactions)
            {
                try
                {
                    transactionRepository.Delete(transaction);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    logger?.Error($"DeleteOldTransactions: Error deleting transaction ID {transaction.Id}. {ex.Message}");
                    errorCount++;
                }
            }

            // Değişiklikleri kaydet
            if (deletedCount > 0)
            {
                await transactionRepository.SaveChangesAsync();
                logger?.Info($"DeleteOldTransactions: Successfully deleted {deletedCount} transactions (older than {oneYearAgo:yyyy-MM-dd}). Errors: {errorCount}");
            }
            else
            {
                logger?.Warn($"DeleteOldTransactions: No transactions were deleted. Errors: {errorCount}");
            }
        }
            catch (Exception ex)
            {
                logger?.Error($"DeleteOldTransactions job error: {ex.Message}");
                throw;
            }
        }
    }

    // Her 2 dakikada bir çalışır
    // Cron expression: "*/2 * * * *" = Her 2 dakikada bir
    // Geçmişte herhangi bir zamanda IsMonthlyRecurring=true olan transaction'ları kontrol eder
    // Bu ay içinde bugüne kadar kaydedilmemiş olanlar için push notification gönderir
    [RecurringJob("*/2 * * * *", RecurringJobId = "CreateMonthlyRecurringTransactions")]
    public static async Task CreateMonthlyRecurringTransactions()
    {
        // Console'a da yaz (debug için)
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CreateMonthlyRecurringTransactions: Job started");
        
        var serviceProvider = ServiceTool.ServiceProvider;
        Console.WriteLine($"ServiceProvider is null: {serviceProvider == null}");
        
        if (serviceProvider == null)
        {
            Console.WriteLine("ERROR: ServiceProvider is null!");
            return;
        }
        
        // Her job için yeni bir scope oluştur (DbContext concurrency sorununu çözmek için)
        using (var scope = serviceProvider.CreateScope())
        {
            var scopedServiceProvider = scope.ServiceProvider;
            
            Console.WriteLine("Getting services from scoped provider...");
            
            ITransactionRepository transactionRepository = null;
            IUserRepository userRepository = null;
            IIncomeCategoryRepository incomeCategoryRepository = null;
            IExpenseCategoryRepository expenseCategoryRepository = null;
            IFirebaseNotificationService firebaseNotificationService = null;
            FileLogger logger = null;
            
            try
            {
                Console.WriteLine("Getting TransactionRepository...");
                transactionRepository = scopedServiceProvider?.GetService<ITransactionRepository>();
                Console.WriteLine($"TransactionRepository obtained: {transactionRepository != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting TransactionRepository: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("Getting UserRepository...");
                userRepository = scopedServiceProvider?.GetService<IUserRepository>();
                Console.WriteLine($"UserRepository obtained: {userRepository != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting UserRepository: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("Getting IncomeCategoryRepository...");
                incomeCategoryRepository = scopedServiceProvider?.GetService<IIncomeCategoryRepository>();
                Console.WriteLine($"IncomeCategoryRepository obtained: {incomeCategoryRepository != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting IncomeCategoryRepository: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("Getting ExpenseCategoryRepository...");
                expenseCategoryRepository = scopedServiceProvider?.GetService<IExpenseCategoryRepository>();
                Console.WriteLine($"ExpenseCategoryRepository obtained: {expenseCategoryRepository != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting ExpenseCategoryRepository: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("Getting FirebaseNotificationService...");
                firebaseNotificationService = scopedServiceProvider?.GetService<IFirebaseNotificationService>();
                Console.WriteLine($"FirebaseNotificationService obtained: {firebaseNotificationService != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting FirebaseNotificationService: {ex.Message}");
            }
            
            try
            {
                Console.WriteLine("Getting FileLogger...");
                logger = scopedServiceProvider?.GetService<FileLogger>();
                Console.WriteLine($"FileLogger obtained: {logger != null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting FileLogger: {ex.Message}");
            }
            
            Console.WriteLine($"TransactionRepository is null: {transactionRepository == null}");
            Console.WriteLine($"UserRepository is null: {userRepository == null}");
            Console.WriteLine($"FirebaseNotificationService is null: {firebaseNotificationService == null}");
            Console.WriteLine($"Logger is null: {logger == null}");
            
            try
            {
            logger?.Info("CreateMonthlyRecurringTransactions: Job started");
            Console.WriteLine("CreateMonthlyRecurringTransactions: Job started (logged)");

            if (transactionRepository == null || userRepository == null || incomeCategoryRepository == null || expenseCategoryRepository == null || firebaseNotificationService == null)
            {
                Console.WriteLine("ERROR: Required services not found!");
                logger?.Error("CreateMonthlyRecurringTransactions: Required services not found");
                return;
            }
            
            Console.WriteLine("All services found, continuing...");

            var today = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            Console.WriteLine($"Today: {today:yyyy-MM-dd}, Current month start: {currentMonthStart:yyyy-MM-dd}");

            // 1. Tüm recurring transaction'ları bir kerede çek
            Console.WriteLine("Fetching recurring transactions...");
            var allRecurringTransactions = await transactionRepository.Query()
                .Where(x => x.IsMonthlyRecurring == true && x.IsActive != false)
                .ToListAsync();

            if (allRecurringTransactions == null || !allRecurringTransactions.Any())
            {
                Console.WriteLine("No recurring transactions found");
                logger?.Info("CreateMonthlyRecurringTransactions: No recurring transactions found");
                return;
            }

            Console.WriteLine($"Found {allRecurringTransactions.Count} recurring transactions");
            logger?.Info($"CreateMonthlyRecurringTransactions: Found {allRecurringTransactions.Count} recurring transactions");

            // 2. İlgili kullanıcı ID'lerini topla
            var userIds = allRecurringTransactions
                .Select(rt => rt.UserId)
                .Distinct()
                .ToList();

            // 3. FCM token'ı olan kullanıcıları bir kerede çek
            var usersWithTokens = await userRepository.Query()
                .Where(u => userIds.Contains(u.UserId) && !string.IsNullOrWhiteSpace(u.FcmToken) && u.Status)
                .Select(u => new { u.UserId, u.FcmToken })
                .ToListAsync();

            if (usersWithTokens == null || !usersWithTokens.Any())
            {
                Console.WriteLine("No users with FCM tokens found");
                logger?.Info("CreateMonthlyRecurringTransactions: No users with FCM tokens found");
                return;
            }

            Console.WriteLine($"Found {usersWithTokens.Count} users with FCM tokens");
            logger?.Info($"CreateMonthlyRecurringTransactions: Found {usersWithTokens.Count} users with FCM tokens");

            var userTokenDict = usersWithTokens.ToDictionary(u => u.UserId, u => u.FcmToken);

            // 4. Tüm gelir ve gider kategorilerini bir kerede çek (kategori isimleri için)
            var allIncomeCategories = await incomeCategoryRepository.Query()
                .Where(c => userIds.Contains(c.UserId ?? 0) && c.IsActive != false)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            
            var allExpenseCategories = await expenseCategoryRepository.Query()
                .Where(c => userIds.Contains(c.UserId ?? 0) && c.IsActive != false)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            
            var incomeCategoryDict = allIncomeCategories.ToDictionary(c => c.Id, c => c.Name ?? "");
            var expenseCategoryDict = allExpenseCategories.ToDictionary(c => c.Id, c => c.Name ?? "");

            // 5. Bu ay için mevcut transaction'ları bir kerede çek (bulk check)
            var existingTransactionsThisMonth = await transactionRepository.Query()
                .Where(x => userIds.Contains(x.UserId)
                    && x.Date >= currentMonthStart
                    && x.Date <= today
                    && x.IsActive != false
                    && x.IsMonthlyRecurring == true)
                .Select(x => new
                {
                    x.UserId,
                    x.Type,
                    x.IncomeCategoryId,
                    x.ExpenseCategoryId,
                    x.DayOfMonth
                })
                .ToListAsync();

            // 6. Mevcut transaction'ları hash set'e çevir (hızlı lookup için)
            // DayOfMonth'u da dahil et - aynı kategoride farklı DayOfMonth değerleri ayrı işlenmeli
            var existingTransactionsSet = existingTransactionsThisMonth
                .Select(x => $"{x.UserId}_{x.Type}_{x.IncomeCategoryId}_{x.ExpenseCategoryId}_{x.DayOfMonth}")
                .ToHashSet();

            // 7. Notification gönderilecek transaction'ları grupla
            var groupedRecurringTransactions = allRecurringTransactions
                .GroupBy(rt => new
                {
                    rt.UserId,
                    rt.Type,
                    IncomeCategoryId = rt.IncomeCategoryId,
                    ExpenseCategoryId = rt.ExpenseCategoryId,
                    rt.DayOfMonth
                })
                .Select(g => g.OrderByDescending(rt => rt.Date).First())
                .Where(rt =>
                {
                    // Kullanıcının FCM token'ı var mı?
                    if (!userTokenDict.ContainsKey(rt.UserId))
                        return false;

                    var isIncome = rt.IncomeCategoryId.HasValue && !rt.ExpenseCategoryId.HasValue && rt.Type == TransactionType.Income;
                    var isExpense = rt.IncomeCategoryId.HasValue && rt.ExpenseCategoryId.HasValue && rt.Type == TransactionType.Expense;

                    if (!isIncome && !isExpense)
                        return false;

                    // DayOfMonth kontrolü: Sadece bugünün günü DayOfMonth'a eşit veya geçtiğinde bildirim gönder
                    // Örnek: DayOfMonth=21 ise, sadece ayın 21'inde veya sonrasında bildirim gönder
                    if (rt.DayOfMonth.HasValue && today.Day < rt.DayOfMonth.Value)
                        return false;

                    // Bu ay için transaction var mı? (in-memory check)
                    // DayOfMonth'u da kontrol et - aynı kategoride farklı DayOfMonth değerleri ayrı işlenmeli
                    var key = $"{rt.UserId}_{rt.Type}_{rt.IncomeCategoryId}_{rt.ExpenseCategoryId}_{rt.DayOfMonth}";
                    return !existingTransactionsSet.Contains(key);
                })
                .ToList();

            if (!groupedRecurringTransactions.Any())
            {
                Console.WriteLine("No transactions to notify (all already created or DayOfMonth not reached)");
                logger?.Info("CreateMonthlyRecurringTransactions: No transactions to notify (all already created or DayOfMonth not reached)");
                return;
            }

            Console.WriteLine($"{groupedRecurringTransactions.Count} transactions eligible for notification");
            logger?.Info($"CreateMonthlyRecurringTransactions: {groupedRecurringTransactions.Count} transactions eligible for notification");

            // 8. Notification'ları gönder
            // Firebase rate limit'i için batch'ler halinde gönder (200'lük gruplar)
            const int batchSize = 200;
            var batches = groupedRecurringTransactions
                .Select((rt, index) => new { rt, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.rt).ToList())
                .ToList();

            int totalSent = 0;
            int totalFailed = 0;

            foreach (var batch in batches)
            {
                var notificationTasks = batch.Select(async rt =>
                {
                    try
                    {
                        if (!userTokenDict.TryGetValue(rt.UserId, out var fcmToken))
                            return false;

                        var transactionType = rt.Type == TransactionType.Income ? "income" : "expense";
                        var title = rt.Type == TransactionType.Income ? "Aylık Gelir Hatırlatması" : "Aylık Gider Hatırlatması";
                        
                        // Kategori ismini al
                        string categoryName = "";
                        if (rt.Type == TransactionType.Income && rt.IncomeCategoryId.HasValue)
                        {
                            categoryName = incomeCategoryDict.TryGetValue(rt.IncomeCategoryId.Value, out var incName) ? incName : "";
                        }
                        else if (rt.Type == TransactionType.Expense && rt.ExpenseCategoryId.HasValue)
                        {
                            categoryName = expenseCategoryDict.TryGetValue(rt.ExpenseCategoryId.Value, out var expName) ? expName : "";
                        }
                        
                        // Body mesajını oluştur - gün bilgisi yok, sadece kategori adı
                        var body = rt.Type == TransactionType.Income
                            ? (!string.IsNullOrEmpty(categoryName) 
                                ? $"{categoryName} gelir kaydı eklemeniz gerekiyor."
                                : "Gelir kaydı eklemeniz gerekiyor.")
                            : (!string.IsNullOrEmpty(categoryName)
                                ? $"{categoryName} gider kaydı eklemeniz gerekiyor."
                                : "Gider kaydı eklemeniz gerekiyor.");

                        // Notification data - deep link kaldırıldı, sadece bilgi amaçlı data gönderiliyor
                        var notificationData = new
                        {
                            type = "recurring_transaction",
                            transactionType = transactionType,
                            incomeCategoryId = rt.IncomeCategoryId?.ToString() ?? "",
                            expenseCategoryId = rt.ExpenseCategoryId?.ToString() ?? "",
                            amount = rt.Amount.ToString(),
                            description = rt.Description ?? "",
                            dayOfMonth = rt.DayOfMonth?.ToString() ?? ""
                        };

                        return await firebaseNotificationService.SendNotificationAsync(
                            fcmToken,
                            title,
                            body,
                            notificationData
                        );
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"CreateMonthlyRecurringTransactions: Error sending notification to user {rt.UserId}. {ex.Message}");
                        return false;
                    }
                });

                var results = await Task.WhenAll(notificationTasks);
                totalSent += results.Count(r => r);
                totalFailed += results.Count(r => !r);

                // Rate limit için bekleme (her batch arasında)
                if (batches.Count > 1)
                {
                    await Task.Delay(1500); // 1.5 saniye bekleme
                }
            }

            Console.WriteLine($"CreateMonthlyRecurringTransactions: Sent {totalSent} notifications, Failed: {totalFailed}");
            logger?.Info($"CreateMonthlyRecurringTransactions: Sent {totalSent} notifications, Failed: {totalFailed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateMonthlyRecurringTransactions: Exception - {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Logger might be null, so try to get it again
                try
                {
                    var errorLogger = scopedServiceProvider?.GetService<FileLogger>();
                    errorLogger?.Error($"CreateMonthlyRecurringTransactions job error: {ex.Message}");
                    errorLogger?.Error($"Stack trace: {ex.StackTrace}");
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log error: {logEx.Message}");
                }
                throw;
            }
        }
    }
}

