using Newtonsoft.Json;
using System;

namespace AllianceBot
{
    [Serializable]
    public class DatabaseSettings : DatabaseBase
    {
        [JsonProperty("TimeSinceLastMessage")] private DateTime _timeSinceLastMessage = DateTime.MinValue;
        [JsonProperty("SavedDayOfWeek")] private DayOfWeek _savedDayOfWeek = DateTime.UtcNow.DayOfWeek;

        public DatabaseSettings()
        {
            Type = DatabaseType.Settings;
        }

        public DateTime GetTimeSinceLastMessage()
        {
            return _timeSinceLastMessage;
        }

        public void SetTimeSinceLastMessage(DateTime time)
        {
            _timeSinceLastMessage = time;
        }

        public DayOfWeek GetSavedDayOfWeek()
        {
            return _savedDayOfWeek;
        }

        public void SetSavedDayOfWeek(DayOfWeek dayOfWeek)
        {
            _savedDayOfWeek = dayOfWeek;
        }
    }
}
