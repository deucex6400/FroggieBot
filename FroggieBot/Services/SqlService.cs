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
            try
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
            catch (Exception ex)
            {
                return new List<Claimable>();
            }
        }

        public async Task<int> AddClaimable(string nftName, string nftData, string connectionString)
        {
            int result = 0;
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesClaimableExistParameters = new { NftData = nftData };
                    var doesClaimableExist = await db.QueryAsync<Claimable>($"select * from claimable where nftdata = @NftData", doesClaimableExistParameters);
                    if (doesClaimableExist.ToList().Count == 0)
                    {
                        var insertParameters = new
                        {
                            NftName = nftName,
                            NftData = nftData
                        };
                        result = await db.ExecuteAsync("INSERT INTO Claimable (NftName,NftData) VALUES (@NftName, @NftData)", insertParameters); // 1 when inserted
                    }
                    else
                    {
                        result = -1; // -1 when already exists
                    }
                }
            }
            catch (Exception ex)
            {
              
            }
            return result; //0 if something goes wrong
        }

        public async Task<int> RemoveClaimable(string nftData, string connectionString)
        {
            int result = 0;
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesClaimableExistParameters = new { NftData = nftData };
                    var doesClaimableExist = await db.QueryAsync<Claimable>($"select * from claimable where nftdata = @NftData", doesClaimableExistParameters);
                    if (doesClaimableExist.ToList().Count == 0)
                    {
                        var deleteParameters = new
                        {
                            NftData = nftData
                        };
                        result = await db.ExecuteAsync("DELETE from Claimable where nftdata = @NftData", deleteParameters); // > 0 when removed
                    }
                    else
                    {
                        result = -1; // -1 when already removed
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return result; //0 if something goes wrong
        }
    }
}
