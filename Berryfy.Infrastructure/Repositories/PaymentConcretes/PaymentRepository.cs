using Berryfy.Application.Dtos.PaymentDtos;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;


namespace Berryfy.Infrastructure.Repositories.PaymentConcretes
{
    class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgreSQLServer");
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            string sql = @"SELECT * from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Where p.Id = @Id
                           Limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = null;

            while (await reader.ReadAsync())
            {
                if(payment == null)
                {
                    payment = new Payment()
                    {
                        Id = id,
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };


                    payment.User = new Domain.Entities.AuthEntities.User
                    {
                        FirstName = payment.User.FirstName,
                        LastName = payment.User.LastName,
                        PhoneNumber = payment.User.PhoneNumber
                    };

                    payment.Order = new Domain.Entities.OrderEntities.Order
                    {
                        CartId = payment.Order.CartId
                    };
                }
            }

            return payment;
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            string sql = @"SELECT * from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Where p.TransactionId = @TransactionId
                           Limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@TransactionId", transactionId);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = null;

            while (await reader.ReadAsync())
            {
                if (payment == null)
                {
                    payment = new Payment()
                    {
                        Id=reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = transactionId,
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };


                    payment.User = new Domain.Entities.AuthEntities.User
                    {
                        FirstName = payment.User.FirstName,
                        LastName = payment.User.LastName,
                        PhoneNumber = payment.User.PhoneNumber
                    };

                    payment.Order = new Domain.Entities.OrderEntities.Order
                    {
                        CartId = payment.Order.CartId
                    };
                }
            }

            return payment;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            string query = "Select Payment p.*, u.*, o.* from" +
                "           Payments p Left join" +
                "           Users u on(p.UserId = u.Id)" +
                "           Left join Orders o on(p.OrderId = o.Id)" +
                "           where p.OrderId = @OrderId" +
                "           order by case" +
                "           When p.status in (completed, PartiallyRefunded, Refunded) then 1" +
                "           else 0 End Desc, case when p.Status = Processing else 0 end," +
                "           p.createdAt desc, p.Id desc limit 1;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = null;

