using ConejitoTicket.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConejitoTicket.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<SystemApp> SystemApps => Set<SystemApp>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AdminUser>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(100);
        });

        b.Entity<SystemApp>(e =>
        {
            e.HasIndex(x => x.ClientId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.ClientId).HasMaxLength(100);
        });

        b.Entity<Ticket>(e =>
        {
            e.Property(x => x.TicketNumber).UseIdentityAlwaysColumn();
            e.HasIndex(x => x.TicketNumber).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.TenantId).HasMaxLength(100);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MetadataJson).HasColumnType("jsonb");
            e.HasOne(x => x.SystemApp).WithMany().HasForeignKey(x => x.SystemAppId);
            e.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.Status, x.Priority });
            e.HasIndex(x => x.TenantId);
        });

        b.Entity<PushSubscription>(e =>
        {
            e.HasIndex(x => x.Endpoint).IsUnique();
            e.HasOne<AdminUser>().WithMany().HasForeignKey(x => x.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
