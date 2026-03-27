namespace FinPilot.Application.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<ApiError>? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string message = "Operation completed") => new()
    {
        Success = true,
        Data = data,
        Message = message,
        Errors = null
    };

    public static ApiResponse<T> Fail(string message, IReadOnlyCollection<ApiError>? errors = null) => new()
    {
        Success = false,
        Data = default,
        Message = message,
        Errors = errors
    };
}
