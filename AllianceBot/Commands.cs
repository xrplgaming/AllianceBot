using AllianceBot;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xrpl.Client;
using Xrpl.Client.Json.Converters;
using Xrpl.Client.Model;
using XUMM.NET.SDK.EMBRS;
using XUMM.NET.SDK.Models.Payload;

namespace AllianceBot_Discord
{
    public class Commands
    {
        private readonly DiscordSocketClient _discordClient;
        private readonly XummWebSocket _webSocketClient;
        private readonly XummMiscAppStorageClient _appStorageClient;
        private readonly XummMiscClient _miscClient;
        private readonly XummPayloadClient _payloadClient;
        private readonly XummHttpClient _httpClient;

        public Commands(DiscordSocketClient discordClient, XummWebSocket webSocketClient, XummMiscAppStorageClient appStorageClient,
                        XummMiscClient miscClient, XummPayloadClient payloadClient, XummHttpClient httpClient)
        {
            _discordClient = discordClient;
            _webSocketClient = webSocketClient;
            _appStorageClient = appStorageClient;
            _miscClient = miscClient;
            _payloadClient = payloadClient;
            _httpClient = httpClient;
        }

        public async Task HandleSlashCommandAsync(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;

                switch (command.Data.Name)
                {
                    case "addtopic":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "addtopic"));
                            var addTopicTask = Task.Run(async () =>
                            {
                                await HandleAddTopicCommand(command);
                            });
                            break;
                        }
                    case "faucet":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "faucet"));
                            var faucetTask = Task.Run(async () =>
                            {
                                await HandleFaucetCommand(command);
                            });
                            break;
                        }
                    case "help":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "help"));
                            var helpTask = Task.Run(async () =>
                            {
                                await HandleHelpCommand(command);
                            });
                            break;
                        }
                    case "maintenance":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "maintenance"));
                            var maintenanceTask = Task.Run(async () =>
                            {
                                await HandleMaintenanceCommand(command);
                            });
                            break;
                        }
                    case "register":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "register"));
                            var registerTask = Task.Run(async () =>
                            {
                                await HandleRegisterCommand(command);
                            });
                            break;
                        }
                    case "setwinner":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "setwinner"));
                            var setwinnerTask = Task.Run(async () =>
                            {
                                await HandleSetWinnerCommand(command);
                            });
                            break;
                        }
                    case "status":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "status"));
                            var statusTask = Task.Run(async () =>
                            {
                                await HandleStatusCommand(command);
                            });
                            break;
                        }
                    case "swap":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "swap"));
                            var swapTask = Task.Run(async () =>
                            {
                                await HandleSwapCommand(command);
                            });
                            break;
                        }
                    case "tip":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tip"));
                            var tipTask = Task.Run(async () =>
                            {
                                await HandleTipCommand(command);
                            });
                            break;
                        }
                    case "tournament":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tournament"));
                            var tournamentTask = Task.Run(async () =>
                            {
                                await HandleTournamentCommand(command);
                            });
                            break;
                        }
                    case "tournamentgoal":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tournamentgoal"));
                            var tournamentGoalTask = Task.Run(async () =>
                            {
                                await HandleTournamentGoalCommand(command);
                            });
                            break;
                        }
                    case "tournamentreward":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tournamentreward"));
                            var tournamentRewardTask = Task.Run(async () =>
                            {
                                await HandleTournamentRewardCommand(command);
                            });
                            break;
                        }
                    case "tournamentsponsor":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tournamentsponsor"));
                            var tournamentSponsorTask = Task.Run(async () =>
                            {
                                await HandleTournamentSponsorCommand(command);
                            });
                            break;
                        }
                    case "tournamentstatus":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "tournamentstatus"));
                            var tournamentStatusTask = Task.Run(async () =>
                            {
                                await HandleTournamentStatusCommand(command);
                            });
                            break;
                        }
                    case "vote":
                        {
                            await Program.Log(new LogMessage(LogSeverity.Info, "Command by " + userInfo.Username + "#" + userInfo.Discriminator, "vote"));
                            var voteTask = Task.Run(async () =>
                            {
                                await HandleVoteCommand(command);
                            });
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleAddTopicCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync();
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfRegistered(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                
                var header = (string)command.Data.Options.SingleOrDefault(r => r.Name == "header").Value;
                var content = (string)command.Data.Options.SingleOrDefault(r => r.Name == "content").Value;

                var thread = await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).CreateThread(userInfo, header, content, _discordClient);
                await command.FollowupAsync("New thread created in channel #" + thread.GetThreadChannelName() + " under governance category!");

                Database.IsDirty = true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleFaucetCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfRegistered(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;
                if (await CheckIfReceivedFaucetReward(command)) return;

                var userInfo = command.User as SocketGuildUser;
                await XRPL.SendRewardAsync(null, command, null, userInfo, Settings.FaucetTokenAmt, "TOKENGOESHERE", false, true);
                Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).ModEMBRSEarned(float.Parse(Settings.FaucetTokenAmt));
                Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).SetReceivedFaucetReward(true);
                Database.IsDirty = true;

            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleHelpCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;

                var stringBuilder = new StringBuilder();
                stringBuilder.Append("To use slash commands on **mobile**, please type out the /command until it shows listed above. Tap the command, and then tap each parameter (if required) and it will allow you to fill them out individually.");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
                stringBuilder.Append("To use slash commands on **desktop**, please type out the /command until it shows listed above. Click it and it should automatically jump to the first parameter (if required). Click the box next to each parameter to fill them out individually.");
                stringBuilder.AppendLine();
                stringBuilder.Append("-----");

                var swapStringBuilder = new StringBuilder();
                swapStringBuilder.Append("Swap between trading pairs EMBRS/XRP, EMBRS/USD, and USD/XRP. Sign transaction in XUMM Wallet for safety and security (#bot-commands)");
                swapStringBuilder.AppendLine();
                swapStringBuilder.AppendLine();
                swapStringBuilder.Append("Example 1: **/swap EMBRS XRP 10** will swap 10 EMBRS into equal in value amount of XRP (exchange rate shown in XUMM)");
                swapStringBuilder.AppendLine();
                swapStringBuilder.AppendLine();
                swapStringBuilder.Append("Example 2: **/swap USD EMBRS 10** will swap an equal in value amount of USD into 10 EMBRS (exchange rate shown in XUMM)");

                var tipStringBuilder = new StringBuilder();
                tipStringBuilder.Append("Tip a registered user, and sign transaction in XUMM Wallet for safety and security (#lounge)");
                tipStringBuilder.AppendLine();
                tipStringBuilder.AppendLine();
                tipStringBuilder.Append("Example: **/tip @Vaernus 10** will tip 10 EMBRS as a payment to @Vaernus (who will most likely tip you back because he's pretty cool)");

                var embedBuiler = new EmbedBuilder()
                    .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                    .WithDescription(stringBuilder.ToString())
                    .WithColor(Color.Orange)
                    .AddField("/addtopic", "Create a new governance topic and generate a Discord channel for discussion and voting (#bot-commands)")
                    .AddField("/faucet", "Receive daily tokens from faucet (#bot-commands)")
                    .AddField("/help", "Show bot command list (#bot-commands)")
                    .AddField("/register <xrpaddress>", "Register your Discord username and XRP address for faucet, rewards, DEX swaps, and tips (#bot-commands)")
                    .AddField("/status", "Check status of your XRP address, balances, earned tokens, etc. in alliance bot (#bot-commands)")
                    .AddField("/swap <from> <to> <amount>", swapStringBuilder.ToString())
                    .AddField("/tip <recipient> <amount>", tipStringBuilder.ToString())
                    .AddField("/tournament", "Sign-up for the current week's tournament (#bot-commands)")
                    .AddField("/tournamentstatus", "Show the current week's tournament information (#bot-commands)")
                    .AddField("/vote", "Vote on a governance topic within the Discord channel (any governance topic channel)");

                await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleMaintenanceCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfAdmin(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var guild = _discordClient.GetGuild(ulong.Parse(Settings.GuildID));
                var updateChannel = guild.TextChannels.FirstOrDefault(x => x.Name == "updates");
                await updateChannel.SendMessageAsync("**Alliance bot is shutting down for maintenance!**");
                await command.FollowupAsync("Alliance bot maintenance ready", ephemeral: true);
                await Program.Shutdown();

            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleRegisterCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;
                if (!await CheckIfValidXRPAddress(command)) return;
                if (!await CheckIfUniqueXRPAddress(command)) return;

                var userInfo = command.User as SocketGuildUser;
                var xrpAddress = (string)command.Data.Options.First().Value;
                if (!Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetIsRegistered())
                {
                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).RegisterAccount(userInfo.Id, xrpAddress);
                    await command.FollowupAsync("You are registered with alliance bot!", ephemeral: true);
                    Database.IsDirty = true;
                }
                else
                {
                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).RegisterAccount(userInfo.Id, xrpAddress);
                    await command.FollowupAsync("You updated your XRP address in alliance bot!", ephemeral: true);
                    Database.IsDirty = true;
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleSetWinnerCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync();
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfAdmin(command)) return;
                if (!await CheckIfCorrectChannel(command, "verify")) return;

                var userInfo = command.User as SocketGuildUser;
                var guildUser = (SocketGuildUser)command.Data.Options.First().Value;
                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(guildUser.Id).GetIsRegistered())
                {
                    await Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).AddTournamentWinner(_discordClient, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(guildUser.Id));
                    await command.FollowupAsync($"A winner is {guildUser.Username}#{guildUser.Discriminator}!");
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleStatusCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;

                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetIsRegistered())
                {
                    var xrp = 0.0m;
                    var embrs = 0.0m;
                    var stx = 0.0m;
                    var usd = 0.0m;

                    IRippleClient client = new RippleClient(Settings.WebSocketUrl);
                    client.Connect();
                    {
                        xrp = await XRPL.ReturnAccountBalance(client, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetXRPAddress());
                        System.Threading.Thread.Sleep(Settings.AccountLinesThrottle * 1000);

                        string marker = "";
                        do
                        {
                            var returnObj = await XRPL.ReturnTrustLines(client, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetXRPAddress(), marker);
                            if (embrs == 0.0m) embrs = returnObj.EMBRSBalance;
                            if (stx == 0.0m) stx = returnObj.STXBalance;
                            if (usd == 0.0m) usd = returnObj.USDBalance;
                            marker = returnObj.Marker;
                            System.Threading.Thread.Sleep(Settings.AccountLinesThrottle * 1000);
                        } while (marker != "" && marker != null);
                    }
                    client.Disconnect();

                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append("**XRP**: " + xrp.ToString());
                    stringBuilder.AppendLine();
                    stringBuilder.Append("**EMBRS**: " + embrs.ToString());
                    stringBuilder.AppendLine();
                    stringBuilder.Append("**STX**: " + stx.ToString());
                    stringBuilder.AppendLine();
                    stringBuilder.Append("**USD**: " + usd.ToString());

                    var embedBuiler = new EmbedBuilder()
                        .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                        .WithTitle(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetXRPAddress())
                        .WithColor(Color.Orange)
                        .AddField("Balances", stringBuilder.ToString())
                        .AddField("EMBRS Earned", Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetEMBRSEarned())
                        .AddField("Received Faucet Reward", Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).ReceivedFaucetReward())
                        .AddField("In Tournament", Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetInTournament())
                        .AddField("Won Tournament", Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetTournamentWinner());

                    await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);
                }
                else
                {
                    var embedBuiler = new EmbedBuilder()
                        .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                        .WithTitle("NOT REGISTERED")
                        .WithDescription("Use /register command to link your XRP address!")
                        .WithColor(Color.Orange);
                    await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleSwapCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenSwapOrTip(command)) return;
                if (!await CheckIfRegistered(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var from = (string)command.Data.Options.SingleOrDefault(r => r.Name == "from").Value;
                var to = (string)command.Data.Options.SingleOrDefault(r => r.Name == "to").Value;
                var amount = (double)command.Data.Options.SingleOrDefault(r => r.Name == "amount").Value;

                XummPayloadResponse createdPayload = null;

                if ((from.ToLower() == "embrs" || from.ToLower() == "embers" || from.ToLower() == "usd" || from.ToLower() == "xrp") &&
                   (to.ToLower() == "embrs" || to.ToLower() == "embers" || to.ToLower() == "usd" || to.ToLower() == "xrp") &&
                   from.ToLower() != to.ToLower())
                {
                    IRippleClient client = new RippleClient(Settings.WebSocketUrl);
                    client.Connect();
                    var initialMidPrice = await XRPL.GetBookOffers(client, from, to);
                    var value = initialMidPrice.Midprice;
                    client.Disconnect();

                    Currency fromCurrency = null;
                    Currency toCurrency = null;
                    var toAmount = decimal.Round(Convert.ToDecimal(amount) * value.Value, 6);

                    if ((from.ToLower() == "embrs" || from.ToLower() == "embers") && to.ToLower() == "xrp")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.CurrencyCode,
                            Issuer = Settings.IssuerAddress,
                            Value = amount.ToString()
                        };

                        toCurrency = new Currency()
                        {
                            ValueAsXrp = toAmount
                        };
                    }
                    else if ((to.ToLower() == "embrs" || to.ToLower() == "embers") && from.ToLower() == "xrp")
                    {
                        fromCurrency = new Currency()
                        {
                            ValueAsXrp = toAmount
                        };

                        toCurrency = new Currency
                        {
                            CurrencyCode = Settings.CurrencyCode,
                            Issuer = Settings.IssuerAddress,
                            Value = amount.ToString()
                        };
                    }
                    else if ((from.ToLower() == "embrs" || from.ToLower() == "embers") && to.ToLower() == "usd")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.CurrencyCode,
                            Issuer = Settings.IssuerAddress,
                            Value = amount.ToString()
                        };

                        toCurrency = new Currency
                        {
                            CurrencyCode = Settings.USDCurrencyCode,
                            Issuer = Settings.USDIssuerAddress,
                            Value = toAmount.ToString()
                        };
                    }
                    else if ((to.ToLower() == "embrs" || to.ToLower() == "embers") && from.ToLower() == "usd")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.USDCurrencyCode,
                            Issuer = Settings.USDIssuerAddress,
                            Value = toAmount.ToString()
                        };

                        toCurrency = new Currency
                        {
                            CurrencyCode = Settings.CurrencyCode,
                            Issuer = Settings.IssuerAddress,
                            Value = amount.ToString()
                        };
                    }
                    else if (from.ToLower() == "usd" && to.ToLower() == "xrp")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.USDCurrencyCode,
                            Issuer = Settings.USDIssuerAddress,
                            Value = amount.ToString()
                        };

                        toCurrency = new Currency()
                        {
                            ValueAsXrp = toAmount
                        };
                    }
                    else if (to.ToLower() == "usd" && from.ToLower() == "xrp")
                    {
                        fromCurrency = new Currency()
                        {
                            ValueAsXrp = toAmount
                        };

                        toCurrency = new Currency
                        {
                            CurrencyCode = Settings.USDCurrencyCode,
                            Issuer = Settings.USDIssuerAddress,
                            Value = amount.ToString()
                        };
                    }
                    else
                    {
                        await command.FollowupAsync("Invalid pairs or parameters!", ephemeral: true);
                        return;
                    }

                    await command.FollowupAsync($"Swapping " + amount.ToString() + " " + from.ToUpper() + "/" + to.ToUpper(), ephemeral: true);

                    var takerGetsConverter = new CurrencyConverter();
                    var takerGets = fromCurrency;
                    var takerGetsResult = JsonConvert.SerializeObject(takerGets, takerGetsConverter);

                    var takerPaysConverter = new CurrencyConverter();
                    var takerPays = toCurrency;
                    var takerPaysResult = JsonConvert.SerializeObject(takerPays, takerPaysConverter);

                    var flags = OfferCreateFlags.tfImmediateOrCancel;
                    var flagsResults = JsonConvert.SerializeObject(flags);

                    var payload = new XummPostJsonPayload("{ \"TransactionType\": \"OfferCreate\", " +
                                                            "\"TakerGets\": " + takerGetsResult + ", " +
                                                            "\"TakerPays\": " + takerPaysResult + ", " +
                                                            "\"Flags\": " + flagsResults + " }");

                    payload.Options = new XummPayloadOptions();
                    payload.Options.Expire = 5;
                    payload.Options.Submit = true;

                    payload.CustomMeta = new XummPayloadCustomMeta();
                    payload.CustomMeta.Instruction = "Swapping " + amount.ToString() + " " + from.ToUpper() + "/" + to.ToUpper();

                    createdPayload = await _payloadClient.CreateAsync(payload);

                    // IF MOBILE, PUSH TO XUMM APP
                    if (userInfo.ActiveClients.Any(r => r == ClientType.Mobile))
                    {
                        var embedBuiler = new EmbedBuilder()
                                            .WithUrl(createdPayload.Next.Always)
                                            .WithDescription("Open In Xumm Wallet")
                                            .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                            .WithTitle("EMBRS Sign Request");
                        await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);

                        var getPayload = await _payloadClient.GetAsync(createdPayload);
                        while (!getPayload.Meta.Expired)
                        {
                            if (getPayload.Meta.Resolved && getPayload.Meta.Signed)
                            {
                                await command.FollowupAsync($"Swap was resolved and signed!", ephemeral: true);
                                Database.IsDirty = true;
                                break;
                            }
                            else if (getPayload.Meta.Resolved && !getPayload.Meta.Signed)
                            {
                                await command.FollowupAsync($"Swap was cancelled by user", ephemeral: true);
                                break;
                            }

                            System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                            getPayload = await _payloadClient.GetAsync(createdPayload);
                        }

                        if (getPayload.Meta.Expired)
                        {
                            await command.FollowupAsync($"Swap sign request expired", ephemeral: true);
                        }
                    }
                    else // IF NOT MOBILE, PUSH FOLLOWUP WITH PNG TO QR SCAN AND SIGN
                    {
                        var qrPNG = createdPayload.Refs.QrPng;
                        var embedBuiler = new EmbedBuilder()
                                            .WithImageUrl(qrPNG)
                                            .WithDescription("Scan In Xumm Wallet")
                                            .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                            .WithTitle("EMBRS Sign Request");
                        await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);

                        var getPayload = await _payloadClient.GetAsync(createdPayload);
                        while (!getPayload.Meta.Expired)
                        {
                            if (getPayload.Meta.Resolved && getPayload.Meta.Signed)
                            {
                                await command.FollowupAsync($"Swap was resolved and signed!", ephemeral: true);
                                Database.IsDirty = true;
                                break;
                            }
                            else if (getPayload.Meta.Resolved && !getPayload.Meta.Signed)
                            {
                                await command.FollowupAsync($"Swap was cancelled by user", ephemeral: true);
                                break;
                            }

                            System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                            getPayload = await _payloadClient.GetAsync(createdPayload);
                        }

                        if (getPayload.Meta.Expired)
                        {
                            await command.FollowupAsync($"Swap sign request expired", ephemeral: true);
                        }
                    }
                }
                else
                {
                    await command.FollowupAsync("Invalid pairs or parameters!", ephemeral: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTipCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenSwapOrTip(command)) return;
                if (!await CheckIfRegistered(command)) return;

                var userInfo = command.User as SocketGuildUser;
                var user = (SocketGuildUser)command.Data.Options.SingleOrDefault(r => r.Name == "user").Value;
                var amount = (double)command.Data.Options.SingleOrDefault(r => r.Name == "amount").Value;

                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).ContainsAccount(user.Id) && Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).GetIsRegistered())
                {
                    var tipAmount = string.Empty;
                    if (userInfo.Roles.Any(r => r.Name == "Leads"))
                    {
                        await command.FollowupAsync("Beginning server tip", ephemeral: true);
                        tipAmount = Math.Min(amount, float.Parse(Settings.MaxTipTokenAmt)).ToString();
                        await XRPL.SendRewardAsync(null, command, userInfo, user, tipAmount, "TOKENGOESHERE", true, false);
                        Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).ModEMBRSEarned(float.Parse(tipAmount));
                        Database.IsDirty = true;
                    }
                    else
                    {
                        await command.FollowupAsync("Beginning tip", ephemeral: true);
                        var destination = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).GetXRPAddress();
                        var currencyAmount = new Currency { CurrencyCode = Settings.CurrencyCode, Issuer = Settings.IssuerAddress, Value = amount.ToString() };

                        var converter = new CurrencyConverter();
                        var result = JsonConvert.SerializeObject(currencyAmount, converter);

                        var payload = new XummPostJsonPayload("{ \"TransactionType\": \"Payment\", " +
                                                                "\"Destination\": \"" + destination + "\", " +
                                                                "\"Amount\": " + result + " }");

                        payload.Options = new XummPayloadOptions();
                        payload.Options.Expire = 5;
                        payload.Options.Submit = true;

                        payload.CustomMeta = new XummPayloadCustomMeta();
                        payload.CustomMeta.Instruction = "Tipping " + amount.ToString() + " EMBRS to " + destination + " (" + user.Username + "#" + user.Discriminator + ")";

                        var createdPayload = await _payloadClient.CreateAsync(payload);

                        // IF MOBILE, PUSH TO XUMM APP
                        if (user.ActiveClients.Any(r => r == ClientType.Mobile))
                        {
                            var embedBuiler = new EmbedBuilder()
                                                .WithUrl(createdPayload.Next.Always)
                                                .WithDescription("Open In Xumm Wallet")
                                                .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                                .WithTitle("EMBRS Sign Request");
                            await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);

                            var getPayload = await _payloadClient.GetAsync(createdPayload);
                            while (!getPayload.Meta.Expired)
                            {
                                if (getPayload.Meta.Resolved && getPayload.Meta.Signed)
                                {
                                    await command.FollowupAsync($"**{userInfo.Username}#{userInfo.Discriminator} sent {user.Username}#{user.Discriminator} a tip of {amount} EMBRS!**");
                                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).ModEMBRSEarned((float)amount);
                                    Database.IsDirty = true;
                                    break;
                                }
                                else if (getPayload.Meta.Resolved && !getPayload.Meta.Signed)
                                {
                                    await command.FollowupAsync($"Tip was cancelled by user", ephemeral: true);
                                    break;
                                }

                                System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                                getPayload = await _payloadClient.GetAsync(createdPayload);
                            }

                            if (getPayload.Meta.Expired)
                            {
                                await command.FollowupAsync($"Tip sign request expired", ephemeral: true);
                            }
                        }
                        else // IF NOT MOBILE, PUSH FOLLOWUP WITH PNG TO QR SCAN AND SIGN
                        {
                            var qrPNG = createdPayload.Refs.QrPng;
                            var embedBuiler = new EmbedBuilder()
                                                .WithImageUrl(qrPNG)
                                                .WithDescription("Scan In Xumm Wallet")
                                                .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                                                .WithTitle("EMBRS Sign Request");
                            await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);

                            var getPayload = await _payloadClient.GetAsync(createdPayload);
                            while (!getPayload.Meta.Expired)
                            {
                                if (getPayload.Meta.Resolved && getPayload.Meta.Signed)
                                {
                                    await command.FollowupAsync($"**{userInfo.Username}#{userInfo.Discriminator} sent {user.Username}#{user.Discriminator} a tip of {amount} EMBRS!**");
                                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).ModEMBRSEarned((float)amount);
                                    Database.IsDirty = true;
                                    break;
                                }
                                else if (getPayload.Meta.Resolved && !getPayload.Meta.Signed)
                                {
                                    await command.FollowupAsync($"Tip was cancelled by user", ephemeral: true);
                                    break;
                                }

                                System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                                getPayload = await _payloadClient.GetAsync(createdPayload);
                            }

                            if (getPayload.Meta.Expired)
                            {
                                await command.FollowupAsync($"Tip sign request expired", ephemeral: true);
                            }
                        }
                    }
                }
                else
                {
                    await command.FollowupAsync("Recipient is not registered for tips!", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTournamentCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfRegistered(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;

                if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Tuesday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Wednesday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Thursday)
                {
                    await Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).AddTournamentParticipant(_discordClient, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id));
                    await command.FollowupAsync("You are signed-up for this week's tournament! Check #tournament for more details.", ephemeral: true);
                    await command.FollowupAsync("A new participant has joined the tournament...");
                }
                else
                {
                    await command.FollowupAsync("Tournament sign-ups for this week are closed. Next week's sign-ups will start on Tuesday!", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTournamentGoalCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfAdmin(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var achievement = (string)command.Data.Options.First().Value;
                Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentAchievement(achievement);
                await command.FollowupAsync($"Current week's tournament achievement is set!", ephemeral: true);
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTournamentRewardCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfAdmin(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var topReward = (string)command.Data.Options.SingleOrDefault(r => r.Name == "topreward").Value;
                var nextreward = (string)command.Data.Options.SingleOrDefault(r => r.Name == "nextreward").Value;
                var normalreward = (string)command.Data.Options.SingleOrDefault(r => r.Name == "normalreward").Value;
                Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).SetTournamentReward(topReward, nextreward, normalreward);
                await command.FollowupAsync($"Current week's tournament rewards are set!", ephemeral: true);
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTournamentSponsorCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfAdmin(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var sponsorName = (string)command.Data.Options.SingleOrDefault(r => r.Name == "sponsorname").Value;
                var sponsorUrl = (string)command.Data.Options.SingleOrDefault(r => r.Name == "sponsorurl").Value;
                var imageUrl = (string)command.Data.Options.SingleOrDefault(r => r.Name == "imageurl").Value;
                var description = (string)command.Data.Options.SingleOrDefault(r => r.Name == "description").Value;
                Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).AddTournamentSponsor(sponsorName, sponsorUrl, imageUrl, description);
                await command.FollowupAsync($"Current week's tournament sponsor is added!", ephemeral: true);
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleTournamentStatusCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfCorrectChannel(command, "bot-commands")) return;

                var userInfo = command.User as SocketGuildUser;
                var status = "";

                switch(Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentStatus())
                {
                    case TournamentStatus.Active:
                        {
                            status = "Active";
                            break;
                        }
                    case TournamentStatus.HandlingRewards:
                        {
                            status = "Handling Tournament Rewards";
                            break;
                        }
                    case TournamentStatus.SignUp:
                        {
                            status = "Sign-ups";
                            break;
                        }
                    case TournamentStatus.StartingSoon:
                        {
                            status = "Starting Soon";
                            break;
                        }
                }

                var embedBuiler = new EmbedBuilder()
                    .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                    .WithDescription("TOURNAMENT WEEK " + Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentWeek())
                    .WithColor(Color.Orange)
                    .AddField("Tournament Status", status)
                    .AddField("Achievement", Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentAchievement())
                    .AddField("Participants", Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentParticipants(_discordClient))
                    .AddField("Winners", Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentWinners(_discordClient))
                    .AddField("Reward", Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentReward())
                    .AddField("Sponsors", (Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentSponsors().Count == 0) ? "None" : "See Below");

                await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);

                foreach(var sponsor in Database.GetDatabase<DatabaseTournament>(DatabaseType.Tournament).GetTournamentSponsors())
                {
                    embedBuiler = new EmbedBuilder()
                        .WithAuthor(sponsor.GetSponsorName(), sponsor.GetSponsorImageUrl())
                        .WithTitle("Website")
                        .WithThumbnailUrl(sponsor.GetSponsorImageUrl())
                        .WithDescription(sponsor.GetDescription())
                        .WithUrl(sponsor.GetSponsorUrl())
                        .WithColor(Color.Orange);
                    await command.FollowupAsync(embed: embedBuiler.Build(), ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task HandleVoteCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);
                if (!await CheckForTimeBetweenCommands(command)) return;
                if (!await CheckIfRegistered(command)) return;

                var userInfo = command.User as SocketGuildUser;
                var result = (string)command.Data.Options.SingleOrDefault(r => r.Name == "result").Value;

                if (result.ToLower() == "yes" || result.ToLower() == "no")
                {
                    var resultBool = (result.ToLower() == "yes") ? true : (result.ToLower() == "no") ? false : false;
                    var channelId = command.Channel.Id;
                    if (Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).ContainsThreadByChannelId(channelId))
                    {
                        var thread = await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).GetThreadByChannelId(channelId);
                        thread.SetVote(userInfo.Id, resultBool);
                        await Database.GetDatabase<DatabaseThreads>(DatabaseType.Threads).UpdateThreadPositionInChannel(thread, _discordClient);
                        await command.FollowupAsync("Your vote has been applied!", ephemeral: true);

                        var embedBuiler = new EmbedBuilder()
                            .WithAuthor(userInfo.ToString(), userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl())
                            .WithDescription("Voted: " + result.ToLower())
                            .WithCurrentTimestamp()
                            .WithColor(Color.Orange)
                            .AddField("Yes Votes", thread.GetYesVotes().ToString(), true)
                            .AddField("No Votes", thread.GetNoVotes().ToString(), true);

                        var message = await command.Channel.SendMessageAsync(null, false, embedBuiler.Build());

                        Database.IsDirty = true;
                    }
                    else
                    {
                        await command.FollowupAsync("Did not find a governance thread to vote on. Please make sure to use /vote command in a thread channel!", ephemeral: true);
                    }
                }
                else
                {
                    await command.FollowupAsync("Parameter incorrect in vote command! Must be yes or no.", ephemeral: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private async Task<bool> CheckIfAdmin(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                if (userInfo.Roles.Any(r => r.Name == "Leads")) return true;
                await command.FollowupAsync("Admin-only command!", ephemeral: true);
                return false;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckIfRegistered(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                if (!Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetIsRegistered())
                {
                    await command.FollowupAsync("Please /register to use this command!", ephemeral: true);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckIfValidXRPAddress(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                var xrpAddress = (string)command.Data.Options.First().Value;
                if(!await XRPL.ReturnValidXRPAddress(xrpAddress))
                {
                    await command.FollowupAsync("Invalid XRP address!", ephemeral: true);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckIfUniqueXRPAddress(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                var xrpAddress = (string)command.Data.Options.First().Value;
                var userAccount = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id);
                if (userAccount.GetXRPAddress() == xrpAddress || userAccount.GetPreviousXRPAddresses().Contains(xrpAddress)) return true;

                foreach (var account in Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccounts())
                {
                    if(account.GetXRPAddress() == xrpAddress || account.GetPreviousXRPAddresses().Contains(xrpAddress))
                    {
                        await command.FollowupAsync("This XRP address is already attached to a Discord user!", ephemeral: true);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckIfCorrectChannel(SocketSlashCommand command, string channelName)
        {
            try
            {
                if (command.Channel.Name == channelName || command.Channel.Name == "testing") return true;
                await command.FollowupAsync("Use in #" + channelName + " channel only!", ephemeral: true);
                return false;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckIfReceivedFaucetReward(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).ReceivedFaucetReward())
                {
                    await command.FollowupAsync("You have already received today's faucet reward!", ephemeral: true);
                    return true;
                }

                var receivedFaucetAddress = new List<string>();
                foreach (var account in Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccounts())
                {
                    receivedFaucetAddress.Add(account.GetReceivedFaucetRewardAtAddress());
                }

                if(receivedFaucetAddress.Contains(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetXRPAddress()))
                {
                    await command.FollowupAsync("You have already received today's faucet reward!", ephemeral: true);
                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).SetReceivedFaucetReward(true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return true;
            }
        }

        private async Task<bool> CheckForTimeBetweenCommands(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).ContainsAccount(userInfo.Id))
                {
                    if ((DateTime.UtcNow - Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetLastCommandTime()).TotalSeconds < Settings.MinCommandTime)
                    {
                        var nextCommandTime = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetLastCommandTime().AddSeconds(Settings.MinCommandTime) - DateTime.UtcNow;
                        string formattedTimeSpan = nextCommandTime.ToString(@"ss");
                        await command.FollowupAsync("Not enough time between commands. Please try again in " + formattedTimeSpan + " seconds!", ephemeral: true);
                        return false;
                    }

                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).SetLastCommandTime(DateTime.UtcNow);
                    Database.IsDirty = true;
                }
                else
                {
                    var account = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).AddAccount(userInfo.Id);
                    account.SetLastCommandTime(DateTime.UtcNow);
                    Database.IsDirty = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        private async Task<bool> CheckForTimeBetweenSwapOrTip(SocketSlashCommand command)
        {
            try
            {
                var userInfo = command.User as SocketGuildUser;
                if (Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).ContainsAccount(userInfo.Id))
                {
                    if ((DateTime.UtcNow - Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetLastTipTime()).TotalSeconds < Settings.MinTipTime)
                    {
                        var nextSwapTipTime = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).GetLastTipTime().AddSeconds(Settings.MinTipTime) - DateTime.UtcNow;
                        string formattedTimeSpan = nextSwapTipTime.ToString(@"ss");
                        await command.FollowupAsync("Swapping/tipping is available once every minute. Please try again in " + formattedTimeSpan + " seconds!", ephemeral: true);
                        return false;
                    }

                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).SetLastCommandTime(DateTime.UtcNow);
                    Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(userInfo.Id).SetLastTipTime(DateTime.UtcNow);
                    Database.IsDirty = true;
                }
                else
                {
                    var account = Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).AddAccount(userInfo.Id);
                    account.SetLastCommandTime(DateTime.UtcNow);
                    account.SetLastTipTime(DateTime.UtcNow);
                    Database.IsDirty = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }
    }
}
