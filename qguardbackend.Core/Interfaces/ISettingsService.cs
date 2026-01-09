using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<CustomResult<CreateOtherSettingModel>> CreateSettings(CreateOtherSettingModel model, string createdBy);
        Task<CustomResult<List<OtherSettingModel>>> UpdateSettings(string name, List<OtherSettingModel> model, string createdBy);
        Task<CustomResult<string>> DeleteSettings(long id, string createdBy);
        Task<CustomResult<IEnumerable<OtherSettingsModel>>> GetAllSettings();
        Task<CustomResult<IEnumerable<OtherSettingsModel>>> GetSettingsByName(string name);
        Task<CustomResult<string>> GetConfigurationSettings(string name, string condition);
        Task<Dictionary<string, string>> GetConfigurationSettings(string name); 
        Task SeedSettings();
    }
}