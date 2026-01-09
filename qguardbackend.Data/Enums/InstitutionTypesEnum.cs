using examportal.Api.ServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.Enums
{


    public enum InstitutionTypesEnum
    {
        [EnumText("SCHOOL")]
        SCHOOL,

        [EnumText("CORPORATE")]

        CORPORATE,
    }
}
