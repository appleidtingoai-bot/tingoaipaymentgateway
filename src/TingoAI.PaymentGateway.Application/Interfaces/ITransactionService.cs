using TingoAI.PaymentGateway.Application.DTOs;

namespace TingoAI.PaymentGateway.Application.Interfaces;

public interface ITransactionService
{
    Task<TransactionDto?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionDto>> GetAllTransactionsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<TransactionSummaryDto> GetTransactionSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<TransactionQueryResultDto> QueryTransactionsAsync(int page = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null, string? name = null, bool includeSummary = false, CancellationToken cancellationToken = default);
}
