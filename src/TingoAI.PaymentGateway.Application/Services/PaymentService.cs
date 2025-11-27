using TingoAI.PaymentGateway.Application.DTOs;
using TingoAI.PaymentGateway.Application.Interfaces;
using TingoAI.PaymentGateway.Domain.Entities;
using TingoAI.PaymentGateway.Domain.Repositories;

namespace TingoAI.PaymentGateway.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IGlobalPayClient _globalPayClient;
    private readonly ITransactionRepository _transactionRepository;

    public PaymentService(
        IGlobalPayClient globalPayClient,
        ITransactionRepository transactionRepository)
    {
        _globalPayClient = globalPayClient ?? throw new ArgumentNullException(nameof(globalPayClient));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public async Task<PaymentResponse> InitiatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Always generate a unique merchant reference for this initiation
            var merchantRef = $"TINGO-{Guid.NewGuid():N}";
            request.MerchantTransactionReference = merchantRef;

            var transaction = new Transaction(
                merchantRef,
                request.Amount,
                request.Currency,
                request.CustomerFirstName,
                request.CustomerLastName,
                request.CustomerEmail,
                request.CustomerPhone,
                request.CustomerAddress
            );

            var globalPayRequest = new GlobalPayPaymentRequest
            {
                Amount = request.Amount,
                MerchantTransactionReference = merchantRef,
                Customer = new GlobalPayCustomer
                {
                    FirstName = request.CustomerFirstName,
                    LastName = request.CustomerLastName,
                    Currency = request.Currency,
                    PhoneNumber = request.CustomerPhone,
                    Address = request.CustomerAddress ?? string.Empty,
                    EmailAddress = request.CustomerEmail
                }
            };

            var globalPayResponse = await _globalPayClient.GeneratePaymentLinkAsync(globalPayRequest, cancellationToken);

            if (globalPayResponse?.Data?.IsSuccessful == true && !string.IsNullOrEmpty(globalPayResponse.Data.CheckoutUrl))
            {
                transaction.SetCheckoutDetails(
                    globalPayResponse.Data.CheckoutUrl,
                    globalPayResponse.Data.AccessCode ?? string.Empty,
                    globalPayResponse.Data.Ref ?? string.Empty
                );

                // Persist transaction before returning so callers can verify immediately
                try
                {
                    await _transactionRepository.AddAsync(transaction, CancellationToken.None);
                }
                catch
                {
                    // swallow persistence errors for now
                }

                return new PaymentResponse
                {
                    Success = true,
                    Message = "Payment link generated successfully",
                    CheckoutUrl = globalPayResponse.Data.CheckoutUrl,
                    TransactionReference = merchantRef,
                    AccessCode = globalPayResponse.Data.AccessCode,
                    TransactionId = transaction.Id
                };
            }

            return new PaymentResponse
            {
                Success = false,
                Message = globalPayResponse?.Data?.Error ?? "Failed to generate payment link"
            };
        }
        catch (Exception ex)
        {
            return new PaymentResponse
            {
                Success = false,
                Message = $"Error initiating payment: {ex.Message}"
            };
        }
    }

    public async Task<TransactionDto?> VerifyTransactionAsync(string reference, CancellationToken cancellationToken = default)
    {
        try
        {
            Transaction? transaction = null;

            if (Guid.TryParse(reference, out var id))
            {
                transaction = await _transactionRepository.GetByIdAsync(id, cancellationToken);
            }

            if (transaction == null)
            {
                transaction = await _transactionRepository.GetByMerchantReferenceAsync(reference, cancellationToken)
                           ?? await _transactionRepository.GetByGlobalPayReferenceAsync(reference, cancellationToken);
            }

            // Decide which merchant reference to query GlobalPay with
            var merchantRefForQuery = transaction?.MerchantTransactionReference ?? reference;
            var globalPayResponse = await _globalPayClient.QueryTransactionByMerchantReferenceAsync(merchantRefForQuery, cancellationToken);

            if (globalPayResponse?.Data != null && transaction != null)
            {
                var tx = transaction;

                var status = globalPayResponse.Data.PaymentStatus?.ToLower() switch
                {
                    "successful" => PaymentStatus.Successful,
                    "failed" => PaymentStatus.Failed,
                    _ => PaymentStatus.Pending
                };

                tx.UpdatePaymentStatus(
                    status,
                    globalPayResponse.Data.ResponseCode,
                    globalPayResponse.Data.SuccessMessage,
                    DateTime.TryParse(globalPayResponse.Data.PaymentDate, out var paymentDate) ? paymentDate : null,
                    globalPayResponse.Data.TransactionChannel
                );

                await _transactionRepository.UpdateAsync(tx, cancellationToken);
            }

            return transaction != null ? MapToDto(transaction) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task ProcessWebhookAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        // Webhook processing will be implemented in Infrastructure layer
        await Task.CompletedTask;
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            MerchantTransactionReference = transaction.MerchantTransactionReference,
            GlobalPayTransactionReference = transaction.GlobalPayTransactionReference,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            CustomerFirstName = transaction.CustomerFirstName,
            CustomerLastName = transaction.CustomerLastName,
            CustomerEmail = transaction.CustomerEmail,
            CustomerPhone = transaction.CustomerPhone,
            CustomerAddress = transaction.CustomerAddress,
            PaymentStatus = transaction.PaymentStatus.ToString(),
            CheckoutUrl = transaction.CheckoutUrl,
            PaymentDate = transaction.PaymentDate,
            PaymentChannel = transaction.PaymentChannel,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
                