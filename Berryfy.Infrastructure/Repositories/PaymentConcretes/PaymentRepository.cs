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
using Berryfy.Domain.Entities;


namespace Berryfy.Infrastructure.Repositories.PaymentConcretes
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(IConfiguration config)
        {
            _connectionString = PostgresConnectionStrings.Resolve(config);
        }

        public async Task<InfrastructureResponse<Payment?>> GetByIdAsync(int id)
        {
            const string sql = @"SELECT * FROM payments p
                                 WHERE p.id = @Id
                                 LIMIT 1;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();

            Payment? payment = null;
            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return new InfrastructureResponse<Payment?>()
            {
                IsSuccess = payment != null,
                Message = payment != null ? "The process completed successfully" : "Payment not found",
                Value = payment
            };
        }

        public async Task<InfrastructureResponse<Payment?>> GetByTransactionIdAsync(string transactionId)
        {
            const string sql = @"SELECT * FROM payments p
                                 WHERE p.transaction_id = @TransactionId
                                 LIMIT 1;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionId", transactionId);

            await using var reader = await command.ExecuteReaderAsync();

            Payment? payment = null;
            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return new InfrastructureResponse<Payment?>()
            {
                IsSuccess = payment != null,
                Message = payment != null ? "The process completed successfully" : "Payment not found",
                Value = payment
            };
        }

        public async Task<InfrastructureResponse<Payment?>> GetByOrderIdAsync(int orderId)
        {
            const string query = @"
                SELECT p.* FROM payments p
                WHERE p.order_id = @OrderId
                ORDER BY
                    CASE WHEN p.status IN (@CompletedStatus, @PartiallyRefundedStatus, @RefundedStatus) THEN 1 ELSE 0 END DESC,
                    CASE WHEN p.status = @ProcessingStatus THEN 1 ELSE 0 END DESC,
                    p.created_at DESC,
                    p.id DESC
                LIMIT 1;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@CompletedStatus", (int)PaymentStatus.Completed);
            command.Parameters.AddWithValue("@PartiallyRefundedStatus", (int)PaymentStatus.PartiallyRefunded);
            command.Parameters.AddWithValue("@RefundedStatus", (int)PaymentStatus.Refunded);
            command.Parameters.AddWithValue("@ProcessingStatus", (int)PaymentStatus.Processing);

            await using var reader = await command.ExecuteReaderAsync();

            Payment? payment = null;
            if (await reader.ReadAsync())
            {
                payment = MapPayment(reader);
            }

            return new InfrastructureResponse<Payment?>()
            {
                IsSuccess = payment != null,
                Message = payment != null ? "The process completed successfully" : "Payment not found",
                Value = payment
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetAllAsync()
        {
            const string sql = @"SELECT p.* FROM payments p
                                 ORDER BY p.created_at DESC;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetByUserIdAsync(int userId)
        {
            const string sql = @"SELECT p.* FROM payments p
                                 WHERE p.user_id = @UserId
                                 ORDER BY p.created_at DESC, p.id DESC;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetByStatusAsync(PaymentStatus status)
        {
            const string sql = @"SELECT p.* FROM payments p
                                 WHERE p.status = @Status
                                 ORDER BY p.created_at DESC;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", (int)status);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"SELECT p.* FROM payments p
                                 WHERE p.created_at >= @StartDate AND p.created_at <= @EndDate
                                 ORDER BY p.created_at DESC;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            const string query = @"SELECT p.* FROM payments p
                                   ORDER BY p.created_at DESC
                                   OFFSET @offset
                                   LIMIT @pageSize;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            int offset = Math.Max(0, (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> GetPaginatedByUserIdAsync(int userId, int pageNumber, int pageSize)
        {
            const string query = @"SELECT p.* FROM payments p
                                   WHERE p.user_id = @UserId
                                   ORDER BY p.created_at DESC
                                   OFFSET @offset
                                   LIMIT @pageSize;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            int offset = Math.Max(0, (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@offset", offset);
            command.Parameters.AddWithValue("@pageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        public async Task<InfrastructureResponse<Payment>> CreateAsync(Payment payment)
        {
            const string query = @"
                INSERT INTO payments (
                    user_id, order_id, transaction_id, status, method, provider, amount, currency,
                    provider_transaction_id, provider_payment_method, card_last4, card_brand, payer_email,
                    payer_name, billing_address1, billing_address2, billing_city, billing_state, billing_postal_code,
                    billing_country, processing_fee, net_amount, processed_at, completed_at, failed_at, refunded_at,
                    error_message, failure_reason, metadata, notes, created_at, updated_at)
                VALUES (
                    @UserId, @OrderId, @TransactionId, @Status, @Method, @Provider, @Amount, @Currency,
                    @ProviderTransactionId, @ProviderPaymentMethod, @CardLast4, @CardBrand, @PayerEmail,
                    @PayerName, @BillingAddress1, @BillingAddress2, @BillingCity, @BillingState, @BillingPostalCode,
                    @BillingCountry, @ProcessingFee, @NetAmount, @ProcessedAt, @CompletedAt, @FailedAt, @RefundedAt,
                    @ErrorMessage, @FailureReason, @Metadata, @Notes, @CreatedAt, @UpdatedAt)
                RETURNING *;";

            var now = DateTime.UtcNow;
            if (payment.CreatedAt == default) payment.CreatedAt = now;
            if (payment.UpdatedAt == default) payment.UpdatedAt = now;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            AddPaymentParameters(command, payment);

            await using var reader = await command.ExecuteReaderAsync();

            Payment? paymentt = null;
            if (await reader.ReadAsync())
            {
                paymentt = MapPayment(reader);
            }

            return new InfrastructureResponse<Payment>()
            {
                IsSuccess = paymentt != null,
                Message = paymentt != null ? "The process completed successfully" : "Failed to create payment",
                Value = paymentt!
            };
        }

        public async Task<InfrastructureResponse<Payment>> UpdateAsync(Payment payment)
        {
            const string query = @"
                UPDATE payments
                SET user_id = @UserId,
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
                    refunded_at = @RefundedAt,
                    error_message = @ErrorMessage,
                    failure_reason = @FailureReason,
                    metadata = @Metadata,
                    notes = @Notes,
                    created_at = @CreatedAt,
                    updated_at = @UpdatedAt
                WHERE id = @Id
                RETURNING *;";

            payment.UpdatedAt = DateTime.UtcNow;

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            AddPaymentParameters(command, payment);
            command.Parameters.AddWithValue("@Id", payment.Id);

            await using var reader = await command.ExecuteReaderAsync();

            Payment? paymentObj = null;
            if (await reader.ReadAsync())
            {
                paymentObj = MapPayment(reader);
            }

            return new InfrastructureResponse<Payment>()
            {
                IsSuccess = paymentObj != null,
                Message = paymentObj != null ? "The process completed successfully" : "Payment not found",
                Value = paymentObj!
            };
        }

        public async Task<InfrastructureResponse<bool>> DeleteAsync(int id)
        {
            const string query = "DELETE FROM payments WHERE id = @Id";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowEffected = await command.ExecuteNonQueryAsync();

            return new InfrastructureResponse<bool>()
            {
                IsSuccess = rowEffected > 0,
                Message = "The process completed successfully",
                Value = rowEffected > 0
            };
        }

        public async Task<InfrastructureResponse<int>> GetTotalCountAsync()
        {
            const string query = @"SELECT COUNT(1) FROM payments";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            var countObj = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = countObj != null ? Convert.ToInt32(countObj) : 0
            };
        }

        public async Task<InfrastructureResponse<int>> GetCountByUserIdAsync(int userId)
        {
            const string query = "SELECT COUNT(1) FROM payments WHERE user_id = @UserId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var countObj = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = countObj != null ? Convert.ToInt32(countObj) : 0
            };
        }

        public async Task<InfrastructureResponse<int>> GetCountByStatusAsync(PaymentStatus status)
        {
            const string query = @"SELECT COUNT(1) FROM payments WHERE status = @Status";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@Status", (int)status);

            var countObj = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<int>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = countObj != null ? Convert.ToInt32(countObj) : 0
            };
        }

        public async Task<InfrastructureResponse<decimal>> GetTotalAmountByUserIdAsync(int userId)
        {
            const string sql = @"SELECT COALESCE(SUM(amount), 0) FROM payments WHERE user_id = @UserId";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            var totalObj = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<decimal>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = totalObj != null && totalObj != DBNull.Value ? Convert.ToDecimal(totalObj) : 0m
            };
        }

        public async Task<InfrastructureResponse<decimal>> GetTotalAmountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string query = "SELECT COALESCE(SUM(amount), 0) FROM payments WHERE created_at >= @StartDate AND created_at <= @EndDate";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            var totalObj = await command.ExecuteScalarAsync();

            return new InfrastructureResponse<decimal>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = totalObj != null && totalObj != DBNull.Value ? Convert.ToDecimal(totalObj) : 0m
            };
        }

        public async Task<InfrastructureResponse<IEnumerable<Payment>>> SearchAsync(string searchTerm, int pageNumber, int pageSize)
        {
            const string query = @"
                SELECT p.* FROM payments p
                LEFT JOIN users u ON p.user_id = u.id
                WHERE @SearchTerm = ''
                   OR p.transaction_id ILIKE '%' || @SearchTerm || '%'
                   OR p.payer_email ILIKE '%' || @SearchTerm || '%'
                   OR p.payer_name ILIKE '%' || @SearchTerm || '%'
                   OR p.provider ILIKE '%' || @SearchTerm || '%'
                   OR u.email ILIKE '%' || @SearchTerm || '%'
                   OR u.user_name ILIKE '%' || @SearchTerm || '%'
                ORDER BY p.created_at DESC
                OFFSET @Offset LIMIT @PageSize;";

            await using var connection = await OpenConnectionAsync();
            await using var command = new NpgsqlCommand(query, connection);

            var normalizeSearchTerm = (searchTerm ?? string.Empty).Trim();
            int offset = Math.Max(0, (pageNumber - 1) * pageSize);

            command.Parameters.AddWithValue("@SearchTerm", normalizeSearchTerm);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();

            List<Payment> payments = new List<Payment>();
            while (await reader.ReadAsync())
            {
                payments.Add(MapPayment(reader));
            }

            return new InfrastructureResponse<IEnumerable<Payment>>()
            {
                IsSuccess = true,
                Message = "The process completed successfully",
                Value = payments
            };
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private Payment MapPayment(NpgsqlDataReader reader)
        {
            var userIdOrd = reader.GetOrdinal("user_id");
            var orderIdOrd = reader.GetOrdinal("order_id");
            var providerMethodOrd = reader.GetOrdinal("provider_payment_method");
            var createdAtOrd = reader.GetOrdinal("created_at");
            var updatedAtOrd = reader.GetOrdinal("updated_at");

            return new Payment()
            {
                Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("id"))),
                UserId = reader.IsDBNull(userIdOrd) ? null : Convert.ToInt32(reader.GetValue(userIdOrd)),
                OrderId = reader.IsDBNull(orderIdOrd) ? null : Convert.ToInt32(reader.GetValue(orderIdOrd)),
                TransactionId = reader.GetString(reader.GetOrdinal("transaction_id")),
                Status = (PaymentStatus)Convert.ToInt32(reader.GetValue(reader.GetOrdinal("status"))),
                Method = (PaymentMethod)Convert.ToInt32(reader.GetValue(reader.GetOrdinal("method"))),
                Provider = reader.GetString(reader.GetOrdinal("provider")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                ProviderTransactionId = reader.IsDBNull(reader.GetOrdinal("provider_transaction_id")) ? null : reader.GetString(reader.GetOrdinal("provider_transaction_id")),
                ProviderPaymentMethodId = reader.IsDBNull(providerMethodOrd) ? null : reader.GetString(providerMethodOrd),
                CardLast4 = reader.IsDBNull(reader.GetOrdinal("card_last4")) ? null : reader.GetString(reader.GetOrdinal("card_last4")),
                CardBrand = reader.IsDBNull(reader.GetOrdinal("card_brand")) ? null : reader.GetString(reader.GetOrdinal("card_brand")),
                PayerEmail = reader.IsDBNull(reader.GetOrdinal("payer_email")) ? null : reader.GetString(reader.GetOrdinal("payer_email")),
                PayerName = reader.IsDBNull(reader.GetOrdinal("payer_name")) ? null : reader.GetString(reader.GetOrdinal("payer_name")),
                BillingAddress1 = reader.IsDBNull(reader.GetOrdinal("billing_address1")) ? null : reader.GetString(reader.GetOrdinal("billing_address1")),
                BillingAddress2 = reader.IsDBNull(reader.GetOrdinal("billing_address2")) ? null : reader.GetString(reader.GetOrdinal("billing_address2")),
                BillingCity = reader.IsDBNull(reader.GetOrdinal("billing_city")) ? null : reader.GetString(reader.GetOrdinal("billing_city")),
                BillingState = reader.IsDBNull(reader.GetOrdinal("billing_state")) ? null : reader.GetString(reader.GetOrdinal("billing_state")),
                BillingPostalCode = reader.IsDBNull(reader.GetOrdinal("billing_postal_code")) ? null : reader.GetString(reader.GetOrdinal("billing_postal_code")),
                BillingCountry = reader.IsDBNull(reader.GetOrdinal("billing_country")) ? null : reader.GetString(reader.GetOrdinal("billing_country")),
                ProcessingFee = reader.IsDBNull(reader.GetOrdinal("processing_fee")) ? 0 : reader.GetDecimal(reader.GetOrdinal("processing_fee")),
                NetAmount = reader.IsDBNull(reader.GetOrdinal("net_amount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("net_amount")),
                ProcessedAt = reader.IsDBNull(reader.GetOrdinal("processed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("processed_at")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("completed_at")),
                FailedAt = reader.IsDBNull(reader.GetOrdinal("failed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("failed_at")),
                RefundedAt = reader.IsDBNull(reader.GetOrdinal("refunded_at")) ? null : reader.GetDateTime(reader.GetOrdinal("refunded_at")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message")),
                FailureReason = reader.IsDBNull(reader.GetOrdinal("failure_reason")) ? null : reader.GetString(reader.GetOrdinal("failure_reason")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("metadata")) ? null : reader.GetString(reader.GetOrdinal("metadata")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                CreatedAt = reader.IsDBNull(createdAtOrd) ? DateTime.UtcNow : reader.GetDateTime(createdAtOrd),
                UpdatedAt = reader.IsDBNull(updatedAtOrd) ? DateTime.UtcNow : reader.GetDateTime(updatedAtOrd)
            };
        }

        private void AddPaymentParameters(NpgsqlCommand command, Payment payment)
        {
            command.Parameters.AddWithValue("@UserId", (object?)payment.UserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrderId", (object?)payment.OrderId ?? DBNull.Value);
            command.Parameters.AddWithValue("@TransactionId", payment.TransactionId ?? string.Empty);
            command.Parameters.AddWithValue("@Status", (int)payment.Status);
            command.Parameters.AddWithValue("@Method", (int)payment.Method);
            command.Parameters.AddWithValue("@Provider", payment.Provider ?? string.Empty);
            command.Parameters.AddWithValue("@Amount", payment.Amount);
            command.Parameters.AddWithValue("@Currency", payment.Currency ?? "USD");
            command.Parameters.AddWithValue("@ProviderTransactionId", (object?)payment.ProviderTransactionId ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardLast4", (object?)payment.CardLast4 ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardBrand", (object?)payment.CardBrand ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayerEmail", (object?)payment.PayerEmail ?? DBNull.Value);
            command.Parameters.AddWithValue("@PayerName", (object?)payment.PayerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress1", (object?)payment.BillingAddress1 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingAddress2", (object?)payment.BillingAddress2 ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCity", (object?)payment.BillingCity ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingState", (object?)payment.BillingState ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingPostalCode", (object?)payment.BillingPostalCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingCountry", (object?)payment.BillingCountry ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProviderPaymentMethod", (object?)payment.ProviderPaymentMethodId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProcessingFee", payment.ProcessingFee);
            command.Parameters.AddWithValue("@NetAmount", payment.NetAmount);
            command.Parameters.AddWithValue("@ProcessedAt", (object?)payment.ProcessedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@CompletedAt", (object?)payment.CompletedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@FailedAt", (object?)payment.FailedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@RefundedAt", (object?)payment.RefundedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@ErrorMessage", (object?)payment.ErrorMessage ?? DBNull.Value);
            command.Parameters.AddWithValue("@FailureReason", (object?)payment.FailureReason ?? DBNull.Value);
            command.Parameters.AddWithValue("@Metadata", (object?)payment.Metadata ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notes", (object?)payment.Notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", payment.CreatedAt == default ? DateTime.UtcNow : payment.CreatedAt);
            command.Parameters.AddWithValue("@UpdatedAt", payment.UpdatedAt == default ? DateTime.UtcNow : payment.UpdatedAt);
        }
    }
}