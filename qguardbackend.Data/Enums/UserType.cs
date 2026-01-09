namespace qguardbackend.Data.Enums
{
    public enum UserType
    {
        Candidate,
        Tutor,
        Proctor,
        ExamOfficer,
        InstitutionAdmin
    }

    public enum ResultStatus
    {
        Published,
        Unpublished
    }

    public enum FilterRange
    {
        Unspecified = 0,
        Today,
        Last7Days,
        ThisMonth,
        LastMonth,
        ThisYear,
        Custom
    }

    public enum RoleType
    {
        PROCTOR = 0,
        EXAMINER,
        INSTITUTIONADMIN,
        CANDIDATE
    }
}