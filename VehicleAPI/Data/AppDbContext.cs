using Microsoft.EntityFrameworkCore;
using VehicleAPI.Models;

namespace VehicleAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

 
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Part> Parts { get; set; }

        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }

        public DbSet<Credit> Credits { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Review> Reviews { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<PartRequest> PartRequests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Phone)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.User)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.VehicleNumber)
                .IsUnique();

            modelBuilder.Entity<Purchase>()
                .HasOne(p => p.Vendor)
                .WithMany(v => v.Purchases)
                .HasForeignKey(p => p.VendorId);


            modelBuilder.Entity<PurchaseItem>()
                .HasOne(pi => pi.Purchase)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(pi => pi.PurchaseId);

            modelBuilder.Entity<PurchaseItem>()
                .HasOne(pi => pi.Part)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(pi => pi.PartId);


            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(si => si.SaleId);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Part)
                .WithMany(p => p.SaleItems)
                .HasForeignKey(si => si.PartId);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Credit)
                .WithOne(c => c.Sale)
                .HasForeignKey<Credit>(c => c.SaleId);


            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId);


            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<PartRequest>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.PartRequests)
                .HasForeignKey(pr => pr.UserId);

            modelBuilder.Entity<PartRequest>()
                .HasOne(pr => pr.Part)
                .WithMany(p => p.PartRequests)
                .HasForeignKey(pr => pr.PartId);


            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, Name = "Admin" },
                new Role { RoleId = 2, Name = "Staff" },
                new Role { RoleId = 3, Name = "Customer" }
            );
        }




    }
}