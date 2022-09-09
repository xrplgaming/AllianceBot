using System;

namespace AllianceBot
{
    [Serializable]
    public enum DatabaseType : byte
    {
        Accounts,
        Settings,
        Threads,
        Tournament,
    }
}
