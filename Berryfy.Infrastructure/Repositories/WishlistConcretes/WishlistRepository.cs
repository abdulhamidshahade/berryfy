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
                FROM Wishlists w
                WHERE w.id = @Id
                ORDER BY w.id;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Wishlist wishlist = MapWishlist(reader);

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = true,
                Message = "Wishlist found",
                Value = wishlist
            };
        }

        public async Task<InfrastructureResponse<Wishlist>> GetUserDefaultWishlistAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at,
                FROM wishlists w
                WHERE w.user_id = @UserId AND w.is_default = true
                ORDER BY w.id;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            Wishlist wishlist = new Wishlist();

            if (await reader.ReadAsync())
            {
                wishlist = MapWishlist(reader);
            }   

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
                INSERT INTO Wishlists (user_id, name, is_default, is_public, created_at, updated_at)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            var connection = await OpenConnectionAsync();

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
            Wishlist wishlist = new Wishlist();

            if (await reader.ReadAsync())
            {
                wishlist = MapWishlist(reader);
            }

            return new InfrastructureResponse<Wishlist>()
            {
                IsSuccess = true,
                Message = "Default wishlist created",
                Value = wishlist
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetUserWishlistsAsync(int userId)
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at,
                FROM wishlists w
                WHERE w.user_id = @UserId;";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            List<Wishlist> wishlists = new List<Wishlist>();

            while (await reader.ReadAsync())
            {
                wishlists.Add(MapWishlist(reader));
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
                INSERT INTO Wishlists (user_id, name, is_default, is_public, created_at, updated_at)
                VALUES (@UserId, @Name, @IsDefault, @IsPublic, @CreatedAt, @UpdatedAt)
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            var connection = await OpenConnectionAsync();

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
                UPDATE Wishlists
                SET name = @Name,
                    is_default = @IsDefault,
                    is_public = @IsPublic,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, user_id, name, is_default, is_public, created_at, updated_at";

            var connection = await OpenConnectionAsync();

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
                Message = "Wishlist updated",
                Value = wishlist
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Wishlists WHERE id = @Id";

            var connection = await OpenConnectionAsync();

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
            const string sql = "SELECT COUNT(1) FROM Wishlists WHERE id = @Id";

            var connection = await OpenConnectionAsync();

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
                FROM wishlist_items wi
                WHERE wi.wishlist_id = @WishlistId AND wi.product_id = @ProductId";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@WishlistId", wishlistId);
            command.Parameters.AddWithValue("@ProductId", productId);

            await using var reader = await command.ExecuteReaderAsync();
            WishlistItem wishlistItem = new WishlistItem();

            if (await reader.ReadAsync())
            {
                wishlistItem = MapWishlistItem(reader);
            }

            return new InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>()
            {
                IsSuccess = true,
                Message = "Wishlist item found",
                Value = wishlistItem
            };
        }

        public async Task<InfrastructureResponse<Domain.Entities.WishlistEntities.WishlistItem>> AddItemAsync(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            const string sql = @"
                INSERT INTO wishlist_items (wishlist_id, product_id, notes, priority, created_at, updated_at)
                VALUES (@WishlistId, @ProductId, @Notes, @Priority, @CreatedAt, @UpdatedAt)
                RETURNING id, wishlist_id, product_id, notes, priority, created_at, updated_at";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            AddWithlistItemParameters(command, item);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                item = MapWishlistItem(reader);
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
                SET notes = @Notes,
                    priority = @Priority,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING id, wishlist_id, product_id, notes, priority, created_at, updated_at";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            AddWithlistItemParameters(command, item);
            command.Parameters.AddWithValue("Id", item.Id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                item = MapWishlistItem(reader);
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
            string sql = "DELETE FROM wishlist_items WHERE wishlist_id = @WishlistId AND product_id = @ProductId";

            var connection = await OpenConnectionAsync();

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

            var connection = await OpenConnectionAsync();

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
                FROM wishlist_items wi
                WHERE wi.wishlist_id = @WishlistId
                ORDER BY wi.created_at DESC";

            var connection = await OpenConnectionAsync();

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
            const string sql = "SELECT COUNT(1) FROM Wishlists WHERE user_id = @UserId";

            var connection = await OpenConnectionAsync();

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
                JOIN Wishlists w ON (wi.wishlist_id = w.id)
                WHERE w.user_id = @UserId";

            var connection = await OpenConnectionAsync();

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

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var result = await command.ExecuteScalarAsync();
            return new InfrastructureResponse<decimal>()
            {
                IsSuccess = true,
                Message = "User total value retrieved",
                Value = result != null ? Convert.ToDecimal(result) : 0
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Wishlist>>> GetAllWishlistsAsync()
        {
            const string sql = @"
                SELECT
                    w.id, w.user_id, w.name, w.is_default, w.is_public, w.created_at, w.updated_at,
                FROM Wishlists w
                ORDER BY w.UpdatedAt DESC";

            var connection = await OpenConnectionAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();

            List<Wishlist> wishlists = new List<Wishlist>();

            while (await reader.ReadAsync())
            {
                wishlists.Add(MapWishlist(reader));
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

            var connection = await OpenConnectionAsync();

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
            var today = DateTime.Today;

            for (int i = 0; i < 5; i++)
            {
                var date = today.AddDays(-i);
                var dateString = date.ToString("yyyy-MM-dd");

                
                await using var newWishlistsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM wishlists
                    WHERE DATE(created_at) = @Date", connection);
                newWishlistsCommand.Parameters.AddWithValue("@Date", dateString);
                var newWishlists = Convert.ToInt32(await newWishlistsCommand.ExecuteScalarAsync());

                
                await using var newItemsCommand = new NpgsqlCommand(@"
                    SELECT COUNT(1)
                    FROM wishlist_items
                    WHERE DATE(created_at) = @Date", connection);
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

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Wishlist MapWishlist(NpgsqlDataReader reader)
        {
            return new Wishlist
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                IsDefault = reader.GetBoolean(reader.GetOrdinal("is_default")),
                IsPublic = reader.GetBoolean(reader.GetOrdinal("is_public")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private void AddWishlistParameters(NpgsqlCommand command, Wishlist wishlist)
        {
            command.Parameters.AddWithValue("@UserId", wishlist.UserId);
            command.Parameters.AddWithValue("@Name", (object?)wishlist.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsDefault", wishlist.IsDefault);
            command.Parameters.AddWithValue("@IsPublic", wishlist.IsPublic);
            command.Parameters.AddWithValue("@CreatedAt", wishlist.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", wishlist.UpdatedAt);
        }

        private WishlistItem MapWishlistItem(NpgsqlDataReader reader)
        {
            return new WishlistItem()
            {
                Id = reader.GetInt16(reader.GetOrdinal("id")),
                WishlistId = reader.GetInt16(reader.GetOrdinal("wishlist_id")),
                ProductId = reader.GetInt16(reader.GetOrdinal("product_id")),
                Notes = reader.GetString(reader.GetOrdinal("notes")),
                Priority = reader.GetInt16(reader.GetOrdinal("priority")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private void AddWithlistItemParameters(NpgsqlCommand command, WishlistItem item)
        {
            command.Parameters.AddWithValue("@WishlistId", item.WishlistId);
            command.Parameters.AddWithValue("@productId", item.ProductId);
            command.Parameters.AddWithValue("@Notes", item.Notes);
            command.Parameters.AddWithValue("@Priority", item.Priority);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt);
        }
    }
}