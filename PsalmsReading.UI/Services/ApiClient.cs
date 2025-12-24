using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PsalmsReading.UI.Json;
using PsalmsReading.UI.Models;

namespace PsalmsReading.UI.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new DateOnlyJsonConverter());
    }

    public async Task<IReadOnlyList<PsalmDto>> GetPsalmsAsync(CancellationToken cancellationToken = default)
    {
        List<PsalmDto>? result = await _httpClient.GetFromJsonAsync<List<PsalmDto>>("psalms", _jsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<ReadingRecordDto>> GetReadingsAsync(
        DateOnly? from = default,
        DateOnly? to = default,
        CancellationToken cancellationToken = default)
    {
        var uri = "readings";
        List<string> queryParts = new();

        if (from.HasValue && to.HasValue)
        {
            queryParts.Add($"from={from:yyyy-MM-dd}");
            queryParts.Add($"to={to:yyyy-MM-dd}");
        }

        if (queryParts.Count > 0)
        {
            uri += $"?{string.Join("&", queryParts)}";
        }

        List<ReadingRecordDto>? result = await _httpClient.GetFromJsonAsync<List<ReadingRecordDto>>(uri, _jsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task CreateReadingAsync(CreateReadingRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("readings", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task UpdateReadingAsync(Guid id, UpdateReadingRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"readings/{id}", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task DeleteReadingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"readings/{id}", cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task<int> BulkDeleteReadingsAsync(IReadOnlyList<DateOnly> dates, CancellationToken cancellationToken = default)
    {
        var request = new BulkDeleteReadingsRequest(dates);
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("readings/bulk-delete", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);

        BulkDeleteReadingsResultDto? result = await response.Content.ReadFromJsonAsync<BulkDeleteReadingsResultDto>(_jsonOptions, cancellationToken);
        return result?.DeletedCount ?? 0;
    }

    public async Task<string> ExportReadingsIcsAsync(string range, int? year, CancellationToken cancellationToken = default)
    {
        var uri = BuildExportUri("readings/export/ics", range, year);
        HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> ExportReadingsJsonAsync(string range, int? year, CancellationToken cancellationToken = default)
    {
        var uri = BuildExportUri("readings/export/json", range, year);
        HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ReadingImportPreviewDto> PreviewReadingsImportAsync(string json, CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _httpClient.PostAsync("readings/import/preview", content, cancellationToken);
        await EnsureSuccess(response);

        ReadingImportPreviewDto? result = await response.Content.ReadFromJsonAsync<ReadingImportPreviewDto>(_jsonOptions, cancellationToken);
        return result ?? new ReadingImportPreviewDto(0, 0, []);
    }

    public async Task<ReadingImportResultDto> ImportReadingsAsync(
        string json,
        string mode,
        CancellationToken cancellationToken = default)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var uri = $"readings/import?mode={Uri.EscapeDataString(mode)}";
        HttpResponseMessage response = await _httpClient.PostAsync(uri, content, cancellationToken);
        await EnsureSuccess(response);

        ReadingImportResultDto? result = await response.Content.ReadFromJsonAsync<ReadingImportResultDto>(_jsonOptions, cancellationToken);
        return result ?? new ReadingImportResultDto(0, 0, 0);
    }

    public async Task<IReadOnlyList<PlannedReadingDto>> GenerateScheduleAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("schedule", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);

        List<PlannedReadingDto>? result = await response.Content.ReadFromJsonAsync<List<PlannedReadingDto>>(_jsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<PlannedReadingDto>> PreviewScheduleAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("schedule/preview", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);

        List<PlannedReadingDto>? result = await response.Content.ReadFromJsonAsync<List<PlannedReadingDto>>(_jsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task<string> GenerateScheduleIcsAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("schedule/ics", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<StatsDto> GetStatsAsync(string range, int? year = default, CancellationToken cancellationToken = default)
    {
        var uri = $"stats?range={Uri.EscapeDataString(range)}";
        if (year.HasValue)
        {
            uri += $"&year={year.Value}";
        }

        StatsDto? result = await _httpClient.GetFromJsonAsync<StatsDto>(uri, _jsonOptions, cancellationToken);
        return result ?? new StatsDto("all", null, null, 0, 0, 0, 0, 0, Array.Empty<TypeStatsDto>());
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var message = string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content;

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(message ?? "Duplicate reading.");
        }

        throw new InvalidOperationException($"Request failed: {(int)response.StatusCode} {message}");
    }

    private static string BuildExportUri(string baseUri, string range, int? year)
    {
        var uri = $"{baseUri}?range={Uri.EscapeDataString(range)}";
        if (year.HasValue)
        {
            uri += $"&year={year.Value}";
        }

        return uri;
    }
}
