using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AllianceBot
{
    [Serializable]
    public class Account
    {
        [JsonProperty("Id")] private ulong _id;
        [JsonProperty("XrpAddress")] private string _xrpAddress;
        [JsonProperty("EMBRSEarned")] private float _embrsEarned;
        [JsonProperty("IsRegistered")] private bool _isRegistered;
        [JsonProperty("InTournament")] private bool _inTournament;
        [JsonProperty("TournamentWinner")] private bool _tournamentWinner;
        [JsonProperty("LastCommandTime")] private DateTime _lastCommandTime;
        [JsonProperty("LastTipTime")] private DateTime _lastTipTime;
        [JsonProperty("ReceivedFaucetReward")] private bool _receivedFaucetReward;
        [JsonProperty("ReceivedFaucetRewardAtAddress")] private string _receivedFaucetRewardAtAddress;
        [JsonProperty("ChangedXRPAddress")] private bool _changedXRPAddress;
        [JsonProperty("PreviousXRPAddresses")] private List<string> _previousXRPAddresses;

        public Account(ulong id, string xrpAddress)
        {
            _id = id;
            _xrpAddress = xrpAddress;
            _embrsEarned = 0;
            _isRegistered = false;
            _inTournament = false;
            _tournamentWinner = false;
            _lastCommandTime = DateTime.UtcNow;
            _lastTipTime = DateTime.MinValue;
            _receivedFaucetReward = false;
            _receivedFaucetRewardAtAddress = string.Empty;
            _changedXRPAddress = false;
            _previousXRPAddresses = new List<string>();
            _previousXRPAddresses.Add(xrpAddress);
        }

        public ulong GetId()
        {
            return _id;
        }

        public string GetXRPAddress()
        {
            return _xrpAddress;
        }

        public void SetXRPAddress(string xrpAddress)
        {
            _xrpAddress = xrpAddress;
            if (_receivedFaucetReward) _changedXRPAddress = true;
            if(!_previousXRPAddresses.Contains(xrpAddress)) _previousXRPAddresses.Add(xrpAddress);
        }

        public List<string> GetPreviousXRPAddresses()
        {
            return _previousXRPAddresses;
        }

        public bool GetIsRegistered()
        {
            return _isRegistered;
        }

        public void SetIsRegistered(bool registered)
        {
            _isRegistered = registered;
        }

        public float GetEMBRSEarned()
        {
            return _embrsEarned;
        }

        public void ModEMBRSEarned(float amount)
        {
            _embrsEarned += amount;
        }

        public bool GetInTournament()
        {
            return _inTournament;
        }

        public void SetInTournament(bool inTournament)
        {
            _inTournament = inTournament;
        }

        public bool GetTournamentWinner()
        {
            return _tournamentWinner;
        }

        public void SetTournamentWinner(bool tournamentWinner)
        {
            _tournamentWinner = tournamentWinner;
        }

        public bool ReceivedFaucetReward()
        {
            return _receivedFaucetReward;
        }

        public void SetReceivedFaucetReward(bool faucetReward)
        {
            _receivedFaucetReward = faucetReward;
            if (faucetReward) _receivedFaucetRewardAtAddress = _xrpAddress;
            else _receivedFaucetRewardAtAddress = string.Empty;
        }

        public string GetReceivedFaucetRewardAtAddress()
        {
            return _receivedFaucetRewardAtAddress;
        }

        public bool ChangedXRPAddress()
        {
            return _changedXRPAddress;
        }

        public void SetChangedXRPAddress(bool changed)
        {
            _changedXRPAddress = changed;
        }

        public void ResetTournament()
        {
            _inTournament = false;
            _tournamentWinner = false;
        }

        public DateTime GetLastCommandTime()
        {
            return _lastCommandTime;
        }

        public void SetLastCommandTime(DateTime commandTime)
        {
            _lastCommandTime = commandTime;
        }

        public DateTime GetLastTipTime()
        {
            return _lastTipTime;
        }

        public void SetLastTipTime(DateTime tipTime)
        {
            _lastTipTime = tipTime;
        }
    }
}
