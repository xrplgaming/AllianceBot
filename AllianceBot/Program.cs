using AllianceBot_Discord;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XUMM.NET.SDK.EMBRS;

namespace AllianceBot
{
    public class Program
    {
        private readonly Commands _commands;
        private static DiscordSocketClient _discordClient;
        private readonly XummWebSocket _webSocketClient;
        private readonly XummMiscAppStorageClient _appStorageClient;
        private readonly XummMiscClient _miscClient;
        private readonly XummPayloadClient _payloadClient;
        private readonly XummHttpClient _httpClient;

        private readonly bool _updateCommands = false; // THIS MUST BE SET TRUE ON FIRST RUN TO REGISTER BOT COMMANDS
        private readonly string _updatesChannel = "";

        private readonly bool _supportFaucet = false;
        private readonly string _faucetChannel = "";

        private readonly bool _supportTournaments = false;
        private readonly string _tournamentNotifyChannel = "";

        private readonly bool _supportRandomMessages = false;
        private readonly string _randomMessageChannel = "";
        private static double _timeBetweenMessagesInHours = 8;
        private static List<string> _randomMessages = new List<string>();

        private static bool _supportLogging = false;
        private static string _loggingChannel = "";

        private readonly bool _supportThreads = false;
        private readonly string _threadsCategory = "";

        private static bool _running = false;
        private static bool _ready = false;

        private static double _timeBetweenDatabaseWritesInMinutes = 10;
        public static DateTime _timeSinceLastDatabaseWrite = DateTime.UtcNow;

        static Task Main(string[] args)
        {
            return new Program().MainAsync();
        }

        private Program()
        {
            Settings.Initialize();
            _discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All,
                LogGatewayIntentWarnings = false,
                LogLevel = LogSeverity.Info,
            });

            _httpClient = new XummHttpClient();
            _webSocketClient = new XummWebSocket();
            _appStorageClient = new XummMiscAppStorageClient(_httpClient);
            _miscClient = new XummMiscClient(_httpClient);
            _payloadClient = new XummPayloadClient(_httpClient, _webSocketClient);
            _commands = new Commands(_discordClient, _webSocketClient, _appStorageClient, _miscClient, _payloadClient, _httpClient);

            _discordClient.Log += Log;
            _running = true;

