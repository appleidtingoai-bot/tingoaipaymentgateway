namespace TingoAI.PaymentGateway.Application.DTOs;

public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? TransactionReference { get; set; }
    public string? AccessCode { get; set; }
    public Guid? TransactionId { get; set; }
}
