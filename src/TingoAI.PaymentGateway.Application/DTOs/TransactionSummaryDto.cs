namespace TingoAI.PaymentGateway.Application.DTOs;

public class TransactionSummaryDto
{
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public decimal SuccessRate { get; set; }
    public Dictionary<string, decimal> TotalAmountByCurrency { get; set; } = new();
    public Dictionary<string, int> TransactionCountByCurrency { get; set; } = new();
}
