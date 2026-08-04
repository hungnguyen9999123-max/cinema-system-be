namespace CinemaSystem.Common.DTOs.Responses;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Success(T? data, string message = "Success") => new()
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

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T? data, string message = "Success") => ApiResponse<T>.Success(data, message);
    public static ApiResponse<T> Fail<T>(string message) => ApiResponse<T>.Fail(message);
    public static ApiResponse<T> Fail<T>(List<string> errors, string message) => ApiResponse<T>.Fail(errors, message);
}
