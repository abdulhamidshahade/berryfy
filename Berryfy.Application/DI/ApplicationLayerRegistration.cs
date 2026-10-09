using Berryfy.Application.Dtos.AuthDtos.AuthValidations;
using Berryfy.Application.Services.Concretes.AuthServiceConcretes;
using Berryfy.Application.Services.Concretes.CheckoutServiceConcretes;
using Berryfy.Application.Services.Concretes.CouponServiceConcretes;
using Berryfy.Application.Services.Concretes.EmailServiceConcretes;
using Berryfy.Application.Services.Concretes.InventoryServiceConcretes;
using Berryfy.Application.Services.Concretes.OrderServiceConcretes;
using Berryfy.Application.Services.Concretes.OrchestrationServiceConcretes;
using Berryfy.Application.Services.Concretes.PaymentConcretes;
using Berryfy.Application.Services.Concretes.ProductServiceConcretes;
using Berryfy.Application.Services.Concretes.ShoppingCartServiceConcretes;
using Berryfy.Application.Services.Concretes.ShopServiceConcretes;
using Berryfy.Application.Services.Concretes.WishlistServiceConcretes;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Application.Services.Interfaces.CheckoutServiceInterfaces;
using Berryfy.Application.Services.Interfaces.CouponServiceInterfaces;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Berryfy.Application.Services.Interfaces.InventoryServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrderServiceInterfaces;
using Berryfy.Application.Services.Interfaces.OrchestrationServiceInterfaces;
using Berryfy.Application.Services.Interfaces.PaymentServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ProductServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShoppingCartServiceInterfaces;
using Berryfy.Application.Services.Interfaces.ShopServiceInterfaces;
using Berryfy.Application.Services.Interfaces.WishlistServiceInterfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Berryfy.Application.DI
{
    public static class ApplicationLayerRegistration
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection serviceDescriptors, 
            IHostBuilder hostBuilder,
            IConfiguration configuration)
        {
            serviceDescriptors.AddScoped<IAuthService, AuthService>();
            serviceDescriptors.AddScoped<ITokenService, TokenService>();
            serviceDescriptors.AddScoped<IUserService, UserService>();

            serviceDescriptors.AddScoped<ICouponService, CouponService>();
            serviceDescriptors.AddScoped<IUserCouponService, UserCouponService>();

            serviceDescriptors.AddScoped<ICategoryService, CategoryService>();
            serviceDescriptors.AddScoped<IProductCategoryService, ProductCategoryService>();
            serviceDescriptors.AddScoped<IProductService, ProductService>();

            serviceDescriptors.AddScoped<ICartService, CartService>();
            serviceDescriptors.AddScoped<IUserCheckoutInfoService, UserCheckoutInfoService>();
            serviceDescriptors.AddScoped<IShopService, ShopService>();

            serviceDescriptors.AddScoped<IInventoryService, InventoryService>();

            serviceDescriptors.AddScoped<IOrderService, OrderService>();

            serviceDescriptors.AddScoped<IRoleManagementService, RoleManagementService>();

            serviceDescriptors.AddScoped<IWishlistService, WishlistService>();

            serviceDescriptors.AddScoped<IPaymentService, PaymentService>();

            //serviceDescriptors.AddScoped<IMailService, ResendMailService>();
            serviceDescriptors.AddScoped<IMailService, MailtrapService>();

            serviceDescriptors.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
            serviceDescriptors.AddScoped<IOrderCancellationService, OrderCancellationService>();
            serviceDescriptors.AddScoped<IRefundOrchestrationService, RefundOrchestrationService>();


            hostBuilder.UseSerilog((context, loggerConfig) =>
            {
                loggerConfig.ReadFrom.Configuration(configuration);
            });


            serviceDescriptors.AddFluentValidationAutoValidation();

            serviceDescriptors.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

            return serviceDescriptors;
        }
    }
}
