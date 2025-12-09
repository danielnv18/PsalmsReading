using PsalmsReading.Domain.Entities;

namespace PsalmsReading.Application.Interfaces;

public interface IPsalmRepository
{
    Task<IReadOnlyList<Psalm>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Psalm?> GetByIdAsync(int psalmId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default);
    Task ReplaceAllAsync(IEnumerable<Psalm> psalms, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
