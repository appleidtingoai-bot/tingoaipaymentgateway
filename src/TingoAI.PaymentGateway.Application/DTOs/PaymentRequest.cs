using TingoAI.PaymentGateway.Domain.ValueObjects;

namespace TingoAI.PaymentGateway.Application.DTOs;

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? MerchantTransactionReference { get; set; }
    public List<CustomField>? CustomFields { get; set; }
}
