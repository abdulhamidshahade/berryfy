using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.ShopDtos;
using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Domain.Entities.ShopEntities;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<ResponseDto<Shop>>> GetById(int id)
        {
            var shop = await _shopService.GetShopAsync(id);

            if (shop == null)
            {
                return new ResponseDto<Shop>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error retrieving shop",
                    Errors = new List<string> { "An unexpected error occurred" }
                };
            }
            var response = new ResponseDto<Shop>
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
        public async Task<ActionResult<ResponseDto<ShopDto>>> Update(int id, [FromBody] UpdateShopDto shopDto)
        {
            var updatedShop = await _shopService.UpdateShopAsync(id, shopDto);

            if (updatedShop == null)
            {
                return new ResponseDto<ShopDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    StatusMessage = "Error updating shop",
                    Errors = new List<string> { "An unexpected error occurred" }
                };

            }
            var response = new ResponseDto<ShopDto>
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
