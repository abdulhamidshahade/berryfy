using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.InventoryDtos.Requests;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Domain.Entities.InventoryEntities;
using Berryfy.Domain.Entities.ProductEntities;
using Microsoft.AspNetCore.Mvc;

namespace Berryfy.API.Controllers
{
    [Route("api/inventories")]
    [ApiController]
    public class InventoriesController : BaseController
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("check-stock/{productId}/{quantity}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> CheckStockQuantity(int productId, int quantity)
        {
            var isInStock = await _inventoryService.IsInStockAsync(productId, quantity);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = $"Stock check completed for product {productId}",
                Data = isInStock.Value
            });
        }
  
        [HttpPost("reserve-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ReserveStock([FromBody] ReserveStockRequest request)
        {
            var success = await _inventoryService.ReserveStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

            if (!success.Value)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    StatusMessage = "Failed to reserve stock",
                    Data = false
                });
            }

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Stock reserved successfully",
                Data = true
            });
        }


        [HttpPost("release-reserved-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ReleaseReservedStock([FromBody] ReleaseStockRequest request)
        {
            var success = await _inventoryService.ReleaseReservedStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = success.Value,
                StatusCode = success.Value ? 200 : 400,
                StatusMessage = success.Value ? "Reserved stock released successfully" : "Failed to release reserved stock",
                Data = success.Value
            });
        }


        [HttpPost("confirm-deduction")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> ConfirmStockDeduction([FromBody] ConfirmDeductionRequest request)
        {
            var success = await _inventoryService.ConfirmStockDeductionAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = success.Value,
                StatusCode = success.Value ? 200 : 400,
                StatusMessage = success.Value ? "Stock deduction confirmed successfully" : "Failed to confirm stock deduction",
                Data = success.Value
            });
        }

        [HttpPost("add-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> AddStock([FromBody] AddStockRequest request)
        {
            var success = await _inventoryService.AddStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.Notes,
                    request.PerformedByUserId);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = success.Value,
                StatusCode = success.Value ? 200 : 400,
                StatusMessage = success.Value ? "Stock added successfully" : "Failed to add stock",
                Data = success.Value
            });
        }


        [HttpPut("adjust-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<bool>>> AdjustStock([FromBody] AdjustStockRequest request)
        {
            var success = await _inventoryService.AdjustStockAsync(
                    request.ProductId,
                    request.NewQuantity,
                    request.Notes,
                    request.PerformedByUserId);

            return Ok(new ApiResponse<bool>
            {
                IsSuccess = success.Value,
                StatusCode = success.Value ? 200 : 400,
                StatusMessage = success.Value ? "Stock adjusted successfully" : "Failed to adjust stock",
                Data = success.Value
            });
        }


        [HttpGet("product/{productId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<Product>>> GetProductWithStock(int productId)
        {
            var product = await _inventoryService.GetProductWithStockInfoAsync(productId);
            if (product == null)
            {
                return NotFound(new ApiResponse<Product>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    StatusMessage = "Product not found"
                });
            }

            return Ok(new ApiResponse<Product>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Product retrieved successfully",
                Data = product.Value
            });
        }


        [HttpGet("low-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<List<Product>>>> GetLowStockProducts([FromQuery] int limit = 50)
        {
            var products = await _inventoryService.GetLowStockProductsAsync(limit);

            return Ok(new ApiResponse<List<Product>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Low stock products retrieved successfully",
                Data = products.Value
            });
        }


        [HttpGet("history/{productId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<List<InventoryLog>>>> GetInventoryHistory(int productId, [FromQuery] int limit = 50)
        {
            var history = await _inventoryService.GetInventoryHistoryAsync(productId, limit);

            return Ok(new ApiResponse<List<InventoryLog>>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Inventory history retrieved successfully",
                Data = history.Value
            });
        }


        [HttpPost("process-notifications")]
        [AdminAndAbove]
        public async Task<ActionResult<ApiResponse<object>>> ProcessStockNotifications()
        {
            await _inventoryService.ProcessStockNotificationsAsync();

            return Ok(new ApiResponse<object>
            {
                IsSuccess = true,
                StatusCode = 200,
                StatusMessage = "Stock notifications processed successfully"
            });
        }
    }
}