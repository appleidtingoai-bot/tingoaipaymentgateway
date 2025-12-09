using System.Net.Http.Json;
using System.Text.Json;
using TingoAI.PaymentGateway.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TingoAI.PaymentGateway.Infrastructure.ExternalServices;

public class GlobalPayClient : IGlobalPayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GlobalPayClient> _logger;

    public GlobalPayClient(HttpClient httpClient, ILogger<GlobalPayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GlobalPayPaymentResponse> GeneratePaymentLinkAsync(GlobalPayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Log the final URL being called (helps diagnose base URL / path issues)
            if (_httpClient.BaseAddress != null)
            {
                var called = new Uri(_httpClient.BaseAddress, "generate-payment-link");
                _logger?.LogInformation("GlobalPay - POSTing to {Url}", called.ToString());
            }

            var response = await _httpClient.PostAsJsonAsync("generate-payment-link", request, cancellationToken);

            // Always read the raw body so we can log it and attempt a resilient parse
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("GlobalPay GeneratePaymentLink returned {Status} with body: {Body}", response.StatusCode, body);
                return new GlobalPayPaymentResponse
                {
                    Data = new GlobalPayData { IsSuccessful = false, Error = $"Status: {(int)response.StatusCode} {response.ReasonPhrase}; Body: {body}" }
                };
            }

            _logger?.LogInformation("GlobalPay GeneratePaymentLink success response body: {Body}", body);

            // Try the high-level JSON deserialization first, but normalize with explicit parsing
            try
            {
                GlobalPayPaymentResponse? result = null;
                try
                {
                    result = JsonSerializer.Deserialize<GlobalPayPaymentResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception jex)
                {
                    _logger?.LogWarning(jex, "Failed to deserialize GlobalPay response into GlobalPayPaymentResponse. Will attempt manual merge parsing.");
                }

                // Parse raw JSON to ensure we pick up fields that may be at root or inside `data`
                try
                {
                    using var doc = JsonDocument.Parse(body);

                    var gpData = new GlobalPayData();

                    // Prefer values inside `data` object
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.ValueKind == JsonValueKind.Object)
                        {
                            if (dataEl.TryGetProperty("checkoutUrl", out var cu)) gpData.CheckoutUrl = cu.GetString();
                            if (dataEl.TryGetProperty("accessCode", out var ac)) gpData.AccessCode = ac.GetString();
                            // Some responses use `transactionReference` or `ref`
                            if (dataEl.TryGetProperty("transactionReference", out var tr)) gpData.Ref = tr.GetString();
                            if (dataEl.TryGetProperty("ref", out var rf)) gpData.Ref ??= rf.GetString();
                            if (dataEl.TryGetProperty("responseCode", out var rc)) gpData.ResponseCode = rc.GetString();
                            if (dataEl.TryGetProperty("successMessage", out var sm)) gpData.SuccessMessage = sm.GetString();
                            if (dataEl.TryGetProperty("isSuccessful", out var isSucc)) gpData.IsSuccessful = isSucc.GetBoolean();
                        }
                    }

                    // If fields missing inside data, try root-level properties
                    if (gpData.CheckoutUrl == null && doc.RootElement.TryGetProperty("checkoutUrl", out var rootCu)) gpData.CheckoutUrl = rootCu.GetString();
                    if (gpData.AccessCode == null && doc.RootElement.TryGetProperty("accessCode", out var rootAc)) gpData.AccessCode = rootAc.GetString();
                    if (gpData.Ref == null)
                    {
                        if (doc.RootElement.TryGetProperty("transactionReference", out var rootTr)) gpData.Ref = rootTr.GetString();
                        if (gpData.Ref == null && doc.RootElement.TryGetProperty("ref", out var rootRf)) gpData.Ref = rootRf.GetString();
                    }
                    if (!gpData.IsSuccessful && doc.RootElement.TryGetProperty("isSuccessful", out var rootIs)) gpData.IsSuccessful = rootIs.GetBoolean();
                    if (gpData.ResponseCode == null && doc.RootElement.TryGetProperty("responseCode", out var rootRc)) gpData.ResponseCode = rootRc.GetString();
                    if (gpData.SuccessMessage == null && doc.RootElement.TryGetProperty("successMessage", out var rootSm)) gpData.SuccessMessage = rootSm.GetString();

                    // If the high-level deserialization produced something, merge non-null values
                    if (result?.Data != null)
                    {
                        var merged = result.Data;
                        merged.CheckoutUrl ??= gpData.CheckoutUrl;
                        merged.AccessCode ??= gpData.AccessCode;
                        merged.Ref ??= gpData.Ref;
                        merged.IsSuccessful = merged.IsSuccessful || gpData.IsSuccessful;
                        merged.ResponseCode ??= gpData.ResponseCode;
                        merged.SuccessMessage ??= gpData.SuccessMessage;
                        return new GlobalPayPaymentResponse { Data = merged };
                    }

                    // Otherwise return what's been parsed from JSON
                    return new GlobalPayPaymentResponse { Data = gpData };
                }
                catch (Exception ex2)
                {
                    _logger?.LogWarning(ex2, "Fallback parsing of GlobalPay response failed.");
                }
            }
            catch (Exception exOuter)
            {
                _logger?.LogWarning(exOuter, "Unexpected error when parsing GlobalPay response body.");
            }

            // As a last resort, return an informative error so calling code can surface it
            return new GlobalPayPaymentResponse
            {
                Data = new GlobalPayData { IsSuccessful = false, Error = "Unable to parse GlobalPay response body." }
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calling GlobalPay GeneratePaymentLink");
            return new GlobalPayPaymentResponse
            {
                Data = new GlobalPayData
                {
                    IsSuccessful = false,
                    Error = ex.Message
                }
            };
        }
    }

    public async Task<GlobalPayTransactionQueryResponse> QueryTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"query-single-transaction/{reference}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogWarning("GlobalPay QueryTransactionByReference returned {Status} with body: {Body}", response.StatusCode, body);
                return new GlobalPayTransactionQueryResponse();
            }

            var result = await response.Content.ReadFromJsonAsync<GlobalPayTransactionQueryResponse>(cancellationToken);
            return result ?? new GlobalPayTransactionQueryResponse();
        }
        catch
        {
            return new GlobalPayTransactionQueryResponse();
        }
    }

    public async Task<GlobalPayTransactionQueryResponse> QueryTransactionByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"query-single-transaction-by-merchant-reference/{merchantReference}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogWarning("GlobalPay QueryTransactionByMerchantReference returned {Status} with body: {Body}", response.StatusCode, body);
                return new GlobalPayTransactionQueryResponse();
            }

            var result = await response.Content.ReadFromJsonAsync<GlobalPayTransactionQueryResponse>(cancellationToken);
            return result ?? new GlobalPayTransactionQueryResponse();
        }
        catch
        {
            return new GlobalPayTransactionQueryResponse();
        }
    }
}
