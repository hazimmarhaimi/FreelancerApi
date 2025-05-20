using FreelancerAPI.Enitites;
using Microsoft.EntityFrameworkCore;

namespace FreelancerAPI.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Freelancer> Freelancers { get; set; }
    public DbSet<Skillset> Skillsets { get; set; }
    public DbSet<Hobby> Hobbies { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Freelancer>()
            .HasMany(f => f.Skillsets)
            .WithOne(s => s.Freelancer)
            .HasForeignKey(s => s.FreelancerId);

        modelBuilder.Entity<Freelancer>()
            .HasMany(f => f.Hobbies)
            .WithOne(h => h.Freelancer)
            .HasForeignKey(h => h.FreelancerId);
    }

}