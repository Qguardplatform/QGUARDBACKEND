
using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Setting : BaseEntity
    {
        public static Setting Create(String paramValue, String valueKey, String settingName)
        {
            return new Setting()
            {
                Name = settingName,
                Key = paramValue,
                Value = valueKey,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };
        }

        public string Name { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
