using Berryfy.Application.Dtos;
using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Microsoft.AspNetCore.Mvc;
using Berryfy.Application.Dtos.ShopDtos.Responses;
using Berryfy.Application.Dtos.ShopDtos.Requests;

namespace Berryfy.API.Controllers
{
    [Route("api/shops")]
    [ApiController]
    public class ShopsController : BaseController
    {
        private readonly IShopService _shopService;
        public ShopsController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpGet]
        [Route("{id}")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ShopResponse>>> GetById(int id)
        {
            var shop = await _shopService.GetShopAsync(id);

            if (shop == null || !shop.IsSuccess || shop.Value == null)
            {
                return NotFound(new ApiResponse<ShopResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    StatusMessage = shop?.ErrorMessage ?? "Shop not found",
                    Errors = new List<string> { shop?.ErrorMessage ?? "Shop not found" }
                });
            }

            var response = new ApiResponse<ShopResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Shop retrieved successfully",
                Data = shop.Value
            };

            return Ok(response);
        }

        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<ShopResponse>>> Update(int id, [FromBody] UpdateShop shopDto)
        {
            var updatedShop = await _shopService.UpdateShopAsync(id, shopDto);

            if (updatedShop == null || !updatedShop.IsSuccess || updatedShop.Value == null)
            {
                return BadRequest(new ApiResponse<ShopResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    StatusMessage = updatedShop?.ErrorMessage ?? "Error updating shop",
                    Errors = new List<string> { updatedShop?.ErrorMessage ?? "An unexpected error occurred" }
                });
            }

            var response = new ApiResponse<ShopResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Shop updated successfully",
                Data = updatedShop.Value
            };

            return Ok(response);
        }
    }
}