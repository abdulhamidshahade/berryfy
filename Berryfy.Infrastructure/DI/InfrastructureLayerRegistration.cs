using Berryfy.Domain.Repositories;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Berryfy.Domain.Repositories.CheckoutInterfaces;
using Berryfy.Domain.Repositories.CouponInterfaces;
using Berryfy.Domain.Repositories.InventoryInterfaces;
using Berryfy.Domain.Repositories.OrderInterfaces;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Berryfy.Domain.Repositories.ProductInterfaces;
using Berryfy.Domain.Repositories.ShopInterfaces;
using Berryfy.Domain.Repositories.ShoppingCartInterfaces;
using Berryfy.Domain.Repositories.WishlistInterfaces;
using Berryfy.Infrastructure.Data;
using Berryfy.Infrastructure.Repositories.AuthConcretes;
using Berryfy.Infrastructure.Repositories.CheckoutConcretes;
using Berryfy.Infrastructure.Repositories.CouponConcretes;
using Berryfy.Infrastructure.Repositories.InventoryConcretes;
using Berryfy.Infrastructure.Repositories.OrderConcretes;
using Berryfy.Infrastructure.Repositories.PaymentConcretes;
using Berryfy.Infrastructure.Repositories.ProductConcretes;
using Berryfy.Infrastructure.Repositories.ShopConcretes;
using Berryfy.Infrastructure.Repositories.ShoppingCartConcretes;
using Berryfy.Infrastructure.Repositories.WishlistConcretes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Berryfy.Infrastructure.DI
{
    public static class InfrastructureLayerRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection serviceDescriptors, IConfiguration configuration)
        {
            var connStr = PostgresConnectionStrings.Resolve(configuration);
            var dataSource = NpgsqlDataSource.Create(connStr);
            serviceDescriptors.AddSingleton(dataSource);

            serviceDescriptors.AddScoped(sp =>
            {
                var source = sp.GetRequiredService<NpgsqlDataSource>();
                return source.CreateConnection();
            });

            serviceDescriptors.AddScoped<ICouponRepository, CouponRepository>();
            serviceDescriptors.AddScoped<IUserCouponRepository, UserCouponRepository>();
            serviceDescriptors.AddScoped<IUserRepository, UserRepository>();
            serviceDescriptors.AddScoped<IRoleRepository, RoleRepository>();

            serviceDescriptors.AddScoped<ICategoryRepository, CategoryRepository>();
            serviceDescriptors.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
            serviceDescriptors.AddScoped<IProductRepository, ProductRepository>();

            serviceDescriptors.AddScoped<ICartRepository, CartRepository>();
            
            serviceDescriptors.AddScoped<IUserCheckoutInfoRepository, UserCheckoutInfoRepository>();

            serviceDescriptors.AddScoped<IInventoryRepository, InventoryRepository>();
            serviceDescriptors.AddScoped<IShopRepository, ShopRepository>();

            serviceDescriptors.AddScoped<IUnitOfWork, UnitOfWork>();

            serviceDescriptors.AddScoped<IOrderRepository, OrderRepository>();

            serviceDescriptors.AddScoped<IWishlistRepository, WishlistRepository>();

            serviceDescriptors.Add(new ServiceDescriptor
            (
                typeof(IPaymentRepository),
                typeof(PaymentRepository),
                ServiceLifetime.Scoped
                ));

            serviceDescriptors.AddScoped<DataSeeder>();

            serviceDescriptors.AddScoped<Npgsql.NpgsqlConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connStr = config.GetConnectionString("Postgres"); // update key as used in appsettings
                return new Npgsql.NpgsqlConnection(connStr);
            });


            return serviceDescriptors;
        }
    }
}
