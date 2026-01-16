using qguardbackend.Api.ServiceExtensions;

namespace qguardbackend.Data.Enums
{
    public enum SSORequired
    {
        [EnumText("False")]
        False= 0,
        [EnumText("True")]
        True = 1,
    }
}
