using AutoMapper;
using Berryfy.Application.Dtos.CategoryDtos.Responses;
using Berryfy.Application.Dtos.ProductDtos.Responses;
using Berryfy.Application.Dtos.WishlistDtos.Requests;
using Berryfy.Application.Dtos.WishlistDtos.Responses;
using Berryfy.Application.Services.Interfaces.WishlistServiceInterfaces;
using Berryfy.Domain.Entities.WishlistEntities;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Berryfy.Domain.Repositories.WishlistInterfaces;

namespace Berryfy.Application.Services.Concretes.WishlistServiceConcretes
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IProductRepository _productRepository;

        public WishlistService(IWishlistRepository wishlistRepository, IProductRepository productRepository)
        {
            _wishlistRepository = wishlistRepository;
            _productRepository = productRepository;

        }

        public async Task<WishlistResponse> GetByIdAsync(int id)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(id);
            return wishlist == null ? null : MapToDto(wishlist);
        }

        public async Task<WishlistResponse> GetUserDefaultWishlistAsync(int userId)
        {
            var wishlist = await _wishlistRepository.GetUserDefaultWishlistAsync(userId);
            return MapToDto(wishlist);
        }

        public async Task<IEnumerable<WishlistResponse>> GetUserWishlistsAsync(int userId)
        {
            var wishlists = await _wishlistRepository.GetUserWishlistsAsync(userId);
            return wishlists.Select(MapToDto);
        }

        public async Task<WishlistResponse> CreateAsync(int userId, CreateWishlist createWishlistDto)
        {
            var wishlist = new Wishlist
            {
                UserId = userId,
                Name = createWishlistDto.Name,
                IsPublic = createWishlistDto.IsPublic,
                IsDefault = false // Only the first wishlist should be default
            };

            var userWishlistCount = await _wishlistRepository.GetUserWishlistCountAsync(userId);
            if (userWishlistCount == 0)
            {
                wishlist.IsDefault = true;
            }

            var createdWishlist = await _wishlistRepository.CreateAsync(wishlist);
            return MapToDto(createdWishlist);
        }

        public async Task<WishlistResponse> UpdateAsync(int id, UpdateWishlist updateWishlistDto)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(id);
            if (wishlist == null) return null;

            wishlist.Name = updateWishlistDto.Name;
            wishlist.IsPublic = updateWishlistDto.IsPublic;

            var updatedWishlist = await _wishlistRepository.UpdateAsync(wishlist);
            return MapToDto(updatedWishlist);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(id);
            if (wishlist == null) return false;


            if (wishlist.IsDefault)
            {
                var userWishlistCount = await _wishlistRepository.GetUserWishlistCountAsync(wishlist.UserId);
                if (userWishlistCount <= 1) return false;

                var userWishlists = await _wishlistRepository.GetUserWishlistsAsync(wishlist.UserId);
                var nextWishlist = userWishlists.FirstOrDefault(w => w.Id != id);
                if (nextWishlist != null)
                {
                    nextWishlist.IsDefault = true;
                    await _wishlistRepository.UpdateAsync(nextWishlist);
                }
            }

            return await _wishlistRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _wishlistRepository.ExistsAsync(id);
        }

        public async Task<Dtos.WishlistDtos.Responses.WishlistItem> AddItemAsync(int userId, AddToWishlist addToWishlistDto)
        {
            Wishlist wishlist;
            if (addToWishlistDto.WishlistId.HasValue)
            {
                wishlist = await _wishlistRepository.GetByIdAsync(addToWishlistDto.WishlistId.Value);
                if (wishlist == null || wishlist.UserId != userId) return null;
            }
            else
            {
                wishlist = await _wishlistRepository.GetUserDefaultWishlistAsync(userId);
            }

            var productExists = await _productRepository.ExistsByIdAsync(addToWishlistDto.ProductId);
            if (!productExists) return null;

            var existingItem = await _wishlistRepository.GetWishlistItemAsync(wishlist.Id, addToWishlistDto.ProductId);
            if (existingItem != null) return MapToItemDto(existingItem);

            var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = addToWishlistDto.ProductId,
                Notes = addToWishlistDto.Notes,
                Priority = addToWishlistDto.Priority
            };

            var addedItem = await _wishlistRepository.AddItemAsync(wishlistItem);
            return MapToItemDto(addedItem);
        }

        public async Task<Dtos.WishlistDtos.Responses.WishlistItem> UpdateItemAsync(int wishlistId, int productId, UpdateWishlistItem updateItemDto)
        {
            var existingItem = await _wishlistRepository.GetWishlistItemAsync(wishlistId, productId);
            if (existingItem == null) return null;

            existingItem.Notes = updateItemDto.Notes;
            existingItem.Priority = updateItemDto.Priority;

            var updatedItem = await _wishlistRepository.UpdateItemAsync(existingItem);
            return MapToItemDto(updatedItem);
        }

        public async Task<bool> RemoveItemAsync(int wishlistId, int productId)
        {
            return await _wishlistRepository.RemoveItemAsync(wishlistId, productId);
        }

        public async Task<bool> IsProductInWishlistAsync(int userId, int productId)
        {
            return await _wishlistRepository.IsProductInWishlistAsync(userId, productId);
        }

        public async Task<IEnumerable<Dtos.WishlistDtos.Responses.WishlistItem>> GetWishlistItemsAsync(int wishlistId)
        {
            var items = await _wishlistRepository.GetWishlistItemsAsync(wishlistId);
            return items.Select(MapToItemDto);
        }

        public async Task<bool> AddMultipleItemsAsync(int userId, int wishlistId, List<int> productIds)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(wishlistId);
            if (wishlist == null || wishlist.UserId != userId) return false;

            foreach (var productId in productIds)
            {
                var productExists = await _productRepository.ExistsByIdAsync(productId);
                if (!productExists) continue;

                var existingItem = await _wishlistRepository.GetWishlistItemAsync(wishlistId, productId);
                if (existingItem != null) continue;

                var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                {
                    WishlistId = wishlistId,
                    ProductId = productId,
                    Priority = 1
                };

                await _wishlistRepository.AddItemAsync(wishlistItem);
            }

            return true;
        }

        public async Task<bool> RemoveMultipleItemsAsync(int wishlistId, List<int> productIds)
        {
            foreach (var productId in productIds)
            {
                await _wishlistRepository.RemoveItemAsync(wishlistId, productId);
            }
            return true;
        }

        public async Task<bool> MoveItemsToWishlistAsync(int fromWishlistId, int toWishlistId, List<int> productIds)
        {
            var fromWishlist = await _wishlistRepository.GetByIdAsync(fromWishlistId);
            var toWishlist = await _wishlistRepository.GetByIdAsync(toWishlistId);

            if (fromWishlist == null || toWishlist == null || fromWishlist.UserId != toWishlist.UserId)
                return false;

            foreach (var productId in productIds)
            {
                var item = await _wishlistRepository.GetWishlistItemAsync(fromWishlistId, productId);
                if (item == null) continue;

                var existingInTarget = await _wishlistRepository.GetWishlistItemAsync(toWishlistId, productId);
                if (existingInTarget != null) continue;

                item.WishlistId = toWishlistId;
                await _wishlistRepository.UpdateItemAsync(item);
            }

            return true;
        }

        public async Task<bool> ClearWishlistAsync(int wishlistId)
        {
            var items = await _wishlistRepository.GetWishlistItemsAsync(wishlistId);
            foreach (var item in items)
            {
                await _wishlistRepository.RemoveItemAsync(wishlistId, item.ProductId);
            }
            return true;
        }

        public async Task<WishlistSummary> GetUserSummaryAsync(int userId)
        {
            var totalWishlists = await _wishlistRepository.GetUserWishlistCountAsync(userId);
            var totalItems = await _wishlistRepository.GetUserTotalItemsAsync(userId);
            var totalValue = await _wishlistRepository.GetUserTotalValueAsync(userId);
            var recentWishlists = (await GetUserWishlistsAsync(userId)).Take(3).ToList();

            return new WishlistSummary
            {
                TotalWishlists = totalWishlists,
                TotalItems = totalItems,
                TotalValue = totalValue,
                RecentWishlists = recentWishlists
            };
        }

        public async Task<bool> ShareWishlistAsync(int wishlistId, bool isPublic)
        {
            var wishlist = await _wishlistRepository.GetByIdAsync(wishlistId);
            if (wishlist == null) return false;

            wishlist.IsPublic = isPublic;
            await _wishlistRepository.UpdateAsync(wishlist);
            return true;
        }

        public async Task<WishlistResponse> DuplicateWishlistAsync(int wishlistId, string newName)
        {
            var originalWishlist = await _wishlistRepository.GetByIdAsync(wishlistId);
            if (originalWishlist == null) return null;

            var newWishlist = new Wishlist
            {
                UserId = originalWishlist.UserId,
                Name = newName,
                IsPublic = false,
                IsDefault = false
            };

            var createdWishlist = await _wishlistRepository.CreateAsync(newWishlist);


            foreach (var item in originalWishlist.WishlistItems)
            {
                var newItem = new Domain.Entities.WishlistEntities.WishlistItem
                {
                    WishlistId = createdWishlist.Id,
                    ProductId = item.ProductId,
                    Notes = item.Notes,
                    Priority = item.Priority
                };

                await _wishlistRepository.AddItemAsync(newItem);
            }

            return await GetByIdAsync(createdWishlist.Id);
        }

        public async Task<IEnumerable<WishlistResponse>> GetAllWishlistsAsync()
        {
            var allWishlists = await _wishlistRepository.GetAllWishlistsAsync();
            return allWishlists.Select(MapToDto);
        }

        public async Task<GlobalWishlistStats> GetGlobalStatsAsync()
        {
            var globalStats = await _wishlistRepository.GetGlobalStatsAsync();
            return new GlobalWishlistStats
            {
                AverageItemsPerWishlist = globalStats.AverageItemsPerWishlist,
                TotalUsers = globalStats.TotalUsers,
                TotalWishlists = globalStats.TotalWishlists,
                TotalItems = globalStats.TotalItems,
                TotalValue = globalStats.TotalValue,
                AverageWishlistsPerUser = globalStats.AverageWishlistsPerUser,
                PublicWishlists = globalStats.PublicWishlists,
                PrivateWishlists = globalStats.PrivateWishlists,
                RecentActivity = globalStats.RecentActivity.Select(a => new RecentActivity
                {
                    Date = a.Date,
                    NewWishlists = a.NewWishlists,
                    NewItems = a.NewItems
                }).ToList()
            };
        }

        private WishlistResponse MapToDto(Wishlist wishlist)
        {
            return new WishlistResponse
            {
                Id = wishlist.Id,
                UserId = wishlist.UserId,
                Name = wishlist.Name,
                IsDefault = wishlist.IsDefault,
                IsPublic = wishlist.IsPublic,
                CreatedDate = wishlist.CreatedAt,
                UpdatedDate = wishlist.UpdatedAt,
                ItemCount = wishlist.WishlistItems?.Count ?? 0,
                TotalValue = wishlist.WishlistItems?.Sum(x => x.Product?.Price ?? 0) ?? 0,
                Items = wishlist.WishlistItems?.Select(MapToItemDto).ToList() ?? new List<Dtos.WishlistDtos.Responses.WishlistItem>()
            };
        }

        private Dtos.WishlistDtos.Responses.WishlistItem MapToItemDto(Domain.Entities.WishlistEntities.WishlistItem item)
        {
            return new Dtos.WishlistDtos.Responses.WishlistItem
            {
                Id = item.Id,
                WishlistId = item.WishlistId,
                ProductId = item.ProductId,
                Notes = item.Notes,
                Priority = item.Priority,
                AddedDate = item.CreatedAt,
                Product = item.Product != null ? MapToProductDto(item.Product) : null
            };
        }

        private ProductResponse MapToProductDto(Domain.Entities.ProductEntities.Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                ProductCategories = product.ProductCategories?.Select(pc => new CategoryResponse
                {
                    Id = pc.Category.Id,
                    Name = pc.Category.Name,
                    Description = pc.Category.Description
                }).ToList() ?? new List<CategoryResponse>()
            };
        }
    }
}
