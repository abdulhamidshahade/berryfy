using Berryfy.Domain.Entities.WishlistEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.WishlistInterfaces;
using Berryfy.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Berryfy.Application.Dtos.WishlistDtos;
using Berryfy.Domain.Entities;

namespace Berryfy.Infrastructure.Repositories.WishlistConcretes
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly string _connectionString;

        public WishlistRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Wishlist>> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at
                FROM wishlists w
                WHERE w.id = @Id
                ORDER BY w.id;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            Wishlist? wishlist = null;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    wishlist = MapWishlist(reader);
                }
            }

            if (wishlist != null)
            {
                await LoadWishlistItemsAsync(connection, new List<Wishlist> { wishlist });
            }

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = wishlist != null,
                Message = wishlist != null ? "Wishlist found" : "Wishlist not found",
                Value = wishlist!
            };
        }

        public async Task<InfrastructureResponse<Wishlist>> GetUserDefaultWishlistAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at
                FROM wishlists w
                WHERE w.user_id = @UserId AND w.is_default = true
                ORDER BY w.id
                LIMIT 1;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            Wishlist? wishlist = null;
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    wishlist = MapWishlist(reader);
                }
            }

            if (wishlist == null)
            {
                return await CreateDefaultWishlistForUser(userId);
            }

            await LoadWishlistItemsAsync(connection, new List<Wishlist> { wishlist });

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = true,
                Message = "Default wishlist found",
                Value = wishlist
            };
        }

        private async Task<InfrastructureResponse<Wishlist>> CreateDefaultWishlistForUser(int userId)
        {
            const string insertSql = @"
                INSERT INTO wishlists (user_id, name, is_default, is_public, created_at, updated_at)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(insertSql, connection);
            AddWishlistParameters(command, new Wishlist
            {
                UserId = userId,
                Name = "Default Wishlist",
                IsDefault = true,
                IsPublic = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await using var reader = await command.ExecuteReaderAsync();
            Wishlist? wishlist = null;

            if (await reader.ReadAsync())
            {
                wishlist = MapWishlist(reader);
            }

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = wishlist != null,
                Message = "Default wishlist created",
                Value = wishlist!
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetUserWishlistsAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at
                FROM wishlists w
                WHERE w.user_id = @UserId
                ORDER BY w.is_default DESC, w.created_at DESC;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            List<Wishlist> wishlists = new List<Wishlist>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    wishlists.Add(MapWishlist(reader));
                }
            }

            if (wishlists.Count > 0)
            {
                await LoadWishlistItemsAsync(connection, wishlists);
            }

            return new InfrastructureResponse<IEnumerable<Wishlist>>()
            {
                IsSuccess = true,
                Message = "Wishlists found",
                Value = wishlists
            };
        }

        public async Task<InfrastructureResponse<Wishlist>> CreateAsync(Wishlist wishlist)
        {
            const string sql = @"
                INSERT INTO wishlists (user_id, name, is_default, is_public, created_at, updated_at)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            var now = DateTime.UtcNow;
            if (wishlist.CreatedAt == default) wishlist.CreatedAt = now;
            if (wishlist.UpdatedAt == default) wishlist.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddWishlistParameters(command, wishlist);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                wishlist = MapWishlist(reader);
            }

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = true,
                Message = "Wishlist created",
                Value = wishlist
            };
        }

        public async Task<InfrastructureResponse<Wishlist>> UpdateAsync(Wishlist wishlist)
        {
            const string sql = @"
                UPDATE wishlists
                SET name = @Name,
                    is_default = @IsDefault,
                    is_public = @IsPublic,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            wishlist.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddWishlistParameters(command, wishlist);
            command.Parameters.AddWithValue("@Id", wishlist.Id);

            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    wishlist = MapWishlist(reader);
                }
            }

            await LoadWishlistItemsAsync(connection, new List<Wishlist> { wishlist });

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = true,
                Message = "Wishlist updated",
                Value = wishlist
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM wishlists WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Wishlist deleted",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM wishlists WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Wishlist exists",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>> GetWishlistItemAsync(int wishlistId, int productId)
        {
            const string sql = @"
                SELECT
                    wi.id, wi.wishlist_id, wi.product_id, wi.notes, wi.priority, wi.created_at, wi.updated_at,
                    p.id AS p_id, p.name AS p_name, p.description AS p_description, p.price AS p_price,
                    p.image_url AS p_image_url, p.is_active AS p_is_active, p.stock_quantity AS p_stock_quantity,
                    p.reserved_stock AS p_reserved_stock, p.low_stock_threshold AS p_low_stock_threshold,
                    p.sku AS p_sku, p.category_id AS p_category_id
                FROM wishlist_items wi
                LEFT JOIN products p ON wi.product_id = p.id
                WHERE wi.wishlist_id = @WishlistId AND wi.product_id = @ProductId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@WishlistId", wishlistId);
            command.Parameters.AddWithValue("@ProductId", productId);

            await using var reader = await command.ExecuteReaderAsync();
            WishlistItem? wishlistItem = null;

            if (await reader.ReadAsync())
            {
                wishlistItem = MapWishlistItem(reader);
            }

            return new InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>()
            {
                IsSuccess = wishlistItem != null,
                Message = wishlistItem != null ? "Wishlist item found" : "Wishlist item not found",
                Value = wishlistItem!
            };
        }

        public async Task<InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>> AddItemAsync(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            const string sql = @"
                INSERT INTO wishlist_items (wishlist_id, product_id, notes, priority, created_at, updated_at)
                VALUES (@WishlistId, @ProductId, @Notes, @Priority, @CreatedAt, @UpdatedAt)
                RETURNING id, wishlist_id, product_id, notes, priority, created_at, updated_at";

            var now = DateTime.UtcNow;
            if (item.CreatedAt == default) item.CreatedAt = now;
            if (item.UpdatedAt == default) item.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddWithlistItemParameters(command, item);

            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    item = MapWishlistItem(reader);
                }
            }

            var loaded = await GetWishlistItemAsync(item.WishlistId, item.ProductId);
            if (loaded.Value != null)
            {
                item = loaded.Value;
            }

            return new InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>()
            {
                IsSuccess = true,
                Message = "Wishlist item added",
                Value = item
            };
        }

        public async Task<InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>> UpdateItemAsync(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            const string sql = @"
                UPDATE wishlist_items
                SET wishlist_id = @WishlistId,
                    notes = @Notes,
                    priority = @Priority,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, wishlist_id, product_id, notes, priority, created_at, updated_at";

            item.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            AddWithlistItemParameters(command, item);
            command.Parameters.AddWithValue("@Id", item.Id);

            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    item = MapWishlistItem(reader);
                }
            }

            var loaded = await GetWishlistItemAsync(item.WishlistId, item.ProductId);
            if (loaded.Value != null)
            {
                item = loaded.Value;
            }

            return new InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>()
            {
                IsSuccess = true,
                Message = "Wishlist item updated",
                Value = item
            };
        }

        public async Task<InfrastructureResponse<bool>> RemoveItemAsync(int wishlistId, int productId)
        {
            const string sql = "DELETE FROM wishlist_items WHERE wishlist_id = @WishlistId AND product_id = @ProductId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@WishlistId", wishlistId);
            command.Parameters.AddWithValue("@ProductId", productId);

            var rowEffected = await command.ExecuteNonQueryAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Wishlist item removed",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<bool>> IsProductInWishlistAsync(int userId, int productId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM wishlist_items wi
                JOIN wishlists w ON wi.wishlist_id = w.id
                WHERE w.user_id = @UserId AND wi.product_id = @ProductId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<bool>()
            {
                IsSuccess = true,
                Message = "Product in wishlist checked",
                Value = count != null && Convert.ToInt32(count) > 0
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Domain.Entities.WishlistEntities.WishlistItem>>> GetWishlistItemsAsync(int wishlistId)
        {
            const string sql = @"
                SELECT
                    wi.id, wi.wishlist_id, wi.product_id, wi.notes, wi.priority, wi.created_at, wi.updated_at,
                    p.id AS p_id, p.name AS p_name, p.description AS p_description, p.price AS p_price,
                    p.image_url AS p_image_url, p.is_active AS p_is_active, p.stock_quantity AS p_stock_quantity,
                    p.reserved_stock AS p_reserved_stock, p.low_stock_threshold AS p_low_stock_threshold,
                    p.sku AS p_sku, p.category_id AS p_category_id
                FROM wishlist_items wi
                LEFT JOIN products p ON wi.product_id = p.id
                WHERE wi.wishlist_id = @WishlistId
                ORDER BY wi.created_at DESC";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@WishlistId", wishlistId);

            await using var reader = await command.ExecuteReaderAsync();

            var wishlistItems = new List<Domain.Entities.WishlistEntities.WishlistItem>();

            while (await reader.ReadAsync())
            {
                wishlistItems.Add(MapWishlistItem(reader));
            }

            return new InfrastructureResponse<IEnumerable<Domain.Entities.WishlistEntities.WishlistItem>>()
            {
                IsSuccess = true,
                Message = "Wishlist items retrieved",
                Value = wishlistItems
            };
        }

        public async Task<InfrastructureResponse<int>> GetUserWishlistCountAsync(int userId)
        {
            const string sql = "SELECT COUNT(1) FROM wishlists WHERE user_id = @UserId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "User wishlist count retrieved",
                Value = count != null ? Convert.ToInt32(count) : 0
            };
        }

        public async Task<InfrastructureResponse<int>> GetUserTotalItemsAsync(int userId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM wishlist_items wi
                JOIN wishlists w ON (wi.wishlist_id = w.id)
                WHERE w.user_id = @UserId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var count = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "User total items retrieved",
                Value = count != null ? Convert.ToInt32(count) : 0
            };
        }

        public async Task<InfrastructureResponse<decimal>> GetUserTotalValueAsync(int userId)
        {
            const string sql = @"
                SELECT COALESCE(SUM(p.price), 0)
                FROM wishlist_items wi
                JOIN wishlists w ON (wi.wishlist_id = w.id)
                JOIN products p ON (wi.product_id = p.id)
                WHERE w.user_id = @UserId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<decimal>()
            {
                IsSuccess = true,
                Message = "User total value retrieved",
                Value = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetAllWishlistsAsync()
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at
                FROM wishlists w
                ORDER BY w.updated_at DESC";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            List<Wishlist> wishlists = new List<Wishlist>();
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    wishlists.Add(MapWishlist(reader));
                }
            }

            if (wishlists.Count > 0)
            {
                await LoadWishlistItemsAsync(connection, wishlists);
            }

            return new InfrastructureResponse<IEnumerable<Wishlist>>()
            {
                IsSuccess = true,
                Message = "All wishlists retrieved",
                Value = wishlists
            };
        }

        public async Task<InfrastructureResponse<GlobalWishlistStats>> GetGlobalStatsAsync()
        {
            int totalUsers = 0;
            int totalWishlists = 0;
            int totalItems = 0;
            decimal totalValue = 0;
            int publicWishlists = 0;
            int privateWishlists = 0;

            await using var connection = await OpenConnectionAsync();

            await using var userCommand = new NpgsqlCommand("SELECT COUNT(1) FROM users", connection);
            totalUsers = Convert.ToInt32(await userCommand.ExecuteScalarAsync());

            await using var wishlistCommand = new NpgsqlCommand("SELECT COUNT(1) FROM wishlists", connection);
            totalWishlists = Convert.ToInt32(await wishlistCommand.ExecuteScalarAsync());

            await using var itemCommand = new NpgsqlCommand("SELECT COUNT(1) FROM wishlist_items", connection);
            totalItems = Convert.ToInt32(await itemCommand.ExecuteScalarAsync());

            await using var valueCommand = new NpgsqlCommand(@"
                SELECT COALESCE(SUM(p.price), 0)
                FROM wishlist_items wi
                JOIN wishlists w ON (wi.wishlist_id = w.id)
                JOIN products p ON (wi.product_id = p.id)", connection);
            totalValue = Convert.ToDecimal(await valueCommand.ExecuteScalarAsync());

            await using var publicCommand = new NpgsqlCommand("SELECT COUNT(1) FROM wishlists WHERE is_public = true", connection);
            publicWishlists = Convert.ToInt32(await publicCommand.ExecuteScalarAsync());
            privateWishlists = totalWishlists - publicWishlists;

            var averageItemsPerWishlist = totalWishlists > 0 ? (double)totalItems / totalWishlists : 0;
            var averageWishlistsPerUser = totalUsers > 0 ? (double)totalWishlists / totalUsers : 0;

            var recentActivity = new List<RecentActivity>();
            var today = DateTime.UtcNow.Date;

            for (int i = 0; i < 5; i++)
            {
                var date = today.AddDays(-i);
                var dateString = date.ToString("yyyy-MM-dd");

                await using var newWishlistsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM wishlists
                    WHERE DATE(created_at) = CAST(@Date AS DATE)", connection);
                newWishlistsCommand.Parameters.AddWithValue("@Date", dateString);
                var newWishlists = Convert.ToInt32(await newWishlistsCommand.ExecuteScalarAsync());

                await using var newItemsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM wishlist_items
                    WHERE DATE(created_at) = CAST(@Date AS DATE)", connection);
                newItemsCommand.Parameters.AddWithValue("@Date", dateString);
                var newItems = Convert.ToInt32(await newItemsCommand.ExecuteScalarAsync());

                recentActivity.Add(new RecentActivity
                {
                    Date = dateString,
                    NewWishlists = newWishlists,
                    NewItems = newItems
                });
            }

            return new InfrastructureResponse<GlobalWishlistStats>()
            {
                IsSuccess = true,
                Message = "Global wishlist stats retrieved",
                Value = new GlobalWishlistStats
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
                }
            };
        }

        private async Task LoadWishlistItemsAsync(NpgsqlConnection connection, IList<Wishlist> wishlists)
        {
            if (wishlists.Count == 0) return;

            var wishlistIds = wishlists.Select(w => w.Id).Distinct().ToArray();
            const string sql = @"
                SELECT
                    wi.id, wi.wishlist_id, wi.product_id, wi.notes, wi.priority, wi.created_at, wi.updated_at,
                    p.id AS p_id, p.name AS p_name, p.description AS p_description, p.price AS p_price,
                    p.image_url AS p_image_url, p.is_active AS p_is_active, p.stock_quantity AS p_stock_quantity,
                    p.reserved_stock AS p_reserved_stock, p.low_stock_threshold AS p_low_stock_threshold,
                    p.sku AS p_sku, p.category_id AS p_category_id
                FROM wishlist_items wi
                LEFT JOIN products p ON wi.product_id = p.id
                WHERE wi.wishlist_id = ANY(@WishlistIds)
                ORDER BY wi.created_at DESC;";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@WishlistIds", wishlistIds);

            var lookup = wishlists.ToDictionary(w => w.Id);
            foreach (var w in wishlists)
            {
                w.WishlistItems ??= new List<WishlistItem>();
                w.WishlistItems.Clear();
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = MapWishlistItem(reader);
                if (lookup.TryGetValue(item.WishlistId, out var parent))
                {
                    parent.WishlistItems.Add(item);
                }
            }
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Wishlist MapWishlist(NpgsqlDataReader reader)
        {
            var nameOrd = reader.GetOrdinal("name");
            return new Wishlist
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                UserId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("user_id"))),
                Name = reader.IsDBNull(nameOrd) ? "My Wishlist" : reader.GetString(nameOrd),
                IsDefault = reader.GetBoolean(reader.GetOrdinal("is_default")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
                WishlistItems = new List<WishlistItem>()
            };
        }

        private void AddWishlistParameters(NpgsqlCommand command, Wishlist wishlist)
        {
            command.Parameters.AddWithValue("@UserId", wishlist.UserId);
            command.Parameters.AddWithValue("@Name", (object?)wishlist.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsDefault", wishlist.IsDefault);
            command.Parameters.AddWithValue("@IsPublic", wishlist.IsPublic);
            command.Parameters.AddWithValue("@CreatedAt", wishlist.CreatedAt == default ? DateTime.UtcNow : wishlist.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", wishlist.UpdatedAt == default ? DateTime.UtcNow : wishlist.UpdatedAt);
        }

        private WishlistItem MapWishlistItem(NpgsqlDataReader reader)
        {
            var notesOrd = reader.GetOrdinal("notes");
            var item = new WishlistItem()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                WishlistId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("wishlist_id"))),
                ProductId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("product_id"))),
                Notes = reader.IsDBNull(notesOrd) ? null : reader.GetString(notesOrd),
                Priority = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("priority"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };

            try
            {
                int pIdOrd = reader.GetOrdinal("p_id");
                if (!reader.IsDBNull(pIdOrd))
                {
                    int pDescOrd = reader.GetOrdinal("p_description");
                    int pImgOrd = reader.GetOrdinal("p_image_url");
                    int pSkuOrd = reader.GetOrdinal("p_sku");
                    int pCatOrd = reader.GetOrdinal("p_category_id");

                    item.Product = new Product
                    {
                        Id = Convert.ToInt32(reader.GetValue(pIdOrd)),
                        Name = reader.GetString(reader.GetOrdinal("p_name")),
                        Description = reader.IsDBNull(pDescOrd) ? string.Empty : reader.GetString(pDescOrd),
                        Price = reader.GetDecimal(reader.GetOrdinal("p_price")),
                        ImageUrl = reader.IsDBNull(pImgOrd) ? string.Empty : reader.GetString(pImgOrd),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("p_is_active")),
                        StockQuantity = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("p_stock_quantity"))),
                        ReservedStock = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("p_reserved_stock"))),
                        LowStockThreshold = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("p_low_stock_threshold"))),
                        SKU = reader.IsDBNull(pSkuOrd) ? string.Empty : reader.GetString(pSkuOrd),
                        CategoryId = reader.IsDBNull(pCatOrd) ? 0 : Convert.ToInt32(reader.GetValue(pCatOrd))
                    };
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Query did not join products
            }

            return item;
        }

        private void AddWithlistItemParameters(NpgsqlCommand command, WishlistItem item)
        {
            command.Parameters.AddWithValue("@WishlistId", item.WishlistId);
            command.Parameters.AddWithValue("@ProductId", item.ProductId);
            command.Parameters.AddWithValue("@Notes", (object?)item.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@Priority", item.Priority);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt == default ? DateTime.UtcNow : item.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt == default ? DateTime.UtcNow : item.UpdatedAt);
        }
    }
}