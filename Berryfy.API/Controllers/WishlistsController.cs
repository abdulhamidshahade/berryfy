using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos.WishlistDtos.Requests;
using Berryfy.Application.Services.Interfaces.WishlistServiceInterfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Berryfy.API.Controllers
{
    [ApiController]
    [Route("api/wishlists")]
    public class WishlistsController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistsController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private bool TryResolveUserId(out int userId)
        {
            userId = 0;
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;
            return !string.IsNullOrWhiteSpace(idClaim)
                && int.TryParse(idClaim, out userId)
                && userId > 0;
        }


        [HttpGet]
        [UserAndAbove]
        public async Task<IActionResult> GetUserWishlists()
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlists = await _wishlistService.GetUserWishlistsAsync(userId);
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = wishlists.Value });
        }


        [HttpGet("{id}")]
        [UserAndAbove]
        public async Task<IActionResult> GetWishlist(int id)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(id);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });

            if (wishlist.Value.UserId != userId && !wishlist.Value.IsPublic)
                return Forbid();

            return Ok(new { IsSuccess = true, StatusCode = 200, Data = wishlist.Value });
        }


        [HttpGet("default")]
        [UserAndAbove]
        public async Task<IActionResult> GetDefaultWishlist()
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetUserDefaultWishlistAsync(userId);
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = wishlist.Value });
        }


        [HttpGet("summary")]
        [UserAndAbove]
        public async Task<IActionResult> GetUserSummary()
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var summary = await _wishlistService.GetUserSummaryAsync(userId);
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = summary.Value });
        }


        [HttpPost]
        [UserAndAbove]
        public async Task<IActionResult> CreateWishlist([FromBody] CreateWishlist createWishlistDto)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wishlist = await _wishlistService.CreateAsync(userId, createWishlistDto);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = "Failed to create wishlist" });

            return CreatedAtAction(nameof(GetWishlist), new { id = wishlist.Value.Id },
                new { IsSuccess = true, StatusCode = 201, Data = wishlist.Value });
        }


        [HttpPut("{id}")]
        [UserAndAbove]
        public async Task<IActionResult> UpdateWishlist(int id, [FromBody] UpdateWishlist updateWishlistDto)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingWishlist = await _wishlistService.GetByIdAsync(id);
            if (existingWishlist == null || !existingWishlist.IsSuccess || existingWishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });

            if (existingWishlist.Value.UserId != userId)
                return Forbid();

            var updatedWishlist = await _wishlistService.UpdateAsync(id, updateWishlistDto);
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = updatedWishlist.Value });
        }


        [HttpDelete("{id}")]
        [UserAndAbove]
        public async Task<IActionResult> DeleteWishlist(int id)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var existingWishlist = await _wishlistService.GetByIdAsync(id);
            if (existingWishlist == null || !existingWishlist.IsSuccess || existingWishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });

            if (existingWishlist.Value.UserId != userId)
                return Forbid();

            var result = await _wishlistService.DeleteAsync(id);
            if (!result.IsSuccess || !result.Value)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = result.ErrorMessage ?? "Cannot delete the default wishlist" });

            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Wishlist deleted successfully" });
        }


        [HttpPost("items/add")]
        [UserAndAbove]
        public async Task<IActionResult> AddItem([FromBody] AddToWishlist addToWishlistDto)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var item = await _wishlistService.AddItemAsync(userId, addToWishlistDto);
            if (item == null || !item.IsSuccess || item.Value == null)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = item?.ErrorMessage ?? "Failed to add item to wishlist" });

            return Ok(new { IsSuccess = true, StatusCode = 200, Data = item.Value, StatusMessage = "Item added to wishlist successfully" });
        }


        [HttpPut("{wishlistId}/items/{productId}")]
        [UserAndAbove]
        public async Task<IActionResult> UpdateItem(int wishlistId, int productId, [FromBody] UpdateWishlistItem updateItemDto)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            var updatedItem = await _wishlistService.UpdateItemAsync(wishlistId, productId, updateItemDto);
            if (updatedItem == null || !updatedItem.IsSuccess || updatedItem.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist item not found" });

            return Ok(new { IsSuccess = true, StatusCode = 200, Data = updatedItem.Value });
        }


        [HttpDelete("{wishlistId}/items/{productId}")]
        [UserAndAbove]
        public async Task<IActionResult> RemoveItem(int wishlistId, int productId)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            var result = await _wishlistService.RemoveItemAsync(wishlistId, productId);
            if (!result.Value)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist item not found" });

            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Item removed from wishlist successfully" });
        }


        [HttpGet("check-product/{productId}")]
        [UserAndAbove]
        public async Task<IActionResult> CheckProductInWishlist(int productId)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var isInWishlist = await _wishlistService.IsProductInWishlistAsync(userId, productId);
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = new { IsInWishlist = isInWishlist.Value } });
        }


        [HttpPost("{wishlistId}/items/bulk-add")]
        [UserAndAbove]
        public async Task<IActionResult> AddMultipleItems(int wishlistId, [FromBody] List<int> productIds)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            var result = await _wishlistService.AddMultipleItemsAsync(userId, wishlistId, productIds);
            if (!result.Value)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = "Failed to add items to wishlist" });

            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Items added to wishlist successfully" });
        }


        [HttpDelete("{wishlistId}/items/bulk-remove")]
        [UserAndAbove]
        public async Task<IActionResult> RemoveMultipleItems(int wishlistId, [FromBody] List<int> productIds)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            await _wishlistService.RemoveMultipleItemsAsync(wishlistId, productIds);
            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Items removed from wishlist successfully" });
        }


        [HttpPut("{wishlistId}/share")]
        [UserAndAbove]
        public async Task<IActionResult> ShareWishlist(int wishlistId, [FromBody] bool isPublic)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            await _wishlistService.ShareWishlistAsync(wishlistId, isPublic);
            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = $"Wishlist {(isPublic ? "shared" : "made private")} successfully" });
        }


        [HttpPost("{wishlistId}/duplicate")]
        [UserAndAbove]
        public async Task<IActionResult> DuplicateWishlist(int wishlistId, [FromBody] string newName)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            var duplicatedWishlist = await _wishlistService.DuplicateWishlistAsync(wishlistId, newName);
            if (duplicatedWishlist == null || !duplicatedWishlist.IsSuccess || duplicatedWishlist.Value == null)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = "Failed to duplicate wishlist" });

            return Ok(new { IsSuccess = true, StatusCode = 200, Data = duplicatedWishlist.Value, StatusMessage = "Wishlist duplicated successfully" });
        }


        [HttpDelete("{wishlistId}/clear")]
        [UserAndAbove]
        public async Task<IActionResult> ClearWishlist(int wishlistId)
        {
            if (!TryResolveUserId(out var userId))
                return Unauthorized();

            var wishlist = await _wishlistService.GetByIdAsync(wishlistId);
            if (wishlist == null || !wishlist.IsSuccess || wishlist.Value == null)
                return NotFound(new { IsSuccess = false, StatusCode = 404, StatusMessage = "Wishlist not found" });
            if (wishlist.Value.UserId != userId)
                return Forbid();

            await _wishlistService.ClearWishlistAsync(wishlistId);
            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Wishlist cleared successfully" });
        }

        // ADMIN ENDPOINTS

        [HttpGet("admin/all")]
        [AdminAndAbove]
        public async Task<IActionResult> GetAllWishlistsForAdmin()
        {
            var allWishlists = await _wishlistService.GetAllWishlistsAsync();
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = allWishlists.Value });
        }

        [HttpGet("admin/stats")]
        [AdminAndAbove]
        public async Task<IActionResult> GetGlobalWishlistStats()
        {
            var stats = await _wishlistService.GetGlobalStatsAsync();
            return Ok(new { IsSuccess = true, StatusCode = 200, Data = stats.Value });
        }

        [HttpDelete("admin/{id}")]
        [AdminAndAbove]
        public async Task<IActionResult> AdminDeleteWishlist(int id)
        {
            var result = await _wishlistService.DeleteAsync(id);
            if (!result.IsSuccess || !result.Value)
                return BadRequest(new { IsSuccess = false, StatusCode = 400, StatusMessage = result.ErrorMessage ?? "Cannot delete the default wishlist" });

            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Wishlist deleted successfully" });
        }

        [HttpDelete("admin/{id}/clear")]
        [AdminAndAbove]
        public async Task<IActionResult> AdminClearWishlist(int id)
        {
            await _wishlistService.ClearWishlistAsync(id);
            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = "Wishlist cleared successfully" });
        }

        [HttpPut("admin/{id}/visibility")]
        [AdminAndAbove]
        public async Task<IActionResult> AdminToggleWishlistVisibility(int id, [FromBody] bool isPublic)
        {
            await _wishlistService.ShareWishlistAsync(id, isPublic);
            return Ok(new { IsSuccess = true, StatusCode = 200, StatusMessage = $"Wishlist {(isPublic ? "shared" : "made private")} successfully" });
        }
    }
}