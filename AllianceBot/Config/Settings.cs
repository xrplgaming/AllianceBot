using Newtonsoft.Json.Linq;
using System.IO;

namespace AllianceBot
{
    public static class Settings
    {
        public static string RestClientAddress { get; set; }
        public static string ApiKey { get; set; }
        public static string ApiSecret { get; set; }
        public static string WebSocketUrl { get; set; }
        public static string RewardAddress { get; set; }
        public static string RewardSecret { get; set; }
        public static string CurrencyCode { get; set; }
        public static string IssuerAddress { get; set; }
        public static string STXCurrencyCode { get; set; }
        public static string STXIssuerAddress { get; set; }
        public static string USDCurrencyCode { get; set; }
        public static string USDIssuerAddress { get; set; }
        public static string GuildID { get; set; }
        public static string BotToken { get; set; }
        public static string FaucetTokenAmt { get; set; }
        public static string RewardTokenAmt { get; set; }
        public static string MaxTipTokenAmt { get; set; }
        public static string TipTokenAmt { get; set; }
        public static int AccountLinesThrottle { get; set; }
        public static int TxnThrottle { get; set; }
        public static decimal FeeMultiplier { get; set; }
        public static int MaximumFee { get; set; }
        public static decimal TransferFee { get; set; }
        public static double MinCommandTime { get; set; }
        public static double MinTipTime { get; set; }
        public static double MinFaucetTime { get; set; }
        public static double ThreadTimeInDays { get; set; }
        public static double IdleThreadTimeToDelete { get; set; }

        public static void Initialize()
        {
            string jsonConfig = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Config/Settings.json"));
            dynamic d = JObject.Parse(jsonConfig);
            RestClientAddress = d.RestClientAddress;
            ApiKey = d.ApiKey;
            ApiSecret = d.ApiSecret;
            WebSocketUrl = d.WebSocketURL;
            RewardAddress = d.RewardAddress;
            RewardSecret = d.RewardSecret;

            string currencyCodeVal = d.CurrencyCode.Value;
            if(currencyCodeVal.Length == 3) CurrencyCode = d.CurrencyCode.Value;
            else CurrencyCode = Utils.AddZeros(Utils.ConvertHex(d.CurrencyCode.Value));
            IssuerAddress = d.IssuerAddress;

            string stxCurrencyCodeVal = d.STXCurrencyCode.Value;
            if (stxCurrencyCodeVal.Length == 3) STXCurrencyCode = d.STXCurrencyCode.Value;
            else STXCurrencyCode = Utils.AddZeros(Utils.ConvertHex(d.STXCurrencyCode.Value));
            STXIssuerAddress = d.STXIssuerAddress;

            string usdCurrencyCodeVal = d.USDCurrencyCode.Value;
            if (usdCurrencyCodeVal.Length == 3) USDCurrencyCode = d.USDCurrencyCode.Value;
            else USDCurrencyCode = Utils.AddZeros(Utils.ConvertHex(d.USDCurrencyCode.Value));
            USDIssuerAddress = d.USDIssuerAddress;

            GuildID = d.GuildID;
            BotToken = d.BotToken;
            TransferFee = d.TransferFee;
            FaucetTokenAmt = d.FaucetTokenAmt;
            RewardTokenAmt = d.RewardTokenAmt;
            MaxTipTokenAmt = d.MaxTipTokenAmt;
            TipTokenAmt = d.TipTokenAmt;
            AccountLinesThrottle = d.AccountLinesThrottle;
            TxnThrottle = d.TxnThrottle;
            FeeMultiplier = d.FeeMultiplier;
            MaximumFee = d.MaximumFee;
            MinCommandTime = d.MinCommandTime;
            MinTipTime = d.MinTipTime;
            MinFaucetTime = d.MinFaucetTime;
            ThreadTimeInDays = d.ThreadTimeInDays;
            IdleThreadTimeToDelete = d.IdleThreadTimeToDelete;
        }
    }
}
