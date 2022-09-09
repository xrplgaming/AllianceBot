using Newtonsoft.Json;
using System;
using System.Text;

namespace AllianceBot
{
    [Serializable]
    public class TournamentReward
    {
        [JsonProperty] private string _topReward;
        [JsonProperty] private string _nextReward;
        [JsonProperty] private string _normalReward;

        public TournamentReward(string topReward, string nextReward, string normalReward)
        {
            _topReward = topReward;
            _nextReward = nextReward;
            _normalReward = normalReward;
        }

        public string GetTopReward()
        {
            return _topReward;
        }

        public string GetNextReward()
        {
            return _nextReward;
        }

        public string GetNormalReward()
        {
            return _normalReward;
        }

        public string GetRewardString()
        {
            var stringBuilder = new StringBuilder();

            if (_topReward == string.Empty && _nextReward == string.Empty && _normalReward == string.Empty)
            {
                stringBuilder.Append("None");
            }
            else
            {
                stringBuilder.Append(_topReward);
                stringBuilder.AppendLine();
                stringBuilder.Append(_nextReward);
                stringBuilder.AppendLine();
                stringBuilder.Append(_normalReward);
            }
            return stringBuilder.ToString();
        }
    }
}
