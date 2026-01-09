using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class RoleModel
    {
        public string RoleId { get; set; }
        public string Name { get; set; }
        public string NormalizedName { get; set; }
    }
    public class CreateRoleModel
    {
        [Required(ErrorMessage = "Role name is required")]
        public string Name { get; set; }
        public List<long> Priviledges { get; set; }
    }
    public class RoleSeedData
    {
        public string RoleName { get; set; }
    }

    public class AddPermissionDto
    {
        public string RoleId { get; set; }
        public List<long> Permissions { get; set; }
    }
    public class CreatePermissionModel
    {
        [Required(ErrorMessage = "Module name is required!")]
        public string ModuleName { get; set; }
        [Required(ErrorMessage = "Action name is required!")]
        public string Action { get; set; }
    }
    public class ModulePermissionLoadModel
    {
        public string ModuleName { get; set; }
        public List<PermissionModel> Permissions { get; set; }
    }
    public class PermissionModel
    {
        public long PermissionId { get; set; }
        public string Action { get; set; }
    }
    public class RolePermissionLoadModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionListModel> Permissions { get; set; }
    }
    public class PermissionListModel
    {
        public long PermissionId { get; set; }
        public string Action { get; set; }
        public string ModuleName { get; set; }
    }
}
