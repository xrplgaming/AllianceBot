using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AllianceBot
{
    [Serializable]
    public class DatabaseAccounts : DatabaseBase
    {
        [JsonProperty] private Dictionary<ulong, Account> _registeredUsers;

        public DatabaseAccounts()
        {
            Type = DatabaseType.Accounts;
            _registeredUsers = new Dictionary<ulong, Account>();
        }

        public Account AddAccount(ulong id)
        {
            var account = new Account(id, string.Empty);
            if (!_registeredUsers.ContainsKey(id)) _registeredUsers.Add(id, account);
            else _registeredUsers[id] = account;
            return account;
        }

        public bool ContainsAccount(ulong id)
        {
            return _registeredUsers.ContainsKey(id) ? true : false;
        }

        public Account GetAccount(ulong id)
        {
            return _registeredUsers.ContainsKey(id) ? _registeredUsers[id] : null;
        }

        public List<Account> GetAccounts()
        {
            var accountList = new List<Account>();
            foreach (var kvp in _registeredUsers) accountList.Add(kvp.Value);
            return accountList;
        }

        public void RegisterAccount(ulong id, string xrpAddress)
        {
            if (_registeredUsers.ContainsKey(id))
            {
                var user = _registeredUsers[id];
                user.SetXRPAddress(xrpAddress);
                user.SetIsRegistered(true);
            }
        }

        public void UpdateRegisteredUsers(Dictionary<ulong, Account> loadUsers)
        {
            _registeredUsers = new Dictionary<ulong, Account>(loadUsers);
        }

        public void UnregisterAccount(ulong id)
        {
            AddAccount(id);
        }
    }
}
