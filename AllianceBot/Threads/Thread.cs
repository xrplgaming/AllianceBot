using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AllianceBot
{
    [Serializable]
    public class Thread
    {
        [JsonProperty] private ulong _threadId;
        [JsonProperty] private ulong _threadChannelId;
        [JsonProperty] private DateTime _threadCreation;
        [JsonProperty] private DateTime _threadExpiration;
        [JsonProperty] private DateTime _threadUpdated;
        [JsonProperty] private ulong _threadAuthor;
        [JsonProperty] private string _threadHeader;
        [JsonProperty] private string _threadContent;
        [JsonProperty] private Dictionary<ulong, bool> _threadVotes;
        [JsonProperty] private List<ThreadMessage> _threadDiscussion;
        [JsonProperty] private ulong _nextThreadMessageId = 1;
        [JsonProperty] private bool _threadRewarded;

        private readonly Regex regex = new Regex("[^a-zA-Z0-9 _]");

        public Thread(ulong id, ulong author, string header, string content)
        {
            _threadId = id;
            _threadChannelId = 0;
            _threadCreation = DateTime.UtcNow;
            _threadExpiration = DateTime.UtcNow.AddDays(Settings.ThreadTimeInDays);
            _threadUpdated = DateTime.UtcNow;
            _threadAuthor = author;
            _threadHeader = header;
            _threadContent = content;
            _threadVotes = new Dictionary<ulong, bool>();
            _threadDiscussion = new List<ThreadMessage>();
            _threadRewarded = false;
        }

        public ulong GetThreadId()
        {
            return _threadId;
        }

        public ulong GetThreadChannelId()
        {
            return _threadChannelId;
        }

        public DateTime GetThreadExpirationTime()
        {
            return _threadExpiration;
        }

        public DateTime GetThreadUpdateTime()
        {
            return _threadUpdated;
        }

        public void SetThreadChannelId(ulong threadChannelId)
        {
            _threadChannelId = threadChannelId;
        }

        public string GetThreadChannelName()
        {
            var channelName = _threadHeader;
            channelName = regex.Replace(channelName, string.Empty);
            channelName = channelName.Replace(' ', '-').ToLower();
            channelName = channelName.Substring(0, (channelName.Length < 32) ? channelName.Length : 32);
            return channelName.ToLower();
        }

        public string GetThreadHeader()
        {
            return _threadHeader;
        }

        public ulong GetThreadAuthor()
        {
            return _threadAuthor;
        }

        public string GetThreadContent()
        {
            return _threadContent;
        }

        public bool GetThreadRewarded()
        {
            return _threadRewarded;
        }

        public void SetThreadRewarded(bool rewarded)
        {
            _threadRewarded = rewarded;
        }

        public List<ThreadMessage> GetThreadDiscussion()
        {
            return _threadDiscussion;
        }

        public Dictionary<ulong, bool> GetVotes()
        {
            return _threadVotes;
        }

        public void SetVote(ulong author, bool vote)
        {
            if (!_threadVotes.ContainsKey(author)) _threadVotes.Add(author, vote);
            else _threadVotes[author] = vote;
        }

        public ThreadMessage AddThreadMessage(ulong author, string description)
        {
            var newThreadMessage = new ThreadMessage(_nextThreadMessageId, author, description);
            _nextThreadMessageId++;
            _threadDiscussion.Add(newThreadMessage);
            return newThreadMessage;
        }

        public uint GetYesVotes()
        {
            uint yesVotes = 0;

            foreach (var vote in _threadVotes)
            {
                if (vote.Value) yesVotes++;
            }

            return yesVotes;
        }

        public uint GetNoVotes()
        {
            uint noVotes = 0;

            foreach (var vote in _threadVotes)
            {
                if (!vote.Value) noVotes++;
            }

            return noVotes;
        }

        public bool ContainsThreadMessageByMessageId(ulong messageId)
        {
            return _threadDiscussion.Any(threadMessage => threadMessage.GetThreadMessageChannelId() == messageId);
        }

        public ThreadMessage GetThreadMessageByMessageId(ulong messageId)
        {
            if (ContainsThreadMessageByMessageId(messageId)) return _threadDiscussion.FirstOrDefault(threadMessage => threadMessage.GetThreadMessageChannelId() == messageId);
            return null;
        }

        public void DeleteThreadMessage(ulong messageId)
        {
            var threadMessage = GetThreadMessageByMessageId(messageId);
            if (threadMessage != null && _threadDiscussion.Contains(threadMessage)) _threadDiscussion.Remove(threadMessage);
        }
    }
}
