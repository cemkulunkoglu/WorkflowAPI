using WorkflowManagemetAPI.Helpers;

namespace WorkflowManagemetAPI.Services.LeaveRequests
{
    public class ApproverResolver
    {
        public int? ResolveApproverEmployeeId(string? employeePath, int requestedDays)
        {
            if (requestedDays <= 0)
                throw new ArgumentException("requestedDays 0'dan büyük olmalıdır.", nameof(requestedDays));

            var chain = EmployeePathHelper.ParseToIds(employeePath);

            // Path yoksa veya sadece kendisi bile yoksa
            if (chain.Count == 0)
                return null;

            var selfIndex = chain.Count - 1;

            // Kural: gün sayısına göre hedef seviye
            var targetLevel = requestedDays <= 5 ? 1 : 2;

            // Kök->...->self zincirinde yukarı çık
            var targetIndex = selfIndex - targetLevel;

            // 2 üst yoksa 1 üste düş
            if (targetIndex < 0 && targetLevel == 2)
                targetIndex = selfIndex - 1;

            // Hâlâ yoksa (selfIndex - 1 < 0) => üst yok (en üst)
            if (targetIndex < 0)
                return null;

            // Approver EmployeeId
            return chain[targetIndex];
        }
    }
}
