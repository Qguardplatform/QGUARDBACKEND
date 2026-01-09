using qguardbackend.Data.Constants;

namespace qguardbackend.Data.DTOs.Results;

public sealed record CustomError
{
    public List<string> Messages { get; set; }

    public CustomError()
    {

    }

    public CustomError(List<string> messages)
    {
        Messages = messages;
    }

    public CustomError(string message)
    {
        Messages = new List<string> { message };
    }

    public static readonly CustomError None = null!;
    public static CustomError EmailError => new CustomError(ResponseMessages.EmailErrorMessage);
    public static CustomError OperationError => new CustomError(ResponseMessages.FailureMessage);
    public static CustomError NotificationTypeError => new CustomError(ResponseMessages.EmailErrorMessage);
    public static CustomError AccountAlreadyExistsError => new CustomError(ResponseMessages.AccountAlreadyExistMessage);
    public static CustomError SystemExceptionError => new CustomError(ResponseMessages.SystemExceptionErrorMessage);
    public static CustomError UserRoleNotFoundError => new CustomError(ResponseMessages.UserRoleNotFoundMessage);
    public static CustomError AccountNotFoundError => new CustomError(ResponseMessages.AccountNotFoundErrorMessage);
    public static CustomError DisableAccountError => new CustomError(ResponseMessages.DisableAccountErrorMessage);
    public static CustomError EnableAccountError => new CustomError(ResponseMessages.EnableAccountErrorMessage);
    public static CustomError ParameterInputNotProvided => new CustomError(ResponseMessages.ParameterInputNotProvided);
    public static CustomError AccountUpdateError => new CustomError(ResponseMessages.AccountUpdateErrorMessage);
    public static CustomError UnableToRetrieveUserProfile => new CustomError(ResponseMessages.UnableToRetrieveUserProfile);
    public static CustomError HttpFailureResponse => new CustomError(ResponseMessages.HttpFailureResponseMessage);
    public static CustomError RecordNotFound => new CustomError(ResponseMessages.NoRecordFound);
    public static CustomError TenantNotFound => new CustomError(ResponseMessages.NoRecordFound);
    public static CustomError UnableToGetToken => new CustomError(ResponseMessages.UnableToGetToken);
    public static CustomError UnableToProfileUser => new CustomError(ResponseMessages.UnableToProfileUser);
    public static CustomError InvalidOTP => new CustomError(ResponseMessages.InvalidOTP);
    public static CustomError RequiresPasswordChange => new CustomError(ResponseMessages.RequiresPasswordChange);
    public static CustomError InvalidUsernameOrPassword => new CustomError(ResponseMessages.InvalidUsernameOrPassword);
    public static CustomError CourseNotExisting => new CustomError(ResponseMessages.CourseNotExisting);
    public static CustomError CourseAlreadyMappedToAnExam => new CustomError(ResponseMessages.CourseAlreadyMappedToAnExam);
    public static CustomError UserClaimsError => new CustomError(ResponseMessages.UserClaimsError);
    public static CustomError InvalidTenant => new CustomError(ResponseMessages.InvalidTenant);
    public static CustomError QuestionNoMappedToExamSchedule => new CustomError(ResponseMessages.QuestionNoMappedToExamSchedule);
    public static CustomError QuestionHasBeenAttemptedAlready => new CustomError(ResponseMessages.QuestionHasBeenAttemptedAlready);
    public static CustomError ExamStartTimeIsPastOrOngoing => new CustomError(ResponseMessages.ExamStartTimeIsPastOrOngoing);
    public static CustomError YourRoleIsNotAuthorisedforThisAction => new CustomError(ResponseMessages.YourRoleIsNotAuthorisedforThisAction);
}