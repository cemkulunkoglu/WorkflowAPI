using MySqlConnector;

namespace MessagesService.Data;

public class MySqlApproverEmailLookup : IApproverEmailLookup
{
    private readonly IConfiguration _config;

    public MySqlApproverEmailLookup(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string?> GetApproverEmailByEmployeeIdAsync(int approverEmployeeId, CancellationToken ct)
    {
        var cs = _config.GetConnectionString("EmployeeDB");
        if (string.IsNullOrWhiteSpace(cs))
            throw new Exception("ConnectionStrings:EmployeeDB missing in MessagesService.");

        // Employee(EmployeeId -> UserId) JOIN Users(UserId -> Email)
        const string sql = @"
                            SELECT u.Email
                            FROM Employee e
                            JOIN Users u ON u.UserId = e.UserId
                            WHERE e.EmployeeId = @approverEmployeeId
                            LIMIT 1;";

        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@approverEmployeeId", approverEmployeeId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result == null || result == DBNull.Value ? null : result.ToString();
    }
}
