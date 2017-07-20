﻿using Neo.Core;
using Neo.IO.Json;
using Neo.Wallets;
using Neo.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.RPC
{
    internal class RpcServerWithWallet : RpcServer
    {
        public RpcServerWithWallet(LocalNode localNode)
            : base(localNode)
        {
        }

        protected override JObject Process(string method, JArray _params)
        {
            switch (method)
            {
                case "getbalance":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                    {
                        UInt256 assetId = UInt256.Parse(_params[0].AsString());
                        IEnumerable<Coin> coins = Program.Wallet.GetCoins().Where(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(assetId));
                        JObject json = new JObject();
                        json["balance"] = coins.Sum(p => p.Output.Value).ToString();
                        json["confirmed"] = coins.Where(p => p.State.HasFlag(CoinState.Confirmed)).Sum(p => p.Output.Value).ToString();
                        return json;
                    }
                case "getunclaimedgas":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                    {
                        Coins coins = new Coins(Program.Wallet, LocalNode);
                        JObject json = new JObject();
                        json["unavailable"] = coins.UnavailableBonus().ToString();
                        json["available"] = coins.AvailableBonus().ToString();
                        return json;
                    }
                case "sendtoaddress":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        UInt256 assetId = UInt256.Parse(_params[0].AsString());
                        UInt160 scriptHash = Wallet.ToScriptHash(_params[1].AsString());
                        Fixed8 value = Fixed8.Parse(_params[2].AsString());
                        Fixed8 fee = _params.Count >= 4 ? Fixed8.Parse(_params[3].AsString()) : Fixed8.Zero;
                        UInt160 change_address = _params.Count >= 5 ? Wallet.ToScriptHash(_params[4].AsString()) : null;
                        if (value <= Fixed8.Zero)
                            throw new RpcException(-32602, "Invalid params");
                        ContractTransaction tx = Program.Wallet.MakeTransaction(new ContractTransaction
                        {
                            Outputs = new[]
                            {
                                new TransactionOutput
                                {
                                    AssetId = assetId,
                                    Value = value,
                                    ScriptHash = scriptHash
                                }
                            }
                        }, change_address: change_address, fee: fee);
                        if (tx == null)
                            throw new RpcException(-300, "Insufficient funds");
                        SignatureContext context = new SignatureContext(tx);
                        Program.Wallet.Sign(context);
                        if (context.Completed)
                        {
                            tx.Scripts = context.GetScripts();
                            Program.Wallet.SaveTransaction(tx);
                            LocalNode.Relay(tx);
                            return tx.ToJson();
                        }
                        else
                        {
                            return context.ToJson();
                        }
                    }
                case "getnewaddress":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        KeyPair key = Program.Wallet.CreateKey();
                        Contract contract = Program.Wallet.GetContracts(key.PublicKeyHash).First(p => p.IsStandard);
                        return contract.Address;
                    }

                case "claimgas":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                    {
                        Coins coins = new Coins(Program.Wallet, LocalNode);

                        ClaimTransaction tx = coins.Claim();
                        if (tx == null)
                        {
                            throw new RpcException(-401, "Could not claim gas");
                        }
                        else
                        {
                            return tx.ToJson();
                        }
                    }

                case "dumpprivkey":
                    if (Program.Wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        UInt160 scriptHash = Wallet.ToScriptHash(_params[0].AsString());
                        KeyPair key = Program.Wallet.GetKeyByScriptHash(scriptHash);
                        return key.Export();
                    }
                default:
                    return base.Process(method, _params);
            }
        }
    }
}
