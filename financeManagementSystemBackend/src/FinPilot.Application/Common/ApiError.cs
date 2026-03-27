namespace FinPilot.Application.Common;

public sealed class ApiError
{
    public string Field { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Messages { get; init; } = Array.Empty<string>();
}
