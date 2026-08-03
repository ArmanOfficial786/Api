using Dapper;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using System.Reflection;

namespace NexgenCosysReport.Utils.Report
{
    public static class DapperTypeMaps
    {
        // SP column name -> DTO property name, for SavingTypeWiseBalanceRowDto.
        // Keys are matched case-insensitively.
        private static readonly Dictionary<string, string> SavingTypeWiseBalanceRenames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Count"] = nameof(SavingTypeWiseBalanceRowDto.TransAccCount),
                ["OfficeName"] = nameof(SavingTypeWiseBalanceRowDto.BranchName),
                ["CenterName"] = nameof(SavingTypeWiseBalanceRowDto.CollectionCenterName),
                ["GroupName"] = nameof(SavingTypeWiseBalanceRowDto.MemberGroupName),
                ["CollectorName"] = nameof(SavingTypeWiseBalanceRowDto.MemberName),
            };

        private static bool _registered;
        private static readonly object _lock = new();

        public static void Register()
        {
            // Idempotent - safe to call more than once (e.g. in tests) without
            // re-registering or throwing.
            if (_registered) return;

            lock (_lock)
            {
                if (_registered) return;

                SqlMapper.SetTypeMap(
                    typeof(SavingTypeWiseBalanceRowDto),
                    BuildRenamingTypeMap(typeof(SavingTypeWiseBalanceRowDto), SavingTypeWiseBalanceRenames));

                _registered = true;
            }
        }

        /// <summary>
        /// Builds a Dapper ITypeMap that first checks an explicit column-name
        /// rename table, then falls back to Dapper's normal case-insensitive
        /// exact-name matching for every other column (SavingType, Opening,
        /// Deposit, Withdraw, Balance, Closing, MemberId, AccountNo, Percentage).
        /// </summary>
        private static SqlMapper.ITypeMap BuildRenamingTypeMap(Type type, IReadOnlyDictionary<string, string> renames)
        {
            return new CustomPropertyTypeMap(type, (t, columnName) =>
            {
                if (renames.TryGetValue(columnName, out var propName))
                {
                    var renamed = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    if (renamed != null)
                        return renamed;
                }

                var exact = t.GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                    return exact;

                // No match at all (e.g. the SP added a column nobody consumes yet).
                // Returning null tells Dapper to ignore this column instead of
                // throwing - matches Dapper's normal default-map behavior.
                return null;
            });
        }
    }
}
