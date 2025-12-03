namespace PsalmsReading.Application.Models;

public sealed record PsalmDto(int Id, string Title, int TotalVerses, string? Type, IReadOnlyList<string> Epigraphs, IReadOnlyList<string> Themes);
