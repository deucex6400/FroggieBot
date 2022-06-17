using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using FroggieBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

var discord = new DiscordClient(new DiscordConfiguration()
{
    Token = "",
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
    .BuildServiceProvider()
});
slash.RegisterCommands<SlashCommands>(settings.DiscordServerId);

await discord.ConnectAsync();
await Task.Delay(-1);