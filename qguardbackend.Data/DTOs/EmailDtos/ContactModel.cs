using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace qguardbackend.Data.DTOs.EmailDtos
{
    public class ContactModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
        public string Company { get; set; }
        [Required(ErrorMessage = "Phone Numebr is required")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }
    }

    public class ContactResponseModel
    {
        public string Message { get; set; }
    }

    public class MessageModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string UserId { get; set; }
        public string Url { get; set; }
        public string UserType { get; set; }
        public string SchoolLogo { get; set; }
        public string SchoolName { get; set; }
    }
}
