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
    // Geçmişte herhangi bir zamanda IsMonthlyRecurring=true olan transaction'ları kontrol eder
    // Bu ay içinde bugüne kadar kaydedilmemiş olanları bugün verisi olarak kaydeder
    [RecurringJob("*/10 * * * *", RecurringJobId = "CreateMonthlyRecurringTransactions")]
    public static async Task CreateMonthlyRecurringTransactions()
    {
        var serviceProvider = ServiceTool.ServiceProvider;
        var transactionRepository = serviceProvider?.GetService<ITransactionRepository>();
        
        try
        {
            if (transactionRepository == null)
            {
                return;
            }

            var today = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var todayDay = today.Day;
            
            var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var currentMonthEnd = new DateTime(today.Year, today.Month, lastDayOfMonth, 23, 59, 59, DateTimeKind.Unspecified);

            var allRecurringTransactions = await transactionRepository.Query()
                .Where(x => x.IsMonthlyRecurring == true 
                     && x.IsActive != false
                )
                .ToListAsync();

            if (allRecurringTransactions == null || !allRecurringTransactions.Any())
            {
                return;
            }

            var groupedRecurringTransactions = allRecurringTransactions
                .GroupBy(rt => new
                {
                    rt.UserId,
                    rt.Type,
                    IncomeCategoryId = rt.IncomeCategoryId,
                    ExpenseCategoryId = rt.ExpenseCategoryId
                })
                .Select(g => g.OrderByDescending(rt => rt.Date).First())
                .ToList();

            var transactionsToCreate = new List<Transaction>();
            
            foreach (var recurringTransaction in groupedRecurringTransactions)
            {
                var isIncome = recurringTransaction.IncomeCategoryId.HasValue && !recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Income;
                var isExpense = recurringTransaction.IncomeCategoryId.HasValue && recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Expense;
                
                if (!isIncome && !isExpense)
                {
                    continue;
                }
                
                var hasExistingThisMonth = await transactionRepository.Query()
                    .AnyAsync(x => x.UserId == recurringTransaction.UserId
                         && x.Type == recurringTransaction.Type
                         && x.Date >= currentMonthStart
                         && x.Date <= today
                         && x.IsActive != false
                         && (
                             (isIncome && x.IncomeCategoryId == recurringTransaction.IncomeCategoryId && !x.ExpenseCategoryId.HasValue) ||
                             (isExpense && x.IncomeCategoryId == recurringTransaction.IncomeCategoryId && x.ExpenseCategoryId == recurringTransaction.ExpenseCategoryId)
                         ));

                if (!hasExistingThisMonth)
                {
                    transactionsToCreate.Add(recurringTransaction);
                }
            }

            if (transactionsToCreate == null || !transactionsToCreate.Any())
            {
                return;
            }

            foreach (var recurringTransaction in transactionsToCreate)
            {
                try
                {
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
                        IsMonthlyRecurring = true,
                        IsBalanceCarriedOver = recurringTransaction.IsBalanceCarriedOver,
                        DayOfMonth = recurringTransaction.DayOfMonth,
                        IsActive = true
                    };

                    transactionRepository.Add(newTransaction);
                }
                catch (Exception)
                {
                    // Hata durumunda sessizce devam et
                }
            }

            if (transactionsToCreate.Any())
            {
                await transactionRepository.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            // Hata durumunda sessizce devam et
        }
    }
}

