using Berryfy.Application.Dtos.OrderDtos;
using Berryfy.Application.Dtos.OrderDtos.Requests;
using Berryfy.Domain.Entities.OrderEntities;

namespace Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces
{
    public interface ICheckoutOrchestrationService
    {
        Task<CheckoutResult> ProcessCheckoutAsync(int cartId, CreateOrder orderDto, int? userId);
    }

    public class CheckoutResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public Order? Order { get; set; }
        public List<string> Warnings { get; set; }
    }
}
