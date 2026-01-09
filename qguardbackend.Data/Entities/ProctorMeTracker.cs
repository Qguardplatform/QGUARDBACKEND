using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class ProctorMeTracker : BaseEntity
    {
        public string ActivityRequest { get; set; }
        public string ActivityDescription { get; set; }
        public string ActivityResponse { get; set; }
        public string ApplicationState { get; set; }
        public string ActivityResponseDesc { get; set; }
    }
}
