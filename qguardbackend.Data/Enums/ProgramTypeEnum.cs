using examportal.Api.ServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.Enums
{
    public enum ProgramTypeEnum
    {
        [EnumText("DOCTORATE")]
        DOCTORATE,

        [EnumText("UNDER_GRADUATE")]

        UNDER_GRADUATE,

        [EnumText("POST_GRADUATE")]
        POST_GRADUATE,
    }


}
