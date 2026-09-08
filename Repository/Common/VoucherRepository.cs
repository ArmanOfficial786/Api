// Repository/Common/VoucherRepository.cs
using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.DbContext;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Common;

namespace NexgenCosysReport.Repository.Common
{
    public class VoucherRepository : IVoucher
    {
        private readonly AppDbContext _context;
        private readonly IDateConverterService _dateConverter;
        private readonly ILogger<VoucherRepository> _logger;

        // SQL Server's minimum valid "datetime" value — used to detect a failed/garbage
        // BS->AD conversion before it reaches SQL Server as an out-of-range parameter.
        private static readonly DateTime SqlDateTimeMin = new DateTime(1753, 1, 1);

        public VoucherRepository(
            AppDbContext context,
            IDateConverterService dateConverter,
            ILogger<VoucherRepository> logger)
        {
            _context = context;
            _dateConverter = dateConverter;
            _logger = logger;
        }

        public async Task<List<VoucherOptionResponse>> GetVoucherListAsync(
            VoucherListRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.FromDate) || string.IsNullOrWhiteSpace(request.ToDate))
                    throw new ArgumentException("From date and To date are required.");

                // Mirrors cComCalender.NepaliToEnglish(ncpFromDateOnBS.ShortNepaliDate) /
                // ncpToDateOnBS.ShortNepaliDate in BindDataToVoucherNo(), done server-side here.
                var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDate);
                var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDate);

                // Guard against a silent conversion failure (e.g. the converter returning
                // DateTime.MinValue on a bad/out-of-range BS string instead of throwing) —
                // without this, an invalid value reaches SQL Server as SqlDateTime overflow
                // instead of a clear validation error.
                if (fromDate <= SqlDateTimeMin || toDate <= SqlDateTimeMin)
                {
                    throw new ArgumentException(
                        $"Could not resolve BS date to a valid date. " +
                        $"FromDateBs='{request.FromDate}' -> {fromDate:yyyy-MM-dd}, " +
                        $"ToDateBs='{request.ToDate}' -> {toDate:yyyy-MM-dd}.");
                }

                if (fromDate > toDate)
                    throw new ArgumentException("From date must be on or before To date.");

                var query = _context.AcoVouchers
                    .Where(v => v.VoucherOn >= fromDate && v.VoucherOn <= toDate);

                // Office filter — mirrors: TotalCount == count -> branchId = -1 (no filter, "All" selected).
                if (request.BranchIds is { Count: > 0 })
                {
                    query = query.Where(v => request.BranchIds.Contains(v.UsmOfficeId));
                }

                return await query
                    .OrderByDescending(v => v.AcoVoucherId)
                    .Select(v => new VoucherOptionResponse
                    {
                        AcoVoucherId = v.AcoVoucherId,
                        VoucherNo = v.VoucherNo
                    })
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVoucherListAsync for VoucherList report");
                throw;
            }
        }

        public async Task<VoucherOptionResponse?> GetByVoucherNoAsync(
            string voucherNo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(voucherNo))
                    throw new ArgumentException("Voucher number is required.");

                return await _context.AcoVouchers
                    .Where(v => v.VoucherNo == voucherNo)
                    .Select(v => new VoucherOptionResponse
                    {
                        AcoVoucherId = v.AcoVoucherId,
                        VoucherNo = v.VoucherNo
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByVoucherNoAsync for VoucherNo: {VoucherNo}", voucherNo);
                throw;
            }
        }
    }
}