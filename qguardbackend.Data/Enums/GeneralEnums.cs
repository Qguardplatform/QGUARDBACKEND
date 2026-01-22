using qguardbackend.Api.ServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.Enums
{
    internal class GeneralEnums
    {
    }

    public enum EntityEnum
    {
        [EnumText("REPORTLOG")]
        REPORTLOG,

        [EnumText("OTHERS")]
        OTHERS,

    }

    public enum FileTypeEnum
    {
        [EnumText("IMAGES")]
        IMAGES,

        [EnumText("DOCUMENT")]
        DOCUMENT,

        [EnumText("VIDEO")]
        VIDEO,

        [EnumText("AUDIO")]
        AUDIO,

    

        [EnumText("OTHERS")]
        OTHERS,

    }
}
