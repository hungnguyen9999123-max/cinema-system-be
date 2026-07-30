namespace CinemaSystem.Common.DTOs.Responses;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T? data, string message) => new()
    {
        IsSuccess = true,
        Message = message,
        Data = data,
        Errors = null
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        IsSuccess = false,
        Message = message,
        Data = default,
        Errors = null
    };

    public static ApiResponse<T> Fail(List<string> errors, string message) => new()
    {
        IsSuccess = false,
        Message = message,
        Data = default,
        Errors = errors
    };
}
