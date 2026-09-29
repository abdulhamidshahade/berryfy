using Berryfy.Application.Dtos.PaymentDtos;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.PaymentEntities;
using Berryfy.Domain.Repositories.PaymentInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Berryfy.Infrastructure.Data;
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
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            string sql = @"SELECT * from payments p
                           Where p.id = @Id
                           Limit 1;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = new Payment();

            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return payment;
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            string sql = @"SELECT * from payments p
                           Where p.transaction_id = @TransactionId
                           Limit 1;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@TransactionId", transactionId);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = null;

            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return payment;
        }

        public async Task<Payment?> GetByOrderIdAsync(int orderId)
        {
            string query = @"Select p.* from
                          payments p
                          where p.order_id = @OrderId
                          order by case
                          When p.status in (completed, PartiallyRefunded, Refunded) then 1
                         else 0 End Desc, case when p.Status = Processing then 1 else 0 end,
                         p.created_at desc, p.id desc limit 1;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);

            var reader = await command.ExecuteReaderAsync();

            Payment payment = new Payment();

            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return null;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            string sql = @"SELECT p.*
                           Order by created_at desc;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);


            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByUserIdAsync(int userId)
        {
            string sql = @"SELECT p.*
                           Where p.user_id = @UserId
                           Order by id asc";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            { 
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status)
        {
            string sql = @"SELECT p.* from payments p
                           Where p.Status = @Status
                           Order by created_at desc";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Status", status);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            string sql = @"SELECT p.* from payments p
                           Where created_at >= @StartDate and created_at <= @EndDate
                           Order by created_at desc";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        public async Task<IEnumerable<Payment>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            string query = @"select p.* from Payments
                             order by p.created_at desc
                             offset @offset
                             limit @pageSize;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (pageNumber - 1) * pageSize;

            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;

        }

        public async Task<IEnumerable<Payment>> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            string query = @"select p.* from Payments
                             where user_id = @UserId
                             order by p.created_at desc
                             offset @offset
                             limit @pageSize;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            int offset = (pageNumber - 1) * pageSize;

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            string query = @"Insert into Payments (user_id, order_id, transaction_id, status, method, provider, amount, currency,
                             provider_transaction_id, provider_payment_method, card_last4, card_brand, payer_email,
                             payer_name, billing_address1, billing_address2, billing_city, billing_state, billing_postal_code,
                             billing_country, processing_fee, net_amount, processed_at, completed_at, failed_at, refunded_at,
                             error_message, failure_reason, metadata, notes, created_at, updated_at)
                             Values (@userId, @orderId, @TransactionId, @Status, @Method, @Provider, @Amount, @Currency,
                                     @ProviderTransactionId, @ProviderPaymentMethod, @CardLast4, @CardBrand, @PayerEmail,
                                     @PayerName, @BillingAddress1, @BillingAddress2, @BillingCity, @BillingState, @BillingPostalCode,
                                     @BillingCountry, @ProccessingFee, @NetAmount, @ProccessedAt, @CompletedAt, @FailedAt, @RefundAt,
                                     @ErrorMessage, @FailureReason, @Metadata, @Notes, @CreatedAt, @UpdatedAt)
                             Returning *;";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);
            AddPaymentParameters(command, payment);

            var reader = await command.ExecuteReaderAsync();

            Payment paymentt = new Payment();

            if (await reader.ReadAsync())
            {
                paymentt = MapPayment(reader);
            }

            return paymentt;
        }

        public async Task<Payment> UpdateAsync(Payment payment)
        {
            string query = @"Update Payments 
                             Set user_id = @UserId,
                                 order_id = @OrderId,
                                 transaction_id = @TransactionId,
                                 status = @Status,
                                 method = @Method,
                                 provider = @Provider,
                                 amount = @Amount,
                                 currency = @Currency,
                                 provider_transaction_id = @ProviderTransactionId,
                                 provider_payment_method = @ProviderPaymentMethod,
                                 card_last4 = @CardLast4,
                                 card_brand = @CardBrand,
                                 payer_email = @PayerEmail,
                                 payer_name = @PayerName,
                                 billing_address1 = @BillingAddress1,
                                 billing_address2 = @BillingAddress2,
                                 billing_city = @BillingCity,
                                 billing_state = @BillingState,
                                 billing_postal_code = @BillingPostalCode,
                                 billing_country = @BillingCountry,
                                 processing_fee = @ProcessingFee,
                                 net_amount = @NetAmount,
                                 processed_at = @ProcessedAt,
                                 completed_at = @CompletedAt,
                                 failed_at = @FailedAt,
                                 refund_at = @RefundAt,
                                 error_message = @ErrorMessage,
                                 failure_reason = @FailureReason,
                                 metadata = @Metadata,
                                 notes = @Notes,
                                 created_at = @CreatedAt,
                                 updated_at = @UpdatedAt
                            WHERE id = @Id
                            Returning *;";
            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", payment.Id);

            var reader = await command.ExecuteReaderAsync();

            Payment paymentObj = new Payment();

            if (await reader.ReadAsync())
            {
                paymentObj = MapPayment(reader);
            }
            return paymentObj;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            string query = "delete from Payments where id = @Id";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowEffected = await command.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            return rowEffected > 0;
        }

        public async Task<int> GetTotalCountAsync()
        {
            string query = @"SELECT COUNT(1) from payments";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            var countObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<int> GetCountByUserIdAsync(int userId)
        {
            string query = "select count(1) from payments where user_id = @UserId";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var countObj = command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<int> GetCountByStatusAsync(PaymentStatus status)
        {
            string query = @"select count(1) from payments where status = @Status";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@Status", status);

            var countObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToInt16(countObj);
        }

        public async Task<decimal> GetTotalAmountByUserIdAsync(int userId)
        {
            string sql = @"select sum(Amount) from payments where user_id = @UserId";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@UserId", userId);

            var totalObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToDecimal(totalObj);
        }

        public async Task<decimal> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            string query = "SELECT sum(amount) from payments where created_at >= @StartDate and created_at <= @EndDate";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var totalObj = await command.ExecuteScalarAsync();

            await connection.CloseAsync();

            return Convert.ToDecimal(totalObj);
        }

        public async Task<IEnumerable<Payment>> SearchAsync(string searchTerm, int pageNumber, int pageSize)
        {
            string query = @"SELECT p.* from payments p
                             where @searchTerm = null or @searchTerm = ''
                             or transaction_id ilike '%' || @SearchTerm || '%'
                             or payer_email ilike '%' || @SearchTerm || '%'
                             or payer_name ilike '%' || @SearchTerm || '%'
                             or provider ilike '%' || @SearchTerm || '%'
                             or email ilike '%' || @SearchTerm || '%'
                             or user_name ilike '%' || @SearchTerm || '%'";

            var connection = await OpenConnectionAsync();

            var command = new NpgsqlCommand(query, connection);

            var normalizeSearchTerm = searchTerm.Trim();

            command.Parameters.AddWithValue("@SearchTerm", normalizeSearchTerm);

            var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();

            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return payments;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Payment MapPayment(NpgsqlDataReader reader)
        {
            payment.Id = reader.GetInt16(reader.GetOrdinal("Id"));
            payment.UserId = reader.GetInt16(reader.GetOrdinal("user_id"));
            payment.OrderId = reader.GetInt16(reader.GetOrdinal("order_id"));
            payment.TransactionId = reader.GetString(reader.GetOrdinal("transaction_id"));
            payment.Status = (PaymentStatus)reader.GetValue(reader.GetOrdinal("status"));
            payment.Method = (PaymentMethod)reader.GetValue(reader.GetOrdinal("method"));
            payment.Provider = reader.GetString(reader.GetOrdinal("provider"));
            payment.Amount = reader.GetDecimal(reader.GetOrdinal("amount"));
            payment.Currency = reader.GetString(reader.GetOrdinal("currency"));
            payment.ProviderTransactionId = reader.IsDBNull(reader.GetOrdinal("provider_transaction_id")) ? null : reader.GetString(reader.GetOrdinal("provider_transaction_id"));
            payment.CardLast4 = reader.IsDBNull(reader.GetOrdinal("card_last4")) ? null : reader.GetString(reader.GetOrdinal("card_last4"));
            payment.CardBrand = reader.IsDBNull(reader.GetOrdinal("card_brand")) ? null : reader.GetString(reader.GetOrdinal("card_brand"));
            payment.PayerEmail = reader.IsDBNull(reader.GetOrdinal("payer_email")) ? null : reader.GetString(reader.GetOrdinal("payer_email"));
            payment.PayerName = reader.IsDBNull(reader.GetOrdinal("payer_name")) ? null : reader.GetString(reader.GetOrdinal("payer_name"));
            payment.BillingAddress1 = reader.IsDBNull(reader.GetOrdinal("billing_address1")) ? null : reader.GetString(reader.GetOrdinal("billing_address1"));
            payment.BillingAddress2 = reader.IsDBNull(reader.GetOrdinal("billing_address2")) ? null : reader.GetString(reader.GetOrdinal("billing_address2"));
            payment.BillingCity = reader.IsDBNull(reader.GetOrdinal("billing_city")) ? null : reader.GetString(reader.GetOrdinal("billing_city"));
            payment.BillingState = reader.IsDBNull(reader.GetOrdinal("billing_state")) ? null : reader.GetString(reader.GetOrdinal("billing_state"));
            payment.BillingPostalCode = reader.IsDBNull(reader.GetOrdinal("billing_postal_code")) ? null : reader.GetString(reader.GetOrdinal("billing_postal_code"));
            payment.BillingCountry = reader.IsDBNull(reader.GetOrdinal("billing_country")) ? null : reader.GetString(reader.GetOrdinal("billing_country"));
        }

        private void AddPaymentParameters(NpgsqlCommand command, Payment payment)
        {
            command.Parameters.AddWithValue("@UserId", payment.UserId);
            command.Parameters.AddWithValue("@OrderId", payment.OrderId);
            command.Parameters.AddWithValue("@TransactionId", payment.TransactionId);
            command.Parameters.AddWithValue("@Status", payment.Status);
            command.Parameters.AddWithValue("@Method", payment.Method);
            command.Parameters.AddWithValue("@Provider", payment.Provider);
            command.Parameters.AddWithValue("@Amount", payment.Amount);
            command.Parameters.AddWithValue("@Currency", payment.Currency);
            command.Parameters.AddWithValue("@ProviderTransactionId", (object)payment.ProviderTransactionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardLast4", (object)payment.CardLast4 ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardBrand", (object)payment.CardBrand ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayerEmail", (object)payment.PayerEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayerName", (object)payment.PayerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress1", (object)payment.BillingAddress1 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress2", (object)payment.BillingAddress2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCity", (object)payment.BillingCity ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingState", (object)payment.BillingState ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingPostalCode", (object)payment.BillingPostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCountry", (object)payment.BillingCountry ?? DBNull.Value);
    }
}