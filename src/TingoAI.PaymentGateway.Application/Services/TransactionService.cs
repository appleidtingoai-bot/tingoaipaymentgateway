using TingoAI.PaymentGateway.Application.DTOs;
using TingoAI.PaymentGateway.Application.Interfaces;
using TingoAI.PaymentGateway.Domain.Entities;
using TingoAI.PaymentGateway.Domain.Repositories;

namespace TingoAI.PaymentGateway.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        return transaction != null ? MapToDto(transaction) : null;
    }

    public async Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetAllAsync(page, pageSize, cancellationToken);
        return transactions.Select(MapToDto);
    }

    public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Transaction> transactions;

        if (startDate.HasValue && endDate.HasValue)
        {
            transactions = await _transactionRepository.GetByDateRangeAsync(startDate.Value, endDate.Value, cancellationToken);
        }
        else
        {
            transactions = await _transactionRepository.GetAllAsync(1, int.MaxValue, cancellationToken);
        }

        var transactionList = transactions.ToList();
        var totalCount = transactionList.Count;
        var successfulCount = transactionList.Count(t => t.PaymentStatus == PaymentStatus.Successful);
        var failedCount = transactionList.Count(t => t.PaymentStatus == PaymentStatus.Failed);
        var pendingCount = transactionList.Count(t => t.PaymentStatus == PaymentStatus.Pending);

        var currencies = new[] { "NGN", "USD", "EUR", "GBP" };
        var amountByCurrency = new Dictionary<string, decimal>();
        var countByCurrency = new Dictionary<string, int>();

        foreach (var currency in currencies)
        {
            var currencyTransactions = transactionList.Where(t => t.Currency == currency).ToList();
            amountByCurrency[currency] = currencyTransactions
                .Where(t => t.PaymentStatus == PaymentStatus.Successful)
                .Sum(t => t.Amount);
            countByCurrency[currency] = currencyTransactions.Count;
        }

        return new TransactionSummaryDto
        {
            TotalTransactions = totalCount,
            SuccessfulTransactions = successfulCount,
            FailedTransactions = failedCount,
            PendingTransactions = pendingCount,
            SuccessRate = totalCount > 0 ? (decimal)successfulCount / totalCount * 100 : 0,
            TotalAmountByCurrency = amountByCurrency,
            TransactionCountByCurrency = countByCurrency
        };
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            MerchantTransactionReference = transaction.MerchantTransactionReference,
            GlobalPayTransactionReference = transaction.GlobalPayTransactionReference,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            CustomerFirstName = transaction.CustomerFirstName,
            CustomerLastName = transaction.CustomerLastName,
            CustomerEmail = transaction.CustomerEmail,
            CustomerPhone = transaction.CustomerPhone,
            CustomerAddress = transaction.CustomerAddress,
            PaymentStatus = transaction.PaymentStatus.ToString(),
            CheckoutUrl = transaction.CheckoutUrl,
            PaymentDate = transaction.PaymentDate,
            PaymentChannel = transaction.PaymentChannel,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    public async Task<TransactionQueryResultDto> QueryTransactionsAsync(int page = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null, string? name = null, bool includeSummary = false, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _transactionRepository.QueryAsync(page, pageSize, startDate, endDate, name, cancellationToken);

        var dtoItems = items.Select(MapToDto).ToList();

        TransactionSummaryDto? summary = null;
        if (includeSummary)
        {
            summary = await GetTransactionSummaryAsync(startDate, endDate, cancellationToken);
        }

        return new TransactionQueryResultDto
        {
            Items = dtoItems,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Summary = summary
        };
    }
}
