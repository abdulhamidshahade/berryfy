using Berryfy.Application.Dtos;
using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Entities.ShopEntities;
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
        public async Task<ActionResult<ApiResponse<Shop>>> GetById(int id)
        {
            var shop = await _shopService.GetShopAsync(id);

            if (shop == null)
            {
                return new ApiResponse<Shop>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving shop",
                    Errors = new List<string> { "An unexpected error occurred" }
                };
            }
            var response = new ApiResponse<Shop>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Shop retrieved successfully",
                Data = shop
            };
            return Ok(response);


        }

        [HttpPut]
        [Route("{id}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<ShopResponse>>> Update(int id, [FromBody] UpdateShop shopDto)
        {
            var updatedShop = await _shopService.UpdateShopAsync(id, shopDto);

            if (updatedShop == null)
            {
                return new ApiResponse<ShopResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating shop",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ApiResponse<ShopResponse>
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                StatusMessage = "Shop updated successfully",
                Data = updatedShop
            };
            return Ok(response);

        }

    }
}
