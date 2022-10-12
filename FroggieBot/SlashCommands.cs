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

        /*
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
        */

        [SlashCommand("trade", "Show marketplace info on a MetaBoy")]
        public async Task MetaboyCommand(InteractionContext ctx, [Option("id", "The MetaBoy ID to Lookup, example: 420")] string id)
        {
            if (ctx.Channel.Id == 996854318009438393 //market
                || ctx.Channel.Id == 996888645384544436 //admin
                || ctx.Channel.Id == 996854317833261166  //mod
                || ctx.Channel.Id == 996876130739032196  //hackers den
                || ctx.Channel.Id == 997178529328398377 //faq
                || ctx.Channel.Id == 999035222135930920 //meta club
                || ctx.Channel.Id == 933963130197917698 //fudgeys fun house
                )
            {
                int metaboyId;
                bool canBeParsed = Int32.TryParse(id, out metaboyId);
                if (canBeParsed)
                {

                    var ranking = Ranks.rankings.FirstOrDefault(x => x.Id == metaboyId);
                    if (ranking == null)
                    {
                        var builder = new DiscordInteractionResponseBuilder()
                       .WithContent("Not a valid MetaBoy id!")
                       .AsEphemeral(true);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                        return;
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
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

                        if (gamestopNFTOrders != null && gamestopNFTOrders.Count > 0)
                        {
                            var gamestopNFTOrder = gamestopNFTOrders.OrderByDescending(x => x.createdAt).ToList().FirstOrDefault();
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
                    var builder = new DiscordInteractionResponseBuilder()
                      .WithContent("Not a valid MetaBoy id!")
                      .AsEphemeral(true);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                    return;
                }
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                      .WithContent("This command is not enabled in this channel")
                      .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }


        }

        [SlashCommand("show", "Show a Metaboy")]
        public async Task ShowCommand(InteractionContext ctx, [Option("id", "The MetaBoy ID to Lookup, example: 420")] string id)
        {

            if (ctx.Channel.Id == 996875233032155197 //Show and tell
                || ctx.Channel.Id == 996888645384544436 //admin
                || ctx.Channel.Id == 996854317833261166  //mod
                || ctx.Channel.Id == 996876130739032196  //hackers den
                || ctx.Channel.Id == 997178529328398377 //faq
                || ctx.Channel.Id == 999035222135930920 //meta club
                || ctx.Channel.Id == 933963130197917698 //fudgeys fun house
                )
            {
                int metaboyId;
                bool canBeParsed = Int32.TryParse(id, out metaboyId);
                if (canBeParsed)
                {
                    var ranking = Ranks.rankings.FirstOrDefault(x => x.Id == metaboyId);
                    if (ranking == null)
                    {
                        var builder = new DiscordInteractionResponseBuilder()
                       .WithContent("Not a valid MetaBoy id!")
                       .AsEphemeral(true);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                        return;
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
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
                            Title = $"Metaboy #{metaboyId}, Rank {ranking.Rank}",
                            Url = ranking.MarketplaceUrl,
                            ImageUrl = imageUrl,
                            Color = new DiscordColor(embedColour)
                        };
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }
                }
                else
                {
                    var builder = new DiscordInteractionResponseBuilder()
                    .WithContent("Not a valid MetaBoy id!")
                    .AsEphemeral(true);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                    return;
                }
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("This command is not enabled in this channel")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }


        }

        [SlashCommand("honorary", "Show information on Honorary")]
        public async Task AstroCommand(InteractionContext ctx)
        {


            if (ctx.Channel.Id == 996854318009438390 //General 
                || ctx.Channel.Id == 996854318009438393 //Market
                || ctx.Channel.Id == 996888645384544436 //admin
                || ctx.Channel.Id == 996854317833261166  //mod
                || ctx.Channel.Id == 996876130739032196  //hackers den
                || ctx.Channel.Id == 997178529328398377 //faq
                || ctx.Channel.Id == 999035222135930920 //meta club
                || ctx.Channel.Id == 933963130197917698 //fudgeys fun house
                )
            {
                try
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                    //astro
                    var gamestopNFTData = await GamestopService.GetNftData("0xd8ada153c760d4acce89d9e612939ea7cc4f0cfc43707e423eb16476e293ff95", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);

                    //monkey
                    var gamestopNFTData2 = await GamestopService.GetNftData("0x930ff4e66577c22563dc8060e0a48ab4b6f0fcebdffa42a03e8a579c5d6b1503", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders2 = await GamestopService.GetNftOrders(gamestopNFTData2.nftId);

                    //froggi
                    var gamestopNFTData3 = await GamestopService.GetNftData("0x8ed8173e66a07c49391b5aee318777258f6a96eafbb0daaf3f7f884a52a33ba4", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders3 = await GamestopService.GetNftOrders(gamestopNFTData3.nftId);

                    //ordinary adam
                    var gamestopNFTData4 = await GamestopService.GetNftData("0x5573521b417e6757c77099578e791f889f0b7495eaf5b29093b2902bfe6813cf", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders4 = await GamestopService.GetNftOrders(gamestopNFTData4.nftId);

                    //british
                    var gamestopNFTData5 = await GamestopService.GetNftData("0x445671e5df1feac09afb4a92f5b2cb9337dbd94abe2c6b55a0855e03cbc4653b", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders5 = await GamestopService.GetNftOrders(gamestopNFTData5.nftId);

                    //reporter
                    var gamestopNFTData6 = await GamestopService.GetNftData("0xb34b96e2294f7b79b6af3f576758febcee688977054438328fcf3e76e9fb9742", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders6 = await GamestopService.GetNftOrders(gamestopNFTData6.nftId);

                    //rockstar
                    var gamestopNFTData7 = await GamestopService.GetNftData("0xb3ca435a05ac1f67102258a14d1ac4687e4a00ca8fd838d7b0faf8ac994eb839", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders7 = await GamestopService.GetNftOrders(gamestopNFTData7.nftId);

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboyhonorary/full/astroboy.gif";
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Honorary",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/MetaBoyHonorary"
                    };
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };

                    var gamestopNFTOrder = gamestopNFTOrders.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText = "";
                    if (gamestopNFTOrder.buyTokenId == 0)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder.buyTokenId == 1)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("AstroBoy List Price", $"[{salePriceText}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xd8ada153c760d4acce89d9e612939ea7cc4f0cfc43707e423eb16476e293ff95)", true);

                    var gamestopNFTOrder5 = gamestopNFTOrders5.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText5 = "";
                    if (gamestopNFTOrder5.buyTokenId == 0)
                    {
                        salePriceText5 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder5.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder5.buyTokenId == 1)
                    {
                        salePriceText5 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder5.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("BritishBoy List Price", $"[{salePriceText5}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x445671e5df1feac09afb4a92f5b2cb9337dbd94abe2c6b55a0855e03cbc4653b)");

                    var gamestopNFTOrder3 = gamestopNFTOrders3.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText3 = "";
                    if (gamestopNFTOrder3.buyTokenId == 0)
                    {
                        salePriceText3 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder3.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder3.buyTokenId == 1)
                    {
                        salePriceText3 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder3.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("FroggiBoy List Price", $"[{salePriceText3}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x8ed8173e66a07c49391b5aee318777258f6a96eafbb0daaf3f7f884a52a33ba4)");

                    var gamestopNFTOrder2 = gamestopNFTOrders2.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText2 = "";
                    if (gamestopNFTOrder2.buyTokenId == 0)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder2.buyTokenId == 1)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("MonkeyBoy List Price", $"[{salePriceText2}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x930ff4e66577c22563dc8060e0a48ab4b6f0fcebdffa42a03e8a579c5d6b1503)");

                    var gamestopNFTOrder4 = gamestopNFTOrders4.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText4 = "";
                    if (gamestopNFTOrder4.buyTokenId == 0)
                    {
                        salePriceText4 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder4.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder4.buyTokenId == 1)
                    {
                        salePriceText4 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder4.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("OrdinaryAdamBoy List Price", $"[{salePriceText4}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x5573521b417e6757c77099578e791f889f0b7495eaf5b29093b2902bfe6813cf)");

                    var gamestopNFTOrder6 = gamestopNFTOrders6.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText6 = "";
                    if (gamestopNFTOrder6.buyTokenId == 0)
                    {
                        salePriceText6 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder6.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder6.buyTokenId == 1)
                    {
                        salePriceText6 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder6.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("ReporterBoy List Price", $"[{salePriceText6}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xb34b96e2294f7b79b6af3f576758febcee688977054438328fcf3e76e9fb9742)");

                    var gamestopNFTOrder7 = gamestopNFTOrders7.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText7 = "";
                    if (gamestopNFTOrder7.buyTokenId == 0)
                    {
                        salePriceText7 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder7.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder7.buyTokenId == 1)
                    {
                        salePriceText7 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder7.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("RockstarBoy List Price", $"[{salePriceText7}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xb3ca435a05ac1f67102258a14d1ac4687e4a00ca8fd838d7b0faf8ac994eb839)");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                catch(Exception ex)
                {
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Honorary",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/MetaBoyHonorary"
                    };

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboyhonorary/full/astroboy.gif";
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };
                    embed.AddField("Oops!", "Something went wrong!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("This command is not enabled in this channel!")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }

        [SlashCommand("airdrop", "Show information on Airdrop")]
        public async Task AirdropCommand(InteractionContext ctx)
        {


            if (ctx.Channel.Id == 996854318009438390 //General 
                || ctx.Channel.Id == 996854318009438393 //Market
                || ctx.Channel.Id == 996888645384544436 //admin
                || ctx.Channel.Id == 996854317833261166  //mod
                || ctx.Channel.Id == 996876130739032196  //hackers den
                || ctx.Channel.Id == 997178529328398377 //faq
                || ctx.Channel.Id == 999035222135930920 //meta club
                || ctx.Channel.Id == 933963130197917698 //fudgeys fun house
                )
            {
                try
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                    //dalmatian metadog
                    var gamestopNFTData = await GamestopService.GetNftData("0xbbcbb61afe23eeadc4a6ca0d8b8379c45e4f846797bf79cb01a23811b87b38ce", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);

                    //mountain metadog
                    var gamestopNFTData2 = await GamestopService.GetNftData("0x004dec4dc078179e624487c2380394a8874a256ce75f781a6e07da3c209c8235", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders2 = await GamestopService.GetNftOrders(gamestopNFTData2.nftId);

                    //chihuahua metadog
                    var gamestopNFTData3 = await GamestopService.GetNftData("0x3615d66402275f0276cd66961e4ff81a828ff51fbecc5b27fa064868231e94dd", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders3 = await GamestopService.GetNftOrders(gamestopNFTData3.nftId);

                    //bordercollie metadog
                    var gamestopNFTData4 = await GamestopService.GetNftData("0x0076ecba9f6f87b3e97ad987a6aaec1ec40af97a0c1a6daf1b6ffd0a012ee620", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders4 = await GamestopService.GetNftOrders(gamestopNFTData4.nftId);

                    //retriever metadog
                    var gamestopNFTData5 = await GamestopService.GetNftData("0xbf024aa2ebf7c4b137124c46f64a50a2c8e3cf733a7af5aac79f56bc61c6165f", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders5 = await GamestopService.GetNftOrders(gamestopNFTData5.nftId);

                    //bedroom metacat
                    var gamestopNFTData6 = await GamestopService.GetNftData("0x80f1525cb6cea164781a2de003564c323bfcafc6d7dbb5c111a370bae95cda73", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders6 = await GamestopService.GetNftOrders(gamestopNFTData6.nftId);


                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboyairdrop/airdrop.gif";
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Airdrop",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/MetaBoyAirdrop"
                    };
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };

                    var gamestopNFTOrder = gamestopNFTOrders.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText = "";
                    if (gamestopNFTOrder.buyTokenId == 0)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder.buyTokenId == 1)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("DalmatianMetaDog List Price", $"[{salePriceText}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xbbcbb61afe23eeadc4a6ca0d8b8379c45e4f846797bf79cb01a23811b87b38ce)", true);

                    var gamestopNFTOrder2 = gamestopNFTOrders2.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText2 = "";
                    if (gamestopNFTOrder2.buyTokenId == 0)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder2.buyTokenId == 1)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("MountainMetaDog List Price", $"[{salePriceText2}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x004dec4dc078179e624487c2380394a8874a256ce75f781a6e07da3c209c8235)");

                    var gamestopNFTOrder3 = gamestopNFTOrders3.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText3 = "";
                    if (gamestopNFTOrder3.buyTokenId == 0)
                    {
                        salePriceText3 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder3.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder3.buyTokenId == 1)
                    {
                        salePriceText3 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder3.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("ChihuahuaMetaDog List Price", $"[{salePriceText3}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x3615d66402275f0276cd66961e4ff81a828ff51fbecc5b27fa064868231e94dd)");

                    var gamestopNFTOrder4 = gamestopNFTOrders4.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText4 = "";
                    if (gamestopNFTOrder4.buyTokenId == 0)
                    {
                        salePriceText4 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder4.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder4.buyTokenId == 1)
                    {
                        salePriceText4 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder4.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("BorderCollieMetaDog List Price", $"[{salePriceText4}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x0076ecba9f6f87b3e97ad987a6aaec1ec40af97a0c1a6daf1b6ffd0a012ee620)");


                    var gamestopNFTOrder5 = gamestopNFTOrders5.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText5 = "";
                    if (gamestopNFTOrder5.buyTokenId == 0)
                    {
                        salePriceText5 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder5.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder5.buyTokenId == 1)
                    {
                        salePriceText5 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder5.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("RetrieverMetaDog List Price", $"[{salePriceText5}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xbf024aa2ebf7c4b137124c46f64a50a2c8e3cf733a7af5aac79f56bc61c6165f)");

                    var gamestopNFTOrder6 = gamestopNFTOrders6.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText6 = "";
                    if (gamestopNFTOrder6.buyTokenId == 0)
                    {
                        salePriceText6 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder6.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder6.buyTokenId == 1)
                    {
                        salePriceText6 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder6.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("BedroomMetaCat List Price", $"[{salePriceText6}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x80f1525cb6cea164781a2de003564c323bfcafc6d7dbb5c111a370bae95cda73)");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                catch (Exception ex)
                {
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Airdrop",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/MetaBoyAirdrop"
                    };

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboyhairdrop/airdrop.gif";
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };
                    embed.AddField("Oops!", "Something went wrong!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("This command is not enabled in this channel!")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }

        [SlashCommand("Celebratory", "Show information on Celebratory")]
        public async Task CelebratoryCommand(InteractionContext ctx)
        {


            if (ctx.Channel.Id == 996854318009438390 //General 
                || ctx.Channel.Id == 996854318009438393 //Market
                || ctx.Channel.Id == 996888645384544436 //admin
                || ctx.Channel.Id == 996854317833261166  //mod
                || ctx.Channel.Id == 996876130739032196  //hackers den
                || ctx.Channel.Id == 997178529328398377 //faq
                || ctx.Channel.Id == 999035222135930920 //meta club
                || ctx.Channel.Id == 933963130197917698 //fudgeys fun house
                )
            {
                try
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                    //astro
                    var gamestopNFTData = await GamestopService.GetNftData("0x2a669f944bb80efdcdd1c86ad1fc340a4803210dce371d03d00f450a33ec11c6", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);
                    var gamestopNFTData2 = await GamestopService.GetNftData("0xd79e1ff7615e2826b2e4e29bbfd6cfa1d4109da4cbabf726b4690e1c9d1b411e", "0x1d006a27bd82e10f9194d30158d91201e9930420");
                    var gamestopNFTOrders2 = await GamestopService.GetNftOrders(gamestopNFTData2.nftId);

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboycelebratory/ethboy.gif";
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Celebratory",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/celebratorymetaboy"
                    };
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };

                    var gamestopNFTOrder = gamestopNFTOrders.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText = "";
                    if (gamestopNFTOrder.buyTokenId == 0)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder.buyTokenId == 1)
                    {
                        salePriceText = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("EthBoy List Price", $"[{salePriceText}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0x2a669f944bb80efdcdd1c86ad1fc340a4803210dce371d03d00f450a33ec11c6)");

                    var gamestopNFTOrder2 = gamestopNFTOrders2.OrderBy(x => Double.Parse(x.pricePerNft)).ToList()[0];
                    var salePriceText2 = "";
                    if (gamestopNFTOrder2.buyTokenId == 0)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} ETH";
                    }
                    else if (gamestopNFTOrder2.buyTokenId == 1)
                    {
                        salePriceText2 = $"{TokenAmountConverter.ToString(Double.Parse(gamestopNFTOrder2.pricePerNft), 18)} LRC";
                    }
                    embed.AddField("FighterBoy List Price", $"[{salePriceText2}](https://nft.gamestop.com/token/0x1d006a27bd82e10f9194d30158d91201e9930420/0xd79e1ff7615e2826b2e4e29bbfd6cfa1d4109da4cbabf726b4690e1c9d1b411e)");


                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                catch (Exception ex)
                {
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"Celebratory",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/celebratorymetaboy"
                    };

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboycelebratory/ethboy.gif";
                    embed.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = imageUrl, Height = 256, Width = 256 };
                    embed.AddField("Oops!", "Something went wrong!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("This command is not enabled in this channel!")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
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

