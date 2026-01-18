using qguardbackend.Api.ServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.Enums
{


    public enum RolesEnum
    {
        [EnumText("SYSTEMADMIN")]
        SYSTEMADMIN,

        [EnumText("ENDUSER")]

        ENDUSER//,

        //[EnumText("CANDIDATE")]
        //CANDIDATE,

        //[EnumText("EXAMINER")]
        //EXAMINER,

        //[EnumText("TUTOR")]
        //TUTOR,

      
    }
}
