using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;
using TicketSystem.Security;

namespace TicketSystem.Data;

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
            entity.HasKey(firm => firm.Id);

            entity.HasIndex(firm => firm.Name).IsUnique();

            entity.HasMany(firm => firm.FirmUsers)
                .WithOne(firmUser => firmUser.Firm)
                .HasForeignKey(firmUser => firmUser.FirmId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasMany(firm => firm.FirmProducts)
                .WithOne(firmProduct => firmProduct.Firm)
                .HasForeignKey(firmProduct => firmProduct.FirmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(new Firm
            {
                Id = 99,
                Name = ProtectedFirm.Name
            });
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);

            entity.HasIndex(product => product.Name).IsUnique();

            entity.HasMany(product => product.FirmProducts)
                .WithOne(firmProduct => firmProduct.Products)
                .HasForeignKey(firmProduct => firmProduct.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(product => product.ProductTickets)
                .WithOne(productTicket => productTicket.Products)
                .HasForeignKey(productTicket => productTicket.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");
            entity.HasKey(ticket => ticket.Id);

            // Tickets are almost always filtered or ordered by status.
            entity.HasIndex(ticket => ticket.Status);

            entity.HasMany(ticket => ticket.AppUserTickets)
                .WithOne(link => link.Ticket)
                .HasForeignKey(link => link.TicketId);

            entity.HasMany(ticket => ticket.ProductTickets)
                .WithOne(link => link.Ticket)
                .HasForeignKey(link => link.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.HasMany(user => user.FirmUsers)
                .WithOne(firmUser => firmUser.AppUser)
                .HasForeignKey(firmUser => firmUser.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(user => user.AppUserTickets)
                .WithOne(link => link.AppUser)
                .HasForeignKey(link => link.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AppUserTicket>(entity =>
        {
            entity.ToTable("AppUserTickets");
            entity.HasKey(link => link.Id);

            entity.HasOne(link => link.AppUser)
                .WithMany(user => user.AppUserTickets)
                .HasForeignKey(link => link.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(link => link.Ticket)
                .WithMany(ticket => ticket.AppUserTickets)
                .HasForeignKey(link => link.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FirmUser>(entity =>
        {
            entity.ToTable("FirmUsers");
            entity.HasKey(firmUser => firmUser.Id);

            entity.HasOne(firmUser => firmUser.Firm)
                .WithMany(firm => firm.FirmUsers)
                .HasForeignKey(firmUser => firmUser.FirmId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasOne(firmUser => firmUser.AppUser)
                .WithMany(user => user.FirmUsers)
                .HasForeignKey(firmUser => firmUser.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FirmProduct>(entity =>
        {
            entity.ToTable("FirmProducts");
            entity.HasKey(firmProduct => firmProduct.Id);

            entity.HasOne(firmProduct => firmProduct.Firm)
                .WithMany(firm => firm.FirmProducts)
                .HasForeignKey(firmProduct => firmProduct.FirmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(firmProduct => firmProduct.Products)
                .WithMany(product => product.FirmProducts)
                .HasForeignKey(firmProduct => firmProduct.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(firmProduct => new { firmProduct.FirmId, firmProduct.ProductId })
                .IsUnique();
        });

        builder.Entity<ProductTicket>(entity =>
        {
            entity.ToTable("ProductTickets");
            entity.HasKey(link => link.Id);

            entity.HasOne(link => link.Ticket)
                .WithMany(ticket => ticket.ProductTickets)
                .HasForeignKey(link => link.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(link => link.Products)
                .WithMany(product => product.ProductTickets)
                .HasForeignKey(link => link.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Identifiers are pinned; see SeededRoleIds for why they must not be generated.
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = SeededRoleIds.Admin,
                Name = AppRoles.Admin,
                NormalizedName = AppRoles.Admin.ToUpperInvariant()
            },
            new IdentityRole
            {
                Id = SeededRoleIds.User,
                Name = AppRoles.User,
                NormalizedName = AppRoles.User.ToUpperInvariant()
            });
    }
}
