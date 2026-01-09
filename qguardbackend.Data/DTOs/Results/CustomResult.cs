using qguardbackend.Data.Constants;

namespace qguardbackend.Data.DTOs.Results;

public class CustomResult<T> 
{
    private readonly T _value;
    public bool IsSuccess { get; set; }
    //public bool IsSuccess => Error == CustomError.None;
    public string ResponseCode { get; set; }
    public string Message { get; set; }
    public DateTime TimeStamp => DateTime.Now;
    public CustomError Error { get; set; }
    public List<string> ValidationErrors { get; set; }
    public T ErrorList { get; set; }

    public T Data
    {
        get
        {
            if (!IsSuccess)
                return default!;

            return _value!;
        }

        private init => _value = value;
    }

    private CustomResult(T value)
    {
        Data = value;
        ResponseCode = ResponseCodes.SuccessCode;
        Message = ResponseMessages.SuccessMessage;
        Error = CustomError.None;
        IsSuccess = true;
    }

    private CustomResult(T value, string description, List<string> validationErrors = null)
    {
        Data = value;
        ResponseCode = ResponseCodes.SuccessCode;
        Message = description;
        Error = CustomError.None;
        ValidationErrors = validationErrors;
        IsSuccess = true;
    }

    private CustomResult(T value, string description ,string code, List<string> validationErrors = null)
    {
        Data = value;
        ResponseCode = code;
        Message = description;
        Error = CustomError.None;
        ValidationErrors = validationErrors;
        IsSuccess = code == ResponseCodes.SuccessCode ? true : false;
    }
    private CustomResult(T value, string description, string code, T errorList, List<string> validationErrors = null)
    {
        Data = value;
        ResponseCode = code;
        Message = description;
        Error = CustomError.None;
        ValidationErrors = validationErrors;
        ErrorList = errorList;
        IsSuccess = code == ResponseCodes.SuccessCode ? true : false;
    }

    private CustomResult(CustomError error, string code)
    {
        if (error == CustomError.None)
            throw new ArgumentException("Invalid error");

        ResponseCode = code;
        Message = ResponseMessages.FailureMessage;
        Error = error;
        IsSuccess = code == ResponseCodes.SuccessCode ? true : false;
    }
    private CustomResult(string code, List<string> validationErrors)
    {
        ResponseCode = code;
        Message = ResponseMessages.FailureMessage;
        Error = CustomError.None;
        ValidationErrors = validationErrors;
        IsSuccess = code == ResponseCodes.SuccessCode ? true : false;
    }

    private CustomResult(string description, string code)
    {
        ResponseCode = code;
        Message = description;
        Error = CustomError.None;
        IsSuccess = code == ResponseCodes.SuccessCode ? true : false;
    }

    public static CustomResult<T> Success(T value) => new CustomResult<T>(value);

    public static CustomResult<T> Success(T value, string description, List<string> validationErrors = null) => new CustomResult<T>(value, description, validationErrors);

    public static CustomResult<T> Failure(CustomError error, string code) => new CustomResult<T>(error, code);
    public static CustomResult<T> ErrorOccured(string description, string code) => new CustomResult<T>(description, code);
    public static CustomResult<T> ErrorOccured(T value,string description, string code) => new CustomResult<T>(value,description, code);
    public static CustomResult<T> Errors(T value,string description, string code, T erroList) => new CustomResult<T>(value,description, code, erroList);

    public static CustomResult<T> ValidationFailure(string code, List<string> validationErrors)
        => new CustomResult<T>(code, validationErrors);
}