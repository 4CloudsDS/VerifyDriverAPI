using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Models;

namespace VerifyDriversAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<UserType> UserTypes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Partner> Partners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure User entity
            modelBuilder.Entity<User>().ToTable("_user")
                .Property(u => u.uID).HasColumnName("uID").IsRequired();
            modelBuilder.Entity<User>().Property(u => u.uNames).HasColumnName("uNames");
            modelBuilder.Entity<User>().Property(u => u.uGender).HasColumnName("uGender");
            modelBuilder.Entity<User>().Property(u => u.uAge).HasColumnName("uAge");
            modelBuilder.Entity<User>().Property(u => u.uRating).HasColumnName("uRating");
            modelBuilder.Entity<User>().Property(u => u.uVID).HasColumnName("uVID");
            modelBuilder.Entity<User>().Property(u => u.uPartner_ID).HasColumnName("uPartner_ID");
            modelBuilder.Entity<User>().Property(u => u.uUsertype_ID).HasColumnName("uUsertype_ID");
            //modelBuilder.Entity<User>().Property(u => u.uPlatform_ID).HasColumnName("uPlatform_ID");

            // Configure relationships for User entity
            modelBuilder.Entity<User>()
                .HasOne(u => u.Vehicle)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(u => u.uVID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Partner)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(u => u.uPartner_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.UserType)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(u => u.uUsertype_ID)
                .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<User>()
            //    .HasOne(u => u.Platform)
            //    .WithMany() // Assuming a one-to-many relationship; adjust if necessary
            //    .HasForeignKey(u => u.uPlatform_ID)
            //    .OnDelete(DeleteBehavior.Restrict);

            // Configure Vehicle entity
            modelBuilder.Entity<Vehicle>().ToTable("_vehicle")
                .Property(v => v.vID).HasColumnName("vID").IsRequired();
            modelBuilder.Entity<Vehicle>().Property(v => v.vregistration).HasColumnName("vregistration");
            modelBuilder.Entity<Vehicle>().Property(v => v.vMake).HasColumnName("vMake");
            modelBuilder.Entity<Vehicle>().Property(v => v.vModel_name).HasColumnName("vModel_name");
            modelBuilder.Entity<Vehicle>().Property(v => v.vModel_year).HasColumnName("vModel_year");
            modelBuilder.Entity<Vehicle>().Property(v => v.vPlatform_ID).HasColumnName("vPlatform_ID");
            modelBuilder.Entity<Vehicle>().Property(v => v.vPartner_ID).HasColumnName("vPartner_ID");

            // Configure relationships for Vehicle entity
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Platform)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(v => v.vPlatform_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Partner)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(v => v.vPartner_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Platform entity
            modelBuilder.Entity<Platform>().ToTable("_platform")
                .Property(p => p.pID).HasColumnName("pID").IsRequired();
            modelBuilder.Entity<Platform>().Property(p => p.pName).HasColumnName("pName");
            modelBuilder.Entity<Platform>().Property(p => p.pType).HasColumnName("pType");

            // Configure UserType entity
            modelBuilder.Entity<UserType>().ToTable("_user_types")
                .Property(ut => ut.U_T_ID).HasColumnName("U_T_ID").IsRequired();
            modelBuilder.Entity<UserType>().Property(ut => ut.U_T_description).HasColumnName("U_T_description");

            // Configure Comment entity
            modelBuilder.Entity<Comment>().ToTable("_comments")
                .Property(c => c.cID).HasColumnName("cID").IsRequired();
            modelBuilder.Entity<Comment>().Property(c => c.cText).HasColumnName("cText");
            modelBuilder.Entity<Comment>().Property(c => c.cDateTime).HasColumnName("cDateTime");
            modelBuilder.Entity<Comment>().Property(c => c.c_Uid).HasColumnName("c_Uid");
            modelBuilder.Entity<Comment>().Property(c => c.c_Pid).HasColumnName("c_Pid");

            // Configure relationships for Comment entity
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(c => c.c_Uid)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Partner)
                .WithMany() // Assuming a one-to-many relationship; adjust if necessary
                .HasForeignKey(c => c.c_Pid)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Partner entity
            modelBuilder.Entity<Partner>().ToTable("_partner")
                .Property(p => p.pID).HasColumnName("pID").IsRequired();
            modelBuilder.Entity<Partner>().Property(p => p.pName).HasColumnName("pName");
        }


    }
}
