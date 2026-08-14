namespace NexgenCosysReport.Dtos.RequestDtos.Common;

public enum FilterOption
{
    StartsWith,
    EndsWith,
    Contains,
    DoesNotContain,
    IsEmpty,
    IsNotEmpty,
    IsGreaterThan,
    IsGreaterThanOrEqualTo,
    IsLessThan,
    IsLessThanOrEqualTo,
    IsEqualTo,
    IsNotEqualTo
}

public enum SortOrder
{
    Asc,
    Desc
}

public class FilterParam
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public FilterOption Option { get; set; }
}

public class SortParam
{
    public SortParam() { }

    public SortParam(string field, SortOrder sortOrder)
    {
        Field = field;
        SortOrder = sortOrder;
    }

    public string Field { get; set; } = string.Empty;
    public SortOrder SortOrder { get; set; }
}

public class Filter
{
    public uint PageNumber { get; set; } = 1;
    public uint PageSize { get; set; } = 20;
    public List<FilterParam> Params { get; set; } = [];
    public List<SortParam> Sort { get; set; } = [];
}




