using Dapper;
using FroggieBot.Models;
using System.Data.SqlClient;

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
                    if (doesClaimableExist.ToList().Count > 0)
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

        public async Task<int> AddToAllowlist(string address, string nftData, string amount, string connectionString)
        {
            int result = 0;
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesAllowListExistParameters = new { Address = address, NftData = nftData };
                    var doesAllowListExist = await db.QueryAsync<AllowList>($"select * from allowlist where nftdata = @NftData and address = @Address", doesAllowListExistParameters);
                    if (doesAllowListExist.ToList().Count == 0)
                    {
                        var addToAllowListParameters = new
                        {
                            Address = address,
                            NftData = nftData,
                            Amount = amount
                        };
                        result = await db.ExecuteAsync("INSERT INTO AllowList (Address,NftData,Amount) Values (@Address,@NftData,@Amount)", addToAllowListParameters); // > 0 when added
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

        public async Task<int> RemoveFromAllowlist(string address, string nftData, string connectionString)
        {
            int result = 0;
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesAllowListExistParameters = new { Address = address, NftData = nftData };
                    var doesAllowListExist = await db.QueryAsync<AllowList>($"select * from allowlist where nftdata = @NftData and address = @Address", doesAllowListExistParameters);
                    if (doesAllowListExist.ToList().Count > 0)
                    {
                        var removeFromAllowListParameters = new
                        {
                            Address = address,
                            NftData = nftData
                        };
                        result = await db.ExecuteAsync("DELETE From allowlist where address = @Address and nftdata = @NftData", removeFromAllowListParameters); // > 0 when added
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

        public async Task<AllowList?> CheckAllowlist(string address, string nftData, string connectionString)
        {
            AllowList? result = new AllowList();
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesAllowListExistParameters = new { Address = address, NftData = nftData };
                    var doesAllowListExist = await db.QueryAsync<AllowList>($"select * from allowlist where nftdata = @NftData and address = @Address", doesAllowListExistParameters);
                    if (doesAllowListExist.ToList().Count > 0)
                    {
                        result = doesAllowListExist.First();
                    }
                    else
                    {
                        result = null; // -1 when already exists
                    }
                }
            }
            catch (Exception ex)
            {
                result.Address = "Error";
            }
            return result; //0 if something goes wrong
        }

        public async Task<int> RemoveFromClaimed(string address, string nftData, string connectionString)
        {
            int result = 0;
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesClaimExistParameters = new { Address = address, NftData = nftData };
                    var doesClaimExist = await db.QueryAsync<Claimed>($"select * from claimed where nftdata = @NftData and address = @Address", doesClaimExistParameters);
                    if (doesClaimExist.ToList().Count > 0)
                    {
                        var removeFromAllowListParameters = new
                        {
                            Address = address,
                            NftData = nftData
                        };
                        result = await db.ExecuteAsync("DELETE From claimed where address = @Address and nftdata = @NftData", removeFromAllowListParameters); // > 0 when added
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

        public async Task<Claimed?> CheckClaimed(string address, string nftData, string connectionString)
        {
            Claimed? result = new Claimed();
            try
            {
                using (SqlConnection db = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    await db.OpenAsync();
                    var doesClaimExistParameters = new { Address = address, NftData = nftData };
                    var doesClaimExist = await db.QueryAsync<Claimed>($"select * from claimed where nftdata = @NftData and address = @Address", doesClaimExistParameters);
                    if (doesClaimExist.ToList().Count > 0)
                    {
                        result = doesClaimExist.First();
                    }
                    else
                    {
                        result = null; // -1 when already exists
                    }
                }
            }
            catch (Exception ex)
            {
                result.Address = "Error";
            }
            return result; //0 if something goes wrong
        }
    }
}
