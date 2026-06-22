using MeUp.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Data;

/// <summary>
/// DbContext gốc của ứng dụng. Kế thừa IdentityDbContext để có sẵn bảng user/role.
/// Các bảng nghiệp vụ (F1–F6) sẽ được thêm vào đây ở các chức năng sau,
/// mỗi bảng gắn UserId để cô lập dữ liệu theo người dùng.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<HealthLog> HealthLogs => Set<HealthLog>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitCheck> HabitChecks => Set<HabitCheck>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<MedicationIntake> MedicationIntakes => Set<MedicationIntake>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WeeklyInsight> WeeklyInsights => Set<WeeklyInsight>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.Gender).HasMaxLength(20);
            e.Property(x => x.Bio).HasMaxLength(500);
            e.Property(x => x.AvatarUrl).HasMaxLength(256);
            e.Property(x => x.TimeZone).HasMaxLength(64);
            e.Property(x => x.Locale).HasMaxLength(10);
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Type).HasMaxLength(10).IsRequired();
            e.Property(x => x.Color).HasMaxLength(7);
            e.HasIndex(x => new { x.UserId, x.Type });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(10).IsRequired();
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<HealthLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Weight).HasColumnType("numeric(5,2)");
            e.Property(x => x.SleepHours).HasColumnType("numeric(4,1)");
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Recurrence).HasMaxLength(10).HasDefaultValue(Recurrence.None);
            e.HasIndex(x => new { x.UserId, x.IsDone });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Goal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Habit>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<HabitCheck>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.HabitId, x.Date }).IsUnique();
            e.HasOne(x => x.Habit)
                .WithMany(h => h.Checks)
                .HasForeignKey(x => x.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<JournalEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.HasIndex(x => new { x.UserId, x.CategoryId }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CalendarEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Note).HasMaxLength(1000);
            e.HasIndex(x => new { x.UserId, x.Date });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Medication>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Dosage).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MedicationIntake>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MedicationId, x.Date }).IsUnique();
            e.HasOne(x => x.Medication)
                .WithMany(m => m.Intakes)
                .HasForeignKey(x => x.MedicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Note>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(20);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(1000);
            e.Property(x => x.Link).HasMaxLength(256);
            e.Property(x => x.DedupKey).HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => new { x.UserId, x.DedupKey });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WeeklyInsight>(e =>
        {
            e.HasKey(x => x.Id);
            // Một bản ghi duy nhất cho mỗi (người dùng, khoảng tuần) → dùng làm cache.
            e.HasIndex(x => new { x.UserId, x.WeekFrom, x.WeekTo }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
