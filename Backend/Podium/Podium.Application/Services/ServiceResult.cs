namespace Podium.Application.Services;

/// <summary>
/// Generic service result for operation outcomes
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public ServiceResultType ResultType { get; set; }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            ResultType = ServiceResultType.Success
        };
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.Failure
        };
    }

    public static ServiceResult<T> Forbidden(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.Forbidden
        };
    }

    public static ServiceResult<T> NotFound(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ResultType = ServiceResultType.NotFound
        };
    }
}
