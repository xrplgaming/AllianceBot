using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xrpl.Client;
using Xrpl.Client.Model;
using Xrpl.Client.Model.Account;
using Xrpl.Client.Model.Transaction;
using Xrpl.Client.Model.Transaction.Interfaces;
using Xrpl.Client.Model.Transaction.TransactionTypes;
using Xrpl.Client.Requests.Account;
using Xrpl.Client.Requests.Transaction;
using Xrpl.Wallet;

namespace AllianceBot
{
    public static class XRPL
    {
        private static Regex regex = new Regex("^[a-zA-Z][a-zA-Z1-9]*$");

        public static async Task SendRewardAsync(DiscordSocketClient discordClient, SocketSlashCommand command, SocketUser sourceUser, SocketUser destinationUser, string amount, string type,
                                                 bool tip = false, bool faucet = false)
        {
            try
            {
                IRippleClient client = new RippleClient(Settings.WebSocketUrl);
                client.Connect();
                uint sequence = await XRPL.GetLatestAccountSequence(client, Settings.RewardAddress);
                var f = await client.Fees();

                while (Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * Settings.FeeMultiplier)) > Settings.MaximumFee)
                {
                    await Program.Log(new LogMessage(LogSeverity.Warning, "XRPL Fees", "Waiting...fees too high. Current Open Ledger Fee: " + f.Drops.OpenLedgerFee));
                    await Program.Log(new LogMessage(LogSeverity.Warning, "XRPL Fees", "Fees configured based on fee multiplier: " + Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * Settings.FeeMultiplier))));
                    await Program.Log(new LogMessage(LogSeverity.Warning, "XRPL Fees", "Maximum Fee Configured: " + Settings.MaximumFee));
                    System.Threading.Thread.Sleep(Settings.AccountLinesThrottle * 1000);
                    f = await client.Fees();
                }

                int feeInDrops = Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * Settings.FeeMultiplier));

                var response = await XRPL.SendXRPPaymentAsync(client, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetXRPAddress(), sequence, feeInDrops, amount, type, Settings.TransferFee);

                //Transaction Node isn't Current. Wait for Network
                if (response.EngineResult == "noCurrent" || response.EngineResult == "noNetwork")
                {
                    int retry = 0;
                    while ((response.EngineResult == "noCurrent" || response.EngineResult == "noNetwork") && retry < 3)
                    {
                        //Throttle for node to catch up
                        System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                        response = await XRPL.SendXRPPaymentAsync(client, Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetXRPAddress(), sequence, feeInDrops, amount, type, Settings.TransferFee);
                        retry++;

                        if ((response.EngineResult == "noCurrent" || response.EngineResult == "noNetwork") && retry == 3)
                        {
                            if (command != null) await command.FollowupAsync("XRP network isn't responding. Please try again later!", ephemeral: true);
                            if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
                        }
                    }
                }
                else if (response.EngineResult == "tefPAST_SEQ")
                {
                    //Get new account sequence + try again
                    sequence = await XRPL.GetLatestAccountSequence(client, Settings.RewardAddress);
                    if (command != null) await command.FollowupAsync("Please try again!", ephemeral: true);
                    if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
                }
                else if (response.EngineResult == "telCAN_NOT_QUEUE_FEE")
                {
                    sequence = await XRPL.GetLatestAccountSequence(client, Settings.RewardAddress);
                    //Throttle, check fees and try again
                    System.Threading.Thread.Sleep(Settings.TxnThrottle * 3000);
                    if (command != null) await command.FollowupAsync("Please try again!", ephemeral: true);
                    if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
                }
                else if (response.EngineResult == "tesSUCCESS" || response.EngineResult == "terQUEUED")
                {
                    //Transaction Accepted by node successfully.
                    sequence++;

                    if (tip)
                    {
                        if (command != null)
                        {
                            var userInfo = command.User;
                            await command.FollowupAsync($"**{sourceUser.Username}#{sourceUser.Discriminator} sent {destinationUser.Username}#{destinationUser.Discriminator} a tip of {amount} EMBRS!**");
                        }
                    }
                    else if (faucet)
                    {
                        if (command != null)
                        {
                            var userInfo = command.User;
                            await command.FollowupAsync($"Faucet payout of " + amount + " EMBRS complete!", ephemeral: true);
                        }
                    }
                    else
                    {
                        if (command != null)
                        {
                            var userInfo = command.User;
                            await command.FollowupAsync($"Tournament reward of " + amount + " " + type + " complete! Congratulations!", ephemeral: true);
                        }
                    }
                }
                else if (response.EngineResult == "tecPATH_DRY" || response.EngineResult == "tecDST_TAG_NEEDED")
                {
                    //Trustline was removed or Destination Tag needed for address
                    if (command != null) await command.FollowupAsync(type + " trustline is not set!", ephemeral: true);
                    if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
                    sequence++;
                }
                else
                {
                    //Failed
                    if (command != null) await command.FollowupAsync("Transaction failed!", ephemeral: true);
                    if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
                    sequence++;
                }

                client.Disconnect();
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                if (tip) Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).SetLastTipTime(Database.GetDatabase<DatabaseAccounts>(DatabaseType.Accounts).GetAccount(destinationUser.Id).GetLastTipTime().AddMinutes(-1));
            }
        }

        public static async Task<Submit> SendXRPPaymentAsync(IRippleClient client, string destinationAddress, uint sequence, int feeInDrops, string amount, string type, decimal transferFee = 0)
        {
            try
            {
                IPaymentTransaction paymentTransaction = new PaymentTransaction
                {
                    Account = Settings.RewardAddress,
                    Destination = destinationAddress,
                    Amount = new Currency { CurrencyCode = (type == "EMBRS") ? Settings.CurrencyCode : Settings.STXCurrencyCode, Issuer = (type == "EMBRS") ? Settings.IssuerAddress : Settings.STXIssuerAddress, Value = amount },
                    Sequence = sequence,
                    Fee = new Currency { CurrencyCode = "XRP", ValueAsNumber = feeInDrops }
                };
                if (transferFee > 0)
                {
                    paymentTransaction.SendMax = new Currency { CurrencyCode = (type == "EMBRS") ? Settings.CurrencyCode : Settings.STXCurrencyCode, Issuer = (type == "EMBRS") ? Settings.IssuerAddress : Settings.STXIssuerAddress, Value = (amount + (Convert.ToDecimal(amount) * (transferFee / 100))).ToString() };
                }

                TxSigner signer = TxSigner.FromSecret(Settings.RewardSecret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(paymentTransaction.ToJson()));

                SubmitBlobRequest request = new SubmitBlobRequest()
                {
                    TransactionBlob = signedTx.TxBlob
                };

                Submit result = await client.SubmitTransactionBlob(request);

                return result;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                throw new Exception(ex.Message);
            }
        }

        public static async Task<uint> GetLatestAccountSequence(IRippleClient client, string account)
        {
            try
            {
                AccountInfo accountInfo = await client.AccountInfo(account);
                return accountInfo.AccountData.Sequence;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static async Task<BookOfferReturnObj> GetBookOffers(IRippleClient client, string from, string to)
        {
            BookOfferReturnObj returnObj = new();

            try
            {
                Currency fromCurrency = null;
                Currency toCurrency = null;

                if ((from.ToLower() == "embrs" || from.ToLower() == "embers" ||
                     to.ToLower() == "embrs" || to.ToLower() == "embers") &&
                     (from.ToLower() == "usd" || to.ToLower() == "usd")) // MUST USE INDIRECT METHOD UNTIL ORDER BOOK EXISTS IN FUTURE
                {
                    var indirectResult = await GetBookOffers(client, "usd", "xrp");
                    var indirectMidPrice = indirectResult.Midprice;

                    fromCurrency = new Currency
                    {
                        CurrencyCode = Settings.CurrencyCode,
                        Issuer = Settings.IssuerAddress
                    };

                    toCurrency = new Currency();

                    BookOffersRequest request1 = new()
                    {
                        TakerGets = fromCurrency,
                        TakerPays = toCurrency
                    };

                    BookOffersRequest request2 = new()
                    {
                        TakerGets = toCurrency,
                        TakerPays = fromCurrency
                    };

                    var offers = await client.BookOffers(request1);
                    System.Threading.Thread.Sleep(Settings.TxnThrottle * 1000);
                    var offers2 = await client.BookOffers(request2);

                    decimal? lowestBid = 100000;
                    for (int i = offers.Offers.Count - 1; i > 0; i--)
                    {
                        var value = offers.Offers[i].TakerPays.ValueAsXrp / offers.Offers[i].TakerGets.ValueAsNumber;
                        if (value < lowestBid) lowestBid = value;
                    }

                    decimal? highestAsk = 0;
                    for (int i = 0; i < offers2.Offers.Count; i++)
                    {
                        var value = offers2.Offers[i].TakerGets.ValueAsXrp / offers2.Offers[i].TakerPays.ValueAsNumber;
                        if (value > highestAsk) highestAsk = value;
                    }

                    var midPrice = ((lowestBid) + (highestAsk)) / 2;
                    returnObj.Midprice = midPrice / indirectMidPrice;
                }
                else
                {
                    if (from.ToLower() == "embrs" || from.ToLower() == "embers" ||
                        to.ToLower() == "embrs" || to.ToLower() == "embers")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.CurrencyCode,
                            Issuer = Settings.IssuerAddress
                        };

                        if (from.ToLower() == "xrp" || to.ToLower() == "xrp") // EMBRS/XRP
                        {
                            toCurrency = new Currency();
                        }
                        else if (from.ToLower() == "usd" || to.ToLower() == "usd") // EMBRS/USD
                        {
                            toCurrency = new Currency
                            {
                                CurrencyCode = Settings.USDCurrencyCode,
                                Issuer = Settings.USDIssuerAddress
                            };
                        }
                    }
                    else if (from.ToLower() == "stx" || to.ToLower() == "stx")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.STXCurrencyCode,
                            Issuer = Settings.STXIssuerAddress
                        };

                        if (from.ToLower() == "xrp" || to.ToLower() == "xrp") // STX/XRP
                        {
                            toCurrency = new Currency();
                        }
                    }
                    else if (from.ToLower() == "usd" || to.ToLower() == "usd")
                    {
                        fromCurrency = new Currency
                        {
                            CurrencyCode = Settings.USDCurrencyCode,
                            Issuer = Settings.USDIssuerAddress
                        };

                        if (from.ToLower() == "xrp" || to.ToLower() == "xrp") // USD/XRP
                        {
                            toCurrency = new Currency();
                        }
                    }

                    BookOffersRequest request1 = new()
                    {
                        TakerGets = fromCurrency,
                        TakerPays = toCurrency
                    };

                    BookOffersRequest request2 = new()
                    {
                        TakerGets = toCurrency,
                        TakerPays = fromCurrency
                    };

                    var offers = await client.BookOffers(request1);
                    System.Threading.Thread.Sleep(Settings.TxnThrottle * 1000);
                    var offers2 = await client.BookOffers(request2);

                    decimal? lowestBid = 100000;
                    for (int i = offers.Offers.Count - 1; i > 0; i--)
                    {
                        var value = offers.Offers[i].TakerPays.ValueAsXrp / offers.Offers[i].TakerGets.ValueAsNumber;
                        if (value < lowestBid) lowestBid = value;
                    }

                    decimal? highestAsk = 0;
                    for (int i = 0; i < offers2.Offers.Count; i++)
                    {
                        var value = offers2.Offers[i].TakerGets.ValueAsXrp / offers2.Offers[i].TakerPays.ValueAsNumber;
                        if (value > highestAsk) highestAsk = value;
                    }

                    var midPrice = ((lowestBid) + (highestAsk)) / 2;
                    returnObj.Midprice = midPrice;
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
            }

            return returnObj;
        }

        public struct BookOfferReturnObj
        {
            public decimal? Midprice { get; set; }
        }

        public static async Task<decimal> ReturnAccountBalance(IRippleClient client, string account)
        {
            try
            {
                AccountInfo accountInfo = await client.AccountInfo(account);
                return accountInfo.AccountData.Balance.ValueAsXrp.HasValue ? accountInfo.AccountData.Balance.ValueAsXrp.Value : 0;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return 0;
            }
        }

        public static async Task<TrustLineReturnObj> ReturnTrustLines(IRippleClient client, string userAddress, string marker)
        {
            TrustLineReturnObj returnObj = new TrustLineReturnObj();
            AccountLinesRequest req = new AccountLinesRequest(userAddress);

            req.Limit = 400;
            if (marker != "")
            {
                req.Marker = marker;
            }

            AccountLines accountLines = await client.AccountLines(req);
            if (accountLines.Marker != null)
            {
                marker = accountLines.Marker.ToString();
            }
            else
            {
                marker = "";
            }
            
            foreach (TrustLine line in accountLines.TrustLines)
            {
                if (line.Currency == Settings.CurrencyCode)
                {
                    try
                    {
                        returnObj.EMBRSBalance = Convert.ToDecimal(line.Balance);
                    }
                    catch (Exception ex)
                    {
                        returnObj.EMBRSBalance = 0;
                        await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                    }
                }
                else if (line.Currency == Settings.STXCurrencyCode)
                {
                    try
                    {
                        returnObj.STXBalance = Convert.ToDecimal(line.Balance);
                    }
                    catch (Exception ex)
                    {
                        returnObj.STXBalance = 0;
                        await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                    }
                }
                else if (line.Currency == Settings.USDCurrencyCode)
                {
                    try
                    {
                        returnObj.USDBalance = Convert.ToDecimal(line.Balance);
                    }
                    catch (Exception ex)
                    {
                        returnObj.USDBalance = 0;
                        await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                    }
                }
            }

            returnObj.Marker = marker;
            return returnObj;
        }

        public static async Task<bool> ReturnValidXRPAddress(string account)
        {
            try
            {
                if (account.StartsWith('r') && !account.Contains('O') && !account.Contains('I') && !account.Contains('l') && account.Length >= 25 && account.Length <= 35 && regex.IsMatch(account)) return true;
                else return false;
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, ex.Source, ex.Message, ex));
                return false;
            }
        }

        public struct TrustLineReturnObj
        {
            public decimal EMBRSBalance { get; set; }
            public decimal STXBalance { get; set; }
            public decimal USDBalance { get; set; }
            public string Marker { get; set; }
        }
    }
}
