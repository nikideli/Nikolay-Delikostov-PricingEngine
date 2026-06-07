using Microsoft.AspNetCore.Mvc;
using PricingEngine.Domain.Strategies;

namespace PricingEngine.Api.Admin;

/// <summary>
/// Internal / admin endpoint that lists every product code that has a registered
/// pricing strategy. Use this to discover which products can accept quote requests
/// and which config keys need to be present.
///
/// Protected by X-Admin-Key header. Not intended for end-user consumption.
/// </summary>
[ApiController]
[Route("api/admin/products")]
[ApiExplorerSettings(GroupName = "admin")]
[ServiceFilter(typeof(AdminApiKeyFilter))]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IPricingStrategyFactory _factory;

    public AdminProductsController(IPricingStrategyFactory factory)
        => _factory = factory;

    /// <summary>Returns all registered product codes in alphabetical order.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetProducts()
    {
        var codes = _factory.GetAllProductCodes();
        return Ok(new ProductListResponse(codes));
    }

    public sealed record ProductListResponse(IReadOnlyList<string> Products);
}
