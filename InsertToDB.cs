using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;

namespace Sql.Helper
{
    internal class InsertToDB
    {
        // Connection to database
        // Save record to database
        public static IActionResult SaveData(string connectionString, string result, ILogger log)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    if (result != null)
                    {
                        var query = $"INSERT INTO [dbo].[TR_AzureUserQuery] (QueryDate, QueryResult) VALUES ('{DateTime.Now}', '{result}')";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // logs information in the console
                log.LogError(ex.ToString());
                return new BadRequestResult();
            }

            return new OkResult();
        }
    }
}
