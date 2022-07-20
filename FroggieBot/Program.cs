using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using FroggieBot;
using FroggieBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

Ranks? ranks;

using (StreamReader r = new StreamReader("ranks.json"))
{
    string json = r.ReadToEnd();
    ranks = JsonConvert.DeserializeObject<Ranks>(json);
}

var discord = new DiscordClient(new DiscordConfiguration()
{
    Token = settings.DiscordToken,
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged,
    MinimumLogLevel = LogLevel.Debug
});

var slash = discord.UseSlashCommands(
new SlashCommandsConfiguration
{
    Services = new ServiceCollection()
    .AddSingleton<LoopringService>()
    .AddSingleton<EtherscanService>()
    .AddSingleton<Random>()
    .AddScoped<SqlService>()
    .AddSingleton<Settings>(settings)
    .AddSingleton<EthereumService>()
    .AddSingleton<GamestopService>()
    .BuildServiceProvider()
});
SlashCommands.Ranks = ranks;
slash.RegisterCommands<SlashCommands>(settings.DiscordServerId);

await discord.ConnectAsync();
await Task.Delay(-1);