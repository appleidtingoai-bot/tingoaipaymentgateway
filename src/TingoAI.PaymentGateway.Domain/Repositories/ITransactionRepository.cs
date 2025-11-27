using TingoAI.PaymentGateway.Domain.Entities;

namespace TingoAI.PaymentGateway.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByGlobalPayReferenceAsync(string globalPayReference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCurrencyAsync(string currency, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Transaction> Items, int TotalCount)> QueryAsync(int page = 1, int pageSize = 50, DateTime? startDate = null, DateTime? endDate = null, string? name = null, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAmountByCurrencyAsync(string currency, CancellationToken cancellationToken = default);
    Task<int> GetSuccessfulTransactionsCountAsync(CancellationToken cancellationToken = default);
}
