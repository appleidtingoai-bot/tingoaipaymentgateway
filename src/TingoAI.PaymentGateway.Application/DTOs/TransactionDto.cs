namespace TingoAI.PaymentGateway.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string MerchantTransactionReference { get; set; } = string.Empty;
    public string? GlobalPayTransactionReference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentChannel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
