using MegaDescontao.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MegaDescontao.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductOffer> Offers => Set<ProductOffer>();

    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    public DbSet<OfferClick> Clicks => Set<OfferClick>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Name).IsRequired().HasMaxLength(80);
            category.Property(c => c.Slug).IsRequired().HasMaxLength(80);
            category.HasIndex(c => c.Slug).IsUnique();
        });

        modelBuilder.Entity<Store>(store =>
        {
            store.Property(s => s.Name).IsRequired().HasMaxLength(80);
            store.Property(s => s.Slug).IsRequired().HasMaxLength(80);
            store.Property(s => s.LogoUrl).HasMaxLength(600);
            store.HasIndex(s => s.Slug).IsUnique();
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Title).IsRequired().HasMaxLength(200);
            product.Property(p => p.Description).HasMaxLength(1000);
            product.Property(p => p.ImageUrl).HasMaxLength(600);
            product.Property(p => p.SearchText).IsRequired().HasMaxLength(1300);
            product.HasIndex(p => p.IsActive);
            product.HasIndex(p => p.SearchText);
            product.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductOffer>(offer =>
        {
            offer.Property(o => o.AffiliateUrl).IsRequired().HasMaxLength(1000);
            offer.Property(o => o.ProductUrl).HasMaxLength(1000);
            offer.Property(o => o.ExternalProductId).HasMaxLength(120);
            offer.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            offer.Ignore(o => o.DiscountPercentage);

            // O SQLite guarda decimal como TEXT, o que faria ORDER BY de preço comparar
            // string ("9,90" > "10,00"). Converter para double mantém a ordem numérica.
            offer.Property(o => o.CurrentPrice).HasConversion<double>();
            offer.Property(o => o.OriginalPrice).HasConversion<double?>();

            offer.HasIndex(o => o.Status);
            offer.HasIndex(o => o.CurrentPrice);
            offer.HasIndex(o => new { o.StoreId, o.ExternalProductId });

            offer.HasOne(o => o.Product)
                .WithMany(p => p.Offers)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            offer.HasOne(o => o.Store)
                .WithMany(s => s.Offers)
                .HasForeignKey(o => o.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceHistory>(price =>
        {
            price.Property(p => p.Price).HasConversion<double>();
            price.HasIndex(p => new { p.ProductOfferId, p.CollectedAt });
            price.HasOne(p => p.ProductOffer)
                .WithMany(o => o.PriceHistory)
                .HasForeignKey(p => p.ProductOfferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OfferClick>(click =>
        {
            click.Property(c => c.UserAgent).HasMaxLength(400);
            click.Property(c => c.Referrer).HasMaxLength(600);
            click.HasIndex(c => new { c.ProductOfferId, c.ClickedAt });
            click.HasOne(c => c.ProductOffer)
                .WithMany(o => o.Clicks)
                .HasForeignKey(c => c.ProductOfferId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
