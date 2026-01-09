using qguardbackend.Core.Autofac;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;

namespace qguardbackend.Core.Interfaces
{
    public interface IRoleService //: IAutoDependencyCore
    {
        Task<CustomResult<RoleModel>> CreateRole(CreateRoleModel role);
        Task<CustomResult<string>> DeleteRole(string roleId);
        Task<CustomResult<RoleModel>> GetRoleById(string roleId);
        Task<CustomResult<RoleModel>> GetRoleByName(string roleName);
        Task<CustomResult<RoleModel>> UpdateRole(string id, CreateRoleModel model);
        Task<CustomResult<RoleModel[]>> GetRoles();
        Task SeedRoles();
    }
}
