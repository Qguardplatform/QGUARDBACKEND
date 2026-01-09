using examportal.Api.ServiceExtensions;

namespace qguardbackend.Data.Enums
{


    public enum ExamScheduleStatusEnum
    {
        [EnumText("DRAFT")]
        DRAFT,

        [EnumText("PUBLISHED")]

        PUBLISHED,
      
    }

    public enum ExamTimingStatusEnum
    {
        [EnumText("UPCOMING")]
        UPCOMING,

        [EnumText("ONGOING")]
        ONGOING,

        [EnumText("PAST")]
        PAST,
    }
}
