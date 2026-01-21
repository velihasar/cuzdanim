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
        // ServiceProvider'dan dependency'leri resolve et
        var serviceProvider = ServiceTool.ServiceProvider;
        var transactionRepository = serviceProvider?.GetService<ITransactionRepository>();
        var logger = serviceProvider?.GetService<FileLogger>();
        
        // Job başladı logu
        var startMessage = "==========================================";
        logger?.Info(startMessage);
        Console.WriteLine(startMessage);
        
        var jobStartedMessage = "CreateMonthlyRecurringTransactions: JOB STARTED";
        logger?.Info(jobStartedMessage);
        Console.WriteLine(jobStartedMessage);
        
        var timeMessage = $"CreateMonthlyRecurringTransactions: Current UTC Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        logger?.Info(timeMessage);
        Console.WriteLine(timeMessage);
        
        try
        {
            if (transactionRepository == null)
            {
                var errorMsg = "CreateMonthlyRecurringTransactions: ITransactionRepository not found in DI container";
                logger?.Error(errorMsg);
                Console.Error.WriteLine(errorMsg);
                
                var endMsg = "CreateMonthlyRecurringTransactions: JOB ENDED - Repository not found";
                logger?.Info(endMsg);
                Console.WriteLine(endMsg);
                
                logger?.Info(startMessage);
                Console.WriteLine(startMessage);
                return;
            }

            var today = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            var todayDay = today.Day;
            
            // Bu ayın başlangıç ve bitiş tarihleri
            var currentMonthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var currentMonthEnd = new DateTime(today.Year, today.Month, lastDayOfMonth, 23, 59, 59, DateTimeKind.Unspecified);

            var dayMessage = $"CreateMonthlyRecurringTransactions: Starting for day {todayDay} (Date: {today:yyyy-MM-dd})";
            logger?.Info(dayMessage);
            Console.WriteLine(dayMessage);
            
            var currentMonthRangeMessage = $"CreateMonthlyRecurringTransactions: Current month range: {currentMonthStart:yyyy-MM-dd} to {currentMonthEnd:yyyy-MM-dd}";
            logger?.Info(currentMonthRangeMessage);
            Console.WriteLine(currentMonthRangeMessage);

            // Geçmişte herhangi bir zamanda IsMonthlyRecurring=true olan tüm transaction'ları bul
            // Tarih kısıtlaması yok, sadece IsMonthlyRecurring=true ve IsActive!=false olanları alıyoruz
            var allRecurringTransactions = await transactionRepository.Query()
                .Where(x => x.IsMonthlyRecurring == true 
                     && x.IsActive != false
                )
                .ToListAsync();

            if (allRecurringTransactions == null || !allRecurringTransactions.Any())
            {
                var noRecurringMessage = $"CreateMonthlyRecurringTransactions: No recurring transactions found";
                logger?.Info(noRecurringMessage);
                Console.WriteLine(noRecurringMessage);
                
                var jobEndedMessage = "CreateMonthlyRecurringTransactions: JOB ENDED - No recurring transactions found";
                logger?.Info(jobEndedMessage);
                Console.WriteLine(jobEndedMessage);
                
                logger?.Info(startMessage);
                Console.WriteLine(startMessage);
                return;
            }

            var foundRecurringMessage = $"CreateMonthlyRecurringTransactions: Found {allRecurringTransactions.Count} recurring transactions (from any time)";
            logger?.Info(foundRecurringMessage);
            Console.WriteLine(foundRecurringMessage);

            // Kullanıcı, Type, IncomeCategoryId, ExpenseCategoryId'ye göre grupla ve en son tarihli olanı seç
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

            var groupedMessage = $"CreateMonthlyRecurringTransactions: Grouped to {groupedRecurringTransactions.Count} unique recurring transactions";
            logger?.Info(groupedMessage);
            Console.WriteLine(groupedMessage);

            // Bu ay içinde bugüne kadar kaydedilmemiş transaction'ları bul
            var transactionsToCreate = new List<Transaction>();
            
            foreach (var recurringTransaction in groupedRecurringTransactions)
            {
                // Transaction tipini belirle
                // Gelir: IncomeCategoryId dolu, ExpenseCategoryId boş, Type = 1 (Income)
                // Gider: Her ikisi de dolu, Type = 2 (Expense)
                var isIncome = recurringTransaction.IncomeCategoryId.HasValue && !recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Income;
                var isExpense = recurringTransaction.IncomeCategoryId.HasValue && recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Expense;
                
                if (!isIncome && !isExpense)
                {
                    var skipMessage = $"CreateMonthlyRecurringTransactions: Skipping transaction - Invalid type configuration. UserId: {recurringTransaction.UserId}, Type: {recurringTransaction.Type}, IncomeCategoryId: {recurringTransaction.IncomeCategoryId}, ExpenseCategoryId: {recurringTransaction.ExpenseCategoryId}";
                    logger?.Warn(skipMessage);
                    Console.WriteLine(skipMessage);
                    continue;
                }
                
                // Bu ay içinde bugüne kadar aynı kategoride transaction var mı kontrol et
                var hasExistingThisMonth = await transactionRepository.Query()
                    .AnyAsync(x => x.UserId == recurringTransaction.UserId
                         && x.Type == recurringTransaction.Type
                         && x.Date >= currentMonthStart
                         && x.Date <= today // Bugüne kadar
                         && x.IsActive != false
                         && (
                             // Gelir kontrolü: IncomeCategoryId eşit olmalı, ExpenseCategoryId null olmalı
                             (isIncome && x.IncomeCategoryId == recurringTransaction.IncomeCategoryId && !x.ExpenseCategoryId.HasValue) ||
                             // Gider kontrolü: Hem IncomeCategoryId hem ExpenseCategoryId eşit olmalı
                             (isExpense && x.IncomeCategoryId == recurringTransaction.IncomeCategoryId && x.ExpenseCategoryId == recurringTransaction.ExpenseCategoryId)
                         ));

                if (!hasExistingThisMonth)
                {
                    transactionsToCreate.Add(recurringTransaction);
                }
            }

            var foundMessage = $"CreateMonthlyRecurringTransactions: Found {groupedRecurringTransactions.Count} grouped recurring transactions, {transactionsToCreate.Count} need to be created (no existing transaction this month until today)";
            logger?.Info(foundMessage);
            Console.WriteLine(foundMessage);

            if (transactionsToCreate == null || !transactionsToCreate.Any())
            {
                var noTransactionsMessage = $"CreateMonthlyRecurringTransactions: No recurring transactions need to be created (all have existing transactions this month)";
                logger?.Info(noTransactionsMessage);
                Console.WriteLine(noTransactionsMessage);
                
                var jobEndedMessage = "CreateMonthlyRecurringTransactions: JOB ENDED - No transactions to process";
                logger?.Info(jobEndedMessage);
                Console.WriteLine(jobEndedMessage);
                
                logger?.Info(startMessage);
                Console.WriteLine(startMessage);
                return;
            }

            int createdCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            var processingMessage = $"CreateMonthlyRecurringTransactions: Processing {transactionsToCreate.Count} recurring transactions to create...";
            logger?.Info(processingMessage);
            Console.WriteLine(processingMessage);

            foreach (var recurringTransaction in transactionsToCreate)
            {
                try
                {
                    // Transaction tipini belirle
                    var isIncome = recurringTransaction.IncomeCategoryId.HasValue && !recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Income;
                    var isExpense = recurringTransaction.IncomeCategoryId.HasValue && recurringTransaction.ExpenseCategoryId.HasValue && recurringTransaction.Type == TransactionType.Expense;
                    
                    var transactionTypeDescription = isIncome ? "Gelir" : (isExpense ? "Gider" : "Bilinmeyen");
                    var processingTransactionMessage = $"CreateMonthlyRecurringTransactions: Processing transaction - UserId: {recurringTransaction.UserId}, Amount: {recurringTransaction.Amount}, Type: {recurringTransaction.Type} ({transactionTypeDescription}), DayOfMonth: {recurringTransaction.DayOfMonth}, IncomeCategoryId: {recurringTransaction.IncomeCategoryId}, ExpenseCategoryId: {recurringTransaction.ExpenseCategoryId}, Original Date: {recurringTransaction.Date:yyyy-MM-dd}";
                    logger?.Info(processingTransactionMessage);
                    Console.WriteLine(processingTransactionMessage);

                    // Yeni transaction oluştur (bugünün tarihiyle, IsMonthlyRecurring=true)
                    // Geçen ayın recurring transaction'ının bilgilerini kullan
                    var newTransaction = new Core.Entities.Concrete.Transaction
                    {
                        UserId = recurringTransaction.UserId,
                        AssetId = recurringTransaction.AssetId,
                        IncomeCategoryId = recurringTransaction.IncomeCategoryId,
                        ExpenseCategoryId = recurringTransaction.ExpenseCategoryId,
                        Amount = recurringTransaction.Amount,
                        Date = today, // Bugün verisi olarak kaydet
                        Description = recurringTransaction.Description,
                        Type = recurringTransaction.Type,
                        IsMonthlyRecurring = true, // Yeni oluşturulan transaction da recurring
                        IsBalanceCarriedOver = recurringTransaction.IsBalanceCarriedOver,
                        DayOfMonth = recurringTransaction.DayOfMonth, // Orijinal DayOfMonth'u koru
                        IsActive = true
                    };

                    transactionRepository.Add(newTransaction);
                    createdCount++;

                    var createdMessage = $"CreateMonthlyRecurringTransactions: Created new transaction for UserId {recurringTransaction.UserId}, Amount {recurringTransaction.Amount}, Type {recurringTransaction.Type} ({transactionTypeDescription}), Date: {today:yyyy-MM-dd}";
                    logger?.Info(createdMessage);
                    Console.WriteLine(createdMessage);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"CreateMonthlyRecurringTransactions: Error creating transaction for UserId {recurringTransaction.UserId}. Exception: {ex.Message}, StackTrace: {ex.StackTrace}";
                    logger?.Error(errorMessage);
                    Console.Error.WriteLine(errorMessage);
                    errorCount++;
                }
            }

            // Değişiklikleri kaydet
            if (createdCount > 0)
            {
                var savingMessage = $"CreateMonthlyRecurringTransactions: Saving {createdCount} new transactions to database...";
                logger?.Info(savingMessage);
                Console.WriteLine(savingMessage);
                
                await transactionRepository.SaveChangesAsync();
                
                var savedMessage = $"CreateMonthlyRecurringTransactions: Successfully saved {createdCount} transactions to database";
                logger?.Info(savedMessage);
                Console.WriteLine(savedMessage);
                
                var summaryMessage = $"CreateMonthlyRecurringTransactions: Summary - Created: {createdCount}, Skipped: {skippedCount}, Errors: {errorCount}";
                logger?.Info(summaryMessage);
                Console.WriteLine(summaryMessage);
            }
            else
            {
                var noNewTransactionsMessage = $"CreateMonthlyRecurringTransactions: No new transactions created. Skipped: {skippedCount}, Errors: {errorCount}";
                logger?.Info(noNewTransactionsMessage);
                Console.WriteLine(noNewTransactionsMessage);
            }

            var completedMessage = "CreateMonthlyRecurringTransactions: JOB COMPLETED SUCCESSFULLY";
            logger?.Info(completedMessage);
            Console.WriteLine(completedMessage);
            
            logger?.Info(startMessage);
            Console.WriteLine(startMessage);
        }
        catch (Exception ex)
        {
            var jobErrorMessage = $"CreateMonthlyRecurringTransactions job error: {ex.Message}";
            logger?.Error(jobErrorMessage);
            Console.Error.WriteLine(jobErrorMessage);
            
            var stackTraceMessage = $"CreateMonthlyRecurringTransactions job StackTrace: {ex.StackTrace}";
            logger?.Error(stackTraceMessage);
            Console.Error.WriteLine(stackTraceMessage);
            
            var jobEndedErrorMessage = "CreateMonthlyRecurringTransactions: JOB ENDED WITH ERROR";
            logger?.Error(jobEndedErrorMessage);
            Console.Error.WriteLine(jobEndedErrorMessage);
            
            logger?.Info(startMessage);
            Console.WriteLine(startMessage);
            throw;
        }
    }
}

