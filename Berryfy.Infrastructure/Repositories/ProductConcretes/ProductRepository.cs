using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ProductConcretes
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    p.Id, p.Name, p.Description, p.StockQuantity, p.ImageUrl, p.Price,
                    p.ReservedStock, p.LowStockThreshold, p.IsActive, p.SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS PcId, pc.CategoryId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Products p
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                ORDER BY p.Id, pc.Id;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var products = new Dictionary<int, Product>();
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                var productId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!products.TryGetValue(productId, out var product))
                {
                    product = new Product
                    {
                        Id = productId,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                        LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                    products.Add(productId, product);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = productId,
                            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        product.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("C_Id")))
                    {
                        if (pc.Category == null)
                        {
                            pc.Category = new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("C_Id")),
                                Name = reader.GetString(reader.GetOrdinal("C_Name")),
                                Description = reader.GetString(reader.GetOrdinal("C_Description")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("C_ImageUrl")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("C_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("C_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return products.Values.ToList().AsReadOnly();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    p.Id, p.Name, p.Description, p.StockQuantity, p.ImageUrl, p.Price,
                    p.ReservedStock, p.LowStockThreshold, p.IsActive, p.SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS PcId, pc.CategoryId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Products p
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE p.Id = @Id
                ORDER BY p.Id, pc.Id;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Product? product = null;
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                if (product == null)
                {
                    product = new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                        LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = product.Id,
                            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        product.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("C_Id")))
                    {
                        if (pc.Category == null)
                        {
                            pc.Category = new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("C_Id")),
                                Name = reader.GetString(reader.GetOrdinal("C_Name")),
                                Description = reader.GetString(reader.GetOrdinal("C_Description")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("C_ImageUrl")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("C_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("C_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return product;
        }

        public async Task<Product> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    p.Id, p.Name, p.Description, p.StockQuantity, p.ImageUrl, p.Price,
                    p.ReservedStock, p.LowStockThreshold, p.IsActive, p.SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS PcId, pc.CategoryId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Products p
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE p.Name = @Name
                ORDER BY p.Id, pc.Id;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Product? product = null;
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                if (product == null)
                {
                    product = new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                        LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = product.Id,
                            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        product.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("C_Id")))
                    {
                        if (pc.Category == null)
                        {
                            pc.Category = new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("C_Id")),
                                Name = reader.GetString(reader.GetOrdinal("C_Name")),
                                Description = reader.GetString(reader.GetOrdinal("C_Description")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("C_ImageUrl")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("C_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("C_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return product;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            const string sql = @"
                INSERT INTO Products (Name, Description, StockQuantity, ImageUrl, Price, ReservedStock,
                    LowStockThreshold, IsActive, SKU, CreatedAt, UpdatedAt)
                VALUES (@Name, @Description, @StockQuantity, @ImageUrl, @Price, @ReservedStock,
                    @LowStockThreshold, @IsActive, @SKU, @CreatedAt, @UpdatedAt)
                RETURNING Id, CreatedAt, UpdatedAt;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", product.Name);
            command.Parameters.AddWithValue("Description", (object?)product.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("ImageUrl", (object?)product.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("Price", product.Price);
            command.Parameters.AddWithValue("ReservedStock", product.ReservedStock);
            command.Parameters.AddWithValue("LowStockThreshold", product.LowStockThreshold);
            command.Parameters.AddWithValue("IsActive", product.IsActive);
            command.Parameters.AddWithValue("SKU", product.SKU);
            command.Parameters.AddWithValue("CreatedAt", product.CreatedAt);
            command.Parameters.AddWithValue("UpdatedAt", product.UpdatedAt);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                product.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                product.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                product.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
            }

            return product;
        }

        public async Task<Product> UpdateAsync(int id, Product product)
        {
            const string sql = @"
                UPDATE Products
                SET Name = @Name,
                    Description = @Description,
                    StockQuantity = @StockQuantity,
                    ImageUrl = @ImageUrl,
                    Price = @Price,
                    ReservedStock = @ReservedStock,
                    LowStockThreshold = @LowStockThreshold,
                    IsActive = @IsActive,
                    SKU = @SKU,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id
                RETURNING Id, Name, Description, StockQuantity, ImageUrl, Price, ReservedStock,
                    LowStockThreshold, IsActive, SKU, CreatedAt, UpdatedAt;";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", product.Name);
            command.Parameters.AddWithValue("Description", (object?)product.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("ImageUrl", (object?)product.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("Price", product.Price);
            command.Parameters.AddWithValue("ReservedStock", product.ReservedStock);
            command.Parameters.AddWithValue("LowStockThreshold", product.LowStockThreshold);
            command.Parameters.AddWithValue("IsActive", product.IsActive);
            command.Parameters.AddWithValue("SKU", product.SKU);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                product.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                product.Name = reader.GetString(reader.GetOrdinal("Name"));
                product.Description = reader.GetString(reader.GetOrdinal("Description"));
                product.StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"));
                product.ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl"));
                product.Price = reader.GetDecimal(reader.GetOrdinal("Price"));
                product.ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock"));
                product.LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold"));
                product.IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
                product.SKU = reader.GetString(reader.GetOrdinal("SKU"));
                product.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                product.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return product;
            }

            return null;
        }

        public async Task<bool> DeleteAsync(Product product)
        {
            const string sql = "DELETE FROM Products WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", product.Id);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM Products WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM Products WHERE Name = @Name";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", name);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<int> GetTotalCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM Products";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            var count = await command.ExecuteScalarAsync();
            return count != null ? Convert.ToInt32(count) : 0;
        }

        public async Task<IReadOnlyList<Product>> GetFilteredAsync(string? searchTerm = null, string? category = null,
            string? sortBy = "name", decimal? minPrice = null, decimal? maxPrice = null,
            bool? isActive = true, int pageNumber = 1, int pageSize = 10)
        {
            var sql = @"
                SELECT
                    p.Id, p.Name, p.Description, p.StockQuantity, p.ImageUrl, p.Price,
                    p.ReservedStock, p.LowStockThreshold, p.IsActive, p.SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS PcId, pc.CategoryId, pc.CreatedAt AS PcCreatedAt, pc.UpdatedAt AS PcUpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Products p
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.Name LIKE @SearchTerm OR p.Description LIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM ProductCategories pc2 INNER JOIN Categories c2 ON pc2.CategoryId = c2.Id WHERE pc2.ProductId = p.Id AND LOWER(c2.Name) = LOWER(@CategoryName)) ";
            }

            if (minPrice.HasValue)
            {
                sql += "AND p.Price >= @MinPrice ";
            }

            if (maxPrice.HasValue)
            {
                sql += "AND p.Price <= @MaxPrice ";
            }

            if (isActive.HasValue)
            {
                sql += "AND p.IsActive = @IsActive ";
            }

            string orderByClause = sortBy?.ToLower() switch
            {
                "price-low" => "ORDER BY p.Price ASC",
                "price-high" => "ORDER BY p.Price DESC",
                "newest" => "ORDER BY p.CreatedAt DESC",
                _ => "ORDER BY p.Name ASC" 
            };
            sql += orderByClause + " ";

            sql += "OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                command.Parameters.AddWithValue("SearchTerm", $"%{searchTerm}%");
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                command.Parameters.AddWithValue("CategoryName", category);
            }
            if (minPrice.HasValue)
            {
                command.Parameters.AddWithValue("MinPrice", minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                command.Parameters.AddWithValue("MaxPrice", maxPrice.Value);
            }
            if (isActive.HasValue)
            {
                command.Parameters.AddWithValue("IsActive", isActive.Value);
            }
            command.Parameters.AddWithValue("Offset", (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();

            var products = new Dictionary<int, Product>();
            var productCategories = new Dictionary<int, ProductCategory>();

            while (await reader.ReadAsync())
            {
                var productId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!products.TryGetValue(productId, out var product))
                {
                    product = new Product
                    {
                        Id = productId,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        ReservedStock = reader.GetInt32(reader.GetOrdinal("ReservedStock")),
                        LowStockThreshold = reader.GetInt32(reader.GetOrdinal("LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                        ProductCategories = new List<ProductCategory>()
                    };
                    products.Add(productId, product);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("PcId")))
                {
                    var pcId = reader.GetInt32(reader.GetOrdinal("PcId"));
                    if (!productCategories.TryGetValue(pcId, out var pc))
                    {
                        pc = new ProductCategory
                        {
                            Id = pcId,
                            ProductId = productId,
                            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("PcCreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("PcUpdatedAt"))
                        };
                        productCategories.Add(pcId, pc);
                        product.ProductCategories.Add(pc);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("C_Id")))
                    {
                        if (pc.Category == null)
                        {
                            pc.Category = new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("C_Id")),
                                Name = reader.GetString(reader.GetOrdinal("C_Name")),
                                Description = reader.GetString(reader.GetOrdinal("C_Description")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("C_ImageUrl")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("C_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("C_UpdatedAt"))
                            };
                        }
                    }
                }
            }

            return products.Values.ToList().AsReadOnly();
        }

        public async Task<int> GetFilteredCountAsync(string? searchTerm = null, string? category = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? isActive = true)
        {
            var sql = @"
                SELECT COUNT(DISTINCT p.Id)
                FROM Products p
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.Name LIKE @SearchTerm OR p.Description LIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM ProductCategories pc2 INNER JOIN Categories c2 ON pc2.CategoryId = c2.Id WHERE pc2.ProductId = p.Id AND LOWER(c2.Name) = LOWER(@CategoryName)) ";
            }

            if (minPrice.HasValue)
            {
                sql += "AND p.Price >= @MinPrice ";
            }

            if (maxPrice.HasValue)
            {
                sql += "AND p.Price <= @MaxPrice ";
            }

            if (isActive.HasValue)
            {
                sql += "AND p.IsActive = @IsActive ";
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                command.Parameters.AddWithValue("SearchTerm", $"%{searchTerm}%");
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                command.Parameters.AddWithValue("CategoryName", category);
            }
            if (minPrice.HasValue)
            {
                command.Parameters.AddWithValue("MinPrice", minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                command.Parameters.AddWithValue("MaxPrice", maxPrice.Value);
            }
            if (isActive.HasValue)
            {
                command.Parameters.AddWithValue("IsActive", isActive.Value);
            }

            var count = await command.ExecuteScalarAsync();
            return count != null ? Convert.ToInt32(count) : 0;
        }
    }
}