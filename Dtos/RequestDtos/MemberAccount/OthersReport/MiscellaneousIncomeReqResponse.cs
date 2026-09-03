
namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class MiscellaneousIncomeRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;

        // SQL filter (comma-separated UsmOfficeIds), e.g. "12,15,20" or "-1" for all
        public string? BranchIds { get; set; }

        // Display name for the header block, e.g. "Kathmandu Branch" or "All Branches"
        public string? BranchName { get; set; }

        public string OrderBy { get; set; } = "Member Id";
        public string ReportType { get; set; } = "Miscellaneous";  // "Miscellaneous" or "Fund"

        // Nullable long — matches request.MemberId.HasValue / request.MemberId.Value in the repository.
        // -1 (or null) means "no specific member selected".
        public long? MemberId { get; set; }

        public bool VisualReport { get; set; } = false;
    }

    public class MiscellaneousIncomeRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Particulars { get; set; }   // Group header, e.g. "Account Closing Charges"
        public string? Details { get; set; }        // Per-transaction line, e.g. "Loan Issue Deposit (SL-01-17)"
        public decimal? Amount { get; set; }
        public string? Operator { get; set; }
    }

    public class MiscellaneousIncomeData
    {
        public List<MiscellaneousIncomeRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; }
        public string? SelectedMemberId { get; set; }
        public string? SelectedMemberName { get; set; }
    }

    // Kept intentionally (even if currently unused) so the file's public type set
    // stays stable across edits — removing a class while the app is running under
    // the debugger triggers ENC0033 ("Deleting class ... requires restarting the
    // application"), which forces a full restart instead of a hot-reload apply.
    public class MiscellaneousIncomeReqResponse
    {
    }
}