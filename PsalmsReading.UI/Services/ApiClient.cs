using System.Net.Http.Json;
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
        var result = await _httpClient.GetFromJsonAsync<List<PsalmDto>>("psalms", _jsonOptions, cancellationToken);
        return result ?? new List<PsalmDto>();
    }

    public async Task<IReadOnlyList<ReadingRecordDto>> GetReadingsAsync(DateOnly? from = default, DateOnly? to = default, CancellationToken cancellationToken = default)
    {
        var uri = "readings";
        if (from.HasValue && to.HasValue)
        {
            uri += $"?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        }

        var result = await _httpClient.GetFromJsonAsync<List<ReadingRecordDto>>(uri, _jsonOptions, cancellationToken);
        return result ?? new List<ReadingRecordDto>();
    }

    public async Task CreateReadingAsync(CreateReadingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("readings", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task UpdateReadingAsync(Guid id, UpdateReadingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"readings/{id}", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task DeleteReadingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"readings/{id}", cancellationToken);
        await EnsureSuccess(response);
    }

    public async Task<IReadOnlyList<PlannedReadingDto>> GenerateScheduleAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("schedule", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);

        var result = await response.Content.ReadFromJsonAsync<List<PlannedReadingDto>>(_jsonOptions, cancellationToken);
        return result ?? new List<PlannedReadingDto>();
    }

    public async Task<IReadOnlyList<PlannedReadingDto>> PreviewScheduleAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("schedule/preview", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);

        var result = await response.Content.ReadFromJsonAsync<List<PlannedReadingDto>>(_jsonOptions, cancellationToken);
        return result ?? new List<PlannedReadingDto>();
    }

    public async Task<string> GenerateScheduleIcsAsync(ScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("schedule/ics", request, _jsonOptions, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadAsStringAsync(cancellationToken);
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
}
