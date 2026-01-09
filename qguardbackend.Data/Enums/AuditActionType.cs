using examportal.Api.ServiceExtensions;

namespace qguardbackend.Data.Enums
{
    public enum AuditActionType
    {
        [EnumText("Create")]
        Create,
        [EnumText("Edit")]
        Edit,
        [EnumText("Delete")]
        Delete
    }
}
