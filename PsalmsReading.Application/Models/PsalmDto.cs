namespace PsalmsReading.Application.Models;

public sealed record PsalmDto(int Id, string Title, int TotalVerses, string? Type, string? Epigraphs, IReadOnlyList<string> Themes);
