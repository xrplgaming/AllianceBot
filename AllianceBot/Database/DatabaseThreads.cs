using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AllianceBot
{
    [Serializable]
    public class DatabaseThreads : DatabaseBase
    {
        [JsonProperty] private Dictionary<ulong, Thread> _registeredThreads;
        [JsonProperty] private ulong _nextThreadId = 1;
        [JsonProperty] private ulong _categoryId;
        [JsonProperty] private List<ulong> _threadChannels;

        public DatabaseThreads()
        {
            Type = DatabaseType.Threads;
            _registeredThreads = new Dictionary<ulong, Thread>();
            _threadChannels = new List<ulong>();
        }

        public void SetCategoryId(ulong categoryId)
        {
            _categoryId = categoryId;      
        }

        public async Task<Thread> CreateThread(SocketGuildUser author, string header, string content, DiscordSocketClient client = null)
        {
            var newThread = new Thread(_nextThreadId, author.Id, header, content);
            _nextThreadId++;

            _registeredThreads.Add(_nextThreadId, newThread);
            if (client != null)
            {
                await LinkThreadToChannel(author, newThread, client);
                await UpdateThreadPositionInChannel(newThread, client);
            }

            return newThread;
        }

        private async Task LinkThreadToChannel(SocketGuildUser author, Thread linkedThread, DiscordSocketClient client = null)
        {
            var guild = client.GetGuild(ulong.Parse(Settings.GuildID));    
            
            var channel = await guild.CreateTextChannelAsync(linkedThread.GetThreadChannelName(), prop => prop.SlowModeInterval = 10);
            await channel.ModifyAsync(prop => prop.CategoryId = _categoryId);
            linkedThread.SetThreadChannelId(channel.Id);

            var embedBuiler = new EmbedBuilder()
                .WithAuthor(author.ToString(), author.GetAvatarUrl() ?? author.GetDefaultAvatarUrl())
                .WithTitle(linkedThread.GetThreadHeader())
                .WithDescription(linkedThread.GetThreadContent())
                .AddField("Yes Votes", "0", true)
                .AddField("No Votes", "0", true)
                .WithCurrentTimestamp()
                .WithColor(Color.Orange);

            var message = await channel.SendMessageAsync(null, false, embedBuiler.Build());
        }

        public async Task UpdateThreadPositionInChannel(Thread thread, DiscordSocketClient client = null)
        {
            var guild = client.GetGuild(ulong.Parse(Settings.GuildID));
            var category = guild.CategoryChannels.FirstOrDefault(category => category.Id == _categoryId);
            var categoryChannels = category.Channels.ToList();
            var indexOfThread = 0;

            for (int i = 0; i < categoryChannels.Count; i++)
            {
                if(categoryChannels[i].Id == thread.GetThreadChannelId())
                {
                    indexOfThread = i;
                    break;
                }
            }

            var threadChannel = categoryChannels[indexOfThread];
            categoryChannels.RemoveAt(indexOfThread);
            categoryChannels.Insert(0, threadChannel);

            for (int i = 0; i < categoryChannels.Count; i++)
            {
                await categoryChannels[i].ModifyAsync(x =>
                {
                    x.Position = i;
                });
            }
        }

        public bool ContainsThreadByChannelId(ulong channelId)
        {
            return _registeredThreads.Any(thread => thread.Value.GetThreadChannelId() == channelId);
        }

        public async Task<Thread> GetThreadByChannelId(ulong channelId)
        {
            if (ContainsThreadByChannelId(channelId)) return _registeredThreads.FirstOrDefault(thread => thread.Value.GetThreadChannelId() == channelId).Value;
            return null;
        }

        public async Task DeleteThread(ulong channelId)
        {
            var thread = await GetThreadByChannelId(channelId);
            if(thread != null && _registeredThreads.ContainsKey(thread.GetThreadId())) _registeredThreads.Remove(thread.GetThreadId());
        }

        public List<Thread> GetAllThreads()
        {
            var threads = new List<Thread>();
            foreach (var thread in _registeredThreads) threads.Add(thread.Value);
            return threads;
        }

        public async Task TestAllThreads(DiscordSocketClient client)
        {
            var threadsToDelete = new List<Thread>();
            foreach (var thread in _registeredThreads)
            {
                // HANDLE DELETE
                if (((DateTime.UtcNow - thread.Value.GetThreadUpdateTime()).TotalDays >= Settings.IdleThreadTimeToDelete && thread.Value.GetThreadExpirationTime() > DateTime.UtcNow) ||
                    thread.Value.GetThreadChannelId() == 0)
                {
                    threadsToDelete.Add(thread.Value);
                }

                // HANDLE REWARD
                if (DateTime.UtcNow > thread.Value.GetThreadExpirationTime() && !thread.Value.GetThreadRewarded())
                {
                    var yesVotes = 0;
                    var noVotes = 0;

                    var author = thread.Value.GetThreadAuthor();

                    var rewardedUsers = new List<ulong>();
                    foreach (var vote in thread.Value.GetVotes())
                    {
                        if (vote.Value) yesVotes++;
                        else noVotes++;
                        if(vote.Key != author) rewardedUsers.Add(vote.Key);
                    }

                    foreach (var threadMessage in thread.Value.GetThreadDiscussion())
                    {
                        if (!rewardedUsers.Contains(threadMessage.GetThreadMessageAuthor()) && threadMessage.GetThreadMessageAuthor() != author) rewardedUsers.Add(threadMessage.GetThreadMessageAuthor());
                    }

                    if(yesVotes > noVotes)
                    {
                        thread.Value.SetThreadRewarded(true);
                        var guild = client.GetGuild(ulong.Parse(Settings.GuildID));
                        var authorUser = guild.GetUser(author);
                        var embrsChannel = guild.TextChannels.FirstOrDefault(x => x.Name == "embrs");

                        await XRPL.SendRewardAsync(client, null, null, authorUser, "100", "EMBRS");
                        Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(authorUser.Id).ModEMBRSEarned(100);

                        for (int i = 0; i < rewardedUsers.Count; i++)
                        {
                            var user = guild.GetUser(rewardedUsers[i]);
                            await XRPL.SendRewardAsync(client, null, null, user, "10", "EMBRS");
                            Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(user.Id).ModEMBRSEarned(10);
                        }

                        var embedBuiler = new EmbedBuilder()
                            .WithAuthor(authorUser.ToString(), authorUser.GetAvatarUrl() ?? authorUser.GetDefaultAvatarUrl())
                            .WithTitle(thread.Value.GetThreadHeader())
                            .WithDescription(thread.Value.GetThreadContent())
                            .AddField("Successful Governance Topic", "The above topic has made it through 30 days of discussion and has a majority of yes votes! Rewards have been sent out to the topic author and anyone who discussed or voted in it.")
                            .WithCurrentTimestamp()
                            .WithColor(Color.Orange);

                        var message = await embrsChannel.SendMessageAsync(null, false, embedBuiler.Build());
                    }
                }
            }

            for (int i = 0; i < threadsToDelete.Count; i++)
            {
                if (threadsToDelete[i].GetThreadChannelId() != 0)
                {
                    var guild = client.GetGuild(ulong.Parse(Settings.GuildID));
                    var channel = guild.TextChannels.FirstOrDefault(x => x.Id == threadsToDelete[i].GetThreadChannelId());
                    await channel.DeleteAsync();
                }
                else
                {
                    if (_registeredThreads.ContainsKey(threadsToDelete[i].GetThreadId())) _registeredThreads.Remove(threadsToDelete[i].GetThreadId());
                }
            }

            foreach (var thread in _registeredThreads)
            {

            }
        }
    }
}
