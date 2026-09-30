using Berryfy.Domain.Entities.ProductEntities;
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

        public async Task<IReadOnlyList<Product>> GetAllAsync()
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at
                FROM Products p
                ORDER BY p.id;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            
            var products = new List<Product>();

            while (await reader.ReadAsync())
            {
                Product product = MapProduct(reader, new Product());
                products.Add(product);
            }
            
            return products;
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at 
                FROM Products p
                WHERE p.id = @Id
                ORDER BY p.id;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Product product = MapProduct(reader, new Product());

            return product;
        }

        public async Task<Product> GetByNameAsync(string name)
        {
            const string sql = @"
                SELECT
                    p.id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at
                FROM Products p
                WHERE p.name = @Name
                ORDER BY p.id, pc.id;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();

            Product product = MapProduct(reader, new Product());

            return product;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            const string sql = @"
                INSERT INTO products (name, description, stock_quantity, image_url, price, reserved_stock,
                    low_stock_threshold, is_active, sku, created_at, updated_at)
                VALUES (@Name, @Description, @StockQuantity, @ImageUrl, @Price, @ReservedStock,
                    @LowStockThreshold, @IsActive, @SKU, @CreatedAt, @UpdatedAt)
                RETURNING id, name, description, stock_quantity, image_url, price,
                reserved_stock, low_stock_threshold, is_active, sku, created_at, updated_at;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            AddProductParameters(command, product);

            await using var reader = await command.ExecuteReaderAsync();
            
            Product createdProduct = MapProduct(reader, new Product());

            return createdProduct;
        }

        public async Task<Product> UpdateAsync(int id, Product product)
        {
            const string sql = @"
                UPDATE Products
                SET name = @Name,
                    description = @Description,
                    stock_quantity = @StockQuantity,
                    image_url = @ImageUrl,
                    price = @Price,
                    reserved_stock = @ReservedStock,
                    low_stock_quantity = @LowStockThreshold,
                    is_active = @IsActive,
                    sku = @SKU,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, name, description, stock_quantity, image_url, price, reserved_stock,
                    low_stock_quantity, is_active, sku, created_at, updated_at;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            AddProductParameters(command, product);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            
            if(await reader.ReadAsync())
            {
                Product updatedProduct = MapProduct(reader, new Product());
                return updatedProduct;
            }
            else
            {
                throw new Exception($"Product with ID {id} not found.");
            }
        }

        public async Task<bool> DeleteAsync(Product product)
        {
            const string sql = "DELETE FROM Products WHERE id = @Id";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", product.Id);

            var rowEffected = await command.ExecuteNonQueryAsync();
            return rowEffected > 0;
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM Products WHERE id = @Id";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            const string sql = "SELECT COUNT(1) FROM Products WHERE name = @Name";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<int> GetTotalCountAsync()
        {
            const string sql = "SELECT COUNT(1) FROM Products";

            var connection = await OpenConnectionAsync();

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
                    p.Id, p.name, p.description, p.stock_quantity, p.image_url, p.price,
                    p.reserved_stock, p.low_stock_threshold, p.is_active, p.sku,
                    p.created_at, p.updated_at
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.name ILIKE @SearchTerm OR p.description ILIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM ProductCategories pc INNER JOIN Categories c ON pc.CategoryId = c.Id WHERE pc.ProductId = p.Id AND LOWER(c.Name) = LOWER(@CategoryName)) ";
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

            var connection = await OpenConnectionAsync();

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

            var products = new List<Product>();

            while (await reader.ReadAsync())
            {
                Product product = MapProduct(reader, new Product());
                products.Add(product);
            }

            return products;
        }

        public async Task<int> GetFilteredCountAsync(string? searchTerm = null, string? category = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? isActive = true)
        {
            var sql = @"
                SELECT COUNT(DISTINCT p.Id)
                FROM Products p
                WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql += "AND (p.name ILIKE @SearchTerm OR p.description ILIKE @SearchTerm) ";
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                sql += "AND EXISTS (SELECT 1 FROM ProductCategories pc INNER JOIN Categories c ON pc.CategoryId = c.Id WHERE pc.ProductId = p.Id AND LOWER(c.name) = LOWER(@CategoryName)) ";
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

            var connection = await OpenConnectionAsync();

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

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Product MapProduct(NpgsqlDataReader reader, Product product)
        {
            product = new Product()
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                StockQuantity = reader.GetInt16(reader.GetOrdinal("stock_quantity")),
                ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
                Price = reader.GetDecimal(reader.GetOrdinal("price")),
                ReservedStock = reader.GetInt16(reader.GetOrdinal("reserved_stock")),
                LowStockThreshold = reader.GetInt16(reader.GetOrdinal("low_stock_threshold")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                SKU = reader.GetString(reader.GetOrdinal("sku")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
            return product;
        }

        private void AddProductParameters(NpgsqlCommand command, Product product)
        {
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
        }
    }
}