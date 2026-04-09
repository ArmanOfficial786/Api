using System.ComponentModel.DataAnnotations;

namespace JsSampleReport.Utils.Enum
{
    public class Enums
    {
        // ✅ MemberIdCard OrderBy
    public enum MemberIdCardOrderBy
    {
        [Display(Name = "Member Name")]
        name = 1,
        [Display(Name = "Registration Date")]
        sex = 2,
        memberId = 3,
        [Display(Name = "Age")]
        BirthOnBs = 4,
        [Display(Name = "Registration Date")]
        RegistrationOn = 5
    }

    // ✅ SavingTypeWiseBalance OrderBy
    public enum SavingTypeWiseBalanceOrderBy
    {
        SavingType = 1,
        Deposit = 2,
        Withdraw = 3,
        Balance = 4
    }

    // ✅ Add more report enums here as needed
    // public enum AccountStatementOrderBy { ... }
    }
}
