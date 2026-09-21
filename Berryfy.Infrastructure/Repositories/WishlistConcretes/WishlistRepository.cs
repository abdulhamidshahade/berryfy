using Berryfy.Domain.Entities.WishlistEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.WishlistInterfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Berryfy.Application.Dtos.WishlistDtos.Responses;

namespace Berryfy.Infrastructure.Repositories.WishlistConcretes
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly string _connectionString;

        public WishlistRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<Wishlist> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    w.Id, w.UserId, w.Name, w.IsDefault, w.IsPublic, w.CreatedAt, w.UpdatedAt,
                    wi.Id AS Wi_Id, wi.ProductId AS Wi_ProductId, wi.Notes AS Wi_Notes, wi.Priority AS Wi_Priority,
                    wi.CreatedAt AS Wi_CreatedAt, wi.UpdatedAt AS Wi_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS Pc_Id, pc.CategoryId AS Pc_CategoryId, pc.CreatedAt AS Pc_CreatedAt, pc.UpdatedAt AS Pc_UpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Wishlists w
                LEFT JOIN WishlistItems wi ON w.Id = wi.WishlistId
                LEFT JOIN Products p ON wi.ProductId = p.Id
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE w.Id = @Id
                ORDER BY w.Id, wi.Id, pc.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Wishlist? wishlist = null;
            var wishlistItems = new Dictionary<int, Domain.Entities.WishlistEntities.WishlistItem>();

            while (await reader.ReadAsync())
            {
                int wishlistId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        Id = wishlistId,
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                        IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        WishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Wi_Id")))
                {
                    var wishlistItemId = reader.GetInt32(reader.GetOrdinal("Wi_Id"));
                    if (!wishlistItems.TryGetValue(wishlistItemId, out var wishlistItem))
                    {
                        wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                        {
                            Id = wishlistItemId,
                            WishlistId = wishlistId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("Wi_ProductId")),
                            Notes = reader.GetString(reader.GetOrdinal("Wi_Notes")),
                            Priority = reader.GetInt32(reader.GetOrdinal("Wi_Priority")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_UpdatedAt")),
                            Product = null
                        };
                        wishlistItems.Add(wishlistItemId, wishlistItem);
                        wishlist.WishlistItems.Add(wishlistItem);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (wishlistItem.Product == null)
                        {
                            wishlistItem.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                                ProductCategories = new List<ProductCategory>()
                            };
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("Pc_Id")))
                        {
                            var pcId = reader.GetInt32(reader.GetOrdinal("Pc_Id"));
                            var pc = wishlistItem.Product.ProductCategories.FirstOrDefault(pcItem => pcItem.Id == pcId);
                            if (pc == null)
                            {
                                pc = new ProductCategory
                                {
                                    Id = pcId,
                                    ProductId = wishlistItem.Product.Id,
                                    CategoryId = reader.GetInt32(reader.GetOrdinal("Pc_CategoryId")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_CreatedAt")),
                                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_UpdatedAt"))
                                };
                                wishlistItem.Product.ProductCategories.Add(pc);
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
                }
            }

            return wishlist;
        }

        public async Task<Wishlist> GetUserDefaultWishlistAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.Id, w.UserId, w.Name, w.IsDefault, w.IsPublic, w.CreatedAt, w.UpdatedAt,
                    wi.Id AS Wi_Id, wi.ProductId AS Wi_ProductId, wi.Notes AS Wi_Notes, wi.Priority AS Wi_Priority,
                    wi.CreatedAt AS Wi_CreatedAt, wi.UpdatedAt AS Wi_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS Pc_Id, pc.CategoryId AS Pc_CategoryId, pc.CreatedAt AS Pc_CreatedAt, pc.UpdatedAt AS Pc_UpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM Wishlists w
                LEFT JOIN WishlistItems wi ON w.Id = wi.WishlistId
                LEFT JOIN Products p ON wi.ProductId = p.Id
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE w.UserId = @UserId AND w.IsDefault = true
                ORDER BY w.Id, wi.Id, pc.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            Wishlist? wishlist = null;
            var wishlistItems = new Dictionary<int, Domain.Entities.WishlistEntities.WishlistItem>();

            while (await reader.ReadAsync())
            {
                int wishlistId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        Id = wishlistId,
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                        IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        WishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Wi_Id")))
                {
                    var wishlistItemId = reader.GetInt32(reader.GetOrdinal("Wi_Id"));
                    if (!wishlistItems.TryGetValue(wishlistItemId, out var wishlistItem))
                    {
                        wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                        {
                            Id = wishlistItemId,
                            WishlistId = wishlistId,
                            ProductId = reader.GetInt32(reader.GetOrdinal("Wi_ProductId")),
                            Notes = reader.GetString(reader.GetOrdinal("Wi_Notes")),
                            Priority = reader.GetInt32(reader.GetOrdinal("Wi_Priority")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_UpdatedAt")),
                            Product = null
                        };
                        wishlistItems.Add(wishlistItemId, wishlistItem);
                        wishlist.WishlistItems.Add(wishlistItem);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        if (wishlistItem.Product == null)
                        {
                            wishlistItem.Product = new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                                Name = reader.GetString(reader.GetOrdinal("P_Name")),
                                Description = reader.GetString(reader.GetOrdinal("P_Description")),
                                StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                                ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                                Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                                ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                                LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                                IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                                SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                                ProductCategories = new List<ProductCategory>()
                            };
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("Pc_Id")))
                        {
                            var pcId = reader.GetInt32(reader.GetOrdinal("Pc_Id"));
                            var pc = wishlistItem.Product.ProductCategories.FirstOrDefault(pcItem => pcItem.Id == pcId);
                            if (pc == null)
                            {
                                pc = new ProductCategory
                                {
                                    Id = pcId,
                                    ProductId = wishlistItem.Product.Id,
                                    CategoryId = reader.GetInt32(reader.GetOrdinal("Pc_CategoryId")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_CreatedAt")),
                                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_UpdatedAt"))
                                };
                                wishlistItem.Product.ProductCategories.Add(pc);
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
                }

                if (!reader.HasRows && wishlist == null)
                {
                }
            }

            if (wishlist == null)
            {
                wishlist = await CreateDefaultWishlistForUser(userId);
            }

            return wishlist;
        }

        private async Task<Wishlist> CreateDefaultWishlistForUser(int userId)
        {
            const string insertSql = @"
                INSERT INTO Wishlists (UserId, Name, IsDefault, IsPublic, CreatedAt, UpdatedAt)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING Id, UserId, Name, IsDefault, IsPublic, CreatedAt, UpdatedAt";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("Name", "My Wishlist");
            command.Parameters.AddWithValue("IsDefault", true);
            command.Parameters.AddWithValue("IsPublic", false);
            command.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Wishlist
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                    IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    WishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>()
                };
            }

            throw new InvalidOperationException("Failed to create default wishlist");
        }

        public async Task<IEnumerable<Wishlist>> GetUserWishlistsAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.Id, w.UserId, w.Name, w.IsDefault, w.IsPublic, w.CreatedAt, w.UpdatedAt,
                    wi.Id AS Wi_Id, wi.ProductId AS Wi_ProductId, wi.Notes AS Wi_Notes, wi.Priority AS Wi_Priority,
                    wi.CreatedAt AS Wi_CreatedAt, wi.UpdatedAt AS Wi_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt
                FROM Wishlists w
                LEFT JOIN WishlistItems wi ON w.Id = wi.WishlistId
                LEFT JOIN Products p ON wi.ProductId = p.Id
                WHERE w.UserId = @UserId
                ORDER BY w.IsDefault DESC, w.UpdatedAt DESC, w.Id, wi.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            var wishlists = new Dictionary<int, Wishlist>();

            while (await reader.ReadAsync())
            {
                int wishlistId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!wishlists.TryGetValue(wishlistId, out var wishlist))
                {
                    wishlist = new Wishlist
                    {
                        Id = wishlistId,
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                        IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        WishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>()
                    };
                    wishlists.Add(wishlistId, wishlist);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Wi_Id")))
                {
                    var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Wi_Id")),
                        WishlistId = wishlistId,
                        ProductId = reader.GetInt32(reader.GetOrdinal("Wi_ProductId")),
                        Notes = reader.GetString(reader.GetOrdinal("Wi_Notes")),
                        Priority = reader.GetInt32(reader.GetOrdinal("Wi_Priority")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_UpdatedAt")),
                        Product = null
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        wishlistItem.Product = new Product
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_Name")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                        };
                    }

                    wishlist.WishlistItems.Add(wishlistItem);
                }
            }

            return wishlists.Values;
        }

        public async Task<Wishlist> CreateAsync(Wishlist wishlist)
        {
            const string sql = @"
                INSERT INTO Wishlists (UserId, Name, IsDefault, IsPublic, CreatedAt, UpdatedAt)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING Id, UserId, Name, IsDefault, IsPublic, CreatedAt, UpdatedAt";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", wishlist.UserId);
            command.Parameters.AddWithValue("Name", (object?)wishlist.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("IsDefault", wishlist.IsDefault);
            command.Parameters.AddWithValue("IsPublic", wishlist.IsPublic);
            command.Parameters.AddWithValue("CreatedAt", wishlist.CreatedAt);
            command.Parameters.AddWithValue("UpdatedAt", wishlist.UpdatedAt);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                wishlist.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                wishlist.UserId = reader.GetInt32(reader.GetOrdinal("UserId"));
                wishlist.Name = reader.GetString(reader.GetOrdinal("Name"));
                wishlist.IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault"));
                wishlist.IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic"));
                wishlist.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                wishlist.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return wishlist;
            }

            return null;
        }

        public async Task<Wishlist> UpdateAsync(Wishlist wishlist)
        {
            const string sql = @"
                UPDATE Wishlists
                SET Name = @Name,
                    IsDefault = @IsDefault,
                    IsPublic = @IsPublic,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id
                RETURNING Id, UserId, Name, IsDefault, IsPublic, CreatedAt, UpdatedAt";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Name", (object?)wishlist.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("IsDefault", wishlist.IsDefault);
            command.Parameters.AddWithValue("IsPublic", wishlist.IsPublic);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("Id", wishlist.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                wishlist.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                wishlist.UserId = reader.GetInt32(reader.GetOrdinal("UserId"));
                wishlist.Name = reader.GetString(reader.GetOrdinal("Name"));
                wishlist.IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault"));
                wishlist.IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic"));
                wishlist.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                wishlist.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return wishlist;
            }

            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Wishlists WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM Wishlists WHERE Id = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", id);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<Domain.Entities.WishlistEntities.WishlistItem> GetWishlistItemAsync(int wishlistId, int productId)
        {
            const string sql = @"
                SELECT
                    wi.Id, wi.WishlistId, wi.ProductId, wi.Notes, wi.Priority, wi.CreatedAt, wi.UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt
                FROM WishlistItems wi
                LEFT JOIN Products p ON wi.ProductId = p.Id
                WHERE wi.WishlistId = @WishlistId AND wi.ProductId = @ProductId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("WishlistId", wishlistId);
            command.Parameters.AddWithValue("ProductId", productId);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    WishlistId = reader.GetInt32(reader.GetOrdinal("WishlistId")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Notes = reader.GetString(reader.GetOrdinal("Notes")),
                    Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                {
                    wishlistItem.Product = new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                        Name = reader.GetString(reader.GetOrdinal("P_Name")),
                        Description = reader.GetString(reader.GetOrdinal("P_Description")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                        ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                        Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                        ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                        LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                        SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                    };
                }

                return wishlistItem;
            }

            return null;
        }

        public async Task<Domain.Entities.WishlistEntities.WishlistItem> AddItemAsync(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            const string sql = @"
                INSERT INTO WishlistItems (WishlistId, ProductId, Notes, Priority, CreatedAt, UpdatedAt)
                VALUES (@WishlistId, @ProductId, @Notes, @Priority, @CreatedAt, @UpdatedAt)
                RETURNING Id, WishlistId, ProductId, Notes, Priority, CreatedAt, UpdatedAt";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("WishlistId", item.WishlistId);
            command.Parameters.AddWithValue("ProductId", item.ProductId);
            command.Parameters.AddWithValue("Notes", (object?)item.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("Priority", item.Priority);
            command.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                item.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                item.WishlistId = reader.GetInt32(reader.GetOrdinal("WishlistId"));
                item.ProductId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                item.Notes = reader.GetString(reader.GetOrdinal("Notes"));
                item.Priority = reader.GetInt32(reader.GetOrdinal("Priority"));
                item.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                item.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return item;
            }

            return null;
        }

        public async Task<Domain.Entities.WishlistEntities.WishlistItem> UpdateItemAsync(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            const string sql = @"
                UPDATE WishlistItems
                SET Notes = @Notes,
                    Priority = @Priority,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id
                RETURNING Id, WishlistId, ProductId, Notes, Priority, CreatedAt, UpdatedAt";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Notes", (object?)item.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("Priority", item.Priority);
            command.Parameters.AddWithValue("UpdatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("Id", item.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                item.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                item.WishlistId = reader.GetInt32(reader.GetOrdinal("WishlistId"));
                item.ProductId = reader.GetInt32(reader.GetOrdinal("ProductId"));
                item.Notes = reader.GetString(reader.GetOrdinal("Notes"));
                item.Priority = reader.GetInt32(reader.GetOrdinal("Priority"));
                item.CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                item.UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"));
                return item;
            }

            return null;
        }

        public async Task<bool> RemoveItemAsync(int wishlistId, int productId)
        {
            const string sql = "DELETE FROM WishlistItems WHERE WishlistId = @WishlistId AND ProductId = @ProductId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("WishlistId", wishlistId);
            command.Parameters.AddWithValue("ProductId", productId);

            var affected = await command.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> IsProductInWishlistAsync(int userId, int productId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM WishlistItems wi
                JOIN Wishlists w ON wi.WishlistId = w.Id
                WHERE w.UserId = @UserId AND wi.ProductId = @ProductId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);
            command.Parameters.AddWithValue("ProductId", productId);

            var count = await command.ExecuteScalarAsync();
            return count != null && Convert.ToInt32(count) > 0;
        }

        public async Task<IEnumerable<Domain.Entities.WishlistEntities.WishlistItem>> GetWishlistItemsAsync(int wishlistId)
        {
            const string sql = @"
                SELECT
                    wi.Id, wi.WishlistId, wi.ProductId, wi.Notes, wi.Priority, wi.CreatedAt, wi.UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    pc.Id AS Pc_Id, pc.CategoryId AS Pc_CategoryId, pc.CreatedAt AS Pc_CreatedAt, pc.UpdatedAt AS Pc_UpdatedAt,
                    c.Id AS C_Id, c.Name AS C_Name, c.Description AS C_Description, c.ImageUrl AS C_ImageUrl,
                    c.CreatedAt AS C_CreatedAt, c.UpdatedAt AS C_UpdatedAt
                FROM WishlistItems wi
                LEFT JOIN Products p ON wi.ProductId = p.Id
                LEFT JOIN ProductCategories pc ON p.Id = pc.ProductId
                LEFT JOIN Categories c ON pc.CategoryId = c.Id
                WHERE wi.WishlistId = @WishlistId
                ORDER BY wi.CreatedAt DESC";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("WishlistId", wishlistId);

            await using var reader = await command.ExecuteReaderAsync();

            var wishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>();
            var products = new Dictionary<int, Product>();

            while (await reader.ReadAsync())
            {
                int wishlistItemId = reader.GetInt32(reader.GetOrdinal("Id"));
                var wishlistItem = wishlistItems.FirstOrDefault(wi => wi.Id == wishlistItemId);
                if (wishlistItem == null)
                {
                    wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                    {
                        Id = wishlistItemId,
                        WishlistId = reader.GetInt32(reader.GetOrdinal("WishlistId")),
                        ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        Product = null
                    };
                    wishlistItems.Add(wishlistItem);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                {
                    int productId = reader.GetInt32(reader.GetOrdinal("P_Id"));
                    if (!products.TryGetValue(productId, out var product))
                    {
                        product = new Product
                        {
                            Id = productId,
                            Name = reader.GetString(reader.GetOrdinal("P_Name")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt")),
                            ProductCategories = new List<ProductCategory>()
                        };
                        products.Add(productId, product);
                    }

                    wishlistItem.Product = product;

                    if (!reader.IsDBNull(reader.GetOrdinal("Pc_Id")))
                    {
                        var pcId = reader.GetInt32(reader.GetOrdinal("Pc_Id"));
                        var pc = product.ProductCategories.FirstOrDefault(pcItem => pcItem.Id == pcId);
                        if (pc == null)
                        {
                            pc = new ProductCategory
                            {
                                Id = pcId,
                                ProductId = product.Id,
                                CategoryId = reader.GetInt32(reader.GetOrdinal("Pc_CategoryId")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_CreatedAt")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Pc_UpdatedAt"))
                            };
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
            }

            return wishlistItems;
        }

        public async Task<int> GetUserWishlistCountAsync(int userId)
        {
            const string sql = "SELECT COUNT(1) FROM Wishlists WHERE UserId = @UserId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);

            var count = await command.ExecuteScalarAsync();
            return count != null ? Convert.ToInt32(count) : 0;
        }

        public async Task<int> GetUserTotalItemsAsync(int userId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM WishlistItems wi
                JOIN Wishlists w ON wi.WishlistId = w.Id
                WHERE w.UserId = @UserId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);

            var count = await command.ExecuteScalarAsync();
            return count != null ? Convert.ToInt32(count) : 0;
        }

        public async Task<decimal> GetUserTotalValueAsync(int userId)
        {
            const string sql = @"
                SELECT COALESCE(SUM(p.Price), 0)
                FROM WishlistItems wi
                JOIN Wishlists w ON wi.WishlistId = w.Id
                JOIN Products p ON wi.ProductId = p.Id
                WHERE w.UserId = @UserId";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        public async Task<IEnumerable<Wishlist>> GetAllWishlistsAsync()
        {
            const string sql = @"
                SELECT
                    w.Id, w.UserId, w.Name, w.IsDefault, w.IsPublic, w.CreatedAt, w.UpdatedAt,
                    wi.Id AS Wi_Id, wi.ProductId AS Wi_ProductId, wi.Notes AS Wi_Notes, wi.Priority AS Wi_Priority,
                    wi.CreatedAt AS Wi_CreatedAt, wi.UpdatedAt AS Wi_UpdatedAt,
                    p.Id AS P_Id, p.Name AS P_Name, p.Description AS P_Description, p.StockQuantity AS P_StockQuantity,
                    p.ImageUrl AS P_ImageUrl, p.Price AS P_Price, p.ReservedStock AS P_ReservedStock,
                    p.LowStockThreshold AS P_LowStockThreshold, p.IsActive AS P_IsActive, p.SKU AS P_SKU,
                    p.CreatedAt AS P_CreatedAt, p.UpdatedAt AS P_UpdatedAt,
                    u.Id AS U_Id, u.UserName AS U_UserName, u.Email AS U_Email
                FROM Wishlists w
                LEFT JOIN WishlistItems wi ON w.Id = wi.WishlistId
                LEFT JOIN Products p ON wi.ProductId = p.Id
                LEFT JOIN AspNetUsers u ON w.UserId = u.Id
                ORDER BY w.UpdatedAt DESC, w.Id, wi.Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();

            var wishlists = new Dictionary<int, Wishlist>();
            var users = new Dictionary<int, User>();

            while (await reader.ReadAsync())
            {
                int wishlistId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (!wishlists.TryGetValue(wishlistId, out var wishlist))
                {
                    wishlist = new Wishlist
                    {
                        Id = wishlistId,
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        IsDefault = reader.GetBoolean(reader.GetOrdinal("IsDefault")),
                        IsPublic = reader.GetBoolean(reader.GetOrdinal("IsPublic")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        WishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>()
                    };

                    int userId = reader.GetInt32(reader.GetOrdinal("U_Id"));
                    if (!users.TryGetValue(userId, out var user))
                    {
                        user = new User
                        {
                            Id = userId,
                            UserName = reader.GetString(reader.GetOrdinal("U_UserName")),
                            Email = reader.GetString(reader.GetOrdinal("U_Email"))
                        };
                        users.Add(userId, user);
                    }

                    wishlist.User = user;
                    wishlists.Add(wishlistId, wishlist);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("Wi_Id")))
                {
                    var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Wi_Id")),
                        WishlistId = wishlistId,
                        ProductId = reader.GetInt32(reader.GetOrdinal("Wi_ProductId")),
                        Notes = reader.GetString(reader.GetOrdinal("Wi_Notes")),
                        Priority = reader.GetInt32(reader.GetOrdinal("Wi_Priority")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("Wi_UpdatedAt")),
                        Product = null
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("P_Id")))
                    {
                        wishlistItem.Product = new Product
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("P_Id")),
                            Name = reader.GetString(reader.GetOrdinal("P_Name")),
                            Description = reader.GetString(reader.GetOrdinal("P_Description")),
                            StockQuantity = reader.GetInt32(reader.GetOrdinal("P_StockQuantity")),
                            ImageUrl = reader.GetString(reader.GetOrdinal("P_ImageUrl")),
                            Price = reader.GetDecimal(reader.GetOrdinal("P_Price")),
                            ReservedStock = reader.GetInt32(reader.GetOrdinal("P_ReservedStock")),
                            LowStockThreshold = reader.GetInt32(reader.GetOrdinal("P_LowStockThreshold")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("P_IsActive")),
                            SKU = reader.GetString(reader.GetOrdinal("P_SKU")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("P_CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("P_UpdatedAt"))
                        };
                    }

                    wishlist.WishlistItems.Add(wishlistItem);
                }
            }

            return wishlists.Values;
        }

        public async Task<GlobalWishlistStats> GetGlobalStatsAsync()
        {
 
            int totalUsers = 0;
            int totalWishlists = 0;
            int totalItems = 0;
            decimal totalValue = 0;
            int publicWishlists = 0;
            int privateWishlists = 0;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var userCommand = new NpgsqlCommand("SELECT COUNT(1) FROM \"AspNetUsers\"", connection);
            totalUsers = Convert.ToInt32(await userCommand.ExecuteScalarAsync());

            await using var wishlistCommand = new NpgsqlCommand("SELECT COUNT(1) FROM Wishlists", connection);
            totalWishlists = Convert.ToInt32(await wishlistCommand.ExecuteScalarAsync());

            await using var itemCommand = new NpgsqlCommand("SELECT COUNT(1) FROM WishlistItems", connection);
            totalItems = Convert.ToInt32(await itemCommand.ExecuteScalarAsync());

            await using var valueCommand = new NpgsqlCommand(@"
                SELECT COALESCE(SUM(p.Price), 0)
                FROM WishlistItems wi
                JOIN Wishlists w ON wi.WishlistId = w.Id
                JOIN Products p ON wi.ProductId = p.Id", connection);
            totalValue = Convert.ToDecimal(await valueCommand.ExecuteScalarAsync());

            await using var publicCommand = new NpgsqlCommand("SELECT COUNT(1) FROM Wishlists WHERE IsPublic = true", connection);
            publicWishlists = Convert.ToInt32(await publicCommand.ExecuteScalarAsync());
            privateWishlists = totalWishlists - publicWishlists;

            var averageItemsPerWishlist = totalWishlists > 0 ? (double)totalItems / totalWishlists : 0;
            var averageWishlistsPerUser = totalUsers > 0 ? (double)totalWishlists / totalUsers : 0;

            var recentActivity = new List<RecentActivity>();
            var today = DateTime.Today;

            for (int i = 0; i < 5; i++)
            {
                var date = today.AddDays(-i);
                var dateString = date.ToString("yyyy-MM-dd");

                // New Wishlists
                await using var newWishlistsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM Wishlists
                    WHERE DATE(CreatedAt) = @Date", connection);
                newWishlistsCommand.Parameters.AddWithValue("Date", dateString);
                var newWishlists = Convert.ToInt32(await newWishlistsCommand.ExecuteScalarAsync());

                // New Items
                await using var newItemsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM WishlistItems
                    WHERE DATE(CreatedAt) = @Date", connection);
                newItemsCommand.Parameters.AddWithValue("Date", dateString);
                var newItems = Convert.ToInt32(await newItemsCommand.ExecuteScalarAsync());

                recentActivity.Add(new RecentActivity
                {
                    Date = dateString,
                    NewWishlists = newWishlists,
                    NewItems = newItems
                });
            }

            return new GlobalWishlistStats
            {
                TotalUsers = totalUsers,
                TotalWishlists = totalWishlists,
                TotalItems = totalItems,
                TotalValue = totalValue,
                AverageItemsPerWishlist = averageItemsPerWishlist,
                AverageWishlistsPerUser = averageWishlistsPerUser,
                PublicWishlists = publicWishlists,
                PrivateWishlists = privateWishlists,
                RecentActivity = recentActivity
            };
        }
    }
}