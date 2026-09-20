using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.OrderEntities;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Berryfy.Application.Dtos.OrderDtos.Responses
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CartId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal Total { get; set; }
        public decimal DiscountTotal { get; set; } = 0;
        public string CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? PaymentProvider { get; set; }
        public int PaymentTransactionId { get; set; }
        public bool IsPaid { get; set; }
        public DateTime PaidAt { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? ShippingName { get; set; }
        public string? ShippingAddress1 { get; set; }
        public string? ShippingAddress2 { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static Order MapToOrder(OrderResponse response)
        {
            return new Order
            {
                Id = response.Id,
                UserId = response.UserId,
                CartId = response.CartId,
                Status = response.Status,
                SubTotal = response.SubTotal,
                TaxAmount = response.TaxAmount,
                ShippingAmount = response.ShippingAmount,
                Total = response.Total,
                DiscountTotal = response.DiscountTotal,
                CustomerEmail = response.CustomerEmail,
                CustomerPhone = response.CustomerPhone,
                isPaid = response.IsPaid,
                ReferenceNumber = response.ReferenceNumber,
                CompletedAt = response.CompletedAt,
                CancalledAt = response.CancelledAt,
                ShippingName = response.ShippingName,
                ShippingAddress1 = response.ShippingAddress1,
                ShippingAddress2 = response.ShippingAddress2,
                ShippingCity = response.ShippingCity,
                ShippingState = response.ShippingState,
                ShippingPostalCode = response.ShippingPostalCode,
                ShippingCountry = response.ShippingCountry,
                CreatedAt = response.CreatedAt,
                UpdatedAt = response.UpdatedAt
            };
        }

        public static OrderResponse MapFromOrder(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                CartId = order.CartId,
                Status = order.Status,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                ShippingAmount = order.ShippingAmount,
                Total = order.Total,
                DiscountTotal = order.DiscountTotal,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                IsPaid = order.isPaid,
                ReferenceNumber = order.ReferenceNumber,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancalledAt,
                ShippingName = order.ShippingName,
                ShippingAddress1 = order.ShippingAddress1,
                ShippingAddress2 = order.ShippingAddress2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }

        public static List<OrderResponse> MapFromOrder(List<Order> orders)
        {
            var orderResponse = new List<OrderResponse>();

            foreach(Order order in orders)
            {
                orderResponse.Add(MapFromOrder(order));
            }

            return orderResponse;
        }

        public static List<Order> MapToOrder(List<OrderResponse> orderResponses)
        {
            List<Order> orders = new List<Order>();

            foreach(OrderResponse orderResponse in orderResponses)
            {
                orders.Add(MapToOrder(orderResponse));
            }

            return orders;
        }
    }
}
