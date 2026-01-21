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
        var assetTypeRepository = serviceProvider?.GetService<IAssetTypeRepository>();
        var assetTypePriceService = serviceProvider?.GetService<IAssetTypePriceService>();
        var logger = serviceProvider?.GetService<FileLogger>();
        
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

    // Her gün saat 03:00'de çalışır
    // Cron expression: "0 3 * * *" = Her gün saat 03:00
    // Son bir yıldan eski olan transaction'ları siler (son bir yılın verileri kalır)
    [RecurringJob("0 3 * * *", RecurringJobId = "DeleteOldTransactions")]
    public static async Task DeleteOldTransactions()
    {
        // ServiceProvider'dan dependency'leri resolve et
        var serviceProvider = ServiceTool.ServiceProvider;
        var transactionRepository = serviceProvider?.GetService<ITransactionRepository>();
        var logger = serviceProvider?.GetService<FileLogger>();
        
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

    // Her 10 dakikada bir çalışır
    // Cron expression: "*/10 * * * *" = Her 10 dakikada bir
    // IsMonthlyRecurring olan transaction'lar için, DayOfMonth bugünün gününe eşitse yeni transaction oluşturur
    // 31. gün sorunu: DayOfMonth 31 ise ve bugün ayın son günüyse (ay 31 gün çekmiyorsa) transaction oluşturur
    [RecurringJob("*/10 * * * *", RecurringJobId = "CreateMonthlyRecurringTransactions")]
    public static async Task CreateMonthlyRecurringTransactions()
    {
        // ServiceProvider'dan dependency'leri resolve et
        var serviceProvider = ServiceTool.ServiceProvider;
        var transactionRepository = serviceProvider?.GetService<ITransactionRepository>();
        var logger = serviceProvider?.GetService<FileLogger>();
        
        // Job başladı logu
        logger?.Info("==========================================");
        logger?.Info("CreateMonthlyRecurringTransactions: JOB STARTED");
        logger?.Info($"CreateMonthlyRecurringTransactions: Current UTC Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        
        try
        {
            if (transactionRepository == null)
            {
                logger?.Error("CreateMonthlyRecurringTransactions: ITransactionRepository not found in DI container");
                logger?.Info("CreateMonthlyRecurringTransactions: JOB ENDED - Repository not found");
                logger?.Info("==========================================");
                return;
            }

            var today = DateTime.UtcNow;
            var todayDay = today.Day;
            var lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            logger?.Info($"CreateMonthlyRecurringTransactions: Starting for day {todayDay} (Date: {today:yyyy-MM-dd})");
            logger?.Info($"CreateMonthlyRecurringTransactions: Last day of month: {lastDayOfMonth}");
            logger?.Info($"CreateMonthlyRecurringTransactions: Month range: {monthStart:yyyy-MM-dd} to {monthEnd:yyyy-MM-dd}");

            // IsMonthlyRecurring == true ve DayOfMonth == bugünün günü OLAN VEYA
            // DayOfMonth == 31 ve bugün ayın son günüyse (ay 31 gün çekmiyorsa) transaction'ları bul
            var recurringTransactions = await transactionRepository.GetListAsync(
                x => x.IsMonthlyRecurring == true 
                     && x.DayOfMonth.HasValue 
                     && (
                         // Normal durum: DayOfMonth bugünün gününe eşit
                         (x.DayOfMonth.Value == todayDay) ||
                         // 31. gün durumu: DayOfMonth 31 ise ve bugün ayın son günüyse (ay 31 gün çekmiyorsa)
                         (x.DayOfMonth.Value == 31 && todayDay == lastDayOfMonth && lastDayOfMonth < 31)
                     )
                     && x.IsActive != false
            );

            logger?.Info($"CreateMonthlyRecurringTransactions: Found {recurringTransactions?.Count() ?? 0} recurring transactions for day {todayDay}");

            if (recurringTransactions == null || !recurringTransactions.Any())
            {
                logger?.Info($"CreateMonthlyRecurringTransactions: No recurring transactions found for day {todayDay}");
                logger?.Info("CreateMonthlyRecurringTransactions: JOB ENDED - No transactions to process");
                logger?.Info("==========================================");
                return;
            }

            int createdCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            logger?.Info($"CreateMonthlyRecurringTransactions: Processing {recurringTransactions.Count()} recurring transactions...");

            foreach (var recurringTransaction in recurringTransactions)
            {
                try
                {
                    logger?.Info($"CreateMonthlyRecurringTransactions: Processing transaction - UserId: {recurringTransaction.UserId}, Amount: {recurringTransaction.Amount}, Type: {recurringTransaction.Type}, DayOfMonth: {recurringTransaction.DayOfMonth}");

                    // Bu ay için bu recurring transaction'dan zaten bir transaction oluşturulmuş mu kontrol et
                    // Kullanıcı manuel olarak oluşturmuşsa (Description farklı olsa bile) otomatik oluşturma
                    var existingTransaction = await transactionRepository.GetAsync(
                        x => x.UserId == recurringTransaction.UserId
                             && x.Amount == recurringTransaction.Amount
                             && x.Type == recurringTransaction.Type
                             && ((recurringTransaction.IncomeCategoryId.HasValue && x.IncomeCategoryId == recurringTransaction.IncomeCategoryId)
                                 || (!recurringTransaction.IncomeCategoryId.HasValue && !x.IncomeCategoryId.HasValue))
                             && ((recurringTransaction.ExpenseCategoryId.HasValue && x.ExpenseCategoryId == recurringTransaction.ExpenseCategoryId)
                                 || (!recurringTransaction.ExpenseCategoryId.HasValue && !x.ExpenseCategoryId.HasValue))
                             && x.AssetId == recurringTransaction.AssetId
                             // Description kontrolünü kaldırdık - kullanıcı manuel oluşturmuşsa Description farklı olabilir
                             && x.Date >= monthStart
                             && x.Date <= monthEnd
                             && x.IsActive != false
                    );

                    if (existingTransaction != null)
                    {
                        logger?.Info($"CreateMonthlyRecurringTransactions: Transaction already exists for UserId {recurringTransaction.UserId}, Amount {recurringTransaction.Amount}, Type {recurringTransaction.Type} (skipping - user may have created manually)");
                        logger?.Info($"CreateMonthlyRecurringTransactions: Existing transaction Date: {existingTransaction.Date:yyyy-MM-dd}, Id: {existingTransaction.Id}");
                        skippedCount++;
                        continue;
                    }

                    logger?.Info($"CreateMonthlyRecurringTransactions: No existing transaction found, creating new one...");

                    // Yeni transaction oluştur (bugünün tarihiyle, IsMonthlyRecurring=false)
                    var newTransaction = new Core.Entities.Concrete.Transaction
                    {
                        UserId = recurringTransaction.UserId,
                        AssetId = recurringTransaction.AssetId,
                        IncomeCategoryId = recurringTransaction.IncomeCategoryId,
                        ExpenseCategoryId = recurringTransaction.ExpenseCategoryId,
                        Amount = recurringTransaction.Amount,
                        Date = today,
                        Description = recurringTransaction.Description,
                        Type = recurringTransaction.Type,
                        IsMonthlyRecurring = false, // Yeni oluşturulan transaction recurring değil
                        IsBalanceCarriedOver = recurringTransaction.IsBalanceCarriedOver,
                        DayOfMonth = null, // Yeni transaction'da DayOfMonth null
                        IsActive = true
                    };

                    transactionRepository.Add(newTransaction);
                    createdCount++;

                    logger?.Info($"CreateMonthlyRecurringTransactions: Created new transaction for UserId {recurringTransaction.UserId}, Amount {recurringTransaction.Amount}, Type {recurringTransaction.Type}, Date: {today:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    logger?.Error($"CreateMonthlyRecurringTransactions: Error creating transaction for UserId {recurringTransaction.UserId}. Exception: {ex.Message}, StackTrace: {ex.StackTrace}");
                    errorCount++;
                }
            }

            // Değişiklikleri kaydet
            if (createdCount > 0)
            {
                logger?.Info($"CreateMonthlyRecurringTransactions: Saving {createdCount} new transactions to database...");
                await transactionRepository.SaveChangesAsync();
                logger?.Info($"CreateMonthlyRecurringTransactions: Successfully saved {createdCount} transactions to database");
                logger?.Info($"CreateMonthlyRecurringTransactions: Summary - Created: {createdCount}, Skipped: {skippedCount}, Errors: {errorCount}");
            }
            else
            {
                logger?.Info($"CreateMonthlyRecurringTransactions: No new transactions created. Skipped: {skippedCount}, Errors: {errorCount}");
            }

            logger?.Info("CreateMonthlyRecurringTransactions: JOB COMPLETED SUCCESSFULLY");
            logger?.Info("==========================================");
        }
        catch (Exception ex)
        {
            logger?.Error($"CreateMonthlyRecurringTransactions job error: {ex.Message}");
            logger?.Error($"CreateMonthlyRecurringTransactions job StackTrace: {ex.StackTrace}");
            logger?.Error($"CreateMonthlyRecurringTransactions: JOB ENDED WITH ERROR");
            logger?.Info("==========================================");
            throw;
        }
    }
}

