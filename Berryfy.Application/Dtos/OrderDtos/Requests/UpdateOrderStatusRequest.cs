using Berryfy.Domain.Constants;

namespace Berryfy.Application.Dtos.OrderDtos.Requests
{
    public class UpdateOrderStatusRequest
    {
        public OrderStatus NewStatus { get; set; }
    }

}
