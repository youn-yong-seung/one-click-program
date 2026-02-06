using Microsoft.EntityFrameworkCore;
using OneClick.Server.Models;

namespace OneClick.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 기본 설정들
        modelBuilder.Entity<Category>().HasMany(c => c.Modules)
            .WithOne(m => m.Category)
            .HasForeignKey(m => m.CategoryId);
            
        modelBuilder.Entity<Subscription>().HasOne(s => s.User)
            .WithMany(u => u.Subscriptions) // 명시적으로 연결
            .HasForeignKey(s => s.UserId);
            
        modelBuilder.Entity<Subscription>().HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .IsRequired(false);
            
        // (UserId, CategoryId) 조합은 유니크해야 함 (동일 카테고리 중복 구독 방지)
        // 주의: CategoryId가 NULL인 경우(올인원)도 중복 방지가 DBMS마다 다를 수 있음(Postgres는 NULL 중복 허용이 기본)
        // 하지만 비즈니스 로직에서 체크하는 게 가장 안전함
        modelBuilder.Entity<Subscription>()
            .HasIndex(s => new { s.UserId, s.CategoryId })
            .IsUnique();
    }
}
