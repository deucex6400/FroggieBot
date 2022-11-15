using Dapper;
using FroggieBot.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FroggieBot
{
    public class SqlService
    {
        public async Task<List<Claimable>> GetClaimable(string connectionString)
        {
            using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
            {
                await db.OpenAsync();
                var result = await db
                    .QueryAsync<Claimable>
                    ($"select * from claimable");
                return result.ToList();
            }
        }
    }
}
