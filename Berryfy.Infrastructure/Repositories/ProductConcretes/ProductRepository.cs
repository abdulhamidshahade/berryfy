using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.ShopEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Berryfy.Infrastructure.Repositories.ProductConcretes
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<IReadOnlyList<Product>>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at, p.category_id
                FROM products p
                ORDER BY p.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            
            var products = new List<Product>();

            while (await reader.ReadAsync())
            {
                Product product = MapProduct(reader);
                products.Add(product);
            }
            
            return new InfrastructureResponse<IReadOnlyList<Product>>()
            {
                IsSuccess = true,
                Message = "Products retrieved successfully",
                Value = products
            };
        }

        public async Task<InfrastructureResponse<Product>> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at, p.category_id
                FROM products p
                WHERE p.id = @Id
                ORDER BY p.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Product? product = null;

            if (await reader.ReadAsync())
            {
                product = MapProduct(reader);
            }

            return new InfrastructureResponse<Product>()
            {
                IsSuccess = product != null,
                Message = product != null ? "Product retrieved successfully" : "Product not found",
                Value = product!
            };
        }

        public async Task<InfrastructureResponse<Product>> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at, p.category_id
                FROM products p
                WHERE LOWER(p.name) = LOWER(@Name)
                ORDER BY p.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Product? product = null;

            if (await reader.ReadAsync())
            {
                product = MapProduct(reader);
            }

            return new InfrastructureResponse<Product>()
            {
                IsSuccess = product != null,
                Message = product != null ? "Product retrieved successfully" : "Product not found",
                Value = product!
            };
        }

        public async Task<InfrastructureResponse<Product>> CreateAsync(Product product)
        {
            const string sql = @"
                INSERT INTO products (name, description, stock_quantity, image_url, price, reserved_stock,
                    low_stock_threshold, is_active, sku, created_at, updated_at, category_id)
                VALUES (@Name, @Description, @StockQuantity, @ImageUrl, @Price, @ReservedStock,
                    @LowStockThreshold, @IsActive, @SKU, @CreatedAt, @UpdatedAt, @CategoryId)
                RETURNING id, name, description, stock_quantity, image_url, price,
                reserved_stock, low_stock_threshold, is_active, sku, created_at, updated_at, category_id;";

            var now = DateTime.UtcNow;
            if (product.CreatedAt == default) product.CreatedAt = now;
            if (product.UpdatedAt == default) product.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddProductParameters(command, product);

            await using var reader = await command.ExecuteReaderAsync();

            Product? createdProduct = null;

            if (await reader.ReadAsync())
            {
                createdProduct = MapProduct(reader);
            }

            return new InfrastructureResponse<Product>()
            {
                IsSuccess = createdProduct != null,
                Message = createdProduct != null ? "Product created successfully" : "Failed to create product",
                Value = createdProduct!
            };
        }

        public async Task<InfrastructureResponse<Product>> UpdateAsync(int id, Product product)
        {
            const string sql = @"
                UPDATE products
                SET name = @Name,
                    description = @Description,
                    stock_quantity = @StockQuantity,
                    image_url = @ImageUrl,
                    price = @Price,
                    reserved_stock = @ReservedStock,
                    low_stock_threshold = @LowStockThreshold,
                    is_active = @IsActive,
                    sku = @SKU,
                    updated_at = @UpdatedAt,
                    category_id = @CategoryId
                WHERE id = @Id
                RETURNING id, name, description, stock_quantity, image_url, price, reserved_stock,
                    low_stock_threshold, is_active, sku, created_at, updated_at, category_id;";

            if (product.UpdatedAt == default) product.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            AddProductParameters(command, product);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                Product updatedProduct = MapProduct(reader);
                return new InfrastructureResponse<Product>()
                {
                    IsSuccess = true,
                    Message = "Product updated successfully",
                    Value = updatedProduct
                };
            }

            return new InfrastructureResponse<Product>()
            {
                IsSuccess = false,
                Message = $"Product with ID {id} not found.",
                Value = null!
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(Product product)
        {
            const string sql = "DELETE FROM products WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", product.Id);

            var rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Product deleted successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM products WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Product exists",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsByNameAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM products WHERE LOWER(name) = LOWER(@Name)";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Product exists",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<int>> GetTotalCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM products";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "Total count retrieved successfully",
                Value = count != null ? Convert.ToInt32(count) : 0
            };
        }

        public async Task<InfrastructureResponse<IReadOnlyList<Product>>> GetFilteredAsync(string? searchTerm = null, string? category = null,
            string? sortBy = "name", decimal? minPrice = null, decimal? maxPrice = null,
            bool? isActive = true, int pageNumber = 1, int pageSize = 10)
        {
            var sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at, p.category_id
                FROM products p
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.name ILIKE @SearchTerm OR p.description ILIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM categories c WHERE c.id = p.category_id AND (LOWER(c.name) = LOWER(@CategoryName) OR CAST(c.id AS TEXT) = @CategoryName)) ";
            }

            if (minPrice.HasValue)
            {
                sql += "AND p.price >= @MinPrice ";
            }

            if (maxPrice.HasValue)
            {
                sql += "AND p.price <= @MaxPrice ";
            }

            if (isActive.HasValue)
            {
                sql += "AND p.is_active = @IsActive ";
            }

            string orderByClause = sortBy?.ToLower() switch
            {
                "price-low" or "price_asc" => "ORDER BY p.price ASC",
                "price-high" or "price_desc" => "ORDER BY p.price DESC",
                "newest" => "ORDER BY p.created_at DESC",
                _ => "ORDER BY p.name ASC" 
            };
            sql += orderByClause + " ";

            sql += "LIMIT @PageSize OFFSET @Offset";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                command.Parameters.AddWithValue("@CategoryName", category);
            }
            if (minPrice.HasValue)
            {
                command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
            }
            if (isActive.HasValue)
            {
                command.Parameters.AddWithValue("@IsActive", isActive.Value);
            }
            command.Parameters.AddWithValue("@Offset", Math.Max(0, (pageNumber - 1) * pageSize));
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();

            var products = new List<Product>();

            while (await reader.ReadAsync())
            {
                Product product = MapProduct(reader);
                products.Add(product);
            }

            return new InfrastructureResponse<IReadOnlyList<Product>>()
            {
                IsSuccess = true,
                Message = "Products retrieved successfully",
                Value = products
            };
        }

        public async Task<InfrastructureResponse<int>> GetFilteredCountAsync(string? searchTerm = null, string? category = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? isActive = true)
        {
            var sql = @"
                SELECT COUNT(DISTINCT p.id)
                FROM products p
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.name ILIKE @SearchTerm OR p.description ILIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM categories c WHERE c.id = p.category_id AND (LOWER(c.name) = LOWER(@CategoryName) OR CAST(c.id AS TEXT) = @CategoryName)) ";
            }

            if (minPrice.HasValue)
            {
                sql += "AND p.price >= @MinPrice ";
            }

            if (maxPrice.HasValue)
            {
                sql += "AND p.price <= @MaxPrice ";
            }

            if (isActive.HasValue)
            {
                sql += "AND p.is_active = @IsActive ";
            }

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                command.Parameters.AddWithValue("@CategoryName", category);
            }
            if (minPrice.HasValue)
            {
                command.Parameters.AddWithValue("@MinPrice", minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                command.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);
            }
            if (isActive.HasValue)
            {
                command.Parameters.AddWithValue("@IsActive", isActive.Value);
            }

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "Filtered count retrieved successfully",
                Value = count != null ? Convert.ToInt32(count) : 0
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Product MapProduct(NpgsqlDataReader reader)
        {
            var descOrd = reader.GetOrdinal("description");
            var imgOrd = reader.GetOrdinal("image_url");
            var skuOrd = reader.GetOrdinal("sku");
            var catOrd = reader.GetOrdinal("category_id");

            return new Product()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.IsDBNull(descOrd) ? string.Empty : reader.GetString(descOrd),
                StockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("stock_quantity"))),
                ImageUrl = reader.IsDBNull(imgOrd) ? string.Empty : reader.GetString(imgOrd),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ReservedStock = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("reserved_stock"))),
                LowStockThreshold = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("low_stock_threshold"))),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                SKU = reader.IsDBNull(skuOrd) ? string.Empty : reader.GetString(skuOrd),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                CategoryId = reader.IsDBNull(catOrd) ? 0 : Convert.ToInt32(reader.GetValue(catOrd))
            };
        }

        private void AddProductParameters(NpgsqlCommand command, Product product)
        {
            command.Parameters.AddWithValue("@Name", product.Name ?? string.Empty);
            command.Parameters.AddWithValue("@Description", product.Description ?? string.Empty);
            command.Parameters.AddWithValue("@StockQuantity", product.StockQuantity);
            command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl ?? string.Empty);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ReservedStock", product.ReservedStock);
            command.Parameters.AddWithValue("@LowStockThreshold", product.LowStockThreshold);
            command.Parameters.AddWithValue("@IsActive", product.IsActive);
            command.Parameters.AddWithValue("@SKU", product.SKU ?? string.Empty);
            command.Parameters.AddWithValue("@CreatedAt", product.CreatedAt == default ? DateTime.UtcNow : product.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", product.UpdatedAt == default ? DateTime.UtcNow : product.UpdatedAt);
            command.Parameters.AddWithValue("@CategoryId", product.CategoryId > 0 ? product.CategoryId : DBNull.Value);
        }

        //public async Task<InfrastructureResponse<ShopCategory>> GetShopCategoryByProduct(int productId)
        //{
        //    const string query = "select sc.* from products p inner join shop_categories sc on (sc.id = p.shop_category_id) where p.id = @ProductId;";

        //    using var connectoin = await OpenConnectionAsync();

        //    using var command = new NpgsqlCommand(query, connectoin);
        //    command.Parameters.AddWithValue("@ProductId", productId);

        //    ShopCategory shopCategory = new ShopCategory();

        //    using var reader = await command.ExecuteReaderAsync();

        //    if(await reader.ReadAsync())
        //    {
        //        shopCategory = MapShopCategory(reader);
        //    }

        //    return new InfrastructureResponse<ShopCategory>()
        //    {
        //        IsSuccess = true,
        //        Message = "Shop category that related to the product retrived successfully!",
        //        Value = shopCategory
        //    };
        //}


        //private static ShopCategory MapShopCategory(NpgsqlDataReader reader)
        //{
        //    var descriptionOrdinal = reader.GetOrdinal("description");
        //    var imageUrlOrdinal = reader.GetOrdinal("image_url");
        //    var parentOrdinal = reader.GetOrdinal("parent_category_id");

        //    return new ShopCategory
        //    {
        //        Id = checked((int)reader.GetInt64(reader.GetOrdinal("id"))),
        //        ShopId = checked((int)reader.GetInt64(reader.GetOrdinal("shop_id"))),
        //        Name = reader.GetString(reader.GetOrdinal("name")),
        //        Description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
        //        ImageUrl = reader.IsDBNull(imageUrlOrdinal) ? null : reader.GetString(imageUrlOrdinal),
        //        Slug = reader.GetString(reader.GetOrdinal("slug")),
        //        ParentCategoryId = reader.IsDBNull(parentOrdinal) ? null : checked((int)reader.GetInt64(parentOrdinal)),
        //        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
        //        DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order")),
        //        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
        //        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        //    };
        //}
    }
}