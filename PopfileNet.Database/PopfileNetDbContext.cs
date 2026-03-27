using Microsoft.EntityFrameworkCore;
using PopfileNet.Common;

namespace PopfileNet.Database;

public class PopfileNetDbContext(DbContextOptions<PopfileNetDbContext> options) : DbContext(options)
{
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<Bucket> Buckets { get; set; } = null!;
    public DbSet<MailFolder> MailFolders { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ImapUid)
                .HasMaxLength(500)
                .IsRequired(false);
            
            entity.HasIndex(e => new { e.ImapUid, e.Folder }).IsUnique();
            
            entity.HasIndex(e => e.Folder);
            
            entity.OwnsOne(e => e.UniqueId, uid =>
            {
                uid.Property(u => u.Validity).HasColumnName("Validity");
                uid.Property(u => u.Id).HasColumnName("UniqueId");
            });
            
            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Body)
                .IsRequired();
            
            entity.Property(e => e.FromAddress)
                .IsRequired()
                .HasMaxLength(320);
            
            entity.Property(e => e.ReceivedDate)
                .IsRequired();

            entity.Property(e => e.Folder);

            entity.HasOne(e => e.FolderNavigation)
                .WithMany()
                .HasForeignKey(e => e.Folder)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ToAddresses);

            entity.HasMany(e => e.Headers)
                .WithOne(h => h.Email)
                .HasForeignKey(h => h.EmailId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<MailHeader>()
            .HasOne(a => a.Email)
            .WithMany(e => e.Headers)
            .HasForeignKey(a => a.EmailId)
            .IsRequired(false);

        modelBuilder.Entity<MailFolder>(entity =>
        {
            entity.HasKey(f => f.Id);
            
            entity.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.HasIndex(f => f.Name).IsUnique();
        });

        modelBuilder.Entity<Bucket>(entity =>
        {
            entity.HasKey(b => b.Id);
            
            entity.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(b => b.Description)
                .HasMaxLength(500);
            
            entity.HasMany(b => b.Folders)
                .WithOne(f => f.Bucket)
                .HasForeignKey(f => f.BucketId)
                .IsRequired(false);
        });

        modelBuilder.Entity<Settings>(entity =>
        {
            entity.HasKey(s => s.Id);
        });
    }
}