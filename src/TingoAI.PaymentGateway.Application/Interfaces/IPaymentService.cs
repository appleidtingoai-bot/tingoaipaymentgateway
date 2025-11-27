using TingoAI.PaymentGateway.Application.DTOs;

namespace TingoAI.PaymentGateway.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> InitiatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<TransactionDto?> VerifyTransactionAsync(string reference, CancellationToken cancellationToken = default);
    Task ProcessWebhookAsync(string encryptedData, CancellationToken cancellationToken = default);
}
