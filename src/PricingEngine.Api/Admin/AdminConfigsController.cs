using Microsoft.AspNetCore.Mvc;
using PricingEngine.Application.Configs;

namespace PricingEngine.Api.Admin;

/// <summary>
/// Internal / admin endpoints for reading and updating product configuration values
/// (tariff rates, fees, tax rates, etc.) without a code change or redeployment.
///
/// Protected by X-Admin-Key header. Not intended for end-user consumption.
/// </summary>
[ApiController]
[Route("api/admin/configs")]
[ApiExplorerSettings(GroupName = "admin")]
[ServiceFilter(typeof(AdminApiKeyFilter))]
public sealed class AdminConfigsController : ControllerBase
{
    private readonly IProductConfigService _configService;

    public AdminConfigsController(IProductConfigService configService)
        => _configService = configService;

    /// <summary>Returns all currently active config entries for a product.</summary>
    [HttpGet("{productCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConfigs(string productCode, CancellationToken ct)
    {
        var configs = await _configService.GetActiveConfigsAsync(productCode, ct);
        return Ok(configs);
    }

    /// <summary>
    /// Creates or updates a single config key for a product.
    /// If the key already exists its value is updated in place.
    /// If the key does not exist a new row is created, effective immediately.
    /// </summary>
    [HttpPut("{productCode}/{key}")]
    [ProducesResponseType(typeof(ProductConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpsertConfig(
        string productCode,
        string key,
        [FromBody] UpsertConfigRequest request,
        CancellationToken ct)
    {
        var result = await _configService.UpsertAsync(productCode, key, request, ct);
        return Ok(result);
    }
}
