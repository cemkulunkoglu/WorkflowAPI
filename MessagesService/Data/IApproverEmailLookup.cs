namespace MessagesService.Data;

public interface IApproverEmailLookup
{
    Task<string?> GetApproverEmailByEmployeeIdAsync(int approverEmployeeId, CancellationToken ct);
}
