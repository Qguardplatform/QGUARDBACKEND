using examportal.Api.ServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.Enums
{
    

    public enum ExportFormatEnum
    {
        //[EnumText("PDF")]
        //PDF,

        [EnumText("CSV")]
        CSV,

        [EnumText("EXCEL")]
        EXCEL
    }
}
