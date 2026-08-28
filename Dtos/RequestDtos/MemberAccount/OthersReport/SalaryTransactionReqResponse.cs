namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SalaryTransactionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Staff Name";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
        public int ReportType { get; set; } = 0; // 0=Detail, 1=Summary
        public string TransferOn { get; set; } = "A"; // A=All, S=Saving, B=Bank
        public string StaffSelection { get; set; } = "O"; // O=Office Wise, A=All Staff
        public long? StaffId { get; set; } = -1;
    }

    public class SalaryTransactionRowDto
    {
        public string? Date { get; set; }
        public string? StaffName { get; set; }
        public string? AccountNo { get; set; }
        public decimal? SalaryAmount { get; set; }
        public decimal? TdsAmount { get; set; }
        public decimal? AllowanceAmount { get; set; }
        public decimal? PFFO { get; set; } // Provident Fund From Office
        public decimal? PFFS { get; set; } // Provident Fund From Salary
        public decimal? OverTimeSalaryAmount { get; set; }
        public decimal? LeaveDeductedAmount { get; set; }
        public decimal? AdvanceDeductedAmount { get; set; }
        public decimal? NetSalary { get; set; }
        public string? Operator { get; set; }
        public string? TransferOn { get; set; }
        public string? Description { get; set; }
    }

    public class SalaryTransactionData
    {
        public List<SalaryTransactionRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalSalaryAmount { get; set; }
        public decimal TotalTdsAmount { get; set; }
        public decimal TotalAllowanceAmount { get; set; }
        public decimal TotalPFFO { get; set; }
        public decimal TotalPFFS { get; set; }
        public decimal TotalOverTimeSalary { get; set; }
        public decimal TotalLeaveDeduction { get; set; }
        public decimal TotalAdvanceDeduction { get; set; }
        public decimal TotalNetSalary { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public int ReportType { get; set; }
        public string? TransferOn { get; set; }
        public string? StaffSelection { get; set; }
        public string? SelectedStaffName { get; set; }
    }

    public class SalaryTransactionReqResponse
    {
    }
}
