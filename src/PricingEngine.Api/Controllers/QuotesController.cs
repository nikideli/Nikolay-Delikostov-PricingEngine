using Microsoft.AspNetCore.Mvc;
using PricingEngine.Application.Quotes;

namespace PricingEngine.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[Produces("application/json")]
public sealed class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public QuotesController(IQuoteService quoteService) => _quoteService = quoteService;

    /// <summary>
    /// Calculate and persist a new insurance quote.
    /// </summary>
    /// <remarks>
    /// The <c>inputs</c> object is product-specific. For HOME_BASIC, include:
    /// <code>{ "insuredSum": 250000 }</code>
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(CreateQuoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateQuote(
        [FromBody] Models.CreateQuoteHttpRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateQuoteRequest(request.ProductCode, request.Inputs);
        var response = await _quoteService.CreateQuoteAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetQuote),
            new { id = response.QuoteId },
            response);
    }

    /// <summary>
    /// Retrieve a previously calculated quote by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CreateQuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuote(Guid id, CancellationToken cancellationToken)
    {
        var response = await _quoteService.GetQuoteAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
