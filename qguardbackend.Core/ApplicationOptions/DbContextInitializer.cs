using qguardbackend.Core.Interfaces;
using qguardbackend.Data.DbContext;
using Microsoft.EntityFrameworkCore;

namespace qguardbackend.Core.ApplicationOptions
{
    public static class DbContextInitializer
    {
        public static async Task Initialize(AppDbContext applicationDbContext,
            ISettingsService sSvc, IStaticDatas stSvc, IRoleService rSvc,
            IUserManagementService uSvc///, IInstitutionService iISvc, ISemesterService sesSvc, 
            //ISessionService seSvc, IEnforcementActionService aSvc, IEnforcementModeService mSvc
            )
        {
            // Check, if db ApplicationDbContext is created
            //await applicationDbContext.Database.EnsureCreatedAsync();
            await applicationDbContext.Database.MigrateAsync();
            // seed settings
            await sSvc.SeedSettings();
            // seed country, state and city
            await stSvc.SeedDefaultCountry();
            //seed role
            await rSvc.SeedRoles();
            //seed institution
            //await iISvc.SeedDefaultInstitution();
            ////seed semester
            //await sesSvc.SeedDefaultSemester();
            ////seed session
            //await seSvc.SeedDefaultSession();
            //seed default system user
            await uSvc.SeedDefaultUser();
            //seed default enforcement action
            //await aSvc.SeedDefaultEnforcementAction();
            ////seed default enforcement mode
            //await mSvc.SeedDefaultEnforcementMode();
        }
    }
}
