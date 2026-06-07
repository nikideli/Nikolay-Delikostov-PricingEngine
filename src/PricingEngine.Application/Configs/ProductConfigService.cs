using PricingEngine.Application.Interfaces;
using PricingEngine.Domain.Entities;

namespace PricingEngine.Application.Configs;

public sealed class ProductConfigService : IProductConfigService
{
    private readonly IProductConfigRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public ProductConfigService(IProductConfigRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductConfigDto>> GetActiveConfigsAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var configs = await _repo.GetByProductCodeAsync(productCode, cancellationToken);
        return configs.Select(ToDto).ToList();
    }

    public async Task<ProductConfigDto> UpsertAsync(
        string productCode,
        string configKey,
        UpsertConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repo.FindAsync(productCode, configKey, cancellationToken);

        if (existing is not null)
        {
            // Update the tracked entity inside a retriable transaction
            await _unitOfWork.ExecuteInTransactionAsync(() =>
            {
                existing.UpdateValue(request.Value);
                return Task.CompletedTask;
            }, cancellationToken);

            return ToDto(existing);
        }
        else
        {
            var config = ProductConfig.Create(productCode, configKey, request.Value);

            await _unitOfWork.ExecuteInTransactionAsync(() =>
            {
                _repo.Add(config);
                return Task.CompletedTask;
            }, cancellationToken);

            return ToDto(config);
        }
    }

    private static ProductConfigDto ToDto(ProductConfig c) =>
        new(c.Id, c.ProductCode, c.ConfigKey, c.ConfigValue, c.EffectiveFrom, c.EffectiveTo);
}
