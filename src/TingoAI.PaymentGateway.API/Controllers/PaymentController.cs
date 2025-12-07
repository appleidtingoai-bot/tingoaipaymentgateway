using Microsoft.AspNetCore.Mvc;
using TingoAI.PaymentGateway.Application.DTOs;
using TingoAI.PaymentGateway.Application.Interfaces;

namespace TingoAI.PaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(TingoAI.PaymentGateway.API.Filters.BasicAuthFilter))]
[TingoAI.PaymentGateway.API.Filters.RequireBasicAuth]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate a new payment and generate checkout URL
    /// </summary>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<PaymentResponse>> InitiatePayment([FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Initiating payment for {Amount} {Currency}", request.Amount, request.Currency);

        var response = await _paymentService.InitiatePaymentAsync(request, cancellationToken);

        if (response.Success)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    /// <summary>
    /// Verify transaction status by reference
    /// </summary>
    [HttpGet("verify/{reference}")]
    [ProducesResponseType(typeof(TransactionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TransactionDto>> VerifyTransaction(string reference, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying transaction {Reference}", reference);

        var transaction = await _paymentService.VerifyTransactionAsync(reference, cancellationToken);

        if (transaction == null)
        {
            return NotFound(new { message = "Transaction not found" });
        }

        return Ok(transaction);
    }

    /// <summary>
    /// Handle GlobalPay webhook notifications
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> HandleWebhook([FromBody] WebhookPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received webhook notification");

        try
        {
            await _paymentService.ProcessWebhookAsync(payload.EncryptedData, cancellationToken);
            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { message = "Error processing webhook" });
        }
    }
}

public class WebhookPayload
{
    public string EncryptedData { get; set; } = string.Empty;
}
