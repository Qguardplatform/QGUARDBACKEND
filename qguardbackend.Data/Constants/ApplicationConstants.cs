namespace qguardbackend.Data.Constants;

public class ApplicationConstants
{
    public const string AppSettingsKey = "AppSettings";

    public const string AllowedCORSOrigins = "AppSettings:AllowedCORSOrigins";

    public const string SUCCESS_CODE = "00";

    public const string STANDARD_DATE_TIME_FORMAT = "dd/MM/yyyy hh:mm tt";

    public const string SERVER_DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:sszzz";

    public const string APPLICATION_CONTENT_TYPE_JSON = "application/json";
}
public static class ApprovalStatuses
{
    public static readonly string Pending = "PENDING";
    public static readonly string Approved = "APPROVED";
    public static readonly string Rejected = "REJECTED";
}

public static class UserStatus
{
    public static readonly string Deleted = "DELETED";
    public static readonly string Active = "ACTIVE";
    public static readonly string Blocked = "BLOCKED";
}