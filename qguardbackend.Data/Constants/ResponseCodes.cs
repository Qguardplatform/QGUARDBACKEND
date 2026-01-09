namespace qguardbackend.Data.Constants;

public static class ResponseCodes
{
    public const string SuccessCode = "00";
    public const string EmailErrorCode = "01";
    public const string AlreadyExistErrorCode = "01";
    public const string NotFoundErrorCode = "04";
    public const string ModelValidationErrorCode = "05";
    public const string BadRequestErrorCode = "05";
    public const string SystemExceptionErrorCode = "99";
    public const string UnableToGetToken = "99";
    public const string OperationError = "99";
    public const string RoleNotFoundErrorCode = "99";
    public const string CandidateStatusChanged = "00";
    public const string UnableToProfileUser = "99";
    public const string InvalidOTP = "99";
    public const string RequiresPasswordChange = "01";
    public const string InvalidUserClaims = "99";
    public const string InvalidTenant = "99";    
    public const string YourRoleIsNotAuthorisedforThisAction = "99";    
    public const string InvalidUsernameOrPassword = "02";    
}