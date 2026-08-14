using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using System.Linq.Expressions;
using System.Reflection;



namespace NexgenCosysReport.Repository.Common
{
    public static class QueryableFilterExtensions
    {
        public static IQueryable<T> ApplyFilterParams<T>(this IQueryable<T> source, List<FilterParam> filters)
        {
            if (filters is not { Count: > 0 }) return source;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combined = null;

            foreach (var filter in filters)
            {
                var property = typeof(T).GetProperty(
                    filter.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property is null) continue; // unknown key from client -> ignore, don't 500

                var member = Expression.Property(parameter, property);
                var expression = BuildComparison(member, property.PropertyType, filter.Value, filter.Option);
                if (expression is null) continue;

                combined = combined is null ? expression : Expression.AndAlso(combined, expression);
            }

            if (combined is null) return source;

            var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
            return source.Where(lambda);
        }

        public static IQueryable<T> ApplySortParams<T>(this IQueryable<T> source, List<SortParam> sorts)
        {
            if (sorts is not { Count: > 0 }) return source;

            IOrderedQueryable<T>? ordered = null;

            foreach (var sort in sorts)
            {
                var property = typeof(T).GetProperty(
                    sort.Field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property is null) continue;

                var parameter = Expression.Parameter(typeof(T), "x");
                var member = Expression.Property(parameter, property);
                var keySelector = Expression.Lambda(member, parameter);

                var methodName = ordered is null
                    ? (sort.SortOrder == SortOrder.Asc ? "OrderBy" : "OrderByDescending")
                    : (sort.SortOrder == SortOrder.Asc ? "ThenBy" : "ThenByDescending");

                var method = typeof(Queryable).GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), property.PropertyType);

                ordered = (IOrderedQueryable<T>)method.Invoke(null, [ordered ?? source, keySelector])!;
            }

            return ordered ?? source;
        }

        public static async Task<Pagination<T>> ToPaginationAsync<T>(
            this IQueryable<T> source, Filter filter, CancellationToken ct = default)
        {
            var pageSize = filter.PageSize == 0 ? 20 : (int)filter.PageSize;
            var pageNumber = filter.PageNumber == 0 ? 1 : (int)filter.PageNumber;

            var totalCount = await source.CountAsync(ct);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new Pagination<T>
            {
                Items = items,
                totalRecord = totalCount,
                currentPage = pageNumber,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNextPage = pageNumber < totalPages,
                hasPreviousPage = pageNumber > 1
            };
        }


        public static async Task<Pagination<T>> ToPagedResultAsync<T>(
            this IQueryable<T> source, Filter filter, CancellationToken ct = default)
        {
            var pageSize = filter.PageSize == 0 ? 20 : (int)filter.PageSize;
            var pageNumber = filter.PageNumber == 0 ? 1 : (int)filter.PageNumber;

            var totalCount = await source.CountAsync(ct);
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new Pagination<T>
            {
                Items = items,
                totalRecord = totalCount,
                currentPage = pageNumber,
                pageSize = pageSize,
                totalPages = totalPages
            };
        }

        private static Expression? BuildComparison(
            MemberExpression member, Type propertyType, string value, FilterOption option)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
                var startsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
                var endsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
                var isNullOrEmptyMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!;

                var isNullOrEmpty = Expression.Call(isNullOrEmptyMethod, member);

                if (option == FilterOption.IsEmpty) return isNullOrEmpty;
                if (option == FilterOption.IsNotEmpty) return Expression.Not(isNullOrEmpty);

                var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
                var memberLower = Expression.Call(member, toLower);
                var constantLower = Expression.Constant(value.ToLower());

                Expression comparison = option switch
                {
                    FilterOption.Contains => Expression.Call(memberLower, contains, constantLower),
                    FilterOption.DoesNotContain => Expression.Not(Expression.Call(memberLower, contains, constantLower)),
                    FilterOption.StartsWith => Expression.Call(memberLower, startsWith, constantLower),
                    FilterOption.EndsWith => Expression.Call(memberLower, endsWith, constantLower),
                    FilterOption.IsEqualTo => Expression.Equal(memberLower, constantLower),
                    FilterOption.IsNotEqualTo => Expression.NotEqual(memberLower, constantLower),
                    _ => Expression.Constant(true)
                };

                return Expression.AndAlso(notNull, comparison);
            }

            object typedValue;
            try
            {
                typedValue = underlyingType == typeof(Guid)
                    ? Guid.Parse(value)
                    : Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                return null; // unparseable value for this column type -> skip, don't 500
            }

            var typedConstant = Expression.Constant(typedValue, propertyType);

            return option switch
            {
                FilterOption.IsEqualTo => Expression.Equal(member, typedConstant),
                FilterOption.IsNotEqualTo => Expression.NotEqual(member, typedConstant),
                FilterOption.IsGreaterThan => Expression.GreaterThan(member, typedConstant),
                FilterOption.IsGreaterThanOrEqualTo => Expression.GreaterThanOrEqual(member, typedConstant),
                FilterOption.IsLessThan => Expression.LessThan(member, typedConstant),
                FilterOption.IsLessThanOrEqualTo => Expression.LessThanOrEqual(member, typedConstant),
                _ => null
            };
        }
    }
}
