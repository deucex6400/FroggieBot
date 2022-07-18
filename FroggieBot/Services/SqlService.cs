using Dapper;
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
        public async Task<List<NftGiveaway>> GetNftGiveaway(string channelId, string connectionString)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Open();
                var result = await db
                    .QueryAsync<NftGiveaway>
                    ($"select * from NftGiveaway where ChannelId = '{channelId}'");
                return result.ToList();
            }
        }

        public async Task<List<NftRedemption>> GetNftRedemption(string channelId, string userId, string nftTokenId, string nftData,string connectionString)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Open();
                var result = await db
                    .QueryAsync<NftRedemption>
                    ($"select * from NftRedemption where ChannelId = '{channelId}' and UserId = '{userId}' and NftTokenId = '{nftTokenId}' and NftData = '{nftData}' ");
                return result.ToList();
            }
        }

        public async Task AddNftRedemption(string channelId, string userId, string nftTokenId, string nftData, string connectionString)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                db.Open();
                var insertParameters = new
                {
                    UserId = userId,
                    ChannelId = channelId,
                    NftTokenId = nftTokenId,
                    NftData = nftData
                };

                await db.ExecuteAsync("INSERT INTO NftRedemption VALUES (@UserId, @ChannelId, @NftTokenId, @NftData)", insertParameters);
            }
        }


    }
}
