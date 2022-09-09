using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AllianceBot
{
    public static class Database
    {
        public static bool IsDirty;
        private static Dictionary<DatabaseType, DatabaseBase> _tables;

        public static async Task Initialize()
        {
            _tables = new Dictionary<DatabaseType, DatabaseBase>();
            _tables.Add(DatabaseType.Accounts, new DatabaseAccounts());
            _tables.Add(DatabaseType.Settings, new DatabaseSettings());
            _tables.Add(DatabaseType.Threads, new DatabaseThreads());
            _tables.Add(DatabaseType.Tournament, new DatabaseTournament());
            await Read();
            IsDirty = false;
        }

        public static T GetDatabase<T>(DatabaseType type) where T : DatabaseBase
        {
            if (_tables.ContainsKey(type))
            {
                return (T)_tables[type];
            }

            return null;
        }

        public static async Task Write()
        {
            try
            {
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                var result = JsonConvert.SerializeObject(_tables, settings);
                await File.WriteAllTextAsync("EMBRSDatabase2.dat", result);
                await Program.Log(new LogMessage(LogSeverity.Info, "Database Write", "Complete"));
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }

        private static async Task Read()
        {
            try
            {
                if (File.Exists("EMBRSDatabase2.dat"))
                {
                    string value = await File.ReadAllTextAsync("EMBRSDatabase2.dat");
                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                    _tables = JsonConvert.DeserializeObject<Dictionary<DatabaseType, DatabaseBase>>(value, settings);
                    if (GetDatabase<DatabaseTournament>(DatabaseType.Tournament) == null)
                    {
                        _tables.Add(DatabaseType.Tournament, new DatabaseTournament());
                    }
                    await Program.Log(new LogMessage(LogSeverity.Info, "Database Read", "Complete"));
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }
        }
    }
}
