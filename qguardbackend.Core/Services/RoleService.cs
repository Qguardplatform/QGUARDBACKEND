using AutoMapper;
using DocumentFormat.OpenXml.Wordprocessing;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using qguardbackend.Api.ServiceExtensions;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Model;
using Firebase.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;

namespace qguardbackend.Core.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleService> _logger;
        private readonly IUserManagementService _userManagementService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public RoleService(AppDbContext context,
                    RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
            IUserManagementService userManagementService, ILogger<RoleService> logger)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<RoleModel>> CreateRole(CreateRoleModel model)
        {
            var errorList = new List<string>();
            try
            {
                var exists = await _context.Roles.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());
                if (exists)
                {
                    return CustomResult<RoleModel>.ErrorOccured("Role name already exists", ResponseCodes.AlreadyExistErrorCode);
                }

                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    NormalizedName = model.Name.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                if (model.Priviledges != null && model.Priviledges.Count > 0)
                {
                    foreach (var item in model.Priviledges)
                    {
                        //get details of selected priviledges
                        var permission = await _context.Priviledges.FirstOrDefaultAsync(p => p.Id == item);
                        if (permission == null)
                        {
                            errorList.Add($"Invalid Permission Id - {item}");
                            continue;
                        }
                        var rolePermission = RolePriviledge.Create(role.Id, item);
                        _context.RolePriviledges.Add(rolePermission);
                    }
                }
                _context.Roles.Add(role);

                await _context.SaveChangesAsync();

                string error = errorList.Count > 0 ? JsonConvert.SerializeObject(errorList) : "Successful";
                var returnDto = await this.GetRoleById(role.Id);

                return CustomResult<RoleModel>.Success(returnDto.Data, "Role created successfully", errorList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<RoleModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> DeleteRole(string roleId)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id.ToLower() == roleId.ToLower());
                if (role is null)
                {
                    return CustomResult<string>.ErrorOccured("Role not found!", ResponseCodes.NotFoundErrorCode);
                }
                var check = await _context.RolePriviledges.Where(x => x.RoleId == roleId).ToListAsync();
                if (check.Any())
                {
                    return CustomResult<string>.ErrorOccured("Role cannot be deleted because it has already been mapped with role privilede!", ResponseCodes.BadRequestErrorCode);
                }
                _context.Remove(role);
                await _context.SaveChangesAsync();
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Role successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<RoleModel>> GetRoleById(string roleId)
        {
            try
            {
                var itemFromDb = await _context.Roles.FirstOrDefaultAsync(x => x.Id == roleId);
                if (itemFromDb == null)
                {
                    return CustomResult<RoleModel>.ErrorOccured("Role not found!", ResponseCodes.NotFoundErrorCode);
                }

                var role = new RoleModel
                {
                    RoleId = itemFromDb.Id,
                    Name = itemFromDb.Name,
                    NormalizedName = itemFromDb.NormalizedName
                };
                return CustomResult<RoleModel>.Success(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<RoleModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<RoleModel>> GetRoleByName(string roleName)
        {
            try
            {
                var itemFromDb = await _context.Roles.FirstOrDefaultAsync(x => x.Name.ToLower() == roleName.ToLower());
                if (itemFromDb == null)
                {
                    return CustomResult<RoleModel>.ErrorOccured("Role not found!", ResponseCodes.NotFoundErrorCode);
                }

                var role = new RoleModel
                {
                    RoleId = itemFromDb.Id,
                    Name = itemFromDb.Name,
                    NormalizedName = itemFromDb.NormalizedName
                };
                return CustomResult<RoleModel>.Success(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<RoleModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<RoleModel[]>> GetRoles()
        {
            try
            {
                //get the loggedin user details
                var loggedInUser = await _userManagementService.GetLoggedInUser();

                if (!loggedInUser.IsSuccess || loggedInUser.Data == null)
                    return CustomResult<RoleModel[]>.Failure(loggedInUser.Error, loggedInUser.ResponseCode);

                var userData = loggedInUser.Data;

                var itemFromDb = await _context.Roles.ToListAsync();

                //-----------------------
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<RoleModel[]>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                var getuser = await _userManager.FindByIdAsync(userData.Id.ToString());
                var userRoles = new List<string>();
                var roles = await _userManager.GetRolesAsync(getuser);
                userRoles.AddRange(roles);
                //var insTdetails = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == InsTId.Data);
                //if the request user is a system admin and the request tenant is not Master
                if (roles.Contains(RolesEnum.SYSTEMADMIN.GetEnumText()) /*&& insTdetails.Code.ToLower() != "master"*/)
                {
                    var getUserRoles = await _context.SystemAdminOtherTenantsRole
                   .Include(x => x.ApplicationRole)
                   .Where(x => x.UserId == userData.Id.ToString())
                   .Select(x => new SystemAdminOtherTenantsRoleResponse
                   {
                       RoleName = x.ApplicationRole.Name,
                       InstitutionId = x.InstitutionId,
                       UserId = x.UserId,
                       RoleId = x.RoleId
                   }).ToListAsync();
                    roles = getUserRoles.Select(x => x.RoleName.ToLower()).ToList();
                    userRoles.AddRange(roles);
                }
                userRoles = userRoles.Select(x => x.ToLower()).Distinct().ToList();

                //-----------------------

                //if (loggedInUserRoles.ToLower() != RolesEnum.SYSTEMADMIN.GetEnumText().ToLower())
                //if (!userRoles.Contains( RolesEnum.SYSTEMADMIN.GetEnumText().ToLower())
                //    )
                //{
                //    itemFromDb = itemFromDb.Where(r => r.Name.ToLower() != RolesEnum.SYSTEMADMIN.GetEnumText().ToLower()
                //    && r.Name.ToLower() != RolesEnum.CANDIDATE.GetEnumText().ToLower()
                //    ).ToList();
                //}

                if (itemFromDb.Count > 0)
                {
                    var result = itemFromDb.ToArray();
                    var role = result.Select(role => new RoleModel
                    {
                        Name = role.Name,
                        RoleId = role.Id,
                        NormalizedName = role.NormalizedName
                    }).ToArray();
                    return CustomResult<RoleModel[]>.Success(role);
                }
                return CustomResult<RoleModel[]>.ErrorOccured("No record found!", ResponseCodes.NotFoundErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<RoleModel[]>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedRoles()
        {
            var fullPath = System.IO.File.ReadAllText("Filter/SeedUtility/RoleData.json");

            List<RoleSeedData> roleVM = JsonConvert.DeserializeObject<List<RoleSeedData>>(fullPath);

            if (_context.Roles.Any())
            {
                return;
            }
            foreach (var vm in roleVM)
            {
                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = vm.RoleName,
                    NormalizedName = vm.RoleName.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                await _context.AddAsync(role);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<CustomResult<RoleModel>> UpdateRole(string id, CreateRoleModel model)
        {
            var errorList = new List<string>();
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id);
                if (role == null)
                {
                    return CustomResult<RoleModel>.ErrorOccured("Role not found!", ResponseCodes.NotFoundErrorCode);
                }

                role.Name = model.Name;
                role.NormalizedName = model.Name.ToUpper();

                if (model.Priviledges != null && model.Priviledges.Count > 0)
                {
                    var oldPermission = await _context.RolePriviledges.Where(x => x.RoleId == id).ToListAsync();
                    if (oldPermission.Count > 0)
                    {
                        await PerformErase(id);
                        foreach (var item in model.Priviledges)
                        {
                            //get details of selected priviledges
                            var permission = await _context.Priviledges.FirstOrDefaultAsync(p => p.Id == item);
                            if (permission == null)
                            {
                                errorList.Add($"Invalid Permission Id - {item}");
                                continue;
                            }
                            var rolePermission = RolePriviledge.Create(role.Id, item);
                            _context.RolePriviledges.Add(rolePermission);
                        }
                    }
                    else
                    {
                        foreach (var item in model.Priviledges)
                        {
                            //get details of selected priviledges
                            var permission = await _context.Priviledges.FirstOrDefaultAsync(p => p.Id == item);
                            if (permission == null)
                            {
                                errorList.Add($"Invalid Permission Id - {item}");
                                continue;
                            }
                            var checkOnRolePriviledge = await _context.RolePriviledges.FirstOrDefaultAsync(c => c.PriviledgeId == item && c.RoleId == role.Id);
                            if (checkOnRolePriviledge != null)
                            {
                                errorList.Add($"Permission with Id - ({item}) already exist for this role!");
                                continue;
                            }
                            var rolePermission = RolePriviledge.Create(role.Id, item);
                            _context.RolePriviledges.Add(rolePermission);
                        }
                    }

                }
                _context.Roles.Update(role);

                await _context.SaveChangesAsync();

                string error = errorList.Count > 0 ? JsonConvert.SerializeObject(errorList) : "Updated";
                var returnDto = await this.GetRoleById(role.Id);

                return CustomResult<RoleModel>.Success(returnDto.Data, "Role updated successfully", errorList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<RoleModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task PerformErase(string id)
        {
            var rolePermission = await _context.RolePriviledges.Where(x => x.RoleId == id).ToListAsync();
            if (rolePermission.Any())
            {
                foreach (var delete in rolePermission)
                {
                    _context.RolePriviledges.Remove(delete);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}