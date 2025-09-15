# ShoeShop Application - Complete Development Guide

## Table of Contents
1. [Project Overview and Architecture](#1-project-overview-and-architecture)
2. [Project Setup](#2-project-setup)
3. [ShoeShop.Repository Project](#3-shoeshoprepository-project)
4. [Design-Time Factory Pattern (Deep Dive)](#4-design-time-factory-pattern-deep-dive)
5. [ShoeShop.Services Project](#5-shoeshopservices-project)
6. [ShoeShop.Web Project](#6-shoeshopweb-project)
7. [Configuration and Dependency Injection](#7-configuration-and-dependency-injection)
8. [Testing and Running](#8-testing-and-running)
9. [Architecture Patterns Explained](#9-architecture-patterns-explained)
10. [Advanced Implementation Walkthrough](#10-advanced-implementation-walkthrough)

---

## 1. Project Overview and Architecture

### Clean Architecture Implementation

This ShoeShop application demonstrates a modern, layered architecture following clean architecture principles:

```
┌─────────────────────────────────────────────────────────────┐
│                    ShoeShop.Web                             │
│              (Presentation Layer)                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ Controllers │  │    Views    │  │   Identity Context  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────┬───────────────────────────────────┘
                          │ depends on
┌─────────────────────────▼───────────────────────────────────┐
│                  ShoeShop.Services                          │
│               (Business Logic Layer)                        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │    DTOs     │  │  Services   │  │   Business Rules    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────┬───────────────────────────────────┘
                          │ depends on
┌─────────────────────────▼───────────────────────────────────┐
│                 ShoeShop.Repository                         │
│                (Data Access Layer)                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Models    │  │ Repository  │  │    DbContext        │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────┬───────────────────────────────────┘
                          │ persists to
            ┌─────────────▼─────────────┐
            │        Database           │
            │   SQLite / In-Memory      │
            └───────────────────────────┘
```

### Key Architectural Patterns

1. **Repository Pattern**: Abstracts data access logic
2. **Service Layer Pattern**: Encapsulates business logic
3. **DTO Pattern**: Controls data transfer between layers
4. **Dependency Injection**: Manages object dependencies
5. **Factory Pattern**: Creates objects at design-time
6. **Configuration Pattern**: Environment-specific behavior

---

## 2. Project Setup

### Step 2.1: Create Solution Structure

```powershell
# Create solution directory and navigate to it
mkdir ShoeShop
cd ShoeShop

# Create the solution
dotnet new sln -n ShoeShop

# Create the three layers
dotnet new classlib -n ShoeShop.Repository -f net9.0
dotnet new classlib -n ShoeShop.Services -f net9.0
dotnet new mvc -n ShoeShop.Web -f net9.0 --auth Individual

# Add projects to solution
dotnet sln add ShoeShop.Repository/ShoeShop.Repository.csproj
dotnet sln add ShoeShop.Services/ShoeShop.Services.csproj
dotnet sln add ShoeShop.Web/ShoeShop.Web.csproj
```

### Step 2.2: Set Up Project Dependencies

```powershell
# Repository Layer - Data Access Dependencies
dotnet add ShoeShop.Repository package Microsoft.EntityFrameworkCore
dotnet add ShoeShop.Repository package Microsoft.EntityFrameworkCore.Sqlite
dotnet add ShoeShop.Repository package Microsoft.EntityFrameworkCore.InMemory
dotnet add ShoeShop.Repository package Microsoft.EntityFrameworkCore.Design

# Services Layer - Business Logic Dependencies
dotnet add ShoeShop.Services reference ShoeShop.Repository
dotnet add ShoeShop.Services package Microsoft.Extensions.Configuration.Abstractions

# Web Layer - Presentation Dependencies
dotnet add ShoeShop.Web reference ShoeShop.Repository
dotnet add ShoeShop.Web reference ShoeShop.Services
dotnet add ShoeShop.Web package Microsoft.EntityFrameworkCore.Tools
dotnet add ShoeShop.Web package Microsoft.EntityFrameworkCore.Sqlite
dotnet add ShoeShop.Web package Microsoft.EntityFrameworkCore.InMemory
```

---

## 3. ShoeShop.Repository Project

### Step 3.1: Domain Models

Create the folder structure:
```
ShoeShop.Repository/
├── Models/
├── Data/
├── Interfaces/
└── Repositories/
```

#### Create `Models/Shoe.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ShoeShop.Repository.Models;

/// <summary>
/// Represents a shoe entity in the domain model
/// </summary>
public class Shoe
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Size { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(30)]
    public string BaseColor { get; set; } = string.Empty;
    
    /// <summary>
    /// Current color - can be different from base color due to dynamic color changing
    /// </summary>
    [MaxLength(30)]
    public string? CurrentColor { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value")]
    public decimal Price { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(200)]
    public string? ImageUrl { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Navigation property for one-to-many relationship with color variations
    /// Virtual keyword enables lazy loading and allows EF Core to override for proxying
    /// </summary>
    public virtual ICollection<ShoeColorVariation> ColorVariations { get; set; } = new List<ShoeColorVariation>();
}
```

#### Create `Models/ShoeColorVariation.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ShoeShop.Repository.Models;

/// <summary>
/// Represents available color variations for a shoe
/// Enables dynamic color changing functionality
/// </summary>
public class ShoeColorVariation
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to the parent shoe
    /// </summary>
    public int ShoeId { get; set; }
    
    [Required]
    [MaxLength(30)]
    public string ColorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Hexadecimal color code for UI display (e.g., #FF5733)
    /// </summary>
    [Required]
    [MaxLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid hex color format")]
    public string HexCode { get; set; } = string.Empty;
    
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }
    
    /// <summary>
    /// Whether this color variation is currently available
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property back to the parent shoe
    /// Virtual keyword enables lazy loading
    /// </summary>
    public virtual Shoe Shoe { get; set; } = null!;
}
```

### Step 3.2: Database Context with Fluent API

#### Create `Data/ShoeShopDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ShoeShop.Repository.Models;

namespace ShoeShop.Repository.Data;

/// <summary>
/// Entity Framework DbContext for business data
/// Separate from Identity DbContext following separation of concerns
/// </summary>
public class ShoeShopDbContext : DbContext
{
    public ShoeShopDbContext(DbContextOptions<ShoeShopDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet for Shoe entities
    /// </summary>
    public DbSet<Shoe> Shoes { get; set; } = null!;
    
    /// <summary>
    /// DbSet for ShoeColorVariation entities
    /// </summary>
    public DbSet<ShoeColorVariation> ShoeColorVariations { get; set; } = null!;

    /// <summary>
    /// Configure entity relationships and constraints using Fluent API
    /// Fluent API provides more configuration options than data annotations
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Shoe entity
        modelBuilder.Entity<Shoe>(entity =>
        {
            // Primary key configuration (redundant as Id is convention, but explicit)
            entity.HasKey(e => e.Id);
            
            // Configure decimal precision for price
            entity.Property(e => e.Price)
                  .HasPrecision(18, 2);
            
            // Create indexes for frequently queried columns
            entity.HasIndex(e => e.Name)
                  .HasDatabaseName("IX_Shoes_Name");
                  
            entity.HasIndex(e => e.Brand)
                  .HasDatabaseName("IX_Shoes_Brand");
                  
            // Index for availability filtering
            entity.HasIndex(e => e.IsAvailable)
                  .HasDatabaseName("IX_Shoes_IsAvailable");
        });

        // Configure ShoeColorVariation entity
        modelBuilder.Entity<ShoeColorVariation>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Configure one-to-many relationship
            entity.HasOne(e => e.Shoe)
                  .WithMany(e => e.ColorVariations)
                  .HasForeignKey(e => e.ShoeId)
                  .OnDelete(DeleteBehavior.Cascade); // Delete variations when shoe is deleted
                  
            // Unique constraint: one color per shoe
            entity.HasIndex(e => new { e.ShoeId, e.ColorName })
                  .IsUnique()
                  .HasDatabaseName("IX_ShoeColorVariations_ShoeId_ColorName");
                  
            // Index for active color filtering
            entity.HasIndex(e => new { e.ShoeId, e.IsActive })
                  .HasDatabaseName("IX_ShoeColorVariations_ShoeId_IsActive");
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seeds initial data for development and testing
    /// </summary>
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed sample shoes
        var shoes = new[]
        {
            new Shoe
            {
                Id = 1,
                Name = "Air Max 270",
                Brand = "Nike",
                Size = "US 9",
                BaseColor = "White",
                CurrentColor = "White",
                Price = 150.00m,
                Description = "Comfortable running shoes with air cushioning technology",
                ImageUrl = "/images/shoes/airmax270-white.jpg",
                IsAvailable = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Shoe
            {
                Id = 2,
                Name = "Stan Smith",
                Brand = "Adidas",
                Size = "US 10",
                BaseColor = "White",
                CurrentColor = "White",
                Price = 80.00m,
                Description = "Classic white leather tennis shoes with green accents",
                ImageUrl = "/images/shoes/stansmith-white.jpg",
                IsAvailable = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Shoe
            {
                Id = 3,
                Name = "Chuck Taylor All Star",
                Brand = "Converse",
                Size = "US 8",
                BaseColor = "Black",
                CurrentColor = "Black",
                Price = 65.00m,
                Description = "Iconic canvas high-top sneakers with rubber sole",
                ImageUrl = "/images/shoes/chucktaylor-black.jpg",
                IsAvailable = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        modelBuilder.Entity<Shoe>().HasData(shoes);

        // Seed color variations
        var colorVariations = new[]
        {
            // Air Max 270 variations
            new ShoeColorVariation { Id = 1, ShoeId = 1, ColorName = "White", HexCode = "#FFFFFF", StockQuantity = 15, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 2, ShoeId = 1, ColorName = "Black", HexCode = "#000000", StockQuantity = 12, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 3, ShoeId = 1, ColorName = "Red", HexCode = "#FF0000", StockQuantity = 8, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 4, ShoeId = 1, ColorName = "Blue", HexCode = "#0000FF", StockQuantity = 10, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Stan Smith variations
            new ShoeColorVariation { Id = 5, ShoeId = 2, ColorName = "White", HexCode = "#FFFFFF", StockQuantity = 20, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 6, ShoeId = 2, ColorName = "Green", HexCode = "#008000", StockQuantity = 7, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 7, ShoeId = 2, ColorName = "Navy", HexCode = "#000080", StockQuantity = 5, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

            // Chuck Taylor variations
            new ShoeColorVariation { Id = 8, ShoeId = 3, ColorName = "Black", HexCode = "#000000", StockQuantity = 18, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 9, ShoeId = 3, ColorName = "White", HexCode = "#FFFFFF", StockQuantity = 14, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 10, ShoeId = 3, ColorName = "Red", HexCode = "#FF0000", StockQuantity = 6, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ShoeColorVariation { Id = 11, ShoeId = 3, ColorName = "Pink", HexCode = "#FFC0CB", StockQuantity = 4, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        modelBuilder.Entity<ShoeColorVariation>().HasData(colorVariations);
    }
}
```

### Step 3.3: Repository Pattern Implementation

#### Create `Interfaces/IShoeRepository.cs`:

```csharp
using ShoeShop.Repository.Models;

namespace ShoeShop.Repository.Interfaces;

/// <summary>
/// Repository interface following Repository pattern
/// Provides abstraction over data access operations
/// Enables polymorphism - different implementations can be swapped
/// </summary>
public interface IShoeRepository
{
    // Basic CRUD operations
    Task<IEnumerable<Shoe>> GetAllShoesAsync();
    Task<Shoe?> GetShoeByIdAsync(int id);
    Task<IEnumerable<Shoe>> GetShoesByBrandAsync(string brand);
    Task<IEnumerable<Shoe>> SearchShoesAsync(string searchTerm);
    Task<Shoe> CreateShoeAsync(Shoe shoe);
    Task<Shoe> UpdateShoeAsync(Shoe shoe);
    Task<bool> DeleteShoeAsync(int id);
    Task<bool> ShoeExistsAsync(int id);
    
    // Color variation operations
    Task<IEnumerable<ShoeColorVariation>> GetColorVariationsAsync(int shoeId);
    Task<ShoeColorVariation?> GetColorVariationByIdAsync(int id);
    Task<ShoeColorVariation> CreateColorVariationAsync(ShoeColorVariation colorVariation);
    Task<ShoeColorVariation> UpdateColorVariationAsync(ShoeColorVariation colorVariation);
    Task<bool> DeleteColorVariationAsync(int id);
    
    // Dynamic color operations - key business feature
    Task<bool> ChangeShoeColorAsync(int shoeId, string colorName);
    Task<IEnumerable<string>> GetAvailableColorsAsync(int shoeId);
}
```

#### Create `Repositories/ShoeRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ShoeShop.Repository.Data;
using ShoeShop.Repository.Interfaces;
using ShoeShop.Repository.Models;

namespace ShoeShop.Repository.Repositories;

/// <summary>
/// Implementation of IShoeRepository using Entity Framework Core
/// Handles all data access operations for shoes and color variations
/// </summary>
public class ShoeRepository : IShoeRepository
{
    private readonly ShoeShopDbContext _context;

    public ShoeRepository(ShoeShopDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all available shoes with their color variations
    /// Uses eager loading to prevent N+1 query issues
    /// </summary>
    public async Task<IEnumerable<Shoe>> GetAllShoesAsync()
    {
        return await _context.Shoes
            .Include(s => s.ColorVariations)  // Eager loading of related data
            .Where(s => s.IsAvailable)        // Business rule: only available shoes
            .OrderBy(s => s.Name)             // Consistent ordering
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves a specific shoe by ID with color variations
    /// </summary>
    public async Task<Shoe?> GetShoeByIdAsync(int id)
    {
        return await _context.Shoes
            .Include(s => s.ColorVariations)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Searches shoes by brand name (case-insensitive)
    /// </summary>
    public async Task<IEnumerable<Shoe>> GetShoesByBrandAsync(string brand)
    {
        return await _context.Shoes
            .Include(s => s.ColorVariations)
            .Where(s => s.Brand.ToLower().Contains(brand.ToLower()) && s.IsAvailable)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Full-text search across name, brand, and description
    /// </summary>
    public async Task<IEnumerable<Shoe>> SearchShoesAsync(string searchTerm)
    {
        return await _context.Shoes
            .Include(s => s.ColorVariations)
            .Where(s => (s.Name.ToLower().Contains(searchTerm.ToLower()) ||
                        s.Brand.ToLower().Contains(searchTerm.ToLower()) ||
                        s.Description!.ToLower().Contains(searchTerm.ToLower())) &&
                        s.IsAvailable)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a new shoe entity
    /// </summary>
    public async Task<Shoe> CreateShoeAsync(Shoe shoe)
    {
        _context.Shoes.Add(shoe);
        await _context.SaveChangesAsync();
        return shoe;
    }

    /// <summary>
    /// Updates an existing shoe entity
    /// Automatically sets UpdatedAt timestamp
    /// </summary>
    public async Task<Shoe> UpdateShoeAsync(Shoe shoe)
    {
        shoe.UpdatedAt = DateTime.UtcNow;
        _context.Entry(shoe).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return shoe;
    }

    /// <summary>
    /// Soft delete - marks shoe as unavailable rather than physically deleting
    /// Preserves referential integrity and audit trail
    /// </summary>
    public async Task<bool> DeleteShoeAsync(int id)
    {
        var shoe = await _context.Shoes.FindAsync(id);
        if (shoe != null)
        {
            shoe.IsAvailable = false;
            shoe.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a shoe exists in the database
    /// </summary>
    public async Task<bool> ShoeExistsAsync(int id)
    {
        return await _context.Shoes.AnyAsync(s => s.Id == id);
    }

    // Color Variation Methods

    /// <summary>
    /// Gets all active color variations for a shoe
    /// </summary>
    public async Task<IEnumerable<ShoeColorVariation>> GetColorVariationsAsync(int shoeId)
    {
        return await _context.ShoeColorVariations
            .Where(cv => cv.ShoeId == shoeId && cv.IsActive)
            .OrderBy(cv => cv.ColorName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific color variation by ID
    /// </summary>
    public async Task<ShoeColorVariation?> GetColorVariationByIdAsync(int id)
    {
        return await _context.ShoeColorVariations
            .Include(cv => cv.Shoe)
            .FirstOrDefaultAsync(cv => cv.Id == id);
    }

    /// <summary>
    /// Creates a new color variation for a shoe
    /// </summary>
    public async Task<ShoeColorVariation> CreateColorVariationAsync(ShoeColorVariation colorVariation)
    {
        _context.ShoeColorVariations.Add(colorVariation);
        await _context.SaveChangesAsync();
        return colorVariation;
    }

    /// <summary>
    /// Updates an existing color variation
    /// </summary>
    public async Task<ShoeColorVariation> UpdateColorVariationAsync(ShoeColorVariation colorVariation)
    {
        _context.Entry(colorVariation).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return colorVariation;
    }

    /// <summary>
    /// Deletes a color variation
    /// </summary>
    public async Task<bool> DeleteColorVariationAsync(int id)
    {
        var colorVariation = await _context.ShoeColorVariations.FindAsync(id);
        if (colorVariation != null)
        {
            _context.ShoeColorVariations.Remove(colorVariation);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    // Dynamic Color Operations - Core Business Feature

    /// <summary>
    /// Changes the current color of a shoe to an available color variation
    /// This is the key feature that enables dynamic color changing
    /// </summary>
    public async Task<bool> ChangeShoeColorAsync(int shoeId, string colorName)
    {
        var shoe = await _context.Shoes.FindAsync(shoeId);
        var colorVariation = await _context.ShoeColorVariations
            .FirstOrDefaultAsync(cv => cv.ShoeId == shoeId && 
                                      cv.ColorName.ToLower() == colorName.ToLower() && 
                                      cv.IsActive);

        if (shoe != null && colorVariation != null)
        {
            shoe.CurrentColor = colorVariation.ColorName;
            shoe.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all available color names for a shoe (in stock and active)
    /// </summary>
    public async Task<IEnumerable<string>> GetAvailableColorsAsync(int shoeId)
    {
        return await _context.ShoeColorVariations
            .Where(cv => cv.ShoeId == shoeId && cv.IsActive && cv.StockQuantity > 0)
            .Select(cv => cv.ColorName)
            .ToListAsync();
    }
}
```

---

## 4. Design-Time Factory Pattern (Deep Dive)

### What is the Factory Pattern?

The **Factory Pattern** is a creational design pattern that provides an interface for creating objects without specifying their exact class. It encapsulates object creation logic and promotes loose coupling.

### What is a Design-Time Factory?

A **Design-Time Factory** is a specific implementation of the Factory pattern used by Entity Framework Core to create DbContext instances during design-time operations (migrations, scaffolding, etc.).

### Why is the Design-Time Factory Needed?

#### Problem Without Design-Time Factory:
```
EF Core Tools (dotnet ef) → Needs DbContext → How to create it?
                          ↑
                    No DI container available
                    No configuration available
                    No way to inject dependencies
```

#### Solution With Design-Time Factory:
```
EF Core Tools → IDesignTimeDbContextFactory → Creates DbContext with default config
              ↑
        Known interface that EF Core looks for
        Provides DbContext without DI container
        Uses design-time configuration
```

### Detailed Implementation

#### Create `Data/ShoeShopDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShoeShop.Repository.Data;

/// <summary>
/// Design-Time Factory for ShoeShopDbContext
/// 
/// WHY IS THIS NEEDED?
/// ===================
/// 
/// 1. EF Core Tools Problem:
///    - Commands like 'dotnet ef migrations add' run at design-time
///    - At design-time, there's no DI container, no web host, no configuration
///    - EF Core needs to create DbContext instances for these operations
/// 
/// 2. Factory Pattern Solution:
///    - Provides a way to create DbContext without DI
///    - EF Core automatically discovers classes implementing IDesignTimeDbContextFactory
///    - Uses hardcoded configuration suitable for design-time operations
/// 
/// 3. When This Gets Called:
///    - dotnet ef migrations add
///    - dotnet ef migrations remove
///    - dotnet ef database update
///    - dotnet ef database drop
///    - Any EF Core design-time command
/// 
/// 4. Alternative Solutions:
///    - Could use IDbContextFactory<T> but that requires DI
///    - Could use AddDbContextFactory but still needs configuration
///    - Design-time factory is the simplest solution for CLI operations
/// </summary>
public class ShoeShopDbContextFactory : IDesignTimeDbContextFactory<ShoeShopDbContext>
{
    /// <summary>
    /// Creates a DbContext instance for design-time operations
    /// 
    /// IMPORTANT DESIGN DECISIONS:
    /// ==========================
    /// 
    /// 1. Database Provider Choice:
    ///    - Uses SQLite for design-time operations
    ///    - SQLite is file-based, doesn't require server installation
    ///    - Suitable for development and CI/CD pipelines
    /// 
    /// 2. Connection String:
    ///    - Hardcoded for design-time operations
    ///    - Uses a dedicated design-time database file
    ///    - Separate from runtime database configurations
    /// 
    /// 3. Configuration Source:
    ///    - Could read from appsettings.json but that adds complexity
    ///    - Hardcoded approach is simpler and more predictable
    ///    - Runtime configuration is handled by Program.cs
    /// </summary>
    /// <param name="args">Command line arguments passed to EF Core tools</param>
    /// <returns>Configured DbContext instance</returns>
    public ShoeShopDbContext CreateDbContext(string[] args)
    {
        // Configure DbContext options for design-time
        var optionsBuilder = new DbContextOptionsBuilder<ShoeShopDbContext>();
        
        // Use SQLite with a design-time specific database file
        // This file is only used for migrations and design-time operations
        optionsBuilder.UseSqlite("Data Source=shoeshop_designtime.db");
        
        // Optional: Enable sensitive data logging for design-time debugging
        optionsBuilder.EnableSensitiveDataLogging();
        
        // Optional: Enable detailed errors for design-time operations
        optionsBuilder.EnableDetailedErrors();
        
        return new ShoeShopDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Alternative Implementation: Configuration-Aware Factory
/// 
/// This version reads from appsettings.json at design-time
/// More complex but provides better consistency with runtime
/// </summary>
public class ConfigurationAwareShoeShopDbContextFactory : IDesignTimeDbContextFactory<ShoeShopDbContext>
{
    public ShoeShopDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ShoeShopDbContext>();
        
        // Use the same connection string as runtime
        var connectionString = configuration.GetConnectionString("ShoeShopConnection")
                             ?? "Data Source=shoeshop_designtime.db";
                             
        optionsBuilder.UseSqlite(connectionString);
        
        return new ShoeShopDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Factory Pattern Benefits in This Context:
/// 
/// 1. SEPARATION OF CONCERNS:
///    - Design-time concerns separated from runtime concerns
///    - Migration logic separate from application logic
/// 
/// 2. DEPENDENCY INVERSION:
///    - EF Core depends on abstraction (IDesignTimeDbContextFactory)
///    - Concrete factory provides implementation
/// 
/// 3. FLEXIBILITY:
///    - Can switch database providers for different environments
///    - Can use different connection strings for different scenarios
/// 
/// 4. TESTABILITY:
///    - Design-time operations don't interfere with test database
///    - Can mock factory for unit testing if needed
/// 
/// 5. TOOLING SUPPORT:
///    - Enables rich EF Core CLI experience
///    - Supports Visual Studio Package Manager Console commands
///    - Works with CI/CD pipeline automation
/// </summary>
```

### Factory Pattern in Broader Context

#### Traditional Factory Pattern Example:

```csharp
// Abstract factory interface
public interface IShoeFactory
{
    Shoe CreateShoe(string type);
}

// Concrete factory implementations
public class NikeShoeFactory : IShoeFactory
{
    public Shoe CreateShoe(string type) => type switch
    {
        "running" => new NikeRunningShoe(),
        "basketball" => new NikeBasketballShoe(),
        _ => throw new ArgumentException("Unknown shoe type")
    };
}

public class AdidasShoeFactory : IShoeFactory
{
    public Shoe CreateShoe(string type) => type switch
    {
        "running" => new AdidasRunningShoe(),
        "basketball" => new AdidasBasketballShoe(),
        _ => throw new ArgumentException("Unknown shoe type")
    };
}

// Usage with polymorphism
public class ShoeService
{
    private readonly IShoeFactory _factory;
    
    public ShoeService(IShoeFactory factory)
    {
        _factory = factory; // Can be any factory implementation
    }
    
    public Shoe CreateCustomShoe(string brand, string type)
    {
        return _factory.CreateShoe(type); // Factory handles creation logic
    }
}
```

### EF Core Design-Time Factory vs Runtime Factory

| Aspect | Design-Time Factory | Runtime Factory |
|--------|--------------------|--------------------|
| **Purpose** | EF Core tooling | Application runtime |
| **When Used** | Migrations, scaffolding | Request processing |
| **Dependencies** | None (standalone) | DI container, configuration |
| **Configuration** | Hardcoded or minimal | Full application configuration |
| **Interface** | `IDesignTimeDbContextFactory<T>` | `IDbContextFactory<T>` |
| **Discovery** | Automatic by EF Core | Registered in DI |

---

## 5. ShoeShop.Services Project

### Step 5.1: Data Transfer Objects (DTOs)

Create the folder structure:
```
ShoeShop.Services/
├── DTOs/
├── Interfaces/
└── Services/
```

#### Create `DTOs/ShoeDto.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ShoeShop.Services.DTOs;

/// <summary>
/// Data Transfer Object for Shoe entity
/// 
/// DTO PATTERN BENEFITS:
/// ====================
/// 1. API STABILITY: Changes to domain models don't break API contracts
/// 2. SECURITY: Control exactly what data is exposed to clients
/// 3. PERFORMANCE: Transfer only necessary data, avoid over-fetching
/// 4. VALIDATION: Add specific validation rules for different operations
/// 5. VERSIONING: Can maintain multiple DTOs for different API versions
/// </summary>
public class ShoeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string BaseColor { get; set; } = string.Empty;
    public string? CurrentColor { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Nested DTOs for related data
    /// Prevents circular references and controls data exposure
    /// </summary>
    public List<ShoeColorVariationDto> ColorVariations { get; set; } = new();
    
    /// <summary>
    /// Computed property: available color names for quick access
    /// Business logic: only active colors with stock > 0
    /// </summary>
    public List<string> AvailableColors { get; set; } = new();
}

/// <summary>
/// DTO for color variation data
/// Simplified version without navigation properties
/// </summary>
public class ShoeColorVariationDto
{
    public int Id { get; set; }
    public int ShoeId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string HexCode { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating new shoes
/// Contains only fields needed for creation
/// No ID (auto-generated), no timestamps (set by system)
/// </summary>
public class CreateShoeDto
{
    [Required(ErrorMessage = "Shoe name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Brand is required")]
    [MaxLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Size is required")]
    [MaxLength(50, ErrorMessage = "Size cannot exceed 50 characters")]
    public string Size { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Base color is required")]
    [MaxLength(30, ErrorMessage = "Base color cannot exceed 30 characters")]
    public string BaseColor { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [MaxLength(200, ErrorMessage = "Image URL cannot exceed 200 characters")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string? ImageUrl { get; set; }
}

/// <summary>
/// DTO for updating existing shoes
/// Includes ID for identification
/// Contains all updatable fields
/// </summary>
public class UpdateShoeDto
{
    [Required]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Shoe name is required")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Brand is required")]
    [MaxLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Size is required")]
    [MaxLength(50, ErrorMessage = "Size cannot exceed 50 characters")]
    public string Size { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Base color is required")]
    [MaxLength(30, ErrorMessage = "Base color cannot exceed 30 characters")]
    public string BaseColor { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    [MaxLength(200, ErrorMessage = "Image URL cannot exceed 200 characters")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string? ImageUrl { get; set; }
    
    public bool IsAvailable { get; set; }
}

/// <summary>
/// DTO for dynamic color changing operation
/// Minimal data needed for the operation
/// </summary>
public class ChangeColorDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid shoe ID")]
    public int ShoeId { get; set; }
    
    [Required(ErrorMessage = "Color name is required")]
    [MaxLength(30, ErrorMessage = "Color name cannot exceed 30 characters")]
    public string ColorName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating new color variations
/// Used when adding new colors to existing shoes
/// </summary>
public class CreateColorVariationDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid shoe ID")]
    public int ShoeId { get; set; }
    
    [Required(ErrorMessage = "Color name is required")]
    [MaxLength(30, ErrorMessage = "Color name cannot exceed 30 characters")]
    public string ColorName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Hex code is required")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Invalid hex color format")]
    public string HexCode { get; set; } = string.Empty;
    
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }
}
```

### Step 5.2: Service Interface

#### Create `Interfaces/IShoeService.cs`:

```csharp
using ShoeShop.Services.DTOs;

namespace ShoeShop.Services.Interfaces;

/// <summary>
/// Service interface for shoe business logic
/// 
/// SERVICE LAYER RESPONSIBILITIES:
/// ===============================
/// 1. Business logic and validation
/// 2. Data transformation (Entity ↔ DTO mapping)
/// 3. Orchestration of multiple repository calls
/// 4. Configuration-specific behavior
/// 5. Cross-cutting concerns (logging, caching)
/// 
/// INTERFACE BENEFITS (POLYMORPHISM):
/// ==================================
/// 1. Multiple implementations possible (caching, logging, etc.)
/// 2. Easy unit testing with mocks
/// 3. Dependency inversion principle
/// 4. Plugin architecture support
/// </summary>
public interface IShoeService
{
    // Basic CRUD operations returning DTOs
    Task<IEnumerable<ShoeDto>> GetAllShoesAsync();
    Task<ShoeDto?> GetShoeByIdAsync(int id);
    Task<IEnumerable<ShoeDto>> GetShoesByBrandAsync(string brand);
    Task<IEnumerable<ShoeDto>> SearchShoesAsync(string searchTerm);
    Task<ShoeDto> CreateShoeAsync(CreateShoeDto createShoeDto);
    Task<ShoeDto> UpdateShoeAsync(UpdateShoeDto updateShoeDto);
    Task<bool> DeleteShoeAsync(int id);
    Task<bool> ShoeExistsAsync(int id);
    
    // Dynamic color operations - core business feature
    Task<bool> ChangeShoeColorAsync(ChangeColorDto changeColorDto);
    Task<IEnumerable<string>> GetAvailableColorsAsync(int shoeId);
    Task<IEnumerable<ShoeColorVariationDto>> GetColorVariationsAsync(int shoeId);
    Task<ShoeColorVariationDto> CreateColorVariationAsync(CreateColorVariationDto createDto);
    
    // Configuration and diagnostic operations
    string GetDataSourceInfo();
    Task<bool> TestConnectionAsync();
    
    // Business analytics (future extension points)
    Task<IEnumerable<ShoeDto>> GetPopularShoesAsync();
    Task<IEnumerable<ShoeDto>> GetLowStockShoesAsync();
}
```

### Step 5.3: Service Implementation

#### Create `Services/ShoeService.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoeShop.Repository.Interfaces;
using ShoeShop.Repository.Models;
using ShoeShop.Services.DTOs;
using ShoeShop.Services.Interfaces;

namespace ShoeShop.Services.Services;

/// <summary>
/// Implementation of IShoeService
/// Encapsulates business logic and orchestrates repository operations
/// </summary>
public class ShoeService : IShoeService
{
    private readonly IShoeRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShoeService> _logger;
    private readonly string _currentCountry;

    public ShoeService(
        IShoeRepository repository, 
        IConfiguration configuration,
        ILogger<ShoeService> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
        _currentCountry = _configuration["AppSettings:Country"] ?? "US";
    }

    /// <summary>
    /// Gets all shoes with enriched data
    /// Business logic: enriches DTOs with available colors
    /// </summary>
    public async Task<IEnumerable<ShoeDto>> GetAllShoesAsync()
    {
        _logger.LogInformation("Retrieving all shoes");
        
        var shoes = await _repository.GetAllShoesAsync();
        var result = new List<ShoeDto>();

        foreach (var shoe in shoes)
        {
            var shoeDto = MapToShoeDto(shoe);
            // Business logic: enrich with available colors
            shoeDto.AvailableColors = (await _repository.GetAvailableColorsAsync(shoe.Id)).ToList();
            result.Add(shoeDto);
        }

        _logger.LogInformation("Retrieved {Count} shoes", result.Count);
        return result;
    }

    /// <summary>
    /// Gets a specific shoe by ID with enriched data
    /// </summary>
    public async Task<ShoeDto?> GetShoeByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving shoe with ID {ShoeId}", id);
        
        var shoe = await _repository.GetShoeByIdAsync(id);
        if (shoe == null)
        {
            _logger.LogWarning("Shoe with ID {ShoeId} not found", id);
            return null;
        }

        var shoeDto = MapToShoeDto(shoe);
        shoeDto.AvailableColors = (await _repository.GetAvailableColorsAsync(id)).ToList();
        
        return shoeDto;
    }

    /// <summary>
    /// Searches shoes by brand
    /// </summary>
    public async Task<IEnumerable<ShoeDto>> GetShoesByBrandAsync(string brand)
    {
        _logger.LogInformation("Searching shoes by brand: {Brand}", brand);
        
        var shoes = await _repository.GetShoesByBrandAsync(brand);
        var result = new List<ShoeDto>();

        foreach (var shoe in shoes)
        {
            var shoeDto = MapToShoeDto(shoe);
            shoeDto.AvailableColors = (await _repository.GetAvailableColorsAsync(shoe.Id)).ToList();
            result.Add(shoeDto);
        }

        return result;
    }

    /// <summary>
    /// Full-text search across shoes
    /// </summary>
    public async Task<IEnumerable<ShoeDto>> SearchShoesAsync(string searchTerm)
    {
        _logger.LogInformation("Searching shoes with term: {SearchTerm}", searchTerm);
        
        var shoes = await _repository.SearchShoesAsync(searchTerm);
        var result = new List<ShoeDto>();

        foreach (var shoe in shoes)
        {
            var shoeDto = MapToShoeDto(shoe);
            shoeDto.AvailableColors = (await _repository.GetAvailableColorsAsync(shoe.Id)).ToList();
            result.Add(shoeDto);
        }

        return result;
    }

    /// <summary>
    /// Creates a new shoe with business validation and rules
    /// </summary>
    public async Task<ShoeDto> CreateShoeAsync(CreateShoeDto createShoeDto)
    {
        _logger.LogInformation("Creating new shoe: {ShoeName}", createShoeDto.Name);
        
        // Business validation
        await ValidateCreateShoeAsync(createShoeDto);

        // Map DTO to domain model
        var shoe = MapToShoe(createShoeDto);

        // Repository operation
        var createdShoe = await _repository.CreateShoeAsync(shoe);
        
        // Business rule: Create default color variation
        await CreateDefaultColorVariationAsync(createdShoe.Id, createShoeDto.BaseColor);

        _logger.LogInformation("Created shoe with ID {ShoeId}", createdShoe.Id);
        
        return MapToShoeDto(createdShoe);
    }

    /// <summary>
    /// Updates an existing shoe
    /// </summary>
    public async Task<ShoeDto> UpdateShoeAsync(UpdateShoeDto updateShoeDto)
    {
        _logger.LogInformation("Updating shoe with ID {ShoeId}", updateShoeDto.Id);
        
        // Business validation
        var existingShoe = await _repository.GetShoeByIdAsync(updateShoeDto.Id);
        if (existingShoe == null)
        {
            _logger.LogWarning("Shoe with ID {ShoeId} not found for update", updateShoeDto.Id);
            throw new ArgumentException($"Shoe with ID {updateShoeDto.Id} not found");
        }

        // Apply updates
        MapUpdateToShoe(updateShoeDto, existingShoe);

        // Repository operation
        var updatedShoe = await _repository.UpdateShoeAsync(existingShoe);
        
        _logger.LogInformation("Updated shoe with ID {ShoeId}", updatedShoe.Id);
        
        return MapToShoeDto(updatedShoe);
    }

    /// <summary>
    /// Deletes a shoe (soft delete)
    /// </summary>
    public async Task<bool> DeleteShoeAsync(int id)
    {
        _logger.LogInformation("Deleting shoe with ID {ShoeId}", id);
        
        var result = await _repository.DeleteShoeAsync(id);
        
        if (result)
            _logger.LogInformation("Deleted shoe with ID {ShoeId}", id);
        else
            _logger.LogWarning("Failed to delete shoe with ID {ShoeId}", id);
            
        return result;
    }

    /// <summary>
    /// Checks if a shoe exists
    /// </summary>
    public async Task<bool> ShoeExistsAsync(int id)
    {
        return await _repository.ShoeExistsAsync(id);
    }

    // Dynamic Color Operations

    /// <summary>
    /// Changes shoe color - core business feature
    /// Includes business validation
    /// </summary>
    public async Task<bool> ChangeShoeColorAsync(ChangeColorDto changeColorDto)
    {
        _logger.LogInformation("Changing color for shoe {ShoeId} to {Color}", 
            changeColorDto.ShoeId, changeColorDto.ColorName);
        
        // Business validation: ensure color is available
        var availableColors = await _repository.GetAvailableColorsAsync(changeColorDto.ShoeId);
        if (!availableColors.Contains(changeColorDto.ColorName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Color {Color} not available for shoe {ShoeId}", 
                changeColorDto.ColorName, changeColorDto.ShoeId);
            return false;
        }

        var result = await _repository.ChangeShoeColorAsync(changeColorDto.ShoeId, changeColorDto.ColorName);
        
        if (result)
            _logger.LogInformation("Successfully changed color for shoe {ShoeId} to {Color}", 
                changeColorDto.ShoeId, changeColorDto.ColorName);
        else
            _logger.LogWarning("Failed to change color for shoe {ShoeId} to {Color}", 
                changeColorDto.ShoeId, changeColorDto.ColorName);
        
        return result;
    }

    /// <summary>
    /// Gets available colors for a shoe
    /// </summary>
    public async Task<IEnumerable<string>> GetAvailableColorsAsync(int shoeId)
    {
        return await _repository.GetAvailableColorsAsync(shoeId);
    }

    /// <summary>
    /// Gets color variations for a shoe
    /// </summary>
    public async Task<IEnumerable<ShoeColorVariationDto>> GetColorVariationsAsync(int shoeId)
    {
        var colorVariations = await _repository.GetColorVariationsAsync(shoeId);
        return colorVariations.Select(MapToColorVariationDto);
    }

    /// <summary>
    /// Creates a new color variation
    /// </summary>
    public async Task<ShoeColorVariationDto> CreateColorVariationAsync(CreateColorVariationDto createDto)
    {
        _logger.LogInformation("Creating color variation {Color} for shoe {ShoeId}", 
            createDto.ColorName, createDto.ShoeId);
        
        // Business validation: ensure shoe exists
        if (!await _repository.ShoeExistsAsync(createDto.ShoeId))
        {
            throw new ArgumentException($"Shoe with ID {createDto.ShoeId} not found");
        }

        var colorVariation = new ShoeColorVariation
        {
            ShoeId = createDto.ShoeId,
            ColorName = createDto.ColorName,
            HexCode = createDto.HexCode,
            StockQuantity = createDto.StockQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateColorVariationAsync(colorVariation);
        return MapToColorVariationDto(created);
    }

    // Configuration and Diagnostic Operations

    /// <summary>
    /// Gets information about the current data source
    /// Configuration-aware business logic
    /// </summary>
    public string GetDataSourceInfo()
    {
        var dataSourceType = _currentCountry.ToUpper() switch
        {
            "PH" => "SQLite (Persistent)",
            "US" => "In-Memory (Fast)",
            _ => "In-Memory (Default)"
        };
        
        return $"Country: {_currentCountry}, Data Source: {dataSourceType}";
    }

    /// <summary>
    /// Tests database connectivity
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing database connection");
            await _repository.GetAllShoesAsync();
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    // Future business analytics methods
    public async Task<IEnumerable<ShoeDto>> GetPopularShoesAsync()
    {
        // TODO: Implement popularity algorithm based on color changes, views, etc.
        _logger.LogInformation("Getting popular shoes (placeholder implementation)");
        return await GetAllShoesAsync();
    }

    public async Task<IEnumerable<ShoeDto>> GetLowStockShoesAsync()
    {
        // TODO: Implement low stock detection across color variations
        _logger.LogInformation("Getting low stock shoes (placeholder implementation)");
        return await GetAllShoesAsync();
    }

    // Private Helper Methods

    /// <summary>
    /// Business validation for shoe creation
    /// </summary>
    private async Task ValidateCreateShoeAsync(CreateShoeDto createShoeDto)
    {
        if (string.IsNullOrWhiteSpace(createShoeDto.Name))
            throw new ArgumentException("Shoe name is required", nameof(createShoeDto.Name));

        if (createShoeDto.Price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(createShoeDto.Price));

        // Business rule: Check for duplicate names within same brand
        var existingShoes = await _repository.GetShoesByBrandAsync(createShoeDto.Brand);
        if (existingShoes.Any(s => s.Name.Equals(createShoeDto.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"A shoe with name '{createShoeDto.Name}' already exists for brand '{createShoeDto.Brand}'");
        }
    }

    /// <summary>
    /// Creates default color variation for new shoes
    /// Business rule: every shoe must have at least one color variation
    /// </summary>
    private async Task CreateDefaultColorVariationAsync(int shoeId, string baseColor)
    {
        var colorVariation = new ShoeColorVariation
        {
            ShoeId = shoeId,
            ColorName = baseColor,
            HexCode = GetDefaultHexCode(baseColor),
            StockQuantity = 10, // Default stock
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateColorVariationAsync(colorVariation);
    }

    /// <summary>
    /// Maps color names to hex codes
    /// Business logic for color representation
    /// </summary>
    private string GetDefaultHexCode(string colorName)
    {
        return colorName.ToLower() switch
        {
            "white" => "#FFFFFF",
            "black" => "#000000",
            "red" => "#FF0000",
            "blue" => "#0000FF",
            "green" => "#008000",
            "yellow" => "#FFFF00",
            "orange" => "#FFA500",
            "purple" => "#800080",
            "pink" => "#FFC0CB",
            "brown" => "#A52A2A",
            "gray" or "grey" => "#808080",
            "navy" => "#000080",
            "beige" => "#F5F5DC",
            "silver" => "#C0C0C0",
            _ => "#808080" // Default to gray
        };
    }

    // Mapping Methods (DTO ↔ Entity conversions)

    /// <summary>
    /// Maps domain entity to DTO
    /// Controls data exposure and adds computed properties
    /// </summary>
    private ShoeDto MapToShoeDto(Shoe shoe)
    {
        return new ShoeDto
        {
            Id = shoe.Id,
            Name = shoe.Name,
            Brand = shoe.Brand,
            Size = shoe.Size,
            BaseColor = shoe.BaseColor,
            CurrentColor = shoe.CurrentColor,
            Price = shoe.Price,
            Description = shoe.Description,
            ImageUrl = shoe.ImageUrl,
            IsAvailable = shoe.IsAvailable,
            CreatedAt = shoe.CreatedAt,
            UpdatedAt = shoe.UpdatedAt,
            ColorVariations = shoe.ColorVariations.Select(MapToColorVariationDto).ToList()
            // AvailableColors is populated separately for performance reasons
        };
    }

    /// <summary>
    /// Maps color variation entity to DTO
    /// </summary>
    private ShoeColorVariationDto MapToColorVariationDto(ShoeColorVariation cv)
    {
        return new ShoeColorVariationDto
        {
            Id = cv.Id,
            ShoeId = cv.ShoeId,
            ColorName = cv.ColorName,
            HexCode = cv.HexCode,
            StockQuantity = cv.StockQuantity,
            IsActive = cv.IsActive,
            CreatedAt = cv.CreatedAt
        };
    }

    /// <summary>
    /// Maps CreateShoeDto to domain entity
    /// </summary>
    private Shoe MapToShoe(CreateShoeDto createDto)
    {
        return new Shoe
        {
            Name = createDto.Name,
            Brand = createDto.Brand,
            Size = createDto.Size,
            BaseColor = createDto.BaseColor,
            CurrentColor = createDto.BaseColor, // Start with base color
            Price = createDto.Price,
            Description = createDto.Description,
            ImageUrl = createDto.ImageUrl,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps UpdateShoeDto to existing domain entity
    /// </summary>
    private void MapUpdateToShoe(UpdateShoeDto updateDto, Shoe shoe)
    {
        shoe.Name = updateDto.Name;
        shoe.Brand = updateDto.Brand;
        shoe.Size = updateDto.Size;
        shoe.BaseColor = updateDto.BaseColor;
        shoe.Price = updateDto.Price;
        shoe.Description = updateDto.Description;
        shoe.ImageUrl = updateDto.ImageUrl;
        shoe.IsAvailable = updateDto.IsAvailable;
        // UpdatedAt is set by repository
    }
}
```

---

## 6. ShoeShop.Web Project

### Step 6.1: Configuration Setup

#### Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=app.db;Cache=Shared",
    "ShoeShopConnection": "Data Source=shoeshop.db"
  },
  "AppSettings": {
    "Country": "US"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ShoeShop": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

#### Create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=app_dev.db;Cache=Shared",
    "ShoeShopConnection": "Data Source=shoeshop_dev.db"
  },
  "AppSettings": {
    "Country": "US"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "ShoeShop": "Trace"
    }
  }
}
```

### Step 6.2: Controllers

#### Create `Controllers/ShoesController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoeShop.Services.Interfaces;
using ShoeShop.Services.DTOs;

namespace ShoeShop.Web.Controllers;

/// <summary>
/// MVC Controller for shoe operations
/// 
/// CONTROLLER RESPONSIBILITIES:
/// ===========================
/// 1. HTTP request/response handling
/// 2. Input validation and model binding
/// 3. Authentication and authorization
/// 4. View selection and data passing
/// 5. Error handling and user feedback
/// 
/// SEPARATION OF CONCERNS:
/// ======================
/// - Controller: HTTP concerns only
/// - Service: Business logic
/// - Repository: Data access
/// </summary>
public class ShoesController : Controller
{
    private readonly IShoeService _shoeService;
    private readonly ILogger<ShoesController> _logger;

    public ShoesController(IShoeService shoeService, ILogger<ShoesController> logger)
    {
        _shoeService = shoeService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the main shoes catalog with search functionality
    /// GET: /Shoes
    /// GET: /Shoes?brand=Nike
    /// GET: /Shoes?searchTerm=running
    /// </summary>
    public async Task<IActionResult> Index(string? brand, string? searchTerm)
    {
        try
        {
            _logger.LogInformation("Loading shoes index page. Brand: {Brand}, SearchTerm: {SearchTerm}", 
                brand, searchTerm);
            
            IEnumerable<ShoeDto> shoes;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                shoes = await _shoeService.SearchShoesAsync(searchTerm);
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SearchResultsCount = shoes.Count();
            }
            else if (!string.IsNullOrEmpty(brand))
            {
                shoes = await _shoeService.GetShoesByBrandAsync(brand);
                ViewBag.Brand = brand;
                ViewBag.FilteredByBrand = brand;
            }
            else
            {
                shoes = await _shoeService.GetAllShoesAsync();
            }

            // Pass configuration info to view for display
            ViewBag.DataSource = _shoeService.GetDataSourceInfo();
            
            return View(shoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shoes index page");
            TempData["ErrorMessage"] = "An error occurred while loading the shoes. Please try again.";
            return View(new List<ShoeDto>());
        }
    }

    /// <summary>
    /// Displays detailed view of a specific shoe
    /// GET: /Shoes/Details/5
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            _logger.LogInformation("Loading shoe details for ID {ShoeId}", id);
            
            var shoe = await _shoeService.GetShoeByIdAsync(id);
            if (shoe == null)
            {
                _logger.LogWarning("Shoe with ID {ShoeId} not found", id);
                TempData["ErrorMessage"] = "Shoe not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get available colors for the color change feature
            ViewBag.AvailableColors = await _shoeService.GetAvailableColorsAsync(id);
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
            
            return View(shoe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shoe details for ID {ShoeId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the shoe details.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Handles dynamic color changing
    /// POST: /Shoes/ChangeColor
    /// Requires authentication
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeColor(ChangeColorDto changeColorDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for color change request. ShoeId: {ShoeId}, Color: {Color}", 
                    changeColorDto.ShoeId, changeColorDto.ColorName);
                TempData["ErrorMessage"] = "Invalid data provided.";
                return RedirectToAction("Details", new { id = changeColorDto.ShoeId });
            }

            _logger.LogInformation("User {UserId} changing color for shoe {ShoeId} to {Color}", 
                User.Identity?.Name, changeColorDto.ShoeId, changeColorDto.ColorName);

            var success = await _shoeService.ChangeShoeColorAsync(changeColorDto);
            if (success)
            {
                TempData["SuccessMessage"] = $"Shoe color changed to {changeColorDto.ColorName} successfully!";
                _logger.LogInformation("Successfully changed color for shoe {ShoeId} to {Color}", 
                    changeColorDto.ShoeId, changeColorDto.ColorName);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to change shoe color. Please ensure the color is available.";
                _logger.LogWarning("Failed to change color for shoe {ShoeId} to {Color}", 
                    changeColorDto.ShoeId, changeColorDto.ColorName);
            }
            
            return RedirectToAction("Details", new { id = changeColorDto.ShoeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing color for shoe {ShoeId} to {Color}", 
                changeColorDto.ShoeId, changeColorDto.ColorName);
            TempData["ErrorMessage"] = "An error occurred while changing the shoe color.";
            return RedirectToAction("Details", new { id = changeColorDto.ShoeId });
        }
    }

    /// <summary>
    /// Displays form for creating a new shoe
    /// GET: /Shoes/Create
    /// Requires authentication
    /// </summary>
    [Authorize]
    public IActionResult Create()
    {
        return View(new CreateShoeDto());
    }

    /// <summary>
    /// Handles new shoe creation
    /// POST: /Shoes/Create
    /// Requires authentication
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShoeDto createShoeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for shoe creation. Name: {ShoeName}", createShoeDto.Name);
                return View(createShoeDto);
            }

            _logger.LogInformation("User {UserId} creating new shoe: {ShoeName}", 
                User.Identity?.Name, createShoeDto.Name);

            var createdShoe = await _shoeService.CreateShoeAsync(createShoeDto);
            
            TempData["SuccessMessage"] = $"Shoe '{createdShoe.Name}' created successfully!";
            _logger.LogInformation("Successfully created shoe with ID {ShoeId}", createdShoe.Id);
            
            return RedirectToAction("Details", new { id = createdShoe.Id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Business validation error creating shoe: {ShoeName}", createShoeDto.Name);
            ModelState.AddModelError("", ex.Message);
            return View(createShoeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shoe: {ShoeName}", createShoeDto.Name);
            ModelState.AddModelError("", "An error occurred while creating the shoe. Please try again.");
            return View(createShoeDto);
        }
    }

    /// <summary>
    /// Displays form for editing an existing shoe
    /// GET: /Shoes/Edit/5
    /// Requires authentication
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var shoe = await _shoeService.GetShoeByIdAsync(id);
            if (shoe == null)
            {
                TempData["ErrorMessage"] = "Shoe not found.";
                return RedirectToAction(nameof(Index));
            }

            var updateDto = new UpdateShoeDto
            {
                Id = shoe.Id,
                Name = shoe.Name,
                Brand = shoe.Brand,
                Size = shoe.Size,
                BaseColor = shoe.BaseColor,
                Price = shoe.Price,
                Description = shoe.Description,
                ImageUrl = shoe.ImageUrl,
                IsAvailable = shoe.IsAvailable
            };

            return View(updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shoe for edit. ID: {ShoeId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the shoe.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Handles shoe updates
    /// POST: /Shoes/Edit/5
    /// Requires authentication
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateShoeDto updateShoeDto)
    {
        if (id != updateShoeDto.Id)
        {
            _logger.LogWarning("ID mismatch in shoe edit request. URL ID: {UrlId}, DTO ID: {DtoId}", 
                id, updateShoeDto.Id);
            return NotFound();
        }

        try
        {
            if (!ModelState.IsValid)
            {
                return View(updateShoeDto);
            }

            _logger.LogInformation("User {UserId} updating shoe {ShoeId}", 
                User.Identity?.Name, updateShoeDto.Id);

            var updatedShoe = await _shoeService.UpdateShoeAsync(updateShoeDto);
            
            TempData["SuccessMessage"] = $"Shoe '{updatedShoe.Name}' updated successfully!";
            _logger.LogInformation("Successfully updated shoe {ShoeId}", updatedShoe.Id);
            
            return RedirectToAction("Details", new { id = updatedShoe.Id });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Business validation error updating shoe {ShoeId}", updateShoeDto.Id);
            ModelState.AddModelError("", ex.Message);
            return View(updateShoeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shoe {ShoeId}", updateShoeDto.Id);
            ModelState.AddModelError("", "An error occurred while updating the shoe. Please try again.");
            return View(updateShoeDto);
        }
    }

    /// <summary>
    /// API endpoint for AJAX color changes
    /// POST: /api/shoes/{id}/change-color
    /// Returns JSON response for single-page app scenarios
    /// </summary>
    [Authorize]
    [HttpPost]
    [Route("api/shoes/{id}/change-color")]
    public async Task<IActionResult> ChangeColorApi(int id, [FromBody] ChangeColorDto changeColorDto)
    {
        try
        {
            changeColorDto.ShoeId = id; // Ensure consistency
            
            var success = await _shoeService.ChangeShoeColorAsync(changeColorDto);
            if (success)
            {
                var updatedShoe = await _shoeService.GetShoeByIdAsync(id);
                return Json(new { 
                    success = true, 
                    newColor = updatedShoe?.CurrentColor,
                    message = $"Color changed to {changeColorDto.ColorName} successfully!" 
                });
            }
            else
            {
                return Json(new { 
                    success = false, 
                    message = "Failed to change shoe color. Please ensure the color is available." 
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in color change API for shoe {ShoeId}", id);
            return Json(new { 
                success = false, 
                message = "An error occurred while changing the shoe color." 
            });
        }
    }

    /// <summary>
    /// API endpoint to get shoe data as JSON
    /// GET: /api/shoes/{id}
    /// Useful for AJAX requests and mobile apps
    /// </summary>
    [HttpGet]
    [Route("api/shoes/{id}")]
    public async Task<IActionResult> GetShoeApi(int id)
    {
        try
        {
            var shoe = await _shoeService.GetShoeByIdAsync(id);
            if (shoe == null)
            {
                return NotFound(new { message = "Shoe not found" });
            }

            return Json(shoe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shoe data for API. ID: {ShoeId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
```

### Step 6.3: Views

#### Create `Views/Shoes/Index.cshtml`:

```html
@model IEnumerable<ShoeShop.Services.DTOs.ShoeDto>
@{
    ViewData["Title"] = "Shoe Collection";
    var hasSearch = !string.IsNullOrEmpty(ViewBag.SearchTerm as string);
    var hasBrandFilter = !string.IsNullOrEmpty(ViewBag.Brand as string);
}

<div class="container-fluid">
    <div class="row">
        <div class="col-12">
            <!-- Page Header -->
            <div class="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <h1 class="h2 mb-1">@ViewData["Title"]</h1>
                    @if (ViewBag.DataSource != null)
                    {
                        <small class="text-muted">
                            <i class="fas fa-database me-1"></i>@ViewBag.DataSource
                        </small>
                    }
                </div>
                
                @if (User.Identity.IsAuthenticated)
                {
                    <a href="@Url.Action("Create")" class="btn btn-success">
                        <i class="fas fa-plus me-2"></i>Add New Shoe
                    </a>
                }
            </div>

            <!-- Search and Filter Bar -->
            <div class="card mb-4">
                <div class="card-body">
                    <form method="get" class="row g-3">
                        <div class="col-md-8">
                            <div class="input-group">
                                <span class="input-group-text">
                                    <i class="fas fa-search"></i>
                                </span>
                                <input type="text" 
                                       name="searchTerm" 
                                       value="@ViewBag.SearchTerm" 
                                       class="form-control" 
                                       placeholder="Search by name, brand, or description..."
                                       aria-label="Search shoes">
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="d-grid gap-2 d-md-flex">
                                <button type="submit" class="btn btn-primary flex-fill">
                                    <i class="fas fa-search me-1"></i>Search
                                </button>
                                <a href="@Url.Action("Index")" class="btn btn-outline-secondary flex-fill">
                                    <i class="fas fa-times me-1"></i>Clear
                                </a>
                            </div>
                        </div>
                    </form>
                    
                    <!-- Active Filters -->
                    @if (hasSearch || hasBrandFilter)
                    {
                        <div class="mt-3">
                            <small class="text-muted me-2">Active filters:</small>
                            @if (hasSearch)
                            {
                                <span class="badge bg-primary me-2">
                                    Search: "@ViewBag.SearchTerm"
                                    <a href="@Url.Action("Index", new { brand = ViewBag.Brand })" 
                                       class="text-white text-decoration-none ms-1">×</a>
                                </span>
                            }
                            @if (hasBrandFilter)
                            {
                                <span class="badge bg-secondary me-2">
                                    Brand: @ViewBag.Brand
                                    <a href="@Url.Action("Index", new { searchTerm = ViewBag.SearchTerm })" 
                                       class="text-white text-decoration-none ms-1">×</a>
                                </span>
                            }
                        </div>
                    }
                </div>
            </div>

            <!-- Results Info -->
            @if (Model.Any())
            {
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <span class="text-muted">
                        Showing @Model.Count() shoe@(Model.Count() != 1 ? "s" : "")
                        @if (hasSearch)
                        {
                            <text> matching "@ViewBag.SearchTerm"</text>
                        }
                        @if (hasBrandFilter)
                        {
                            <text> from @ViewBag.Brand</text>
                        }
                    </span>
                </div>
            }

            <!-- Shoes Grid -->
            @if (Model.Any())
            {
                <div class="row row-cols-1 row-cols-md-2 row-cols-lg-3 row-cols-xl-4 g-4">
                    @foreach (var shoe in Model)
                    {
                        <div class="col">
                            <div class="card h-100 shoe-card" data-shoe-id="@shoe.Id">
                                <!-- Shoe Image -->
                                <div class="position-relative">
                                    @if (!string.IsNullOrEmpty(shoe.ImageUrl))
                                    {
                                        <img src="@shoe.ImageUrl" 
                                             class="card-img-top shoe-image" 
                                             alt="@shoe.Name"
                                             style="height: 200px; object-fit: cover;">
                                    }
                                    else
                                    {
                                        <div class="card-img-top bg-light d-flex align-items-center justify-content-center" 
                                             style="height: 200px;">
                                            <i class="fas fa-shoe-prints fa-3x text-muted"></i>
                                        </div>
                                    }
                                    
                                    <!-- Availability Badge -->
                                    @if (!shoe.IsAvailable)
                                    {
                                        <div class="position-absolute top-0 start-0 m-2">
                                            <span class="badge bg-danger">Unavailable</span>
                                        </div>
                                    }
                                    
                                    <!-- Price Badge -->
                                    <div class="position-absolute top-0 end-0 m-2">
                                        <span class="badge bg-success fs-6">$@shoe.Price</span>
                                    </div>
                                </div>
                                
                                <!-- Card Body -->
                                <div class="card-body d-flex flex-column">
                                    <h5 class="card-title mb-1">@shoe.Name</h5>
                                    <h6 class="card-subtitle mb-2">
                                        <a href="@Url.Action("Index", new { brand = shoe.Brand })" 
                                           class="text-muted text-decoration-none">
                                            @shoe.Brand
                                        </a>
                                    </h6>
                                    
                                    @if (!string.IsNullOrEmpty(shoe.Description))
                                    {
                                        <p class="card-text flex-grow-1 small">
                                            @(shoe.Description.Length > 100 
                                                ? shoe.Description.Substring(0, 100) + "..." 
                                                : shoe.Description)
                                        </p>
                                    }
                                    
                                    <!-- Shoe Details -->
                                    <div class="mt-auto">
                                        <div class="row g-2 mb-2">
                                            <div class="col-6">
                                                <small class="text-muted">Size:</small>
                                                <div class="badge bg-primary">@shoe.Size</div>
                                            </div>
                                            <div class="col-6">
                                                <small class="text-muted">Current Color:</small>
                                                <div class="badge bg-secondary">@shoe.CurrentColor</div>
                                            </div>
                                        </div>
                                        
                                        <div class="mb-2">
                                            <small class="text-muted">
                                                @shoe.AvailableColors.Count available color@(shoe.AvailableColors.Count != 1 ? "s" : "")
                                            </small>
                                            @if (shoe.AvailableColors.Any())
                                            {
                                                <div class="mt-1">
                                                    @foreach (var color in shoe.AvailableColors.Take(5))
                                                    {
                                                        <span class="badge bg-light text-dark me-1 small">@color</span>
                                                    }
                                                    @if (shoe.AvailableColors.Count > 5)
                                                    {
                                                        <small class="text-muted">+@(shoe.AvailableColors.Count - 5) more</small>
                                                    }
                                                </div>
                                            }
                                        </div>
                                        
                                        <!-- Action Buttons -->
                                        <div class="d-grid gap-2">
                                            <a href="@Url.Action("Details", new { id = shoe.Id })" 
                                               class="btn btn-primary btn-sm">
                                                <i class="fas fa-eye me-1"></i>View Details
                                            </a>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    }
                </div>
            }
            else
            {
                <!-- Empty State -->
                <div class="text-center py-5">
                    <div class="mb-4">
                        <i class="fas fa-shoe-prints fa-5x text-muted"></i>
                    </div>
                    <h3 class="text-muted mb-3">No shoes found</h3>
                    @if (hasSearch || hasBrandFilter)
                    {
                        <p class="text-muted mb-3">
                            Try adjusting your search criteria or 
                            <a href="@Url.Action("Index")" class="text-decoration-none">view all shoes</a>.
                        </p>
                    }
                    else
                    {
                        <p class="text-muted mb-3">There are no shoes in the collection yet.</p>
                        @if (User.Identity.IsAuthenticated)
                        {
                            <a href="@Url.Action("Create")" class="btn btn-success">
                                <i class="fas fa-plus me-2"></i>Add the first shoe
                            </a>
                        }
                    }
                </div>
            }
        </div>
    </div>
</div>

<!-- Success/Error Messages -->
@if (TempData["SuccessMessage"] != null)
{
    <div class="toast-container position-fixed bottom-0 end-0 p-3">
        <div class="toast show" role="alert" data-bs-autohide="true" data-bs-delay="5000">
            <div class="toast-header bg-success text-white">
                <i class="fas fa-check-circle me-2"></i>
                <strong class="me-auto">Success</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                @TempData["SuccessMessage"]
            </div>
        </div>
    </div>
}

@if (TempData["ErrorMessage"] != null)
{
    <div class="toast-container position-fixed bottom-0 end-0 p-3">
        <div class="toast show" role="alert" data-bs-autohide="true" data-bs-delay="5000">
            <div class="toast-header bg-danger text-white">
                <i class="fas fa-exclamation-circle me-2"></i>
                <strong class="me-auto">Error</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                @TempData["ErrorMessage"]
            </div>
        </div>
    </div>
}

@section Scripts {
    <script>
        // Initialize tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl)
        });

        // Auto-hide toasts
        document.addEventListener('DOMContentLoaded', function() {
            var toastElList = [].slice.call(document.querySelectorAll('.toast'))
            var toastList = toastElList.map(function (toastEl) {
                return new bootstrap.Toast(toastEl)
            });
        });
    </script>
}

@section Styles {
    <style>
        .shoe-card {
            transition: transform 0.2s ease-in-out, box-shadow 0.2s ease-in-out;
        }
        
        .shoe-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
        }
        
        .shoe-image {
            transition: transform 0.3s ease-in-out;
        }
        
        .shoe-card:hover .shoe-image {
            transform: scale(1.05);
        }
        
        .badge {
            font-size: 0.75em;
        }
    </style>
}
```

---

## 7. Configuration and Dependency Injection

### Step 7.1: Complete Program.cs Setup

#### Update `ShoeShop.Web/Program.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShoeShop.Web.Data;
using ShoeShop.Repository.Data;
using ShoeShop.Repository.Interfaces;
using ShoeShop.Repository.Repositories;
using ShoeShop.Services.Interfaces;
using ShoeShop.Services.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/shoeshop-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting ShoeShop application");

    // 1. IDENTITY SETUP - Authentication DbContext
    var identityConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(identityConnectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    {
        // Password settings
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        
        // User settings
        options.User.AllowedUserNameCharacters = 
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;
        
        // Sign in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // 2. BUSINESS DATA SETUP - Configuration-based DbContext
    var country = builder.Configuration["AppSettings:Country"] ?? "US";
    Log.Information("Configuring database for country: {Country}", country);

    if (country.ToUpper() == "PH")
    {
        // SQLite for Philippines - Persistent data
        var shoeShopConnectionString = builder.Configuration.GetConnectionString("ShoeShopConnection") 
            ?? "Data Source=shoeshop.db";
            
        Log.Information("Using SQLite database: {ConnectionString}", shoeShopConnectionString);
        
        builder.Services.AddDbContext<ShoeShopDbContext>(options =>
        {
            options.UseSqlite(shoeShopConnectionString);
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }
    else
    {
        // In-Memory for US and others - Fast development
        Log.Information("Using In-Memory database for development");
        
        builder.Services.AddDbContext<ShoeShopDbContext>(options =>
        {
            options.UseInMemoryDatabase("ShoeShopInMemoryDb");
            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
    }

    // 3. DEPENDENCY INJECTION - Register services
    // Repository Pattern - Interface to Implementation mapping
    builder.Services.AddScoped<IShoeRepository, ShoeRepository>();

    // Service Layer - Interface to Implementation mapping
    builder.Services.AddScoped<IShoeService, ShoeService>();

    // 4. ADDITIONAL SERVICES
    // Memory cache for performance
    builder.Services.AddMemoryCache();
    
    // HTTP context accessor for services that need it
    builder.Services.AddHttpContextAccessor();
    
    // Configure cookie policy
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.CheckConsentNeeded = context => false; // GDPR compliance
        options.MinimumSameSitePolicy = SameSiteMode.None;
    });

    // 5. MVC SETUP
    builder.Services.AddControllersWithViews(options =>
    {
        // Global filters
        if (!builder.Environment.IsDevelopment())
        {
            options.Filters.Add(new Microsoft.AspNetCore.Mvc.RequireHttpsAttribute());
        }
    });
    
    // Configure JSON options for API endpoints
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

    // 6. HEALTH CHECKS
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("identity-db")
        .AddDbContextCheck<ShoeShopDbContext>("business-db");

    var app = builder.Build();

    // 7. HTTP PIPELINE CONFIGURATION
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts(); // HTTP Strict Transport Security
    }

    // Security headers
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCookiePolicy();

    app.UseRouting();

    // 8. AUTHENTICATION & AUTHORIZATION
    app.UseAuthentication();  // Who is the user?
    app.UseAuthorization();   // What can they do?

    // 9. ENDPOINT MAPPING
    app.MapStaticAssets();

    // MVC routes
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    // Identity pages (login, register, etc.)
    app.MapRazorPages()
       .WithStaticAssets();

    // Health checks endpoint
    app.MapHealthChecks("/health");

    // API routes for AJAX/mobile apps
    app.MapControllerRoute(
        name: "api",
        pattern: "api/{controller}/{action=Index}/{id?}");

    // 10. DATABASE INITIALIZATION
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Initialize Identity database
            var identityContext = services.GetRequiredService<ApplicationDbContext>();
            await identityContext.Database.EnsureCreatedAsync();
            Log.Information("Identity database initialized");

            // Initialize business database
            var businessContext = services.GetRequiredService<ShoeShopDbContext>();
            
            if (country.ToUpper() != "PH")
            {
                // In-Memory: Ensure created and seeded
                await businessContext.Database.EnsureCreatedAsync();
                Log.Information("In-Memory business database initialized with seed data");
            }
            else
            {
                // SQLite: Use migrations
                await businessContext.Database.MigrateAsync();
                Log.Information("SQLite business database migrated");
            }
            
            // Test database connectivity
            var shoeService = services.GetRequiredService<IShoeService>();
            var connectionTest = await shoeService.TestConnectionAsync();
            Log.Information("Database connectivity test: {Status}", connectionTest ? "Passed" : "Failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            Log.Fatal(ex, "Database initialization failed");
            throw; // Re-throw to prevent app startup with invalid database
        }
    }

    Log.Information("ShoeShop application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### Step 7.2: Logging Configuration

#### Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=app.db;Cache=Shared",
    "ShoeShopConnection": "Data Source=shoeshop.db"
  },
  "AppSettings": {
    "Country": "US"
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "ShoeShop": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/shoeshop-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 8. Testing and Running

### Step 8.1: Database Migrations

For SQLite configuration (Country = "PH"):

```powershell
# Navigate to the repository project directory
cd ShoeShop.Repository

# Create initial migration for business data
dotnet ef migrations add InitialCreate --startup-project ../ShoeShop.Web --context ShoeShopDbContext

# Apply migration to create SQLite database
dotnet ef database update --startup-project ../ShoeShop.Web --context ShoeShopDbContext
```

### Step 8.2: Build and Run

```powershell
# Navigate to solution root
cd ShoeShop

# Restore NuGet packages
dotnet restore

# Build the entire solution
dotnet build --configuration Release

# Run the web application
cd ShoeShop.Web
dotnet run --environment Development
```

### Step 8.3: Testing Different Configurations

#### Test In-Memory Database (US):
1. Set `"Country": "US"` in `appsettings.json`
2. Restart the application
3. Navigate to `https://localhost:5001/Shoes`
4. Data will reset on each restart - good for development

#### Test SQLite Database (PH):
1. Set `"Country": "PH"` in `appsettings.json`
2. Run migrations if not already done
3. Restart the application
4. Data will persist between restarts - good for production-like testing

### Step 8.4: Feature Testing Checklist

#### Basic Functionality:
- [ ] Browse shoes catalog at `/Shoes`
- [ ] Search functionality with search box
- [ ] Filter by brand (click on brand names)
- [ ] View individual shoe details
- [ ] See color variations and hex codes
- [ ] Check data source indicator

#### Authentication Required:
- [ ] Register a new user account
- [ ] Login with created account
- [ ] Create a new shoe (authenticated users only)
- [ ] Edit existing shoes (authenticated users only)
- [ ] Change shoe colors (authenticated users only)

#### Dynamic Color Changing:
- [ ] Login as authenticated user
- [ ] Navigate to any shoe details page
- [ ] Click on available color buttons
- [ ] Verify color change is reflected immediately
- [ ] Check that color change persists (SQLite mode)
- [ ] Verify color change resets (In-Memory mode)

#### Configuration Testing:
- [ ] Switch between US and PH configurations
- [ ] Verify different database providers are used
- [ ] Check that data persistence behaves correctly
- [ ] Monitor application logs for database operations

---

## 9. Architecture Patterns Explained

### Repository Pattern Benefits

**Abstraction**: Repository provides a uniform interface to access data, regardless of the underlying storage mechanism.

```csharp
// Client code doesn't know about EF Core, SQL, or database specifics
public class ShoeService
{
    private readonly IShoeRepository _repository; // Abstract dependency
    
    public async Task<ShoeDto> GetShoeByIdAsync(int id)
    {
        var shoe = await _repository.GetShoeByIdAsync(id); // Could be any implementation
        return MapToDto(shoe);
    }
}
```

**Testability**: Easy to create mock implementations for unit testing.

```csharp
[Test]
public async Task GetShoeByIdAsync_ReturnsShoe_WhenShoeExists()
{
    // Arrange
    var mockRepo = new Mock<IShoeRepository>();
    mockRepo.Setup(r => r.GetShoeByIdAsync(1))
           .ReturnsAsync(new Shoe { Id = 1, Name = "Test Shoe" });
    
    var service = new ShoeService(mockRepo.Object, ...);
    
    // Act
    var result = await service.GetShoeByIdAsync(1);
    
    // Assert
    Assert.That(result.Name, Is.EqualTo("Test Shoe"));
}
```

### Service Layer Pattern Benefits

**Business Logic Centralization**: All business rules in one place.

```csharp
public async Task<ShoeDto> CreateShoeAsync(CreateShoeDto createDto)
{
    // Business validation
    if (createDto.Price <= 0)
        throw new ArgumentException("Price must be positive");
    
    // Business rule: Check for duplicates
    var existing = await _repository.GetShoesByBrandAsync(createDto.Brand);
    if (existing.Any(s => s.Name.Equals(createDto.Name, StringComparison.OrdinalIgnoreCase)))
        throw new ArgumentException("Duplicate shoe name for this brand");
    
    // Business rule: Create with default color variation
    var shoe = MapToEntity(createDto);
    var created = await _repository.CreateShoeAsync(shoe);
    await CreateDefaultColorVariationAsync(created.Id, createDto.BaseColor);
    
    return MapToDto(created);
}
```

### DTO Pattern Benefits

**API Evolution**: DTOs can evolve independently of domain models.

```csharp
// V1 DTO - Simple
public class ShoeDto_V1
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// V2 DTO - Enhanced with more data
public class ShoeDto_V2
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public List<string> AvailableColors { get; set; } // New in V2
    public string CurrentColor { get; set; } // New in V2
}

// Domain model remains unchanged
public class Shoe
{
    // Domain model can evolve independently
    // Internal refactoring doesn't break API contracts
}
```

### Dependency Injection Benefits

**Loose Coupling**: Classes depend on abstractions, not concretions.

```csharp
// Without DI - Tight coupling
public class ShoeService
{
    private readonly ShoeRepository _repository; // Concrete dependency
    
    public ShoeService()
    {
        _repository = new ShoeRepository(new ShoeShopDbContext(...)); // Hard-coded
    }
}

// With DI - Loose coupling
public class ShoeService
{
    private readonly IShoeRepository _repository; // Abstract dependency
    
    public ShoeService(IShoeRepository repository) // Injected
    {
        _repository = repository; // Any implementation can be injected
    }
}
```

**Configuration Flexibility**: Different implementations for different environments.

```csharp
// Development: In-Memory repository for fast testing
builder.Services.AddScoped<IShoeRepository, InMemoryShoeRepository>();

// Production: EF Core repository with SQLite
builder.Services.AddScoped<IShoeRepository, EntityFrameworkShoeRepository>();

// Testing: Mock repository
builder.Services.AddScoped<IShoeRepository, MockShoeRepository>();
```

---

## 10. Advanced Implementation Walkthrough

### Polymorphism in Action

The ShoeShop application demonstrates polymorphism at multiple levels:

#### Service Layer Polymorphism

```csharp
// Base service interface
public interface IShoeService
{
    Task<IEnumerable<ShoeDto>> GetAllShoesAsync();
}

// Standard implementation
public class ShoeService : IShoeService
{
    public async Task<IEnumerable<ShoeDto>> GetAllShoesAsync()
    {
        return await _repository.GetAllShoesAsync();
    }
}

// Cached implementation (decorator pattern + polymorphism)
public class CachedShoeService : IShoeService
{
    private readonly IShoeService _innerService;
    private readonly IMemoryCache _cache;
    
    public CachedShoeService(IShoeService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }
    
    public async Task<IEnumerable<ShoeDto>> GetAllShoesAsync()
    {
        const string cacheKey = "all-shoes";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<ShoeDto>? cached))
            return cached!;
        
        var shoes = await _innerService.GetAllShoesAsync();
        _cache.Set(cacheKey, shoes, TimeSpan.FromMinutes(15));
        
        return shoes;
    }
}

// Registration in Program.cs
builder.Services.AddScoped<ShoeService>(); // Concrete service
builder.Services.AddScoped<IShoeService>(provider =>
{
    var baseService = provider.GetRequiredService<ShoeService>();
    var cache = provider.GetRequiredService<IMemoryCache>();
    return new CachedShoeService(baseService, cache); // Wrapped with caching
});
```

#### Repository Layer Polymorphism

```csharp
// Different repository implementations for different scenarios
public class SqliteShoeRepository : IShoeRepository
{
    // SQLite-specific optimizations
    public async Task<IEnumerable<Shoe>> GetAllShoesAsync()
    {
        // SQLite-specific query optimizations
        return await _context.Shoes
            .FromSqlRaw("SELECT * FROM Shoes WHERE IsAvailable = 1 ORDER BY Name COLLATE NOCASE")
            .Include(s => s.ColorVariations)
            .ToListAsync();
    }
}

public class InMemoryShoeRepository : IShoeRepository
{
    // In-memory specific optimizations
    public async Task<IEnumerable<Shoe>> GetAllShoesAsync()
    {
        // Different optimization for in-memory
        return await _context.Shoes
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.Name)
            .Include(s => s.ColorVariations)
            .ToListAsync();
    }
}

// Factory pattern for repository creation
public class ShoeRepositoryFactory
{
    public static IShoeRepository CreateRepository(string databaseType, ShoeShopDbContext context)
    {
        return databaseType.ToLower() switch
        {
            "sqlite" => new SqliteShoeRepository(context),
            "inmemory" => new InMemoryShoeRepository(context),
            _ => new ShoeRepository(context) // Default implementation
        };
    }
}
```

### Configuration-Driven Architecture

The application's architecture adapts based on configuration:

```csharp
// Program.cs - Configuration-driven setup
var country = builder.Configuration["AppSettings:Country"] ?? "US";
var environment = builder.Environment.EnvironmentName;

// Database provider selection
if (country == "PH" && environment == "Production")
{
    // High-performance SQLite with optimizations
    builder.Services.AddDbContext<ShoeShopDbContext>(options =>
        options.UseSqlite(connectionString, sqlite =>
        {
            sqlite.CommandTimeout(30);
        }));
}
else if (environment == "Development")
{
    // Development-friendly in-memory with detailed logging
    builder.Services.AddDbContext<ShoeShopDbContext>(options =>
        options.UseInMemoryDatabase("DevDb")
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors());
}

// Service registration based on environment
if (environment == "Production")
{
    // Production: Cached services, detailed logging, health checks
    builder.Services.Decorate<IShoeService, CachedShoeService>();
    builder.Services.Decorate<IShoeService, LoggingShoeService>();
    builder.Services.AddHealthChecks();
}
else
{
    // Development: Simple services for easier debugging
    builder.Services.AddScoped<IShoeService, ShoeService>();
}
```

### Error Handling Strategy

Comprehensive error handling across all layers:

```csharp
// Repository Layer - Data access errors
public class ShoeRepository : IShoeRepository
{
    public async Task<Shoe> CreateShoeAsync(Shoe shoe)
    {
        try
        {
            _context.Shoes.Add(shoe);
            await _context.SaveChangesAsync();
            return shoe;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true)
        {
            throw new InvalidOperationException($"A shoe with the same name already exists: {shoe.Name}", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Failed to save shoe to database", ex);
        }
    }
}

// Service Layer - Business logic errors
public class ShoeService : IShoeService
{
    public async Task<ShoeDto> CreateShoeAsync(CreateShoeDto createDto)
    {
        try
        {
            // Business validation
            await ValidateCreateShoeAsync(createDto);
            
            var shoe = MapToShoe(createDto);
            var created = await _repository.CreateShoeAsync(shoe);
            
            return MapToShoeDto(created);
        }
        catch (ArgumentException)
        {
            // Business validation errors - re-throw as-is
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Convert repository error to business error
            throw new ArgumentException($"Business rule violation: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Unexpected errors - log and wrap
            _logger.LogError(ex, "Unexpected error creating shoe {ShoeName}", createDto.Name);
            throw new InvalidOperationException("An unexpected error occurred while creating the shoe", ex);
        }
    }
}

// Controller Layer - HTTP errors
public class ShoesController : Controller
{
    public async Task<IActionResult> Create(CreateShoeDto createDto)
    {
        try
        {
            var shoe = await _shoeService.CreateShoeAsync(createDto);
            TempData["SuccessMessage"] = "Shoe created successfully!";
            return RedirectToAction("Details", new { id = shoe.Id });
        }
        catch (ArgumentException ex)
        {
            // Business validation errors - show to user
            ModelState.AddModelError("", ex.Message);
            return View(createDto);
        }
        catch (InvalidOperationException ex)
        {
            // System errors - generic message to user, detailed logging
            _logger.LogError(ex, "System error creating shoe");
            ModelState.AddModelError("", "A system error occurred. Please try again.");
            return View(createDto);
        }
    }
}
```

### Performance Optimization Strategies

#### Async/Await Throughout

```csharp
// Repository - Async data access
public async Task<IEnumerable<Shoe>> GetAllShoesAsync()
{
    return await _context.Shoes
        .Include(s => s.ColorVariations)
        .ToListAsync(); // Async database call
}

// Service - Async business logic
public async Task<IEnumerable<ShoeDto>> GetAllShoesAsync()
{
    var shoes = await _repository.GetAllShoesAsync(); // Await repository
    
    var tasks = shoes.Select(async shoe =>
    {
        var dto = MapToShoeDto(shoe);
        dto.AvailableColors = (await _repository.GetAvailableColorsAsync(shoe.Id)).ToList();
        return dto;
    });
    
    return await Task.WhenAll(tasks); // Parallel processing
}

// Controller - Async action
public async Task<IActionResult> Index()
{
    var shoes = await _shoeService.GetAllShoesAsync(); // Await service
    return View(shoes);
}
```

#### Efficient Data Loading

```csharp
// Eager loading to prevent N+1 queries
public async Task<IEnumerable<Shoe>> GetAllShoesAsync()
{
    return await _context.Shoes
        .Include(s => s.ColorVariations.Where(cv => cv.IsActive)) // Filtered include
        .Where(s => s.IsAvailable)
        .AsSplitQuery() // Split into multiple queries for better performance
        .ToListAsync();
}

// Projection for better performance when full entities aren't needed
public async Task<IEnumerable<ShoeBasicDto>> GetShoeBasicsAsync()
{
    return await _context.Shoes
        .Where(s => s.IsAvailable)
        .Select(s => new ShoeBasicDto
        {
            Id = s.Id,
            Name = s.Name,
            Brand = s.Brand,
            Price = s.Price,
            CurrentColor = s.CurrentColor
        })
        .ToListAsync(); // No unnecessary data loaded
}
```

This comprehensive guide demonstrates how to build a production-ready application with modern architectural patterns, proper separation of concerns, and robust error handling while maintaining code quality and performance.

## Summary

This ShoeShop application serves as a complete example of modern .NET development practices, showcasing:

1. **Clean Architecture** with proper layer separation
2. **Polymorphism** through interfaces and dependency injection
3. **DTO Pattern** for controlled data transfer
4. **Repository Pattern** for data access abstraction
5. **Service Layer** for business logic encapsulation
6. **Configuration-driven behavior** for flexible deployments
7. **Comprehensive error handling** across all layers
8. **Performance optimization** with async patterns
9. **Design-Time Factory** for EF Core tooling support
10. **Modern web UI** with responsive design

The application provides a solid foundation for building scalable, maintainable enterprise applications with ASP.NET Core and Entity Framework Core.

---

**Built with .NET 9, Entity Framework Core 9, and modern architectural patterns** 🏗️
