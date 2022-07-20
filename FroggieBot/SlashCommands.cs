using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using FroggieBot.Models;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using OpenAI_API;
using PoseidonSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FroggieBot
{
    public class SlashCommands : ApplicationCommandModule
    {
        public LoopringService LoopringService { private get; set; }
        public Random Random { private get; set; }

        public SqlService SqlService { private get; set; }

        public Settings Settings { private get; set; }

        public EthereumService EthereumService { private get; set; }

        public EtherscanService EtherscanService { private get; set; }

        public GamestopService GamestopService { private get; set; }

        public static Ranks Ranks { private get; set; }

        [SlashCommand("mintfee", "Get current Loopring NFT Mint fee")]
        public async Task NftMintFeeCommand(InteractionContext ctx, [Option("amount", "mint amount")] string mintAmount = "1")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            int parsedInteger;
            if (int.TryParse(mintAmount, out parsedInteger))
            {
                OffchainFee offChainFee = await LoopringService.GetOffChainFee(
                apiKey: Settings.LoopringApiKey,
                accountId: Settings.LoopringAccountId,
                requestType: 9,
                tokenAddress: Settings.LoopringTokenAddress);
                var lrcFee = offChainFee.fees[1];
                double feeAmount = parsedInteger * Double.Parse(lrcFee.fee);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"It will cost {TokenAmountConverter.ToString(feeAmount, 18)} LRC to mint {mintAmount} NFTs"));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Not a valid mint amount!"));
            }

            if (mintAmount == "1")
            {
                OffchainFee offChainFee = await LoopringService.GetOffChainFee(
                apiKey: Settings.LoopringApiKey,
                accountId: Settings.LoopringAccountId,
                requestType: 9,
                tokenAddress: Settings.LoopringTokenAddress);
                var lrcFee = offChainFee.fees[1];

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"It will cost {TokenAmountConverter.ToString(Double.Parse(lrcFee.fee), 18)} LRC to mint 1 NFT"));
            }
        }

        [SlashCommand("gas", "Get current gas fee")]
        public async Task GasCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var gasPrices = await EtherscanService.GetGas(Settings.EtherScanApiKey);

            var embed = new DiscordEmbedBuilder()
            {
                Title = $"⛽ Current Gas Prices"
            };
            embed.AddField("Slow 🐢 | 10 minutes", gasPrices.result.SafeGasPrice + " Gwei");
            embed.AddField("Average 🚶 | 3 minutes", gasPrices.result.ProposeGasPrice + " Gwei");
            embed.AddField("Fast ⚡ | 15 seconds", gasPrices.result.FastGasPrice + " Gwei");
            embed.WithFooter("Fetched from etherscan.");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("ens", "Find ENSes for an address")]
        public async Task EnsCommand(InteractionContext ctx, [Option("address", "The address in 0x Format")] string address)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            try
            {
                string ens = "";
                var resultOne = await LoopringService.GetENS(Settings.LoopringApiKey, address.Trim().ToLower());
                if (!string.IsNullOrEmpty(resultOne.data))
                {
                    ens += resultOne.data + ";";

                }

                var resultTwo = await EthereumService.GetEnsFromAddessAsync(address.Trim().ToLower());

                if (!string.IsNullOrEmpty(resultTwo))
                {
                    ens += resultTwo + ";";
                }

                if (!string.IsNullOrEmpty(ens))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{address} resolves to the following ENSes: {ens} "));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not find ENSes for {address}"));
                }

            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong with searching for the ENS! Try again later."));
            }
        }

        [SlashCommand("id", "Show information on a MetaBoy")]
        public async Task MetaboyCommand(InteractionContext ctx, [Option("id", "The MetaBoy ID to Lookup, example: 420")] string id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            int metaboyId;
            bool canBeParsed = Int32.TryParse(id, out metaboyId);
            if (canBeParsed)
            {

                var ranking = Ranks.rankings.FirstOrDefault(x => x.Id == metaboyId);
                if (ranking == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Not a valid MetaBoy id!"));
                    return;
                }
                else
                {
                    string contractAddress = ranking.MarketplaceUrl.Replace("https://nft.gamestop.com/token/", "").Split('/')[0];
                    string tokenId = ranking.MarketplaceUrl.Replace("https://nft.gamestop.com/token/", "").Split('/')[1];

                    var gamestopNFTData = await GamestopService.GetNftData(tokenId, contractAddress);
                    var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);

                    var rarityTier = RarityTierConverter.RarityTier(ranking.Rank, 8661);

                    var embedColour = "";

                    switch (rarityTier)
                    {
                        case "Common":
                            embedColour = "#FFFFFF"; //white
                            break;
                        case "Uncommon":
                            embedColour = "#1EFF00"; //green
                            break;
                        case "Rare":
                            embedColour = "#0070DD"; //blue
                            break;
                        case "Epic":
                            embedColour = "#A335EE"; //purple
                            break;
                        case "Legendary":
                            embedColour = "#FF8000"; //orange
                            break;
                        case "Mythical":
                            embedColour = "#E6CC80"; //light gold
                            break;
                        case "Transcendent":
                            embedColour = "#00CCFF"; //cyan
                            break;
                        case "Godlike":
                            embedColour = "#FD0000"; //gme red
                            break;
                    }

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboys/full/{metaboyId}.gif";
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Metaboy #{metaboyId}",
                        Url = ranking.MarketplaceUrl,
                        Color = new DiscordColor(embedColour)
                    };
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };
                    embed.AddField("Rank", ranking.Rank.ToString());
                    embed.AddField("Rarity Score", ranking.RarityScore.ToString());

                    if (gamestopNFTOrders != null && gamestopNFTOrders.Count > 0)
                    {
                        var gamestopNFTOrder = gamestopNFTOrders[0];
                        var salePriceText = "";
                        if (gamestopNFTOrder.buyTokenId == 0)
                        {
                            salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} ETH";
                        }
                        else if (gamestopNFTOrder.buyTokenId == 1)
                        {
                            salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} LRC";
                        }
                        embed.AddField("List Price", salePriceText);
                    }
                    else if (gamestopNFTOrders != null && gamestopNFTOrders.Count == 0)
                    {
                        embed.AddField("List Price", "Not Listed!");
                    }

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;

                }
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Not a valid MetaBoy id!"));
                return;
            }
        }

        [SlashCommand("astro", "Show information on AstroBoy")]
        public async Task MetaboyCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var gamestopNFTData = await GamestopService.GetNftData("0xd8ada153c760d4acce89d9e612939ea7cc4f0cfc43707e423eb16476e293ff95", "0x1d006a27bd82e10f9194d30158d91201e9930420");
            var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);
            var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboyhonorary/full/astroboy.gif";
            var embed = new DiscordEmbedBuilder()
            {
                Title = $"AstroBoy",
                Color = new DiscordColor("#FD0000"),
                Url = "https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xd8ada153c760d4acce89d9e612939ea7cc4f0cfc43707e423eb16476e293ff95"
            };
            embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };
            if (gamestopNFTOrders != null && gamestopNFTOrders.Count > 0)
            {
                var gamestopNFTOrder = gamestopNFTOrders.OrderBy(x=> Double.Parse(x.pricePerNft)).ToList()[0];
                var salePriceText = "";
                if (gamestopNFTOrder.buyTokenId == 0)
                {
                    salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} ETH";
                }
                else if (gamestopNFTOrder.buyTokenId == 1)
                {
                    salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} LRC";
                }
                embed.AddField("List Price", salePriceText);
            }
            else if (gamestopNFTOrders != null && gamestopNFTOrders.Count == 0)
            {
                embed.AddField("List Price", "Not Listed!");
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            return;
        }

    }

    /*
    [SlashCommand("giveaway", "Get a free nft!")]
    public async Task GiveawayCommand(InteractionContext ctx, [Option("address", "Loopring Layer 2 address")] string address = "0x")
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        try
        {
            if (address.ToLower().Trim().Contains(".eth"))
            {
                var varHexAddress = await LoopringService.GetHexAddress(Settings.LoopringApiKey, address.ToLower().Trim());
                if (!String.IsNullOrEmpty(varHexAddress.data))
                {
                    address = varHexAddress.data;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Not a valid ENS!"));
                    return;
                }
            }

            if (address.Trim() == "0x")
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Please enter an address!"));
                return;
            }
            else if (address.Trim().Length != 42 || !address.Trim().StartsWith("0x"))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Address should be 42 characters long and should start with 0x, or be an ENS address!"));
                return;
            }
            else
            {
                string loopringApiKey = Settings.LoopringApiKey;//you can either set an environmental variable or input it here directly. You can export this from your account using loopring.io
                string loopringPrivateKey = Settings.LoopringPrivateKey; //you can either set an environmental variable or input it here directly. You can export this from your account using loopring.io
                var fromAddress = Settings.LoopringAddress; //your loopring address
                var fromAccountId = Settings.LoopringAccountId; //your loopring account id
                var validUntil = 1700000000; //the examples seem to use this number
                var maxFeeTokenId = 1; //0 should be for ETH, 1 is for LRC?
                var exchange = Settings.Exchange; //loopring exchange address, shouldn't need to change this,
                var metamaskPrivateKey = Settings.MetamaskPrivateKey;
                var sqlServerConnectionString = Settings.SqlServerConnectionString;

                string nftAmount = "1"; //only send 1 nft per call
                int nftTokenId = 0; 
                string nftData = ""; 
                int toAccountId = 0; //leave as 0
                string toAddress = address.Trim();

                var nftGiveawayList = await SqlService.GetNftGiveaway(ctx.Channel.Id.ToString(), sqlServerConnectionString);

                if (nftGiveawayList.Count > 0)
                {
                    nftTokenId = Int32.Parse(nftGiveawayList[0].NftTokenId);
                    nftData = nftGiveawayList[0].NftData;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, there are no giveaways in this channel!"));
                    return;
                }

                var nftRedemption = await SqlService.GetNftRedemption(ctx.Channel.Id.ToString(), ctx.User.Id.ToString(), nftGiveawayList[0].NftTokenId, nftGiveawayList[0].NftData, sqlServerConnectionString);
                if (nftRedemption.Count > 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, you've already redeemed an NFT from the current giveaway!"));
                    return;
                }

                //Storage id
                var storageId = await LoopringService.GetNextStorageId(loopringApiKey, fromAccountId, nftTokenId);
                if (storageId == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Issue getting storage id!"));
                    return;
                }

                //Getting the offchain fee
                var offChainFee = await LoopringService.GetOffChainFeeNftTransfer(loopringApiKey, fromAccountId, 11, "0");
                if (offChainFee == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Issue getting offchain fee"));
                    return;
                }

                //Calculate eddsa signautre
                BigInteger[] poseidonInputs =
                {
            Utils.ParseHexUnsigned(exchange),
            (BigInteger) fromAccountId,
            (BigInteger) toAccountId,
            (BigInteger) nftTokenId,
            BigInteger.Parse(nftAmount),
            (BigInteger) maxFeeTokenId,
            BigInteger.Parse(offChainFee.fees[maxFeeTokenId].fee),
            Utils.ParseHexUnsigned(toAddress),
            (BigInteger) 0,
            (BigInteger) 0,
            (BigInteger) validUntil,
            (BigInteger) storageId.offchainId
};
                Poseidon poseidon = new Poseidon(13, 6, 53, "poseidon", 5, _securityTarget: 128);
                BigInteger poseidonHash = poseidon.CalculatePoseidonHash(poseidonInputs);
                Eddsa eddsa = new Eddsa(poseidonHash, loopringPrivateKey);
                string eddsaSignature = eddsa.Sign();

                //Calculate ecdsa
                string primaryTypeName = "Transfer";
                TypedData eip712TypedData = new TypedData();
                eip712TypedData.Domain = new Domain()
                {
                    Name = "Loopring Protocol",
                    Version = "3.6.0",
                    ChainId = 1,
                    VerifyingContract = "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4",
                };
                eip712TypedData.PrimaryType = primaryTypeName;
                eip712TypedData.Types = new Dictionary<string, MemberDescription[]>()
                {
                    ["EIP712Domain"] = new[]
                        {
                    new MemberDescription {Name = "name", Type = "string"},
                    new MemberDescription {Name = "version", Type = "string"},
                    new MemberDescription {Name = "chainId", Type = "uint256"},
                    new MemberDescription {Name = "verifyingContract", Type = "address"},
                },
                    [primaryTypeName] = new[]
                        {
                    new MemberDescription {Name = "from", Type = "address"},            // payerAddr
                    new MemberDescription {Name = "to", Type = "address"},              // payeeAddr
                    new MemberDescription {Name = "tokenID", Type = "uint16"},          // token.tokenId 
                    new MemberDescription {Name = "amount", Type = "uint96"},           // token.volume 
                    new MemberDescription {Name = "feeTokenID", Type = "uint16"},       // maxFee.tokenId
                    new MemberDescription {Name = "maxFee", Type = "uint96"},           // maxFee.volume
                    new MemberDescription {Name = "validUntil", Type = "uint32"},       // validUntill
                    new MemberDescription {Name = "storageID", Type = "uint32"}         // storageId
                },

                };
                eip712TypedData.Message = new[]
                {
            new MemberValue {TypeName = "address", Value = fromAddress},
            new MemberValue {TypeName = "address", Value = toAddress},
            new MemberValue {TypeName = "uint16", Value = nftTokenId},
            new MemberValue {TypeName = "uint96", Value = BigInteger.Parse(nftAmount)},
            new MemberValue {TypeName = "uint16", Value = maxFeeTokenId},
            new MemberValue {TypeName = "uint96", Value = BigInteger.Parse(offChainFee.fees[maxFeeTokenId].fee)},
            new MemberValue {TypeName = "uint32", Value = validUntil},
            new MemberValue {TypeName = "uint32", Value = storageId.offchainId},
        };

                TransferTypedData typedData = new TransferTypedData()
                {
                    domain = new TransferTypedData.Domain()
                    {
                        name = "Loopring Protocol",
                        version = "3.6.0",
                        chainId = 1,
                        verifyingContract = "0x0BABA1Ad5bE3a5C0a66E7ac838a129Bf948f1eA4",
                    },
                    message = new TransferTypedData.Message()
                    {
                        from = fromAddress,
                        to = toAddress,
                        tokenID = nftTokenId,
                        amount = nftAmount,
                        feeTokenID = maxFeeTokenId,
                        maxFee = offChainFee.fees[maxFeeTokenId].fee,
                        validUntil = (int)validUntil,
                        storageID = storageId.offchainId
                    },
                    primaryType = primaryTypeName,
                    types = new TransferTypedData.Types()
                    {
                        EIP712Domain = new List<Type>()
                {
                    new Type(){ name = "name", type = "string"},
                    new Type(){ name="version", type = "string"},
                    new Type(){ name="chainId", type = "uint256"},
                    new Type(){ name="verifyingContract", type = "address"},
                },
                        Transfer = new List<Type>()
                {
                    new Type(){ name = "from", type = "address"},
                    new Type(){ name = "to", type = "address"},
                    new Type(){ name = "tokenID", type = "uint16"},
                    new Type(){ name = "amount", type = "uint96"},
                    new Type(){ name = "feeTokenID", type = "uint16"},
                    new Type(){ name = "maxFee", type = "uint96"},
                    new Type(){ name = "validUntil", type = "uint32"},
                    new Type(){ name = "storageID", type = "uint32"},
                }
                    }
                };

                Eip712TypedDataSigner signer = new Eip712TypedDataSigner();
                var ethECKey = new Nethereum.Signer.EthECKey(metamaskPrivateKey.Replace("0x", ""));
                var encodedTypedData = signer.EncodeTypedData(eip712TypedData);
                var ECDRSASignature = ethECKey.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedTypedData));
                var serializedECDRSASignature = EthECDSASignature.CreateStringSignature(ECDRSASignature);
                var ecdsaSignature = serializedECDRSASignature + "0" + (int)2;

                //Submint nft transfer
                var nftTransferResponse = await LoopringService.SubmitNftTransfer(
                    apiKey: loopringApiKey,
                    exchange: exchange,
                    fromAccountId: fromAccountId,
                    fromAddress: fromAddress,
                    toAccountId: toAccountId,
                    toAddress: toAddress,
                    nftTokenId: nftTokenId,
                    nftAmount: nftAmount,
                    maxFeeTokenId: maxFeeTokenId,
                    maxFeeAmount: offChainFee.fees[maxFeeTokenId].fee,
                    storageId.offchainId,
                    validUntil: validUntil,
                    eddsaSignature: eddsaSignature,
                    ecdsaSignature: ecdsaSignature,
                    nftData: nftData
                    );

                if (nftTransferResponse != null)
                {
                    if (nftTransferResponse.Contains("processing"))
                    {
                        await SqlService.AddNftRedemption(ctx.Channel.Id.ToString(), ctx.User.Id.ToString(), nftGiveawayList[0].NftTokenId, nftGiveawayList[0].NftData, sqlServerConnectionString);
                        var nftBalance = await LoopringService.GetNftBalance(loopringApiKey, fromAccountId, nftData);
                        if(nftBalance.totalNum > 0 )
                        {
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Giveaway NFT claimed! There are {nftBalance.data[0].total} NFTs left to claim in this giveaway."));
                        }
                        else
                        {
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Giveaway NFT claimed! There are 0 NFTs left to claim in this giveaway."));
                        }
                    }
                    else if (nftTransferResponse.Contains("not enough"))
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, I've run out of giveaway NFTs!"));

                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not claim giveaway NFT! Try again later..."));
                    }
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not claim giveaway NFT! Try again later..."));
                }
            }
        }
        catch (SqlException sex)
        {
            Console.WriteLine($"Sql Exception: {sex.Message}");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not claim giveaway NFT! Try again later..."));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: { ex.Message}");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Could not claim giveaway NFT! Try again later..."));
        }
    }
    */

}

