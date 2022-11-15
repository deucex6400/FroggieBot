using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using FroggieBot.Models;
using FroggieBot.Services;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Newtonsoft.Json;
using OpenAI_API;
using PoseidonSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
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

        public MetaBoyApiService MetaBoyApiService { private get; set;}

        public static Ranks Ranks { private get; set; }


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

        [SlashCommand("show", "Show a MetaBoy")]
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

        [SlashCommand("show_transparent", "Show a transparent MetaBoy")]
        public async Task ShowTransparentCommand(InteractionContext ctx, [Option("id", "The MetaBoy ID to Lookup, example: 420")] string id)
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
                        var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboy/transparent/{metaboyId}_cropped.gif";
                        var thumbnailUrl = $"https://looprarecdn.azureedge.net/images/metaboy/transparent/{metaboyId}_tiny.gif";
                        var embed = new DiscordEmbedBuilder()
                        {
                            Title = $"Metaboy #{metaboyId}, Rank {ranking.Rank}",
                            Url = ranking.MarketplaceUrl,
                            ImageUrl = imageUrl,
                            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail() { Url = thumbnailUrl },
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

        [SlashCommand("gmerica", "Show information on GMErica")]
        public async Task GMEricaCommand(InteractionContext ctx)
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

                    //boston
                    var gamestopNFTData = await GamestopService.GetNftData("0x8c34436aa426f27077a6c9df5c314bd6013c85f3201d6ec3520a6cc0a706e271", "0x0c589fcd20f99a4a1fe031f50079cfc630015184");
                    var gamestopNFTOrders = await GamestopService.GetNftOrders(gamestopNFTData.nftId);

                    //canaveral
                    var gamestopNFTData2 = await GamestopService.GetNftData("0x5fa20ebf94a504c98b09b4c9e14ad96644effc7e05c8009e7f1eacfe4d194ed2", "0x0c589fcd20f99a4a1fe031f50079cfc630015184");
                    var gamestopNFTOrders2 = await GamestopService.GetNftOrders(gamestopNFTData2.nftId);

                    //hollywood
                    var gamestopNFTData3 = await GamestopService.GetNftData("0xe83cbd0ce56c8d986b31075e3bcd9f5974b76d06d48c877a4e5faaac70264636", "0x0c589fcd20f99a4a1fe031f50079cfc630015184");
                    var gamestopNFTOrders3 = await GamestopService.GetNftOrders(gamestopNFTData3.nftId);

                    //nyc
                    var gamestopNFTData4 = await GamestopService.GetNftData("0x8d3e53420e7f15a1ac5b54aed3eaa429b5e75046abb1af99d5b5040ed1beea0a", "0x0c589fcd20f99a4a1fe031f50079cfc630015184");
                    var gamestopNFTOrders4 = await GamestopService.GetNftOrders(gamestopNFTData4.nftId);

                    //seattle
                    var gamestopNFTData5 = await GamestopService.GetNftData("0x9bf31ca7985ac20239c026ae15b4c9241aaf06cab6365d92f98c99b34b409d60", "0x0c589fcd20f99a4a1fe031f50079cfc630015184");
                    var gamestopNFTOrders5 = await GamestopService.GetNftOrders(gamestopNFTData5.nftId);


                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboygmerica/gmerica.gif";
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"GMErica",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/Gmricaxmetaboy"
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
                    embed.AddField("Boston List Price", $"[{salePriceText}](https://nft.gamestop.com/token/0x0c589fcd20f99a4a1fe031f50079cfc630015184/0x8c34436aa426f27077a6c9df5c314bd6013c85f3201d6ec3520a6cc0a706e271)", true);

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
                    embed.AddField("Cape Canaveral List Price", $"[{salePriceText2}](https://nft.gamestop.com/token/0x0c589fcd20f99a4a1fe031f50079cfc630015184/0x5fa20ebf94a504c98b09b4c9e14ad96644effc7e05c8009e7f1eacfe4d194ed2)");

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
                    embed.AddField("Hollywood List Price", $"[{salePriceText3}](https://nft.gamestop.com/token/0x0c589fcd20f99a4a1fe031f50079cfc630015184/0xe83cbd0ce56c8d986b31075e3bcd9f5974b76d06d48c877a4e5faaac70264636)");

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
                    embed.AddField("NYC List Price", $"[{salePriceText4}](https://nft.gamestop.com/token/0x0c589fcd20f99a4a1fe031f50079cfc630015184/0x8d3e53420e7f15a1ac5b54aed3eaa429b5e75046abb1af99d5b5040ed1beea0a)");


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
                    embed.AddField("Seattle List Price", $"[{salePriceText5}](https://nft.gamestop.com/token/0x0c589fcd20f99a4a1fe031f50079cfc630015184/0x9bf31ca7985ac20239c026ae15b4c9241aaf06cab6365d92f98c99b34b409d60)");

  
  
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                catch (Exception ex)
                {
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = $"GMErica",
                        Color = new DiscordColor("#FD0000"),
                        Url = "https://nft.gamestop.com/collection/Gmricaxmetaboy"
                    };

                    var imageUrl = $"https://looprarecdn.azureedge.net/images/metaboygmerica/gmerica.gif";
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

        [SlashCommand("claim", "Claim NFTs(if eligible)")]
        public async Task ClaimCommand(InteractionContext ctx, [Option("address", "The address in Hex Format for the claim, Example: 0x36Cd6b3b9329c04df55d55D41C257a5fdD387ACd")] string address)
        {
            string ethAddressRegexPattern = @"0x[a-fA-F0-9]{40}";
            address = address.Trim();
            
            var hexAddress = "";
            foreach (Match m in Regex.Matches(address, ethAddressRegexPattern))
            {
                hexAddress = m.Value.ToLower();
                break;
            }

            if (
                 (ctx.Channel.Id == 933963130197917698 && !string.IsNullOrEmpty(hexAddress)) //fudgeys fun house 
                 ||
                 (ctx.Channel.Id == 1036838681048264735 && !string.IsNullOrEmpty(hexAddress)) //metaboy gaias metalab
                )
            {
                  
                    List<NftReciever> nftRecievers = new List<NftReciever>();
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                    
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Hold on let me check and see if there's any claims in the MetaLab waiting for you, I heard we just got in some fresh Experiments!"));

                     var claimable = await MetaBoyApiService.GetClaimable();
                    foreach(var claim in claimable)
                    {
                        var redeemable = await MetaBoyApiService.GetRedeemable(address, claim.nftData);
                        int redeemableAmount;
                        if (redeemable != null)
                        {
                            if (int.TryParse(JsonConvert.DeserializeObject<string>(redeemable), out redeemableAmount))
                            {
                                if (redeemableAmount > 0)
                                {
                                    nftRecievers.Add(new NftReciever() { Address = address, NftData = claim.nftData });
                                }
                            }
                        }
                    }

                    if(nftRecievers.Count > 0)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"There's a claim here with your name on it! I made this one myself so I really hope you love it, Thank you for coming to my MetaLab!"));
                        foreach(var nftReciever in nftRecievers)
                        {
                            await MetaBoyApiService.AddClaim(nftReciever);
                        }
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I just hand delivered your claim to the teleporter and it should be there as soon as possible! Please check your hidden tab and if there's any problems please follow our <#1005560078813896824> instructions and I'll be with you as soon as I can."));
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I'm sorry I can't seem to find a delivery here with your name on it, check back soon though! Take a look at our <#996854317833261174> for the most up to date info! That's where we keep the most valuable information."));
                        return;
                    }
            }
            else if (     
                    (ctx.Channel.Id == 933963130197917698 && string.IsNullOrEmpty(hexAddress)) //fudgeys fun house
                    ||
                    (ctx.Channel.Id == 1036838681048264735 && string.IsNullOrEmpty(hexAddress)) //metaboy gais metalab
                    )
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("Woah woah woah it's like you're speaking another language! My machines can't read that, please type it in Hex Format : Example: 0x36cd6b3b9329c04df55d55d41c257a5fdd387acd")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("UNKNOWN COMMAND. For all claims please visit Gaia's MetaLab and Experiments. <#1036838681048264735>")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }

        [SlashCommand("claimable_show", "Show claimable NFTs")]
        public async Task ShowClaimableCommand(InteractionContext ctx)
        {

            if (
                 (ctx.Channel.Id == 933963130197917698) //fudgeys fun house 
                 ||
                 (ctx.Channel.Id == 1036838681048264735) //metaboy gaias metalab
                )
            {

                List<Claimable> claimableList = new List<Claimable>();
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Hold on checking the claimable NFTs..."));

                var claimables = await SqlService.GetClaimable(Settings.SqlServerConnectionString);
                foreach(var claimable in claimables)
                {
                    claimableList.Add(claimable);
                }

                if (claimableList.Count > 0)
                {
                    string text = "Here are the claimable NFTs I found:\n";
                    foreach(var claimable in claimableList)
                    {
                        text += $"NFT Name: {claimable.nftName}, NFT Data: {claimable.nftData}\n";
                    }
                   await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{text}"));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I'm sorry I can't see to find any claimable NFTs...You can add claimable nfts with the slash command: claimable_add"));
                    return;
                }
            }
            /*
            else if (
                    (ctx.Channel.Id == 933963130197917698 && string.IsNullOrEmpty(hexAddress)) //fudgeys fun house
                    ||
                    (ctx.Channel.Id == 1036838681048264735 && string.IsNullOrEmpty(hexAddress)) //metaboy gais metalab
                    )
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("Woah woah woah it's like you're speaking another language! My machines can't read that, please type it in Hex Format : Example: 0x36cd6b3b9329c04df55d55d41c257a5fdd387acd")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
            */
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("UNKNOWN COMMAND. For all claims please visit Gaia's MetaLab and Experiments. <#1036838681048264735>")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }

        [SlashCommand("claimable_add", "Add a claimable NFT")]
        public async Task AddClaimableCommand(InteractionContext ctx, [Option("nftName", "The  NFT name")] string nftName, [Option("nftData", "The nftData")] string nftData)
        {
            var isValid = false;
            nftName = nftName.Trim();
            nftData = nftData.Trim();

            if(nftName.Length > 0 && nftData.Length == 66 && nftData.StartsWith("0x"))
            {
                isValid = true;
            }

            if (
                 (ctx.Channel.Id == 933963130197917698 && isValid) //fudgeys fun house 
                 ||
                 (ctx.Channel.Id == 1036838681048264735 && isValid) //metaboy gaias metalab
                )
            {

                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Hold on adding the claimable NFT..."));

                var queryResult = await SqlService.AddClaimable(nftName, nftData, Settings.SqlServerConnectionString);

                if (queryResult == 1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I've added the claimable NFT!"));
                }
                else if (queryResult == -1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, the claimable NFT already exists!"));
                    return;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, something went wrong! Please try again later..."));
                    return;
                }
            }
            else if (
                    (ctx.Channel.Id == 933963130197917698 && !isValid) //fudgeys fun house
                    ||
                    (ctx.Channel.Id == 1036838681048264735 && !isValid) //metaboy gais metalab
                    )
            {
                    var builder = new DiscordInteractionResponseBuilder()
                    .WithContent("Woah woah woah it's like you're speaking another language! My machines can't read that, please type the nftData in Hex Format : Example: 0x14e15ad24d034f0883e38bcf95a723244a9a22e17d47eb34aa2b91220be0adc4")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("UNKNOWN COMMAND. For all claims please visit Gaia's MetaLab and Experiments. <#1036838681048264735>")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }

        [SlashCommand("claimable_remove", "Remove a claimable NFT")]
        public async Task RemoveClaimableCommand(InteractionContext ctx, [Option("nftData", "The nftData")] string nftData)
        {
            var isValid = false;
            nftData = nftData.Trim();

            if (nftData.Length == 66 && nftData.StartsWith("0x"))
            {
                isValid = true;
            }

            if (
                 (ctx.Channel.Id == 933963130197917698 && isValid) //fudgeys fun house 
                 ||
                 (ctx.Channel.Id == 1036838681048264735 && isValid) //metaboy gaias metalab
                )
            {

                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Hold on removing the claimable NFT..."));

                var queryResult = await SqlService.RemoveClaimable(nftData, Settings.SqlServerConnectionString);

                if (queryResult > 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I've removed the claimable NFT!"));
                }
                else if (queryResult == -1)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, the claimable NFT has already been removed!"));
                    return;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Sorry, something went wrong! Please try again later..."));
                    return;
                }
            }
            else if (
                    (ctx.Channel.Id == 933963130197917698 && !isValid) //fudgeys fun house
                    ||
                    (ctx.Channel.Id == 1036838681048264735 && !isValid) //metaboy gais metalab
                    )
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("Woah woah woah it's like you're speaking another language! My machines can't read that, please type the nftData in Hex Format : Example: 0x14e15ad24d034f0883e38bcf95a723244a9a22e17d47eb34aa2b91220be0adc4")
            .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
            else
            {
                var builder = new DiscordInteractionResponseBuilder()
                .WithContent("UNKNOWN COMMAND. For all claims please visit Gaia's MetaLab and Experiments. <#1036838681048264735>")
                .AsEphemeral(true);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }
        }
    }

}

