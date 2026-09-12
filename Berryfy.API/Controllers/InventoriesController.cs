using Berryfy.Application.Authorization.Attributes;
using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.InventoryDtos;
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

        public InventoriesController(
            IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("check-stock/{productId}/{quantity}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> CheckStockQuantity(int productId, int quantity)
        {
            try
            {
                var isInStock = await _inventoryService.IsInStockAsync(productId, quantity);

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = $"Stock check completed for product {productId}",
                    Data = isInStock
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error checking stock",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpGet("check-stock/{productId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<object>>> CheckStock(int productId, [FromQuery] int quantity = 1)
        {
            try
            {
                var isInStock = await _inventoryService.IsInStockAsync(productId, quantity);

                return Ok(new ResponseDto<object>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Stock check completed",
                    Data = new { ProductId = productId, Quantity = quantity, IsInStock = isInStock }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error checking stock",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("reserve-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ReserveStock([FromBody] ReserveStockRequest request)
        {
            try
            {
                var success = await _inventoryService.ReserveStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

                if (!success)
                {
                    return BadRequest(new ResponseDto<bool>
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        StatusMessage = "Failed to reserve stock",
                        Data = false
                    });
                }

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Stock reserved successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error reserving stock",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpPost("release-reserved-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ReleaseReservedStock([FromBody] ReleaseStockRequest request)
        {
            try
            {
                var success = await _inventoryService.ReleaseReservedStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = success,
                    StatusCode = success ? 200 : 400,
                    StatusMessage = success ? "Reserved stock released successfully" : "Failed to release reserved stock",
                    Data = success
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error releasing reserved stock",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpPost("confirm-deduction")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> ConfirmStockDeduction([FromBody] ConfirmDeductionRequest request)
        {
            try
            {
                var success = await _inventoryService.ConfirmStockDeductionAsync(
                    request.ProductId,
                    request.Quantity,
                    request.ReferenceId,
                    request.ReferenceType);

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = success,
                    StatusCode = success ? 200 : 400,
                    StatusMessage = success ? "Stock deduction confirmed successfully" : "Failed to confirm stock deduction",
                    Data = success
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error confirming stock deduction",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpPost("add-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> AddStock([FromBody] AddStockRequest request)
        {
            try
            {
                var success = await _inventoryService.AddStockAsync(
                    request.ProductId,
                    request.Quantity,
                    request.Notes,
                    request.PerformedByUserId);

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = success,
                    StatusCode = success ? 200 : 400,
                    StatusMessage = success ? "Stock added successfully" : "Failed to add stock",
                    Data = success
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error adding stock",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpPut("adjust-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<bool>>> AdjustStock([FromBody] AdjustStockRequest request)
        {
            try
            {
                var success = await _inventoryService.AdjustStockAsync(
                    request.ProductId,
                    request.NewQuantity,
                    request.Notes,
                    request.PerformedByUserId);

                return Ok(new ResponseDto<bool>
                {
                    IsSuccess = success,
                    StatusCode = success ? 200 : 400,
                    StatusMessage = success ? "Stock adjusted successfully" : "Failed to adjust stock",
                    Data = success
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error adjusting stock",
                    Errors = new List<string> { ex.Message },
                    Data = false
                });
            }
        }

        [HttpGet("product/{productId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<Product>>> GetProductWithStock(int productId)
        {
            try
            {
                var product = await _inventoryService.GetProductWithStockInfoAsync(productId);
                if (product == null)
                {
                    return NotFound(new ResponseDto<Product>
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        StatusMessage = "Product not found"
                    });
                }

                return Ok(new ResponseDto<Product>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Product retrieved successfully",
                    Data = product
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<Product>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving product",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("low-stock")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<List<Product>>>> GetLowStockProducts([FromQuery] int limit = 50)
        {
            try
            {
                var products = await _inventoryService.GetLowStockProductsAsync(limit);

                return Ok(new ResponseDto<List<Product>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Low stock products retrieved successfully",
                    Data = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<Product>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving low stock products",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("history/{productId}")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<List<InventoryLog>>>> GetInventoryHistory(int productId, [FromQuery] int limit = 50)
        {
            try
            {
                var history = await _inventoryService.GetInventoryHistoryAsync(productId, limit);

                return Ok(new ResponseDto<List<InventoryLog>>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Inventory history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<List<InventoryLog>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error retrieving inventory history",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("process-notifications")]
        [AdminAndAbove]
        public async Task<ActionResult<ResponseDto<object>>> ProcessStockNotifications()
        {
            try
            {
                await _inventoryService.ProcessStockNotificationsAsync();

                return Ok(new ResponseDto<object>
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    StatusMessage = "Stock notifications processed successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto<object>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    StatusMessage = "Error processing stock notifications",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
