namespace TingoAI.PaymentGateway.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string MerchantTransactionReference { get; private set; }
    public string? GlobalPayTransactionReference { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public string CustomerFirstName { get; private set; }
    public string CustomerLastName { get; private set; }
    public string CustomerEmail { get; private set; }
    public string CustomerPhone { get; private set; }
    public string? CustomerAddress { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? AccessCode { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public string? PaymentChannel { get; private set; }
    public string? ResponseCode { get; private set; }
    public string? ResponseMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Private constructor for EF Core
    private Transaction() { }

    public Transaction(
        string merchantTransactionReference,
        decimal amount,
        string currency,
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string customerPhone,
        string? customerAddress = null)
    {
        Id = Guid.NewGuid();
        MerchantTransactionReference = merchantTransactionReference ?? throw new ArgumentNullException(nameof(merchantTransactionReference));
        Amount = amount > 0 ? amount : throw new ArgumentException("Amount must be greater than zero", nameof(amount));
        Currency = ValidateCurrency(currency);
        CustomerFirstName = customerFirstName ?? throw new ArgumentNullException(nameof(customerFirstName));
        CustomerLastName = customerLastName ?? throw new ArgumentNullException(nameof(customerLastName));
        CustomerEmail = customerEmail ?? throw new ArgumentNullException(nameof(customerEmail));
        CustomerPhone = customerPhone ?? throw new ArgumentNullException(nameof(customerPhone));
        CustomerAddress = customerAddress;
        PaymentStatus = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCheckoutDetails(string checkoutUrl, string accessCode, string globalPayReference)
    {
        CheckoutUrl = checkoutUrl ?? throw new ArgumentNullException(nameof(checkoutUrl));
        AccessCode = accessCode ?? throw new ArgumentNullException(nameof(accessCode));
        GlobalPayTransactionReference = globalPayReference ?? throw new ArgumentNullException(nameof(globalPayReference));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePaymentStatus(
        PaymentStatus status,
        string? responseCode = null,
        string? responseMessage = null,
        DateTime? paymentDate = null,
        string? paymentChannel = null)
    {
        PaymentStatus = status;
        ResponseCode = responseCode;
        ResponseMessage = responseMessage;
        PaymentDate = paymentDate;
        PaymentChannel = paymentChannel;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string ValidateCurrency(string currency)
    {
        var validCurrencies = new[] { "NGN", "USD", "EUR", "GBP" };
        if (string.IsNullOrWhiteSpace(currency) || !validCurrencies.Contains(currency.ToUpper()))
        {
            throw new ArgumentException($"Currency must be one of: {string.Join(", ", validCurrencies)}", nameof(currency));
        }
        return currency.ToUpper();
    }
}
