using examportal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using qguardbackend.Data.Entities;
using qguardbackend.Data.DTOs;
using examportal.Data.Model;
using Microsoft.AspNetCore.Identity;

namespace qguardbackend.Data.DbContext;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string,
                        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
                        IdentityRoleClaim<string>, IdentityUserToken<string>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    #region DbSetRegion
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<RefreshToken> RefreshToken { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<Priviledge> Priviledges { get; set; }
    public DbSet<RolePriviledge> RolePriviledges { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<CandidateExamsSubmission> CandidateExamsSubmissions { get; set; }
    public DbSet<Institution> Institutions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<CandidatesCurrentState> CandidatesCurrentStates { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseDepartments> CourseDepartments { get; set; }
    public DbSet<DepartmentExamSchedule> DepartmentExamSchedules { get; set; }
    public DbSet<CourseLevel> CourseLevels { get; set; }
    public DbSet<CourseTutors> CourseTutors { get; set; }
    public DbSet<FacultyProgram> FacultyPrograms { get; set; }
    public DbSet<Program> Programs { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<Faculty> Faculties { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<QuestionBank> QuestionBanks { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<ExamSchedule> ExamSchedules { get; set; }
    public DbSet<ExamQuestion> ExamQuestions { get; set; }
    public DbSet<CandidateExamResult> CandidateExamResults { get; set; }
    public DbSet<CandidateProctorLog> CandidateProctorLogs { get; set; }
    public DbSet<SystemAdminOtherTenantsRole> SystemAdminOtherTenantsRole { get; set; }
    public DbSet<Tutor> Tutors { get; set; }
    public DbSet<EnforcementMode> EnforcementModes { get; set; }
    public DbSet<EnforcementAction> EnforcementActions { get; set; }
    public DbSet<ProctorConfiguration> ProctorConfigurations { get; set; }
    public DbSet<ProctorMeTracker> ProctorMeTrackers { get; set; }
    public DbSet<CandidateExamViewAndAttempts> CandidateExamViewAndAttempts { get; set; }

    #endregion

    #region ModelBuilders
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set all decimal to use proper precision
        var propQuery = from entityType in modelBuilder.Model.GetEntityTypes()
                        from entityProperty in entityType.GetProperties()
                        where entityProperty.ClrType == typeof(decimal)
                        select entityProperty;

        foreach (var property in propQuery)
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Configure CandidateProctorActivity
        modelBuilder.Entity<CandidateProctorLog>(entity =>
        {

            entity.HasIndex(e => e.FlagId).IsUnique();
            entity.HasIndex(e => e.CandidateId);
            entity.HasIndex(e => e.ExamScheduleId);
            entity.HasIndex(e => new { e.CandidateId, e.ExamScheduleId });
        });

        modelBuilder.Entity<ApplicationUserRole>()
            .HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<CandidateExamsSubmission>()
            .HasOne(x => x.ExamSchedule)
            .WithMany()
            .HasForeignKey(x => x.ExamScheduleId)
            .OnDelete(DeleteBehavior.Restrict); // or NoAction

        modelBuilder.Entity<CandidateExamsSubmission>()
            .HasOne(x => x.Candidate)
            .WithMany()
            .HasForeignKey(x => x.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CandidateExamsSubmission>()
            .HasOne(x => x.QuestionBank)
            .WithMany()
            .HasForeignKey(x => x.QuestionBankId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    #endregion
}