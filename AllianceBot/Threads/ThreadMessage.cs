using Newtonsoft.Json;
using System;

namespace AllianceBot
{
    [Serializable]
    public class ThreadMessage
    {
        [JsonProperty] private ulong _threadMessageId;
        [JsonProperty] private ulong _threadMessageChannelId;
        [JsonProperty] private DateTime _threadMessageCreation;
        [JsonProperty] private ulong _threadMessageAuthor;
        [JsonProperty] private string _threadMessageContent;

        public ThreadMessage(ulong id, ulong author, string content)
        {
            _threadMessageId = id;
            _threadMessageChannelId = 0;
            _threadMessageCreation = DateTime.UtcNow;
            _threadMessageAuthor = author;
            _threadMessageContent = content;
        }

        public ulong GetThreadMessageChannelId()
        {
            return _threadMessageChannelId;
        }

        public ulong GetThreadMessageAuthor()
        {
            return _threadMessageAuthor;
        }

        public string GetThreadMessageContent()
        {
            return _threadMessageContent;
        }

        public void SetThreadMessageChannelId(ulong threadMessageChannelId)
        {
            _threadMessageChannelId = threadMessageChannelId;
        }
    }
}
