using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.DAL.DTO;
using BlockExplorerAPI.Services.Models;
using BlockExplorerAPI.Utils;
using BlockExplorerAPI.Services.Models.CreateTransaction;
using CreateTransactrion = BlockExplorerAPI.Models.Requests.CreateTransactionRequest;
using RawTransaction = BlockExplorerAPI.Services.Models.RawTransaction;
using BlockExplorerAPI.Validation;
using BlockExplorerAPI.DAL;
using BlockExplorerAPI.Services.Abstract;
using NLog;

namespace BlockExplorerAPI.Services
{
    public class TransactionService
    {
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TwoLevelTransactionRepository transactionRepository;
        private readonly CryptoRpcClient rpcClient;
        private readonly AddressService addressService;
        private readonly IMessageDeliveryService messageDeliveryService;

        public IExtendedTransactionRepository MemPool { get { return transactionRepository.MemoryPool; } }

        private const decimal CreateTransactionFee = 0.001M;

        public TransactionService(TwoLevelTransactionRepository transactionRepository, CryptoRpcClient rpcClient, AddressService addressService, IMessageDeliveryService messageDeliveryService)
        {
            if (transactionRepository == null)
                throw new ArgumentNullException("transactionRepository");
            if (rpcClient == null)
                throw new ArgumentNullException("rpcClient");
            if (addressService == null)
                throw new ArgumentNullException("addressService");

            this.transactionRepository = transactionRepository;
            this.rpcClient = rpcClient;
            this.addressService = addressService;
            this.messageDeliveryService = messageDeliveryService;
        }

        public void SaveMemPoolTx(JToken tx)
        {
            if (transactionRepository.MemoryPool.Contains(tx["txid"].Value<string>()))
                return;

            var transaction = ParseRawTransaction(tx);
            transactionRepository.MemoryPool.Add(transaction);
            messageDeliveryService?.NewTransaction(new TransactionModel(transaction));
        }

        public void ClearMemPool()
        {
            transactionRepository.MemoryPool.Clear();
        }

        public List<TransactionModel> SaveAllBlockTransactions(JToken block)
        {
            var transactionModels = new List<TransactionModel>();
            int blockNumber = block["height"].Value<int>();
            string blockHash = block["hash"].Value<string>();
            int confirmations = block["confirmations"].Value<int>();
            var txids = block["tx"].Values<string>();
            var rawTransactions = rpcClient.GetRawTransactions(txids);
            var blockTransactions = new Dictionary<string, Transaction>();
            foreach (var tx in rawTransactions)
            {
                var transaction = ParseRawTransaction(tx, blockTransactions);
                transaction.BlockNumber = blockNumber;
                transaction.BlockHash = blockHash;
                blockTransactions.Add(transaction.Id, transaction);
                transactionModels.Add(new TransactionModel(transaction) { Confirmations = confirmations });
            }
            transactionRepository.Block.AddRange(blockTransactions.Values);
            return transactionModels;
        }

        public TransactionModel Find(string txid)
        {
            var tx = transactionRepository.Find(txid);
            if (tx == null)
                return null;

            var model = new TransactionModel(tx);
            if (tx.BlockHash != null)
            {
                var block = rpcClient.GetBlock(tx.BlockHash);
                model.Confirmations = block["confirmations"].Value<int>();
            }
            return model;
        }

        public List<TransactionModel> GetBlockTransactions(JToken block)
        {
            var transactionModels = new List<TransactionModel>();
            string blockHash = block["hash"].Value<string>();
            var transactions = transactionRepository.FindByBlock(blockHash);
            if (transactions.Count != 0)
            {
                int confirmations = block["confirmations"].Value<int>();
                foreach (var transaction in transactions)
                {
                    transactionModels.Add(new TransactionModel(transaction) { Confirmations = confirmations });
                }
            }
            return transactionModels;
        }

        public bool BlockExists(string blockHash)
        {
            var transactions = transactionRepository.FindByBlock(blockHash);
            return transactions.Count != 0;
        }

        public List<TransactionItemModel> GetLast(int count)
        {
            var transactionModels = new List<TransactionItemModel>();
            var transactions = transactionRepository.GetLast(count);
            foreach (var transaction in transactions)
            {
                transactionModels.Add(new TransactionItemModel(transaction));
            }
            return transactionModels;
        }

        public int GetLastBlockNumber()
        {
            return transactionRepository.Block.GetBlockNumber();
        }

