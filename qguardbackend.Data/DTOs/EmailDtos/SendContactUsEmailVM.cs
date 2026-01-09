using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.EmailDtos
{
    public class SendContactUsEmailVM
    {
        [Required(ErrorMessage = "ProspectEmailAddress is required")]
        public string ProspectEmailAddress { get; set; }

        [Required(ErrorMessage = "EmailContent is required")]
        public string EmailContent { get; set; }
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
    }
}
