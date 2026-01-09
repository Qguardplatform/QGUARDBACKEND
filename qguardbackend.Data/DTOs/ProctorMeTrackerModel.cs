using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs
{
    public class ProctorMeTrackerModel
    {
        public long Id { get; set; }
        public string ActivityRequest { get; set; }
        public string ActivityDescription { get; set; }
        public string ActivityResponse { get; set; }
        public string ApplicationState { get; set; }
        public string ActivityResponseDesc { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProctorMeTrackerReadModel
    {
        public long Id { get; set; }
        public string TransactionReference { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
