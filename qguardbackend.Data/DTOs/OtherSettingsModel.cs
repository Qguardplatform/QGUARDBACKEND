using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class OtherSettingsModel
    {
        public string Name { get; set; }
        public List<OtherSettingListModel> settings { get; set; }
    }
    public class CreateOtherSettingModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
        public List<OtherSettingModel> Settings { get; set; }
    }
    public class OtherSettingModel
    {
        [Required(ErrorMessage = "A parameter is required")]
        public string Parameter { get; set; }
        [Required(ErrorMessage = "A value is required")]
        public string Value { get; set; }
    }
    public class OtherSettingListModel
    {
        public string Id { get; set; }
        public string Parameter { get; set; }
        public string Value { get; set; }
    }

    public class UpdateSettingModel
    {
        public string Name { get; set; }
        public List<OtherSettingModel> Settings { get; set; }
    }
}
