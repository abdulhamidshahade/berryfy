using Berryfy.Application.Dtos;
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

        public async Task<ApplicationResponse<WishlistResponse>> GetByIdAsync(int id)
        {
            var wishlist = _wishlistRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<WishlistResponse>
            {
                IsSuccess = wishlist != null,
                Value = wishlist == null ? null : MapToDto(wishlist)
            };
        }

        public async Task<ApplicationResponse<WishlistResponse>> GetUserDefaultWishlistAsync(int userId)
        {
            var wishlist = _wishlistRepository.GetUserDefaultWishlistAsync(userId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<WishlistResponse>
            {
                IsSuccess = wishlist != null,
                Value = wishlist == null ? null : MapToDto(wishlist)
            };
        }

        public async Task<ApplicationResponse<IEnumerable<WishlistResponse>>> GetUserWishlistsAsync(int userId)
        {
            var wishlists = _wishlistRepository.GetUserWishlistsAsync(userId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<WishlistResponse>>
            {
                IsSuccess = true,
                Value = wishlists.Select(MapToDto)
            };
        }

        public async Task<ApplicationResponse<WishlistResponse>> CreateAsync(int userId, CreateWishlist createWishlistDto)
        {
            var wishlist = new Wishlist
            {
                UserId = userId,
                Name = createWishlistDto.Name,
                IsPublic = createWishlistDto.IsPublic,
                IsDefault = false // Only the first wishlist should be default
            };

            var userWishlistCount = _wishlistRepository.GetUserWishlistCountAsync(userId).GetAwaiter().GetResult().Value;
            if (userWishlistCount == 0)
            {
                wishlist.IsDefault = true;
            }

            var createdWishlist = _wishlistRepository.CreateAsync(wishlist).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<WishlistResponse>
            {
                IsSuccess = true,
                Value = MapToDto(createdWishlist)
            };
        }

        public async Task<ApplicationResponse<WishlistResponse>> UpdateAsync(int id, UpdateWishlist updateWishlistDto)
        {
            var wishlist = _wishlistRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (wishlist == null) return new ApplicationResponse<WishlistResponse> { IsSuccess = false, ErrorMessage = "Wishlist not found" };

            wishlist.Name = updateWishlistDto.Name;
            wishlist.IsPublic = updateWishlistDto.IsPublic;

            var updatedWishlist = _wishlistRepository.UpdateAsync(wishlist).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<WishlistResponse>
            {
                IsSuccess = true,
                Value = MapToDto(updatedWishlist)
            };
        }

        public async Task<ApplicationResponse<bool>> DeleteAsync(int id)
        {
            var wishlist = _wishlistRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (wishlist == null) return new ApplicationResponse<bool> { IsSuccess = false, ErrorMessage = "Wishlist not found" };

            if (wishlist.IsDefault)
            {
                var userWishlistCount = _wishlistRepository.GetUserWishlistCountAsync(wishlist.UserId).GetAwaiter().GetResult().Value;
                if (userWishlistCount <= 1) return new ApplicationResponse<bool> { IsSuccess = false, ErrorMessage = "Cannot delete the default wishlist" };

                var userWishlists = _wishlistRepository.GetUserWishlistsAsync(wishlist.UserId).GetAwaiter().GetResult().Value;
                var nextWishlist = userWishlists.FirstOrDefault(w => w.Id != id);
                if (nextWishlist != null)
                {
                    nextWishlist.IsDefault = true;
                    await _wishlistRepository.UpdateAsync(nextWishlist);
                }
            }

            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = _wishlistRepository.DeleteAsync(id).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> ExistsAsync(int id)
        {
            var exists = _wishlistRepository.ExistsAsync(id).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = exists
            };
        }

        public async Task<ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem>> AddItemAsync(int userId, AddToWishlist addToWishlistDto)
        {
            Wishlist wishlist;
            if (addToWishlistDto.WishlistId.HasValue)
            {
                wishlist = _wishlistRepository.GetByIdAsync(addToWishlistDto.WishlistId.Value).GetAwaiter().GetResult().Value;
                if (wishlist == null || wishlist.UserId != userId) return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = false, ErrorMessage = "Wishlist not found" };
            }
            else
            {
                wishlist = _wishlistRepository.GetUserDefaultWishlistAsync(userId).GetAwaiter().GetResult().Value;
            }

            var productExists = _productRepository.ExistsByIdAsync(addToWishlistDto.ProductId).GetAwaiter().GetResult().Value;
            if (!productExists) return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = false, ErrorMessage = "Product not found" };

            var existingItem = _wishlistRepository.GetWishlistItemAsync(wishlist.Id, addToWishlistDto.ProductId).GetAwaiter().GetResult().Value;
            if (existingItem != null) return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = true, Value = MapToItemDto(existingItem) };

            var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = addToWishlistDto.ProductId,
                Notes = addToWishlistDto.Notes,
                Priority = addToWishlistDto.Priority
            };

            var addedItem = _wishlistRepository.AddItemAsync(wishlistItem).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = true, Value = MapToItemDto(addedItem) };
        }

        public async Task<ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem>> UpdateItemAsync(int wishlistId, int productId, UpdateWishlistItem updateItemDto)
        {
            var existingItem = _wishlistRepository.GetWishlistItemAsync(wishlistId, productId).GetAwaiter().GetResult().Value;
            if (existingItem == null) return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = false, ErrorMessage = "Item not found" };

            existingItem.Notes = updateItemDto.Notes;
            existingItem.Priority = updateItemDto.Priority;

            var updatedItem = _wishlistRepository.UpdateItemAsync(existingItem).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<Dtos.WishlistDtos.Responses.WishlistItem> { IsSuccess = true, Value = MapToItemDto(updatedItem) };
        }

        public async Task<ApplicationResponse<bool>> RemoveItemAsync(int wishlistId, int productId)
        {
            return new ApplicationResponse<bool> 
            { IsSuccess = true, Value = _wishlistRepository.RemoveItemAsync(wishlistId, productId).GetAwaiter().GetResult().Value };
        }

        public async Task<ApplicationResponse<bool>> IsProductInWishlistAsync(int userId, int productId)
        {
            return new ApplicationResponse<bool> 
            { IsSuccess = true, Value =_wishlistRepository.IsProductInWishlistAsync(userId, productId).GetAwaiter().GetResult().Value };
        }

        public async Task<ApplicationResponse<IEnumerable<Dtos.WishlistDtos.Responses.WishlistItem>>> GetWishlistItemsAsync(int wishlistId)
        {
            var items = _wishlistRepository.GetWishlistItemsAsync(wishlistId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<Dtos.WishlistDtos.Responses.WishlistItem>> { IsSuccess = true, Value = items.Select(MapToItemDto) };
        }

        public async Task<ApplicationResponse<bool>> AddMultipleItemsAsync(int userId, int wishlistId, List<int> productIds)
        {
            var wishlist = _wishlistRepository.GetByIdAsync(wishlistId).GetAwaiter().GetResult().Value;
            if (wishlist == null || wishlist.UserId != userId) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            foreach (var productId in productIds)
            {
                var productExists = _productRepository.ExistsByIdAsync(productId).GetAwaiter().GetResult().Value;
                if (!productExists) continue;

                var existingItem = _wishlistRepository.GetWishlistItemAsync(wishlistId, productId).GetAwaiter().GetResult().Value;
                if (existingItem != null) continue;

                var wishlistItem = new Domain.Entities.WishlistEntities.WishlistItem
                {
                    WishlistId = wishlistId,
                    ProductId = productId,
                    Priority = 1
                };

                await _wishlistRepository.AddItemAsync(wishlistItem);
            }

            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }

        public async Task<ApplicationResponse<bool>> RemoveMultipleItemsAsync(int wishlistId, List<int> productIds)
        {
            foreach (var productId in productIds)
            {
                await _wishlistRepository.RemoveItemAsync(wishlistId, productId);
            }
            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }

        public async Task<ApplicationResponse<bool>> MoveItemsToWishlistAsync(int fromWishlistId, int toWishlistId, List<int> productIds)
        {
            var fromWishlist = _wishlistRepository.GetByIdAsync(fromWishlistId).GetAwaiter().GetResult().Value;
            var toWishlist = _wishlistRepository.GetByIdAsync(toWishlistId).GetAwaiter().GetResult().Value;

            if (fromWishlist == null || toWishlist == null || fromWishlist.UserId != toWishlist.UserId)
                return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            foreach (var productId in productIds)
            {
                var item = _wishlistRepository.GetWishlistItemAsync(fromWishlistId, productId).GetAwaiter().GetResult().Value;
                if (item == null) continue;

                var existingInTarget = _wishlistRepository.GetWishlistItemAsync(toWishlistId, productId).GetAwaiter().GetResult().Value;
                if (existingInTarget != null) continue;

                item.WishlistId = toWishlistId;
                await _wishlistRepository.UpdateItemAsync(item);
            }

            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }

        public async Task<ApplicationResponse<bool>> ClearWishlistAsync(int wishlistId)
        {
            var items = _wishlistRepository.GetWishlistItemsAsync(wishlistId).GetAwaiter().GetResult().Value;
            foreach (var item in items)
            {
                await _wishlistRepository.RemoveItemAsync(wishlistId, item.ProductId);
            }
            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }

        public async Task<ApplicationResponse<WishlistSummary>> GetUserSummaryAsync(int userId)
        {
            var totalWishlists =  _wishlistRepository.GetUserWishlistCountAsync(userId).GetAwaiter().GetResult().Value;
            var totalItems =  _wishlistRepository.GetUserTotalItemsAsync(userId).GetAwaiter().GetResult().Value;
            var totalValue =  _wishlistRepository.GetUserTotalValueAsync(userId).GetAwaiter().GetResult().Value;
            var recentWishlists = GetUserWishlistsAsync(userId).GetAwaiter().GetResult().Value.Take(3).ToList();

            return new ApplicationResponse<WishlistSummary>()
            {
                IsSuccess = true,
                Value = new WishlistSummary
                {
                    TotalWishlists = totalWishlists,
                    TotalItems = totalItems,
                    TotalValue = totalValue,
                    RecentWishlists = recentWishlists
                }
            };
        }

        public async Task<ApplicationResponse<bool>> ShareWishlistAsync(int wishlistId, bool isPublic)
        {
            var wishlist = _wishlistRepository.GetByIdAsync(wishlistId).GetAwaiter().GetResult().Value;
            if (wishlist == null) return new ApplicationResponse<bool> { IsSuccess = false, Value = false };

            wishlist.IsPublic = isPublic;
            await _wishlistRepository.UpdateAsync(wishlist);
            return new ApplicationResponse<bool> { IsSuccess = true, Value = true };
        }

        public async Task<ApplicationResponse<WishlistResponse>> DuplicateWishlistAsync(int wishlistId, string newName)
        {
            var originalWishlist = _wishlistRepository.GetByIdAsync(wishlistId).GetAwaiter().GetResult().Value;
            if (originalWishlist == null) 
                return new ApplicationResponse<WishlistResponse> { IsSuccess = false, Value = new WishlistResponse { Id = 0, Name = "Original wishlist not found" } };

            var newWishlist = new Wishlist
            {
                UserId = originalWishlist.UserId,
                Name = newName,
                IsPublic = false,
                IsDefault = false
            };

            var createdWishlist = _wishlistRepository.CreateAsync(newWishlist).GetAwaiter().GetResult().Value;


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

            return new ApplicationResponse<WishlistResponse> { IsSuccess = true, Value = GetByIdAsync(createdWishlist.Id).GetAwaiter().GetResult().Value };
        }

        public async Task<ApplicationResponse<IEnumerable<WishlistResponse>>> GetAllWishlistsAsync()
        {
            var allWishlists = _wishlistRepository.GetAllWishlistsAsync().GetAwaiter().GetResult().Value;
            return new ApplicationResponse<IEnumerable<WishlistResponse>> { IsSuccess = true, Value = allWishlists.Select(MapToDto) };
        }

        public async Task<ApplicationResponse<GlobalWishlistStats>> GetGlobalStatsAsync()
        {
            var globalStats = _wishlistRepository.GetGlobalStatsAsync().GetAwaiter().GetResult().Value;
            return new ApplicationResponse<GlobalWishlistStats> { IsSuccess = true, Value = new GlobalWishlistStats
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
            }};
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