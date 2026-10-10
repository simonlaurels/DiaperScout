using Npgsql;
using System.Text.Json;
try {
 await using var connection=new NpgsqlConnection(Environment.GetEnvironmentVariable("ConnectionStrings__diaperscout"));
 await connection.OpenAsync();
 await using var transaction=await connection.BeginTransactionAsync();
 await using(var readOnly=new NpgsqlCommand("SET TRANSACTION READ ONLY",connection,transaction)) await readOnly.ExecuteNonQueryAsync();
 const string sql="""
 SELECT count(*) FILTER (WHERE u."Status"=0),
        count(*) FILTER (WHERE u."Status"=0 AND p."Id" IS NULL),
        count(*) FILTER (WHERE u."Status"=0 AND p."Id" IS NULL AND EXISTS (SELECT 1 FROM diaperscout.passkey_credentials k WHERE k."UserId"=u."Id")),
        count(*) FILTER (WHERE u."Status"=0 AND p."Id" IS NULL AND EXISTS (SELECT 1 FROM diaperscout.privileged_role_assignments r WHERE r."UserId"=u."Id" AND r."RevokedAtUtc" IS NULL))
 FROM diaperscout.users u LEFT JOIN diaperscout.explorer_profiles p ON p."UserId"=u."Id"
 """;
 await using var command=new NpgsqlCommand(sql,connection,transaction);
 await using(var reader=await command.ExecuteReaderAsync()) {
  await reader.ReadAsync();
  Console.WriteLine(JsonSerializer.Serialize(new {activeAccounts=reader.GetInt64(0),activeWithoutProfile=reader.GetInt64(1),passkeyAccountsWithoutProfile=reader.GetInt64(2),privilegedAccountsWithoutProfile=reader.GetInt64(3),readOnly=true}));
 }
 await transaction.RollbackAsync();
} catch(Exception e) { Console.Error.WriteLine("Read-only probe failed: "+e.GetType().Name); Environment.ExitCode=1; }