        public string CreateTransaction(CreateTransactrion transaction)
        {
            if (transaction.Value <= 0)
                throw new Exception("Negative or zero value");

            if (!AddressValidator.Validate(transaction.Address))
                throw new Exception("Invalid address");

            if (!AddressValidator.Validate(transaction.DestinationAddress))
                throw new Exception("Invalid destination address");

            if (!ValidatePrivateKey(transaction.PrivateKey))
                throw new Exception("Invalid private key");

            // Fetch address model
            var unspentOutputs = addressService.FindUnspend(transaction.Address);
            List<AddressModel.UnspentOutputModel> voutForUse = new List<AddressModel.UnspentOutputModel>();            
            decimal amount = 0M;
            foreach (var output in unspentOutputs)
            {
                amount += output.amount;
                voutForUse.Add(output);
                if (amount >= transaction.Value + CreateTransactionFee)
                {
                    List<JToken> rawTransactions = rpcClient.GetRawTransactions(voutForUse.Select(u => u.txid).Distinct());
                    Dictionary<string, RawTransaction> rawTxDict = rawTransactions.Select(tx => tx.ToObject<RawTransaction>()).ToDictionary(tx => tx.txid, tx => tx);
                    // Create transaction
                    var createRequest = new CreateTransactionRpcRequests();
                    var signRequest = new SignTransactionRpcRequests();
                    foreach (var vout in voutForUse)
                    {
                        vout.raw = rawTxDict[vout.txid];
                        var rawvout = vout.raw.vout.Where(x => x.n == vout.vout).ToArray()[0];
                        vout.scriptPubKey = rawvout.scriptPubKey.hex;

                        createRequest.pairs.Add(new CreateTransactionRpcRequests.VoutPair()
                        {
                            txid = vout.txid,
                            vout = vout.vout
                        });

                        signRequest.vout.Add(new SignTransactionRpcRequests.VoutKeyTriple()
                        {
                            txid = vout.txid,
                            vout = vout.vout,
                            scriptPubKey = vout.scriptPubKey
                        });
                    }

                    createRequest.destination.Add(transaction.DestinationAddress, transaction.Value);
                    decimal change = amount - transaction.Value - CreateTransactionFee;
                    if (change > 0)
                        createRequest.destination.Add(output.address, change);

                    var unconfirmedHash = rpcClient.CreateTransaction(createRequest.toJson());
                    if (unconfirmedHash == null)
                        throw new Exception("Create transaction error");

                    signRequest.hash = unconfirmedHash;
                    signRequest.keySet = new List<string>() { transaction.PrivateKey };
                    var confirmedHash = rpcClient.SignTransaction(signRequest.toJson()).ToObject<SignTransactionRpcResponse>();
                    if(!confirmedHash.complete)
                        throw new Exception("Sign transaction error");

                    var finalTxHash = rpcClient.SendTransaction(confirmedHash.hex);
                    if (finalTxHash == null)
                        throw new Exception("Send transaction error");

                    return finalTxHash;
                }
            }

            throw new Exception("Insufficient funds");
        }

        public SignTransactionRpcRequests CreateUnsignedTransaction(CreateTransactrion transaction)
        {
            if (transaction.Value <= 0)
                throw new Exception("Negative or zero value");

            if (!AddressValidator.Validate(transaction.Address))
                throw new Exception("Invalid address");

            if (!AddressValidator.Validate(transaction.DestinationAddress))
                throw new Exception("Invalid destination address");

            // Fetch address model
            var unspentOutputs = addressService.FindUnspend(transaction.Address);
            List<AddressModel.UnspentOutputModel> voutForUse = new List<AddressModel.UnspentOutputModel>();
            decimal amount = 0M;
            foreach (var output in unspentOutputs)
            {
                amount += output.amount;
                voutForUse.Add(output);
                if (amount >= transaction.Value + CreateTransactionFee)
                {
                    List<JToken> rawTransactions = rpcClient.GetRawTransactions(voutForUse.Select(u => u.txid).Distinct());
                    Dictionary<string, RawTransaction> rawTxDict = rawTransactions.Select(tx => tx.ToObject<RawTransaction>()).ToDictionary(tx => tx.txid, tx => tx);
                    // Create transaction
                    var createRequest = new CreateTransactionRpcRequests();
                    var signRequest = new SignTransactionRpcRequests();
                    foreach (var vout in voutForUse)
                    {
                        vout.raw = rawTxDict[vout.txid];
                        var rawvout = vout.raw.vout.Where(x => x.n == vout.vout).ToArray()[0];
                        vout.scriptPubKey = rawvout.scriptPubKey.hex;

                        createRequest.pairs.Add(new CreateTransactionRpcRequests.VoutPair()
                        {
                            txid = vout.txid,
                            vout = vout.vout
                        });

                        signRequest.vout.Add(new SignTransactionRpcRequests.VoutKeyTriple()
                        {
                            txid = vout.txid,
                            vout = vout.vout,
                            scriptPubKey = vout.scriptPubKey
                        });
                    }

                    createRequest.destination.Add(transaction.DestinationAddress, transaction.Value);
                    decimal change = amount - transaction.Value - CreateTransactionFee;
                    if (change > 0)
                        createRequest.destination.Add(output.address, change);

                    var unconfirmedHash = rpcClient.CreateTransaction(createRequest.toJson());
                    if (unconfirmedHash == null)
                        throw new Exception("Create transaction error");

                    signRequest.hash = unconfirmedHash;
                    return signRequest;
                }
            }

            throw new Exception("Insufficient funds");
        }

