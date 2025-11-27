namespace TingoAI.PaymentGateway.Application.Interfaces;

public interface IGlobalPayClient
{
    Task<GlobalPayPaymentResponse> GeneratePaymentLinkAsync(GlobalPayPaymentRequest request, CancellationToken cancellationToken = default);
    Task<GlobalPayTransactionQueryResponse> QueryTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<GlobalPayTransactionQueryResponse> QueryTransactionByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default);
}

// GlobalPay models
public class GlobalPayPaymentRequest
{
    public decimal Amount { get; set; }
    public string MerchantTransactionReference { get; set; } = string.Empty;
    public GlobalPayCustomer Customer { get; set; } = new();
}

public class GlobalPayCustomer
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
}

public class GlobalPayPaymentResponse
{
    public GlobalPayData? Data { get; set; }
}

public class GlobalPayData
{
    public string? CheckoutUrl { get; set; }
    public string? Ref { get; set; }
    public string? AccessCode { get; set; }
    public string? ResponseCode { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Error { get; set; }
}

public class GlobalPayTransactionQueryResponse
{
    public GlobalPayTransactionData? Data { get; set; }
}

public class GlobalPayTransactionData
{
    public string? Txnref { get; set; }
    public string? MerchantId { get; set; }
    public string? Channel { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentDate { get; set; }
    public string? PaymentStatus { get; set; }
    public string? MerchantTxnref { get; set; }
    public decimal InAmount { get; set; }
    public string? InCurrency { get; set; }
    public string? TransactionChannel { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ResponseCode { get; set; }
    public bool IsSuccessful { get; set; }
}
