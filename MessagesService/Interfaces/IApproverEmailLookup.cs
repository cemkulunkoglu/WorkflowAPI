namespace MessagesService.Interfaces;

public interface IApproverEmailLookup
{
    Task<string?> GetApproverEmailByEmployeeIdAsync(int approverEmployeeId, CancellationToken ct);

    Task<string?> GetEmployeeEmailByEmployeeIdAsync(int employeeId, CancellationToken ct);
    Task<string?> GetEmployeeFullNameByEmployeeIdAsync(int employeeId, CancellationToken ct);
    Task<string?> GetEmployeePathByEmployeeIdAsync(int employeeId, CancellationToken ct);
}
