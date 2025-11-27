using Microsoft.EntityFrameworkCore;
using TingoAI.PaymentGateway.Domain.Entities;
using TingoAI.PaymentGateway.Domain.Repositories;
using TingoAI.PaymentGateway.Infrastructure.Persistence;

namespace TingoAI.PaymentGateway.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction?> GetByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.MerchantTransactionReference == merchantReference, cancellationToken);
    }

    public async Task<Transaction?> GetByGlobalPayReferenceAsync(string globalPayReference, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.GlobalPayTransactionReference == globalPayReference, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByCurrencyAsync(string currency, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Currency == currency)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> QueryAsync(int page = 1, int pageSize = 50, DateTime? startDate = null, DateTime? endDate = null, string? name = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions.AsNoTracking().AsQueryable();

        if (startDate.HasValue && endDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= startDate.Value && t.CreatedAt <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.Trim().ToLower();
            query = query.Where(t => (t.CustomerFirstName != null && t.CustomerFirstName.ToLower().Contains(n)) || (t.CustomerLastName != null && t.CustomerLastName.ToLower().Contains(n)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Transactions.CountAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAmountByCurrencyAsync(string currency, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.Currency == currency && t.PaymentStatus == PaymentStatus.Successful)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<int> GetSuccessfulTransactionsCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .CountAsync(t => t.PaymentStatus == PaymentStatus.Successful, cancellationToken);
    }
}
