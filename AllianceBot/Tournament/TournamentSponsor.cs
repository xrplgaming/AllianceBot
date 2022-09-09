using Newtonsoft.Json;
using System;

namespace AllianceBot
{
    [Serializable]
    public class TournamentSponsor
    {
        [JsonProperty] private string _sponsorName;
        [JsonProperty] private string _sponsorUrl;
        [JsonProperty] private string _imageUrl;
        [JsonProperty] private string _description;

        public TournamentSponsor(string sponsor, string url, string image, string description)
        {
            _sponsorName = sponsor;
            _sponsorUrl = url;
            _imageUrl = image;
            _description = description;
        }

        public string GetSponsorName()
        {
            return _sponsorName;
        }

        public string GetSponsorUrl()
        {
            return _sponsorUrl;
        }

        public string GetSponsorImageUrl()
        {
            return _imageUrl;
        }

        public string GetDescription()
        {
            return _description;
        }

        public void UpdateSponsor(string sponsor, string url, string image, string description)
        {
            _sponsorName = sponsor;
            _sponsorUrl = url;
            _imageUrl = image;
            _description = description;
        }
    }
}
