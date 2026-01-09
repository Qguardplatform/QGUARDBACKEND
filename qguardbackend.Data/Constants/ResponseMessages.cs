namespace qguardbackend.Data.Constants;

public static class ResponseMessages
{
    public static readonly string SuccessMessage = "Successful";
    public static readonly string FailureMessage = "Failed";
    public static readonly string EmailErrorMessage = "An error occurred sending email.";
    public static readonly string AccountAlreadyExistMessage = "Account already exist";
    public static readonly string NotificationTypeErrorMessage = "Unsupported notification type.";
    public static readonly string EmailSuccessMessage = "Email sent successfully";
    public static readonly string UserRoleNotFoundMessage = "User role not found";
    public static readonly string SystemExceptionErrorMessage = "Something went wrong while processing. Please try again later.";
    public static readonly string AccountNotFoundErrorMessage = "Account not found";
    public static readonly string DisableAccountErrorMessage = "An error occurred, cannot de-activate account.";
    public static readonly string EnableAccountErrorMessage = "An error occurred, cannot activate account.";
    public static readonly string ParameterInputNotProvided = "Parameter input not provided";
    public static readonly string AccountUpdateErrorMessage = "Account could not be updated";
    public static readonly string UnableToRetrieveUserProfile = "Unable to retrieve user profile";
    public static readonly string UnableToGetTokenFromFireBase = "Unable To Get Token From FireBase";
    public static readonly string HttpFailureResponseMessage = "An error occurred making request.";
    public static readonly string NoRecordFound = "No record found";
    public static readonly string UnableToGetToken = "Unable to get token";
    public static readonly string UserCreatedSuccessfully = "User created Successfully";
    public static readonly string UserUpdatedSuccessfully = "User updated Successfully";
    public static readonly string PasswordchangedSuccessfully = "Password changed Successfully";
    public static readonly string UserRoleAssignedSuccessfull = "user role assigned Successfully";
    public static readonly string PasswordResetSuccessfully = "password reset successfully";
    public static readonly string UserStatusChanged = "user status changed Successfully";
    public static readonly string CandidateDetailsNotFound = "Candidates details not found";
    public static readonly string UnableToProfileUser = "Unable To Profile User";
    public static readonly string OTPValidatedSuccessfully = "OTP Validated Successfully";
    public static readonly string OTPGeneratedAndSentSuccessfully = "OTP Generated and Sent Successfully";
    public static readonly string InvalidOTP = "Invalid OTP";
    public static readonly string RequiresPasswordChange = "Requires Password Change";
    public static readonly string CourseNotExisting = "Course not found";
    public static readonly string CourseAlreadyMappedToAnExam = "Course already mapped to an exam schedule";
    public static readonly string UserClaimsError = "User claim error";
    public static readonly string InvalidTenant = "Invalid Tenant is passed on this request";
    public static readonly string QuestionNoMappedToExamSchedule = "Question not mapped to exam schedule";
    public static readonly string QuestionHasBeenAttemptedAlready = "Question has been attempted already, so you cannot unmap it, instead you can edit it";
    public static readonly string ExamStartTimeIsPastOrOngoing = "Exam schedule is already past of ongoing";
    public static readonly string YourRoleIsNotAuthorisedforThisAction = "Your role is not authorised for this action";
    public static readonly string InvalidUsernameOrPassword = "Invalid email or password!";
}