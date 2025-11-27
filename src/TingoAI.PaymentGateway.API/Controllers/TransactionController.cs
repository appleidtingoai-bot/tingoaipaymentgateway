using Microsoft.AspNetCore.Mvc;
using TingoAI.PaymentGateway.Application.DTOs;
using TingoAI.PaymentGateway.Application.Interfaces;

namespace TingoAI.PaymentGateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(ITransactionService transactionService, ILogger<TransactionController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Query transactions with flexible filters. Supports:
    /// - `transactionId` (returns single transaction)
    /// - `startDate` & `endDate` (date range filter)
    /// - `name` (customer first or last name contains)
    /// - pagination: `page` and `pageSize` (defaults to 10 when not provided)
    /// - `includeSummary` (when true, includes transaction summary for the date range)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionQueryResultDto), 200)]
    [ProducesResponseType(typeof(TransactionDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Query(
        [FromQuery] Guid? transactionId = null,
        [FromQuery] string? name = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeSummary = false,
        CancellationToken cancellationToken = default)
    {
        if (transactionId.HasValue)
        {
            _logger.LogInformation("Fetching transaction by id {TransactionId}", transactionId.Value);
            var tx = await _transactionService.GetTransactionByIdAsync(transactionId.Value, cancellationToken);
            if (tx == null) return NotFound(new { message = "Transaction not found" });
            return Ok(tx);
        }

        _logger.LogInformation("Querying transactions - Page: {Page}, PageSize: {PageSize}, Name: {Name}, Start: {Start}, End: {End}, IncludeSummary: {IncludeSummary}", page, pageSize, name, startDate, endDate, includeSummary);

        var result = await _transactionService.QueryTransactionsAsync(page, pageSize <= 0 ? 10 : pageSize, startDate, endDate, name, includeSummary, cancellationToken);
        return Ok(result);
    }
}
