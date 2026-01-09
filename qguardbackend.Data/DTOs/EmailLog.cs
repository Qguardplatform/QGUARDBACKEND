using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class EmailLog //BaseEntity//: Entity<Guid>
    {
        public EmailLog()
        {
            Retires = 0;
            IsSent = false;
            BCC = string.Empty;
            CC = string.Empty;
            CorrellationId = "None";
            Template_Code = string.Empty;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? UpdatedAt { get; set; }


        [Required]
        [StringLength(1000)]
        public string Sender { get; set; }
        [Required]
        [StringLength(1000)]
        public string Receiver { get; set; }

        [StringLength(1000)]
        public string CC { get; set; }

        public string BCC { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string MessageBody { get; set; }

        public int Retires { get; set; }
        public bool IsSent { get; set; }

        public DateTimeOffset? DateSent { get; set; }

        public DateTimeOffset DateToSend { get; set; }
        [StringLength(1000)]
        public string CorrellationId { get; set; }
        [StringLength(128)]
        public string Template_Code { get; set; }

    }
}
