using Berryfy.Application.Dtos;

namespace Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces
{
    public interface IOrderCancellationService
    {
        Task<ApplicationResponse<CancellationResult>> CancelOrderAsync(int orderId, string reason, int? performedByUserId = null);
    }

    public class CancellationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public bool InventoryRestored { get; set; }
        public bool CouponsReverted { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
