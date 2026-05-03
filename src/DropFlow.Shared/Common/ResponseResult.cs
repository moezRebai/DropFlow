namespace DropFlow.Shared.Common;

public class ResponseResult
{
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public static ResponseResult Success(string? message = null) => new() { Succeeded = true, Message = message};
    public static ResponseResult Failure(string error) => new() { Succeeded = false, Errors = [error] };
    public static ResponseResult Failure(List<string> errors) => new() { Succeeded = false, Errors = errors };
}

public class ResponseResult<T> : ResponseResult
{
    public T? Data { get; set; }
    public static ResponseResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public new static ResponseResult<T> Failure(string error) => new() { Succeeded = false, Errors = [error] };
    public new static ResponseResult<T> Failure(List<string> errors) => new() { Succeeded = false, Errors = errors };
}