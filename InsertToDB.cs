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
        public static void SaveData(string connectionString, string result, ILogger log)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    if (result != null)
                    {
                        using (SqlCommand command = new SqlCommand(
                            "INSERT INTO [dbo].[TR_AzureUserQuery] (QueryDate, QueryResult) VALUES (@queryDate, @queryResult)",
                            connection))
                        {
                            command.Parameters.AddWithValue("@queryDate", DateTime.Now);
                            command.Parameters.AddWithValue("@queryResult", result);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                throw;
            }
        }
    }
}