            if (_supportRandomMessages)
            {
                _randomMessages.Add("Example random message here!"); // IF YOU WANT CUSTOM MESSAGES IN YOUR SERVER, CAN ADD THEM HERE - FUTURE COMMAND WILL BE ADDED TO SUPPORT CUSTOM MESSAGES WITHOUT CHANGING BOT CODE
            }
        }

        private async Task MainAsync()
        {
            await Database.Initialize();
            _discordClient.ChannelDestroyed += HandleChannelDestroyedAsync;
            _discordClient.MessageDeleted += HandleMessageDeletedAsync;
            _discordClient.MessageReceived += HandleMessageReceivedAsync;
            _discordClient.Ready += HandleClientReadyAsync;
            _discordClient.SlashCommandExecuted += _commands.HandleSlashCommandAsync;
            await _discordClient.LoginAsync(TokenType.Bot, Settings.BotToken);
            await _discordClient.StartAsync();

            var loopTask = Task.Run(async () =>
            {
                while (_running)
                {
                    if(_ready) await LoopTasks();
                    await Task.Delay(1000);
                }
            });

            await Task.Delay(Timeout.Infinite);
        }

        private async Task LoopTasks()
        {
            try
            {
                // DATABASE
                if (Database.IsDirty)
                {
                    await Database.Write();
                    Database.IsDirty = false;
                }

                if ((DateTime.UtcNow - _timeSinceLastDatabaseWrite).TotalMinutes >= _timeBetweenDatabaseWritesInMinutes)
                {
                    await Database.Write();
                    Database.IsDirty = false;
                    _timeSinceLastDatabaseWrite = DateTime.UtcNow;
                }

                // DAILY FUNCTIONALITY
                if(Database.GetDatabase<DatabaseSettings>(DatabaseType.Settings).GetSavedDayOfWeek() != DateTime.UtcNow.DayOfWeek)
                {
                    Database.GetDatabase<DatabaseSettings>(DatabaseType.Settings).SetSavedDayOfWeek(DateTime.UtcNow.DayOfWeek);
                    var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));

                    // FAUCET
                    if(_supportFaucet)
                    {
                        foreach (var account in Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccounts())
                        {
                            account.SetReceivedFaucetReward(false);
                            account.SetChangedXRPAddress(false);
                        }

                        var faucetChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _faucetChannel);
                        var faucetBuilder = new EmbedBuilder()
                            .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                            .WithDescription("The faucet has been replenished! If you're registered via /register, you can now use the /faucet command in #bot-commands.")
                            .WithCurrentTimestamp()
                            .WithColor(Color.Orange);

                        await faucetChannel.SendMessageAsync(null, false, faucetBuilder.Build());
                        Database.IsDirty = true;
                    }

                    // MESSAGING/TOURNAMENT
                    switch (DateTime.UtcNow.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            {
                                if(_supportTournaments) Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.Active);
                                break;
                            }
                        case DayOfWeek.Monday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.HandlingRewards);
                                    await Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).HandleRewards(_discordClient);
                                }

                                break;
                            }
                        case DayOfWeek.Tuesday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.SignUp);
                                    await Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).ResetTournament(_discordClient);
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).IncrementTournamentWeek();
                                    var tournamentNotifyChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _tournamentNotifyChannel);
                                    var tournamentNotifyBuilder = new EmbedBuilder()
                                        .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                                        .WithDescription("Tournament sign-ups have started! Use the /tournament command in #bot-commands to sign-up. This week's tournament begins Friday night. If you have not done so already, you can register with me using the /register command!")
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange);

                                    await tournamentNotifyChannel.SendMessageAsync(null, false, tournamentNotifyBuilder.Build());
                                    Database.IsDirty = true;
                                }
                                break;
                            }
                        case DayOfWeek.Wednesday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.SignUp);
                                    var tournamentNotifyChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _tournamentNotifyChannel);
                                    var tournamentNotifyBuilder = new EmbedBuilder()
                                        .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                                        .WithDescription("Tournament sign-ups are still going on! Who else will be joining in this week?!")
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange);

                                    await tournamentNotifyChannel.SendMessageAsync(null, false, tournamentNotifyBuilder.Build());
                                    Database.IsDirty = true;
                                }
                                break;
                            }
                        case DayOfWeek.Thursday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.SignUp);
                                    var tournamentNotifyChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _tournamentNotifyChannel);
                                    var tournamentNotifyBuilder = new EmbedBuilder()
                                        .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                                        .WithDescription("Last day for tournament sign-ups this week! Don't miss out to win special prizes provided by our sponsors!")
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange);

                                    await tournamentNotifyChannel.SendMessageAsync(null, false, tournamentNotifyBuilder.Build());
                                    Database.IsDirty = true;
                                }
                                break;
                            }
                        case DayOfWeek.Friday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.StartingSoon);
                                    var tournamentNotifyChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _tournamentNotifyChannel);
                                    var tournamentNotifyBuilder = new EmbedBuilder()
                                        .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                                        .WithDescription("Tournament sign-ups have ended. If you missed out, no worries! You can earn via the /faucet command while you wait for next week!")
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange);

                                    await tournamentNotifyChannel.SendMessageAsync(null, false, tournamentNotifyBuilder.Build());
                                    Database.IsDirty = true;
                                }
                                break;
                            }
                        case DayOfWeek.Saturday:
                            {
                                if (_supportTournaments)
                                {
                                    Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentStatus(TournamentStatus.Active);
                                    await Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).StartTournament(_discordClient);
                                }
                                break;
                            }
                    }
                }

                // RANDOM MESSAGING
                if (_supportRandomMessages)
                {
                    if ((DateTime.UtcNow - Database.GetDatabase<DatabaseSettings>(DatabaseType.Settings).GetTimeSinceLastMessage()).TotalHours >= _timeBetweenMessagesInHours)
                    {
                        var rng = new System.Random();
                        var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));
                        var randomMessageChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _randomMessageChannel);
                        var randomIndex = rng.Next(0, _randomMessages.Count);
                        var embedBuilder = new EmbedBuilder()
                            .WithAuthor(_discordClient.CurrentUser.ToString(), _discordClient.CurrentUser.GetAvatarUrl() ?? _discordClient.CurrentUser.GetDefaultAvatarUrl())
                            .WithDescription(_randomMessages[randomIndex])
                            .WithCurrentTimestamp()
                            .WithColor(Color.Orange);

                        await randomMessageChannel.SendMessageAsync(null, false, embedBuilder.Build());
                        Database.GetDatabase<DatabaseSettings>(DatabaseType.Settings).SetTimeSinceLastMessage(DateTime.UtcNow);
                        Database.IsDirty = true;
                    }
                }

                // THREAD UPDATES AND DELETION
                ///await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).TestAllThreads(_discordClient);
            }
            catch (Exception ex)
            {
                await Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        public static async Task Shutdown()
        {
            await Database.Write();
            Database.IsDirty = false;
            _running = false;
            _ready = false;
            await _discordClient.StopAsync();
        }

        public static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

            try
            {
                if (_running && _ready)
                {
                    if (_supportLogging)
                    {
                        var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));
                        var logsChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _loggingChannel);
                        logsChannel.SendMessageAsync($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to send to " + _loggingChannel + " channel");
            }

            return Task.CompletedTask;
        }

        private async Task HandleChannelDestroyedAsync(SocketChannel arg)
        {
            try
            {
                if (_supportThreads)
                {
                    var channelId = arg.Id;
                    if (Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).ContainsThreadByChannelId(channelId))
                    {
                        await Program.Log(new LogMessage(LogSeverity.Info, "Channel", "Destroyed"));

                        await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).DeleteThread(channelId);
                        await Program.Log(new LogMessage(LogSeverity.Info, "Thread", "Deleted"));
                        Database.IsDirty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleMessageDeletedAsync(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
        {
            try
            {
                if (_supportThreads)
                {
                    var messageId = arg1.Id;
                    var channelId = arg2.Id;

                    if (Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).ContainsThreadByChannelId(channelId))
                    {
                        await Program.Log(new LogMessage(LogSeverity.Info, "Message", "Deleted"));

                        var thread = await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).GetThreadByChannelId(channelId);
                        if (thread.ContainsThreadMessageByMessageId(messageId))
                        {
                            thread.DeleteThreadMessage(messageId);
                            await Program.Log(new LogMessage(LogSeverity.Info, "Thread Message", "Deleted"));
                            Database.IsDirty = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleMessageReceivedAsync(SocketMessage arg)
        {
            try
            {
                if (_supportThreads)
                {
                    var msg = arg as SocketUserMessage;
                    if (msg == null) return;
                    var channelId = msg.Channel.Id;

                    if (Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).ContainsThreadByChannelId(channelId) && !msg.Author.IsBot)
                    {
                        if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).ContainsAccount(msg.Author.Id) && Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(msg.Author.Id).GetIsRegistered())
                        {
                            var userInfo = msg.Author;
                            var channel = msg.Channel;

                            var thread = await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).GetThreadByChannelId(channelId);
                            var threadMessage = thread.AddThreadMessage(arg.Author.Id, msg.Content);

                            if (arg.Type == MessageType.Reply)
                            {
                                var originalMessageId = arg.Reference.MessageId.Value;
                                if (thread.ContainsThreadMessageByMessageId(originalMessageId))
                                {
                                    var originalThreadMessage = thread.GetThreadMessageByMessageId(originalMessageId);
                                    var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));
                                    var originalUser = guild.GetUser(originalThreadMessage.GetThreadMessageAuthor());

                                    var embedBuiler = new EmbedBuilder()
                                        .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange)
                                        .AddField($"@{originalUser.Username}#{originalUser.Discriminator} wrote", ">>> " + originalThreadMessage.GetThreadMessageContent())
                                        .AddField("---", msg.Content);

                                    var message = await channel.SendMessageAsync(null, false, embedBuiler.Build());
                                    threadMessage.SetThreadMessageChannelId(message.Id);
                                }
                                else
                                {
                                    var embedBuiler = new EmbedBuilder()
                                        .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                        .WithDescription(msg.Content)
                                        .WithCurrentTimestamp()
                                        .WithColor(Color.Orange);

                                    var message = await channel.SendMessageAsync(null, false, embedBuiler.Build());
                                    threadMessage.SetThreadMessageChannelId(message.Id);
                                }
                            }
                            else
                            {
                                var embedBuiler = new EmbedBuilder()
                                    .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                    .WithDescription(msg.Content)
                                    .WithCurrentTimestamp()
                                    .WithColor(Color.Orange);

                                var message = await channel.SendMessageAsync(null, false, embedBuiler.Build());
                                threadMessage.SetThreadMessageChannelId(message.Id);
                            }

                            await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).UpdateThreadPositionInChannel(thread, _discordClient);
                            await msg.DeleteAsync();

                            Database.IsDirty = true;
                        }
                        else await msg.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleClientReadyAsync()
        {
            try
            {
                var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));
                var commands = await guild.GetApplicationCommandsAsync();

                if (_supportThreads)
                {
                    if (!_updateCommands && commands.Any(r => r.Name == "addtopic")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "AddTopic"));
                    else
                    {
                        var addTopicCommand = new SlashCommandBuilder()
                            .WithName("addtopic")
                            .WithDescription("Adds a new governance topic.")
                            .AddOption("header", ApplicationCommandOptionType.String, "Topic header", isRequired: true)
                            .AddOption("content", ApplicationCommandOptionType.String, "Topic content", isRequired: true);
                        await guild.CreateApplicationCommandAsync(addTopicCommand.Build());
                    }
                }

                if (!_updateCommands && commands.Any(r => r.Name == "faucet")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Faucet"));
                else
                {
                    var faucetCommand = new SlashCommandBuilder()
                        .WithName("faucet")
                        .WithDescription("Earn from daily faucet.");
                    await guild.CreateApplicationCommandAsync(faucetCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "help")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Help"));
                else
                {
                    var helpCommand = new SlashCommandBuilder()
                        .WithName("help")
                        .WithDescription("Get alliance bot commands.");
                    await guild.CreateApplicationCommandAsync(helpCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "maintenance")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Maintenance"));
                else
                {
                    var maintenanceCommand = new SlashCommandBuilder()
                        .WithName("maintenance")
                        .WithDescription("Shutdown alliance bot for maintenance.");
                    await guild.CreateApplicationCommandAsync(maintenanceCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "register")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Register"));
                else
                {
                    var registerCommand = new SlashCommandBuilder()
                        .WithName("register")
                        .WithDescription("Register with alliance bot.")
                        .AddOption("xrpaddress", ApplicationCommandOptionType.String, "The XRP address", isRequired: true);
                    await guild.CreateApplicationCommandAsync(registerCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "setwinner")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "SetWinner"));
                else
                {
                    var setWinnerCommand = new SlashCommandBuilder()
                        .WithName("setwinner")
                        .WithDescription("Set tournament winner.")
                        .AddOption("user", ApplicationCommandOptionType.User, "The user to set as winner", isRequired: true);
                    await guild.CreateApplicationCommandAsync(setWinnerCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "status")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Status"));
                else
                {
                    var statusCommand = new SlashCommandBuilder()
                        .WithName("status")
                        .WithDescription("Check your status within alliance bot.");
                    await guild.CreateApplicationCommandAsync(statusCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "swap")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Swap"));
                else
                {
                    var swapCommand = new SlashCommandBuilder()
                        .WithName("swap")
                        .WithDescription("Swap between EMBRS/XRP, EMBRS/USD, and USD/XRP.")
                        .AddOption("from", ApplicationCommandOptionType.String, "EMBRS/USD/XRP", isRequired: true)
                        .AddOption("to", ApplicationCommandOptionType.String, "EMBRS/USD/XRP", isRequired: true)
                        .AddOption("amount", ApplicationCommandOptionType.Number, "The swap amount", isRequired: true);
                    await guild.CreateApplicationCommandAsync(swapCommand.Build());
                }

                if (!_updateCommands && commands.Any(r => r.Name == "tip")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tip"));
                else
                {
                    var tipCommand = new SlashCommandBuilder()
                        .WithName("tip")
                        .WithDescription("Tip another community member.")
                        .AddOption("user", ApplicationCommandOptionType.User, "The user to tip", isRequired: true)
                        .AddOption("amount", ApplicationCommandOptionType.Number, "The tip amount", isRequired: true);
                    await guild.CreateApplicationCommandAsync(tipCommand.Build());
                }

                if (_supportTournaments)
                {
                    if (!_updateCommands && commands.Any(r => r.Name == "tournament")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tournament"));
                    else
                    {
                        var tournamentCommand = new SlashCommandBuilder()
                           .WithName("tournament")
                           .WithDescription("Join tournament during sign-up period.");
                        await guild.CreateApplicationCommandAsync(tournamentCommand.Build());
                    }

                    if (!_updateCommands && commands.Any(r => r.Name == "tournamentgoal")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tournament Goal"));
                    else
                    {
                        var tournamentGoalCommand = new SlashCommandBuilder()
                           .WithName("tournamentgoal")
                           .WithDescription("Set the tournament achievement.")
                           .AddOption("achievement", ApplicationCommandOptionType.String, "Achievement", isRequired: true);
                        await guild.CreateApplicationCommandAsync(tournamentGoalCommand.Build());
                    }

                    if (!_updateCommands && commands.Any(r => r.Name == "tournamentreward")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tournament Reward"));
                    else
                    {
                        var tournamentRewardCommand = new SlashCommandBuilder()
                           .WithName("tournamentreward")
                           .WithDescription("Add the tournament reward.")
                           .AddOption("topreward", ApplicationCommandOptionType.String, "Top Random Reward", isRequired: true)
                           .AddOption("nextreward", ApplicationCommandOptionType.String, "Next Random Reward", isRequired: true)
                           .AddOption("normalreward", ApplicationCommandOptionType.String, "Normal Reward", isRequired: true);
                        await guild.CreateApplicationCommandAsync(tournamentRewardCommand.Build());
                    }

                    if (!_updateCommands && commands.Any(r => r.Name == "tournamentsponsor")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tournament Sponsor"));
                    else
                    {
                        var tournamentSponsorCommand = new SlashCommandBuilder()
                           .WithName("tournamentsponsor")
                           .WithDescription("Add a tournament sponsor.")
                           .AddOption("sponsorname", ApplicationCommandOptionType.String, "Sponsor Name", isRequired: true)
                           .AddOption("sponsorurl", ApplicationCommandOptionType.String, "Sponsor URL", isRequired: true)
                           .AddOption("imageurl", ApplicationCommandOptionType.String, "Sponsor Logo URL", isRequired: true)
                           .AddOption("description", ApplicationCommandOptionType.String, "Sponsor Description", isRequired: true);
                        await guild.CreateApplicationCommandAsync(tournamentSponsorCommand.Build());
                    }

                    if (!_updateCommands && commands.Any(r => r.Name == "tournamentstatus")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Tournament Status"));
                    else
                    {
                        var tournamentStatusCommand = new SlashCommandBuilder()
                           .WithName("tournamentstatus")
                           .WithDescription("Check the current tournament status.");
                        await guild.CreateApplicationCommandAsync(tournamentStatusCommand.Build());
                    }
                }

                if (_supportThreads)
                {
                    if (!_updateCommands && commands.Any(r => r.Name == "vote")) await Log(new LogMessage(LogSeverity.Info, "Command Loaded", "Vote"));
                    else
                    {
                        var voteCommand = new SlashCommandBuilder()
                            .WithName("vote")
                            .WithDescription("Vote on topic in channel.")
                            .AddOption("result", ApplicationCommandOptionType.String, "YES/NO", isRequired: true);
                        await guild.CreateApplicationCommandAsync(voteCommand.Build());
                    }

                    var threadDatabase = Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads);
                    threadDatabase.SetCategoryId(guild.CategoryChannels.FirstOrDefault(category => category.Name.ToLower() == _threadsCategory).Id);
                }

                var updateChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _updatesChannel);
                await updateChannel.SendMessageAsync("**Alliance bot is now online! Please check the XRPL Gaming Alliance's GitHub for latest changes!**");
                _ready = true;
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                await Log(new LogMessage(LogSeverity.Error, exception.Source, json, exception));
            }
        }
    }
}
