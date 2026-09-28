using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using System.Data;
using System.Globalization;
using System.Text;

namespace NexgenCosysReport.Repository.Common
{
    public class RemittanceReceiptRepository : IRemittanceReceipt
    {
        private const string DateFormat = "yyyy-MM-dd";
        private readonly AppDbContext _context;

        public RemittanceReceiptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RemittanceReceiptResponse>> GetRemittanceReceipts(RemittanceReceiptRequest request)
        {
            // The stored procedure executes this string as SQL, so every value is
            // validated or typed before it is appended.
            var filter = new StringBuilder();

            var hasFrom = !string.IsNullOrWhiteSpace(request.FromDate);
            var hasTo = !string.IsNullOrWhiteSpace(request.ToDate);

            if (hasFrom && hasTo)
            {
                if (!DateTime.TryParseExact(request.FromDate, DateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var fromDate))
                    throw new ArgumentException($"FromDate must be in {DateFormat} format");

                if (!DateTime.TryParseExact(request.ToDate, DateFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var toDate))
                    throw new ArgumentException($"ToDate must be in {DateFormat} format");

                if (fromDate > toDate)
                    throw new ArgumentException("From date cannot be after to date");

                // Re-formatted from the parsed DateTime, never the raw input string.
                var from = fromDate.ToString(DateFormat, CultureInfo.InvariantCulture);
                var to = toDate.ToString(DateFormat, CultureInfo.InvariantCulture);
                filter.Append($" And TransactionOn between '{from}' And '{to}' ");
            }

            if (request.OfficeIds is { Count: > 0 })
                filter.Append($" And UsmOfficeId in ({string.Join(",", request.OfficeIds)}) ");

            if (request.CreatedBy.HasValue)
                filter.Append($" And CreatedBy = {request.CreatedBy.Value} ");

            if (request.IsReceived.HasValue)
                filter.Append($" And IsReceived = {(request.IsReceived.Value ? 1 : 0)} ");

            var result = new List<RemittanceReceiptResponse>();

            var connection = _context.Database.GetDbConnection();
            var wasClosed = connection.State == ConnectionState.Closed;
            if (wasClosed) await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "sp_9_67_GetRemittanceReceipt";
                command.CommandType = CommandType.StoredProcedure;

                var param = command.CreateParameter();
                param.ParameterName = "@SqlFilterExp";
                param.Value = filter.ToString();
                command.Parameters.Add(param);

                await using var reader = await command.ExecuteReaderAsync();

                var idOrdinal = reader.GetOrdinal("RemRemittanceDetailsId");
                var nameOrdinal = reader.GetOrdinal("IMECodeNo");

                while (await reader.ReadAsync())
                {
                    result.Add(new RemittanceReceiptResponse
                    {
                        RemittanceReceiptId = Convert.ToInt64(reader.GetValue(idOrdinal)),
                        RemittanceReceiptName = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal)
                    });
                }
            }
            finally
            {
                if (wasClosed) await connection.CloseAsync();
            }

            return result;
        }
    }
}