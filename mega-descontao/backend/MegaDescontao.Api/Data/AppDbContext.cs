using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<OfferClick> Clicks => Set<OfferClick>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var offer = modelBuilder.Entity<Offer>();
        offer.Property(o => o.Title).IsRequired().HasMaxLength(200);
        offer.Property(o => o.Description).HasMaxLength(1000);
        offer.Property(o => o.ImageUrl).HasMaxLength(600);
        offer.Property(o => o.AffiliateUrl).IsRequired().HasMaxLength(1000);
        offer.Property(o => o.Store).IsRequired().HasMaxLength(60);
        offer.Property(o => o.Category).IsRequired().HasMaxLength(60);
        offer.Ignore(o => o.DiscountPercentage);
        offer.HasIndex(o => o.IsActive);
        offer.HasIndex(o => o.Store);
        offer.HasIndex(o => o.Category);

        // O SQLite guarda decimal como TEXT, o que quebra ORDER BY e comparações de preço.
        // Converter para double mantém a ordenação numérica correta no banco.
        offer.Property(o => o.Price).HasConversion<double>();
        offer.Property(o => o.OriginalPrice).HasConversion<double?>();

        var click = modelBuilder.Entity<OfferClick>();
        click.Property(c => c.UserAgent).HasMaxLength(400);
        click.Property(c => c.Referrer).HasMaxLength(600);
        click.HasIndex(c => c.OfferId);
        click.HasIndex(c => c.ClickedAt);
        click.HasOne(c => c.Offer)
            .WithMany(o => o.Clicks)
            .HasForeignKey(c => c.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