            while (await reader.ReadAsync())
            {
                if (payment == null)
                {
                    payment = new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };


                    payment.User = new Domain.Entities.AuthEntities.User
                    {
                        FirstName = payment.User.FirstName,
                        LastName = payment.User.LastName,
                        PhoneNumber = payment.User.PhoneNumber
                    };

                    payment.Order = new Domain.Entities.OrderEntities.Order
                    {
                        CartId = payment.Order.CartId
                    };

                    return payment;
                }
            }

            return null;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            string sql = @"SELECT p.*, o.CartId As O_CartId, u.FirstName As U_Firstname, u.LastName As U_LastName, u.PhoneNumber As U_PhoneNumber from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Order by createdAt desc;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);


            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),




                    User = new Domain.Entities.AuthEntities.User
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                    },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("CartId"))
                        }
                    }
                   );
                }
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(int userId)
        {
            string sql = @"SELECT p.*, o.CartId as O_CartId, u.FirstName as U_FirstName, 
                           u.LastName as U_LastName, u.PhoneNumber As U_PhoneNumber from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Where p.UserId = @UserId
                           Order by Id asc";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status)
        {
            string sql = @"SELECT p.*, o.CartId as O_CartId, u.FirstName as U_FirstName, 
                           u.LastName as U_LastName, u.PhoneNumber As U_PhoneNumber from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Where p.Status = @Status
                           Order by CreatedAt desc";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Status", status);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            string sql = @"SELECT p.*, o.CartId as O_CartId, u.FirstName as U_FirstName, 
                           u.LastName as U_LastName, u.PhoneNumber As U_PhoneNumber from Payments p
                           LEFT JOIN Users u on (u.Id = p.UserId)
                           Left join Orders o on (o.Id = p.OrderId)
                           Where CreatedAt >= @StartDate and CreatedAt <= @EndDate
                           Order by CreatedAt desc";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            string query = @"select p.*, u.*, o.* from Payments
                             left join Users on (p.UserId = u.Id)
                             left join Orders on (p.OrderId = u.OrderId)
                             order by p.CreatedAt desc
                             offset @offset
                             limit @pageSize;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (pageNumber - 1) * pageSize;

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;

        }

        public async Task<IEnumerable<Payment>> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            string query = @"select p.*, u.*, o.* from Payments
                             left join Users on (p.UserId = u.Id)
                             left join Orders on (p.OrderId = u.OrderId)
                             where UserId = @UserId
                             order by p.CreatedAt desc
                             offset @offset
                             limit @pageSize;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (pageNumber - 1) * pageSize;

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            string query = @"Insert into Payments (UserId, OrderId, TransactionId, Status, Method, Provider, Amount, Currency,
                             ProviderTransactionId, ProviderPaymentMethod, CardLast4, CardBrand, PayerEmail,
                             PayerName, BillingAddress1, BillingAddress2, BillingCity, BillingState, BillingPostalCode,
                             BillingCountry, ProcessingFee, NetAmount, ProccessedAt, CompletedAt, FailedAt, RefundedAt,
                             ErrorMessage, FailureReason, Metadata, Notes, CreatedAt, UpdatedAt)
                             Values (@userId, @orderId, @TransactionId, @Status, @Method, @Provider, @Amount, @Currency,
                                     @ProviderTransactionId, @ProviderPaymentMethod, @CardLast4, @CardBrand, @PayerEmail,
                                     @PayerName, @BillingAddress1, @BillingAddress2, @BillingCity, @BillingState, @BillingPostalCode,
                                     @BillingCountry, @ProccessingFee, @NetAmount, @ProccessedAt, @CompletedAt, @FailedAt, @RefundAt,
                                     @ErrorMessage, @FailureReason, @Metadata, @Notes, @CreatedAt, @UpdatedAt)
                             Returning *;";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", payment.UserId);
            command.Parameters.AddWithValue("@OrderId", payment.OrderId);
            command.Parameters.AddWithValue("@TransactionId", payment.TransactionId);
            command.Parameters.AddWithValue("@Status", payment.Status);
            command.Parameters.AddWithValue("@Method", payment.Method);
            command.Parameters.AddWithValue("@Provider", payment.Provider);
            command.Parameters.AddWithValue("@Amount", payment.Amount);
            command.Parameters.AddWithValue("@Currency", payment.Currency);
            command.Parameters.AddWithValue("@ProviderTransactionId", payment.ProviderTransactionId);
            command.Parameters.AddWithValue("@ProviderPaymentMethod", payment.ProviderPaymentMethodId);
            command.Parameters.AddWithValue("@CardLast4", payment.CardLast4);
            command.Parameters.AddWithValue("@CardBrand", payment.CardBrand);
            command.Parameters.AddWithValue("@PayerEmail", payment.PayerEmail);
            command.Parameters.AddWithValue("@PayerName", payment.PayerName);
            command.Parameters.AddWithValue("@BillingAddress1", payment.BillingAddress1);
            command.Parameters.AddWithValue("@BillingAddress2", payment.BillingAddress2);
            command.Parameters.AddWithValue("@BillingCity", payment.BillingCity);
            command.Parameters.AddWithValue("@BillingState", payment.BillingState);
            command.Parameters.AddWithValue("@BillingPostalCode", payment.BillingPostalCode);
            command.Parameters.AddWithValue("@BillingCountry", payment.BillingCountry);
            command.Parameters.AddWithValue("@ProcessingFee", payment.ProcessingFee);
            command.Parameters.AddWithValue("@NetAmount", payment.NetAmount);
            command.Parameters.AddWithValue("@ProccessedAt", payment.ProcessedAt);
            command.Parameters.AddWithValue("@CompletedAt", payment.CompletedAt);
            command.Parameters.AddWithValue("@FailedAt", payment.FailedAt);
            command.Parameters.AddWithValue("@RefundAt", payment.RefundedAt);
            command.Parameters.AddWithValue("@ErrorMessage", payment.ErrorMessage);
            command.Parameters.AddWithValue("@FailureReason", payment.FailureReason);
            command.Parameters.AddWithValue("@Metadata", payment.Metadata);
            command.Parameters.AddWithValue("@Notes", payment.Notes);
            command.Parameters.AddWithValue("@CreatedAt", payment.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", payment.UpdatedAt);

            var reader = await command.ExecuteReaderAsync();

            Payment paymentt = null;

            while (await reader.ReadAsync())
            {
                if (paymentt == null)
                {
                    paymentt = new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };

                    return paymentt;
                }
            }

            return null;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            string query = @"Update Payments 
                             Set UserId = @UserId,
                                 OrderId = @OrderId,
                                 TransactionId = @TransactionId,
                                 Status = @Status,
                                 Method = @Method,
                                 Provider = @Provider,
                                 Amount= @Amount,
                                 Currency = @Currency,
                                 ProviderTransactionId = @ProviderTransactionId,
                                 ProviderPaymentMethod = @ProviderPaymentMethod,
                                 CardLast4 = @CardLast4,
                                 CardBrand = @CardBrand,
                                 PayerEmail = @PayerEmail,
                                 PayerName = @PayerName,
                                 BillingAddress1 = @BillingAddress1,
                                 BillingAddress2 = @BillingAddress2,
                                 BillingCity = @BillingCity,
                                 BillingState = @BillingState,
                                 BillingPostalCode = @BillingPostalCode,
                                 BillingCountry = @BillingCountry,
                                 ProcessingFee = @ProcessingFee,
                                 NetAmount = @NetAmount,
                                 ProcessedAt = @ProcessedAt,
                                 CompletedAt = @CompletedAt,
                                 FailedAt = @FailedAt,
                                 RefundAt = @RefundAt,
                                 ErrorMessage = @ErrorMessage,
                                 FailureReason = @FailureReason,
                                 Metadata = @Metadata,
                                 Notes = @Notes,
                                 CreatedAt = @CreatedAt,
                                 UpdatedAt = @UpdatedAt
                            WHERE Id = @Id
                            Returning *;";
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", payment.Id);

            var reader = await command.ExecuteReaderAsync();

            Payment paymentObj = null;

            while(await reader.ReadAsync())
            {
                if(paymentObj == null)
                {
                    paymentObj = new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };

                    return paymentObj;
                }
            }
            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            string query = "delete from Payments where Id = @Id";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowEffected = await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            return rowEffected > 0;
        }

        public async Task<int> GetTotalCountAsync()
        {
            string query = @"SELECT COUNT(1) from Payments";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            var countObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            string query = "select count(1) from Payments where UserId = @UserId";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var countObj = command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<int> GetCountByStatusAsync(PaymentStatus status)
        {
            string query = @"select count(1) from Payments where Status = @Status";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Status", status);

            var countObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<decimal> GetTotalAmountByUserIdAsync(int userId)
        {
            string sql = @"select sum(Amount) from Payments where UserId = @UserId";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var totalObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToDecimal(totalObj);
        }

        public async Task<decimal> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            string query = "SELECT sum(amount) from Payments where CreatedAt >= @StartDate and CreatedAt <= @EndDate";

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var totalObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToDecimal(totalObj);
        }

        public async Task<IEnumerable<Payment>> SearchAsync(string searchTerm, int pageNumber, int pageSize)
        {
            string query = @"SELECT p.*, u.*, o.* from Payments
                             left join Users u on (p.UserId = u.Id)
                             left join Orders o on (p.OrderId = o.Id)
                             where @searchTerm = null or @searchTerm = ''
                             or TransactionId ilike '%' || @SearchTerm || '%'
                             or PayerEmail ilike '%' || @SearchTerm || '%'
                             or PayerName ilike '%' || @SearchTerm || '%'
                             or Provider ilike '%' || @SearchTerm || '%'
                             or Email ilike '%' || @SearchTerm || '%'
                             or UserName ilike '%' || @SearchTerm || '%'";

            var connection = new NpgsqlConnection(_connectionString);

            var command = new NpgsqlCommand(query, connection);

            var normalizeSearchTerm = searchTerm.Trim();

            command.Parameters.AddWithValue("@SearchTerm", normalizeSearchTerm);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = null;

            while (await reader.ReadAsync())
            {
                if (payments == null)
                {
                    payments.Add(new Payment()
                    {
                        Id = reader.GetInt16(reader.GetOrdinal("Id")),
                        UserId = reader.GetInt16(reader.GetOrdinal("UserId")),
                        OrderId = reader.GetInt16(reader.GetOrdinal("OrderId")),
                        TransactionId = reader.GetString(reader.GetOrdinal("TransactionId")),
                        Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("Status")),
                        Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("Method")),
                        Provider = reader.GetString(reader.GetOrdinal("Provider")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                        Currency = reader.GetString(reader.GetOrdinal("Currency")),
                        ProviderTransactionId = reader.GetString(reader.GetString("ProviderTransactionId") ?? null),
                        CardLast4 = reader.GetString(reader.GetOrdinal("CardLast4")),
                        CardBrand = reader.GetString(reader.GetOrdinal("CardBrand")),
                        PayerEmail = reader.GetString(reader.GetOrdinal("PayerEmail")),
                        PayerName = reader.GetString(reader.GetOrdinal("PayerName")),
                        BillingAddress1 = reader.GetString(reader.GetOrdinal("BillingAddress1")),
                        BillingAddress2 = reader.GetString(reader.GetOrdinal("BillingAddress2")),
                        BillingCity = reader.GetString(reader.GetOrdinal("BillingCity")),
                        BillingState = reader.GetString(reader.GetOrdinal("BillingState")),
                        BillingPostalCode = reader.GetString(reader.GetOrdinal("BillingPostalCode")),
                        BillingCountry = reader.GetString(reader.GetOrdinal("BillingCountry")),
                        ProcessingFee = reader.GetDecimal(reader.GetOrdinal("ProcessingFee")),
                        NetAmount = reader.GetDecimal(reader.GetOrdinal("NetAmount")),
                        ProcessedAt = reader.GetDateTime(reader.GetOrdinal("ProcessedAt")),
                        CompletedAt = reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                        FailedAt = reader.GetDateTime(reader.GetOrdinal("FailedAt")),
                        RefundedAt = reader.GetDateTime(reader.GetOrdinal("RefundAt")),
                        ErrorMessage = reader.GetString(reader.GetOrdinal("ErrorMessage")),
                        FailureReason = reader.GetString(reader.GetOrdinal("FailureReason")),
                        Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),



                        User = new Domain.Entities.AuthEntities.User
                        {
                            FirstName = reader.GetString(reader.GetOrdinal("U_FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("U_LastName")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("U_PhoneNumber"))
                        },

                        Order = new Domain.Entities.OrderEntities.Order
                        {
                            CartId = reader.GetInt16(reader.GetOrdinal("O_CartId"))
                        }
                    });
                }
            }

            return payments;
        }
    }
}
