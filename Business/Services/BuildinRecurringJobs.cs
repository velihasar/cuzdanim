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

    // Her gün sabah saat 9'da çalışır
    // Cron expression: "0 9 * * *" = Her gün saat 09:00'da
    // Geçmişte herhangi bir zamanda IsMonthlyRecurring=true olan transaction'ları kontrol eder
    // Bu ay içinde bugüne kadar kaydedilmemiş olanlar için push notification gönderir
    [RecurringJob("0 9 * * *", RecurringJobId = "CreateMonthlyRecurringTransactions")]
    public static async Task CreateMonthlyRecurringTransactions()
    {
        var serviceProvider = ServiceTool.ServiceProvider;
        
        if (serviceProvider == null)
        {
            return;
        }
        
        // Her job için yeni bir scope oluştur (DbContext concurrency sorununu çözmek için)
        using (var scope = serviceProvider.CreateScope())
        {
            var scopedServiceProvider = scope.ServiceProvider;
            
            ITransactionRepository transactionRepository = null;
            IUserRepository userRepository = null;
            IIncomeCategoryRepository incomeCategoryRepository = null;
            IExpenseCategoryRepository expenseCategoryRepository = null;
            IFirebaseNotificationService firebaseNotificationService = null;
            
            try
            {
                transactionRepository = scopedServiceProvider?.GetService<ITransactionRepository>();
            }
            catch
            {
            }
            
            try
            {
                userRepository = scopedServiceProvider?.GetService<IUserRepository>();
            }
            catch
            {
            }
            
            try
            {
                incomeCategoryRepository = scopedServiceProvider?.GetService<IIncomeCategoryRepository>();
            }
            catch
            {
            }
            
            try
            {
                expenseCategoryRepository = scopedServiceProvider?.GetService<IExpenseCategoryRepository>();
            }
            catch
            {
            }
            
            try
            {
                firebaseNotificationService = scopedServiceProvider?.GetService<IFirebaseNotificationService>();
            }
            catch
            {
            }
            
            try
            {
            if (transactionRepository == null || userRepository == null || incomeCategoryRepository == null || expenseCategoryRepository == null || firebaseNotificationService == null)
            {
                return;
            }

            var today = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

            // 1. Tüm recurring transaction'ları bir kerede çek
            var allRecurringTransactions = await transactionRepository.Query()
                .Where(x => x.IsMonthlyRecurring == true && x.IsActive != false)
                .ToListAsync();

            if (allRecurringTransactions == null || !allRecurringTransactions.Any())
            {
                return;
            }

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
                return;
            }

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

                    // DayOfMonth kontrolü: Sadece bugünün gününe eşit olanlar için bildirim gönder
                    // Eğer ay 31'den az gün çekiyorsa (29-30 günlük aylar) ve DayOfMonth > ayın son günü ise,
                    // ayın son gününde bildirim gönder
                    if (rt.DayOfMonth.HasValue)
                    {
                        var todayDay = today.Day;
                        var dayOfMonth = rt.DayOfMonth.Value;
                        var lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);
                        
                        // Eğer DayOfMonth > ayın son günü ise, sadece ayın son gününde bildirim gönder
                        if (dayOfMonth > lastDayOfMonth)
                        {
                            // Sadece bugün ayın son günüyse bildirim gönder
                            if (todayDay != lastDayOfMonth)
                                return false;
                        }
                        else
                        {
                            // DayOfMonth <= ayın son günü ise, sadece bugün DayOfMonth'a eşitse bildirim gönder
                            if (todayDay != dayOfMonth)
                                return false;
                        }
                    }

                    // Bu ay için transaction var mı? (in-memory check)
                    // DayOfMonth'u da kontrol et - aynı kategoride farklı DayOfMonth değerleri ayrı işlenmeli
                    var key = $"{rt.UserId}_{rt.Type}_{rt.IncomeCategoryId}_{rt.ExpenseCategoryId}_{rt.DayOfMonth}";
                    return !existingTransactionsSet.Contains(key);
                })
                .ToList();

            if (!groupedRecurringTransactions.Any())
            {
                return;
            }

            // 8. Kullanıcı bazında grupla ve tek bildirim gönder
            var userNotifications = groupedRecurringTransactions
                .GroupBy(rt => rt.UserId)
                .Select(userGroup =>
                {
                    var userId = userGroup.Key;
                    var transactions = userGroup.ToList();
                    
                    // Gelir ve gider transaction'larını ayır
                    var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income).ToList();
                    var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense).ToList();
                    
                    // Mesaj oluştur
                    string title = "Aylık İşlem Hatırlatması";
                    string body = "";
                    
                    if (incomeTransactions.Count == 1 && expenseTransactions.Count == 0)
                    {
                        // Tek gelir
                        var rt = incomeTransactions.First();
                        string categoryName = "";
                        if (rt.IncomeCategoryId.HasValue)
                        {
                            categoryName = incomeCategoryDict.TryGetValue(rt.IncomeCategoryId.Value, out var incName) ? incName : "";
                        }
                        body = !string.IsNullOrEmpty(categoryName)
                            ? $"{categoryName} gelir kaydı eklemeniz gerekiyor."
                            : "Gelir kaydı eklemeniz gerekiyor.";
                    }
                    else if (expenseTransactions.Count == 1 && incomeTransactions.Count == 0)
                    {
                        // Tek gider
                        var rt = expenseTransactions.First();
                        string categoryName = "";
                        if (rt.ExpenseCategoryId.HasValue)
                        {
                            categoryName = expenseCategoryDict.TryGetValue(rt.ExpenseCategoryId.Value, out var expName) ? expName : "";
                        }
                        body = !string.IsNullOrEmpty(categoryName)
                            ? $"{categoryName} gider kaydı eklemeniz gerekiyor."
                            : "Gider kaydı eklemeniz gerekiyor.";
                    }
                    else if (incomeTransactions.Count > 1 && expenseTransactions.Count == 0)
                    {
                        // Birden fazla gelir
                        body = "Eklemeniz gereken gelirler var.";
                    }
                    else if (expenseTransactions.Count > 1 && incomeTransactions.Count == 0)
                    {
                        // Birden fazla gider
                        body = "Eklemeniz gereken giderler var.";
                    }
                    else if (incomeTransactions.Count > 0 && expenseTransactions.Count > 0)
                    {
                        // Hem gelir hem gider
                        body = "Eklemeniz gereken gelirler ve giderler var.";
                    }
                    else
                    {
                        // Fallback (olması gerekmeyen durum)
                        body = "Eklemeniz gereken işlemler var.";
                    }
                    
                    return new
                    {
                        UserId = userId,
                        Title = title,
                        Body = body,
                        IncomeCount = incomeTransactions.Count,
                        ExpenseCount = expenseTransactions.Count
                    };
                })
                .ToList();

            if (!userNotifications.Any())
            {
                return;
            }

            // 9. Her kullanıcı için tek bildirim gönder
            var notificationTasks = userNotifications.Select(async notification =>
            {
                try
                {
                    if (!userTokenDict.TryGetValue(notification.UserId, out var fcmToken))
                        return false;

                    var notificationData = new
                    {
                        type = "recurring_transaction",
                        incomeCount = notification.IncomeCount,
                        expenseCount = notification.ExpenseCount
                    };

                    var success = await firebaseNotificationService.SendNotificationAsync(
                        fcmToken,
                        notification.Title,
                        notification.Body,
                        notificationData
                    );

                    return success;
                }
                catch
                {
                    return false;
                }
            });

            await Task.WhenAll(notificationTasks);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

