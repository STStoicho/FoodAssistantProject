using FoodAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodAssistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Recipe> Recipes { get; set; }

        public DbSet<Ingredient> Ingredients { get; set; }

        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Една рецепта има много продукти
            modelBuilder.Entity<Recipe>()
                .HasMany(r => r.Ingredients)
                .WithOne(ri => ri.Recipe)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Един продукт може да участва в много рецепти
            modelBuilder.Entity<Ingredient>()
                .HasMany<RecipeIngredient>()
                .WithOne(ri => ri.Ingredient)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Името на продукта трябва да е уникално
            modelBuilder.Entity<Ingredient>()
                .HasIndex(i => i.Name)
                .IsUnique();
        }
    }
}