        public string SendRawTransaction(SignTransactionRpcResponse signedTransaction)
        {
            try
            {
                signedTransaction.hex.ToByteArray();
            }
            catch(Exception e)
            {
                throw new Exception("Invalid transaction hex");
            }

            var finalTxHash = rpcClient.SendTransaction(signedTransaction.hex);
            if (finalTxHash == null)
                throw new Exception("Send transaction error");

            try
            {
                var tx = rpcClient.GetTransaction(finalTxHash);
                if (tx["confirmations"].Value<int>() == 0)
                    SaveMemPoolTx(tx);
            }
            catch
            {
            }

            return finalTxHash;
        }

        private bool ValidatePrivateKey(string data)
        {
            try
            {
                Base58Check.Base58CheckEncoding.Decode(data);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        private Transaction ParseRawTransaction(JToken tx, Dictionary<string, Transaction> blockTransactions = null)
        {
            var inputs = ParseInputs(tx, blockTransactions);
            var outputs = ParseOutputs(tx);
            
            var transaction = new Transaction()
            {
                Id = tx["txid"].Value<string>(),
                Time = !tx["time"].IsEmpty() ? tx["time"].Value<long>() : UnixTime.Now,
                Size = tx["hex"].Value<string>().Length / 2,
                Inputs = inputs,
                Outputs = outputs,
            };

            foreach (var input in inputs)
            {
                input.TxId = transaction.Id;
                input.Transaction = transaction;
            }
            foreach (var output in outputs)
            {
                output.TxId = transaction.Id;
                output.Transaction = transaction;
            }
            return transaction;
        }

        private List<Input> ParseInputs(JToken transaction, Dictionary<string, Transaction> blockTransactions)
        {
            var vins = transaction["vin"].Children();
            var inputs = new List<Input>();
            var parentTxIds = new HashSet<string>();
            foreach (var vin in vins)
            {
                if (vin["coinbase"].IsEmpty())
                {
                    string parentTxId = vin["txid"].Value<string>();
                    int vout = vin["vout"].Value<int>();
                    var input = new Input() { ParentTxId = parentTxId, Vout = vout };
                    inputs.Add(input);
                    parentTxIds.Add(parentTxId);
                }
            }

            if (inputs.Count != 0)
            {
                var parentTransactionsDictionary = new Dictionary<string, Transaction>();
                foreach (var parentTxId in parentTxIds)
                {
                    Transaction parentTx;
                    if (blockTransactions == null || !blockTransactions.TryGetValue(parentTxId, out parentTx))
                        parentTx = transactionRepository.Find(parentTxId);

                    if (parentTx == null)
                    {
                        Logger.Warn("Parent transaction not found: {0}. Trying to find and add it to db.", parentTxId);
                        //var parent = rpcClient.GetRawTransaction(parentTxId);
                        //parentTx = ParseRawTransaction(parent);
                        //transactionRepository.Block.Add(parentTx);
                    }
                    else
                        parentTransactionsDictionary.Add(parentTxId, parentTx);
                }
                foreach (var input in inputs)
                {
                    Transaction parentTx;
                    if (parentTransactionsDictionary.TryGetValue(input.ParentTxId, out parentTx))
                    {
                        var output = parentTx.Outputs.First(x => x.Vout == input.Vout);
                        input.Value = output.Value;
                        input.Address = output.Address;
                    }
                    else
                    {
                        Logger.Error("Parent transaction not found: {0}", input.ParentTxId);
                    }
                }
            }
            return inputs;
        }

        private List<Output> ParseOutputs(JToken transaction)
        {
            var outputs = new List<Output>();
            var vouts = transaction["vout"].Children();
            foreach (var vout in vouts) //check outputs
            {
                var output = new Output()
                {
                    Vout = vout["n"].Value<int>(),
                    Address = ExtractAddressFromVout(vout),
                    Value = vout["value"].Value<decimal>(),
                };
                outputs.Add(output);
            }
            return outputs;
        }

        private static string ExtractAddressFromVout(JToken vout)
        {
            var addresses = vout["scriptPubKey"]["addresses"];
            if (!addresses.IsEmpty())
            {
                if (addresses.Children().Count() == 1)
                {
                    string address = addresses.First.Value<string>();
                    return address;
                }
            }
            return null;
        }
    }
}