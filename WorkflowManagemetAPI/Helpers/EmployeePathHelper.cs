using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowManagemetAPI.Helpers
{
    public static class EmployeePathHelper
    {
        public static List<int> ParseToIds(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new List<int>();

            return path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
        }
    }
}
