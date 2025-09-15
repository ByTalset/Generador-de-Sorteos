namespace ConnectionManager;

public class Result<T>
{
    public T Value { get; set; }
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
    public int CodeError { get; set; }

    public Result(T value, bool success, string messageError, int codeError) 
    {
        Value = value;
        IsSuccess = success;
        Error = messageError;
        CodeError = codeError;
    }

    public static Result<T> Success(T value) => new Result<T>(value, true, string.Empty, default);
    public static Result<T> Failure(string messageError, int codeError = default) => new Result<T>(default!, false, messageError, codeError);

}
