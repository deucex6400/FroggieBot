# FroggieBot
This is FroggieBot, a discord bot that was originally created for the Froggie Pond discord server! 

FroggieBot has a number of commands relevant to the Froggie Pond discord, including a Loopring NFT giveaway command which can be invoked with the /giveaway slash command. This command will automatically transfer an NFT from a loopring account the bot is associated with to the loopring address/ens specified in the /giveaway command.

# Setup
This is a .NET 6 Console App made with Visual Studio 2022. It uses DSharpPlus to interact with Discord.

You need to create a Discord Bot through the Discord Developer Portal with the following permissions and invite it to your Discord.

![discord](https://user-images.githubusercontent.com/5258063/174226244-5e9b4298-e569-4b6f-be02-e9fb07b961a6.png)

Create an appsettings.json file in the solution directory like below with the "Copy to Output Directory" option set to "Copy always". As always keep your private keys and etc safe and never share them.

```json
{
  "Settings": {
    "LoopringApiKey": "kdlT", //Your loopring api key
    "LoopringPrivateKey": "0x176", //Your loopring private key
    "LoopringAddress": "0x36Cd6b3b9329c04df55d55D41C257a5fdD387ACd", //Your loopring address
    "LoopringAccountId": 40940, //Your loopring account id
    "LoopringTokenAddress": "0xd988", //Your counterfactual nft token address
    "Exchange": "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4", //Loopring Exchange address
    "MetamaskPrivateKey": "aad10", //Metamask private key
    "SqlServerConnectionString": "Server=tcp:blahblah.database.windows.net,1433;Initial Catalog=looprare;Persist Security Info=False;User ID=blah;Password=blah;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;", //SqlServer Connection String
    "EtherScanApiKey": "4DT", //EtherScan Api Key
    "DiscordServerId": 933963129652674671, //Discord Server Id
    "DiscordToken": "OTg3Mj" //Discord Token
  }
}
```
The Loopring related settings and the metamask private key are needed to use the /mintfee and /giveaway commands. So be sure to use the account that will hold the giveaway NFTs. It needs to be a metamask private key as you can not export out the private key from the Loopring Mobile Wallet.

The Sql Server Connection String should point to a Sql Server database. Setup a Sql Server database in Azure or locally with the following tables and columns. Make the columns all varchar(100) in length.

![nft giveaway](https://user-images.githubusercontent.com/5258063/174227346-8a06117c-49e4-41c1-9e86-bffc62b7bdb1.png)

![nft redemption](https://user-images.githubusercontent.com/5258063/174227363-da05986a-6514-4950-b9d6-8f8b232c4ad4.png)

The Nft giveaway table holds the information for the nft giveaway. You can add a new record like below, this must be done manually by you with each new nft giveaway you want to add

```sql
Insert into nftgiveaway
(NftTokenId,
NftData,
ChannelId)
VALUES
(
'34065',
'0x17bfa3e452e01588c51bcdb128e87cde726d04373622a6b044e4760e0ccf8b84',
'987173337019072542'
);
```

In FroggieBot's current state only one NFT can be given away per channel/thread. So if you need to update the channel/thread with a new NFT just update it's current record in the database like below

```sql
update nftgiveaway set nfttokenid = '34064', nftdata = '0x260e9461223687755f583ddec5d69be3d9f2de07ba8ef71dd642ee5808b8e4c0' where channelid = '987173337019072542';
```

NftTokenId and NftData can be found using the following Loopring API Endpoint: https://api3.loopring.io/api/v3/user/nft/balances?accountId=40940&limit=50&metadata=true&offset=0 where accountId holds the nfts to giveaway. ChannelId refers to the discord channel that the giveaway will run in. In the Froggie Pond discord we have a different NFT given away in the channel/thread it is setup in.

The Nft redemption table should automatically update per Nft redemption from a user and ensures that users can only redeem an nft once per giveaway in the specific channel/thread.

The EtherScanApiKey is needed for the /gas command to show the gas.

Remember to plug in your own DiscordServerId and DiscordToken.

# Deploy 
You can either run the bot locally at home on your PC,deploy it as a continous web job on Azure, or your preferred cloud hosting provider.

# Notes
FroggieBot will get rate limited by the Loopring API if many users use the /giveaway command at once.

Also, all fees are paid in LRC so make sure the account giving away the NFTs has enough LRC!

