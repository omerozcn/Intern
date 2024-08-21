using TicketSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace TicketSystem.Data
{
     public class ApplicationDbContext : IdentityDbContext<AppUser>
     {
          public ApplicationDbContext(DbContextOptions options)
              : base(options)
          {

          }
          public DbSet<Ticket> Tickets { get; set; }
          public DbSet<AppUserTicket> AppUserTickets { get; set; }
          public DbSet<FirmUser> FirmUsers { get; set; }
          public DbSet<FirmProduct> FirmProducts { get; set; }
          public DbSet<ProductTicket> ProductTickets { get; set; }
          public DbSet<Product> Products { get; set; }
          public DbSet<Firm> Firms { get; set; }
          public DbSet<Feedback> Feedbacks { get; set; }

          protected override void OnModelCreating(ModelBuilder builder)
          {
               base.OnModelCreating(builder);

               builder.Entity<Firm>(entity =>
               {
                    entity.ToTable("Firms");

                    entity.HasKey(c => c.Id);

                    entity.HasMany(u => u.FirmUsers)
                          .WithOne(aut => aut.Firm)
                          .HasForeignKey(aut => aut.FirmId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(u => u.FirmProducts)
                          .WithOne(aut => aut.Firm)
                          .HasForeignKey(aut => aut.FirmId)
                          .OnDelete(DeleteBehavior.Cascade);

               });

               builder.Entity<Product>(entity =>
               {
                    entity.ToTable("Product");

                    entity.HasKey(s => s.Id);

                    entity.HasMany(s => s.FirmProducts)
                          .WithOne(cs => cs.Products)
                          .HasForeignKey(s => s.ProductId)
                          .OnDelete(DeleteBehavior.Cascade);
                    entity.HasMany(s => s.ProductTickets)
                          .WithOne(st => st.Products)
                          .HasForeignKey(s => s.ProductId)
                          .OnDelete(DeleteBehavior.Cascade);
               });

               builder.Entity<Ticket>(entity =>
               {
                    entity.ToTable("Tickets");

                    entity.HasKey(t => t.Id);

                    entity.HasMany(t => t.AppUserTickets)
                          .WithOne(aut => aut.Ticket)
                          .HasForeignKey(aut => aut.TicketId);
                    entity.HasMany(t => t.ProductTickets)
                          .WithOne(st => st.Ticket)
                          .HasForeignKey(st => st.TicketId)
                          .OnDelete(DeleteBehavior.Cascade);
               });

               builder.Entity<AppUser>(entity =>
               {
                    entity.HasKey(a => a.Id);

                    entity.HasIndex(u => u.UserName)
                         .IsUnique(false);

                    entity.HasMany(u => u.FirmUsers)
                          .WithOne(aut => aut.AppUser)
                          .HasForeignKey(u => u.AppUserId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(u => u.AppUserTickets)
                          .WithOne(aut => aut.AppUser)
                          .HasForeignKey(aut => aut.AppUserId)
                          .OnDelete(DeleteBehavior.Cascade);
               });

               builder.Entity<AppUserTicket>(entity =>
               {
                    entity.ToTable("AppUserTickets");

                    entity.HasKey(c => c.Id);

                    entity.HasOne(aut => aut.AppUser)
                          .WithMany(u => u.AppUserTickets)
                          .HasForeignKey(aut => aut.AppUserId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(aut => aut.Ticket)
                          .WithMany(t => t.AppUserTickets)
                          .HasForeignKey(aut => aut.TicketId)
                          .OnDelete(DeleteBehavior.Cascade);
               });

               builder.Entity<FirmUser>(entity =>
               {
                    entity.ToTable("FirmUsers");

                    entity.HasKey(c => c.Id);

                    entity.HasOne(cut => cut.Firm)
                          .WithMany(u => u.FirmUsers)
                          .HasForeignKey(cut => cut.FirmId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(cut => cut.AppUser)
                          .WithMany(u => u.FirmUsers)
                          .HasForeignKey(cut => cut.AppUserId)
                          .OnDelete(DeleteBehavior.Cascade);
               });

               builder.Entity<FirmProduct>(entity =>
               {
                    entity.ToTable("FirmProducts");

                    entity.HasKey(c => c.Id);

                    entity.HasOne(cst => cst.Firm)
                          .WithMany(s => s.FirmProducts)
                          .HasForeignKey(cst => cst.FirmId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(cst => cst.Products)
                          .WithMany(s => s.FirmProducts)
                          .HasForeignKey(cst => cst.ProductId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasIndex(c => new { c.FirmId, c.ProductId })
              .IsUnique();
               });

               builder.Entity<ProductTicket>(entity =>
               {
                    entity.ToTable("ProductTickets");

                    entity.HasKey(c => c.Id);

                    entity.HasOne(st => st.Ticket)
                          .WithMany(s => s.ProductTickets)
                          .HasForeignKey(st => st.TicketId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(st => st.Products)
                          .WithMany(s => s.ProductTickets)
                          .HasForeignKey(s => s.ProductId)
                          .OnDelete(DeleteBehavior.Cascade);

               });

               List<IdentityRole> roles = new List<IdentityRole>
            {
                new IdentityRole
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"

                },
                new IdentityRole
                {
                    Name = "User",
                    NormalizedName = "USER"

                },
            };
               builder.Entity<IdentityRole>().HasData(roles);
          }
     }


}
