using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NexgenCosysReport.Utils.Caching
{
    public static class ReportCacheKey
    {
        /// <summary>Key from the WHOLE request minus the properties that do not change the DB result.</summary>
        public static string ForData(object request, string reportName, params string[] ignore)
        {
            var node = JsonSerializer.SerializeToNode(request, request.GetType()) as JsonObject ?? new JsonObject();
            foreach (var name in node.Select(p => p.Key)
                                     .Where(k => ignore.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList())
                node.Remove(name);

            return $"{reportName}_DATA_{Hash(node.ToJsonString(), 8)}";
        }

        /// <summary>Same columns in any order/case => same tag. Null/empty => "ALL".</summary>
        public static string Columns(IEnumerable<string>? columns)
        {
            var list = (columns ?? Enumerable.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToLowerInvariant())
                .Distinct()
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            return list.Count == 0 ? "ALL" : Hash(string.Join("|", list), 6);
        }

        private static string Hash(string text, int bytes)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)), 0, bytes);
    }
}