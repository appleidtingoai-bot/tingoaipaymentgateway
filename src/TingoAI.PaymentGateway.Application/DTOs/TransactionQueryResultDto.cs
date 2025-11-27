namespace TingoAI.PaymentGateway.Application.DTOs;

public class TransactionQueryResultDto
{
    public IEnumerable<TransactionDto> Items { get; set; } = Enumerable.Empty<TransactionDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public TransactionSummaryDto? Summary { get; set; }
}
