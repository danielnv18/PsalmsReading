namespace PsalmsReading.Application.Interfaces;

public interface IPsalmImportService
{
    Task ImportIfEmptyAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
