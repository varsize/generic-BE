using System;
using System.Collections.Generic;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.DAL.DTO;
using BlockExplorerAPI.Services.Models;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BlockExplorerAPI.Services
{
    public class AddressService
    {
        private readonly ITransactionRepository transactionRepository;
        private readonly CryptoRpcClient rpcClient;

        public AddressService(ITransactionRepository transactionRepository, CryptoRpcClient rpcClient)
        {
            if (transactionRepository == null)
                throw new ArgumentNullException("transactionRepository");

            this.transactionRepository = transactionRepository;
            this.rpcClient = rpcClient;
        }

        public List<AddressModel.UnspentOutputModel> FindUnspend(string address)
        {
            var unspentOutputs = new List<AddressModel.UnspentOutputModel>();
            var transactions = transactionRepository.FindByAddress(address);
            if (transactions.Count == 0)
                return unspentOutputs;

            transactions = RemoveDoubleSpend(transactions);
            if (transactions.Count == 0)
                return unspentOutputs;

            List<AddressModel.UnspentOutputModel> unspentCandidates = new List<AddressModel.UnspentOutputModel>();            
            foreach (var transaction in transactions)
            {
                foreach (var output in transaction.Outputs)
                {
                    if (output.Address == address)
                    {
                        unspentCandidates.Add(new AddressModel.UnspentOutputModel()
                        {
                            address = address,
                            amount = output.Value,
                            scriptPubKey = null,
                            txid = output.TxId,
                            vout = output.Vout
                        });
                    }
                }
            }

            foreach (var candidate in unspentCandidates)
            {
                bool spent = false;
                foreach (var transaction in transactions)
                {
                    foreach (var input in transaction.Inputs)
                    {
                        if (input.Address == address && input.ParentTxId == candidate.txid && input.Vout == candidate.vout)
                        {
                            spent = true;
                            break;
                        }
                    }
                    if (spent) break;
                }
                if (!spent) unspentOutputs.Add(candidate);
            }

            return unspentOutputs;
        }

        private List<Transaction> RemoveDoubleSpend(List<Transaction> transactions)
        {
            var doubleSpendIndicator = new Dictionary<int, string>();
            var suspectedOrphan = new HashSet<string>();
            foreach (var transaction in transactions)
            {
                if (transaction.BlockNumber.HasValue)
                {
                    string blockHash;
                    if (doubleSpendIndicator.TryGetValue(transaction.BlockNumber.Value, out blockHash))
                    {
                        if (transaction.BlockHash != blockHash) //orphan block found
                        {
                            suspectedOrphan.Add(transaction.BlockHash);
                            suspectedOrphan.Add(blockHash);
                        }
                    }
                    else
                    {
                        doubleSpendIndicator.Add(transaction.BlockNumber.Value, transaction.BlockHash);
                    }
                }
            }

            var orphanBlocks = new HashSet<string>();
            var blocks = rpcClient.GetBlocks(suspectedOrphan);
            foreach (var block in blocks)
            {
                int confirmations = block["confirmations"].Value<int>();
                if (confirmations == -1)
                    orphanBlocks.Add(block["hash"].Value<string>());
            }
            return transactions.Where(x => (x.BlockHash == null) || !orphanBlocks.Contains(x.BlockHash)).ToList();
        }

        public List<AddressModel.UnspentOutputModel> FindUnspendWithPubKey(string address)
        {
            List<AddressModel.UnspentOutputModel> unspentOutputs = FindUnspend(address);
            if (unspentOutputs.Count != 0)
            {
                List<JToken> rawTransactions = rpcClient.GetRawTransactions(unspentOutputs.Select(u => u.txid).Distinct());
                Dictionary<string, RawTransaction> rawTxDict = rawTransactions
                    .Select(tx => tx.ToObject<RawTransaction>())
                    .ToDictionary(tx => tx.txid, tx => tx);
                foreach (var output in unspentOutputs)
                {
                    output.scriptPubKey = rawTxDict[output.txid].vout.First(x => x.n == output.vout).scriptPubKey.hex;
                }
            }
            return unspentOutputs;
        }

        public AddressModel Find(string address)
        {
            var addressModel = new AddressModel(address);
            var transactions = transactionRepository.FindByAddress(address);
            if (transactions.Count == 0)
                return null;

            int blockCount = rpcClient.GetBlockCount();
            var doubleSpendIndicator = new Dictionary<int, string>();
            var suspectedOrphan = new HashSet<string>();
            foreach (var transaction in transactions)
            {
                if (transaction.BlockNumber.HasValue)
                {
                    string blockHash;
                    if (doubleSpendIndicator.TryGetValue(transaction.BlockNumber.Value, out blockHash))
                    {
                        if (transaction.BlockHash != blockHash)
                        {
                            suspectedOrphan.Add(transaction.BlockHash);
                            suspectedOrphan.Add(blockHash);
                        }
                    }
                    else
                    {
                        doubleSpendIndicator.Add(transaction.BlockNumber.Value, transaction.BlockHash);
                    }
                }

                var txModel = new AddressTransactionModel(transaction, blockCount);
                decimal inputsFromAddress = 0;
                foreach (var input in transaction.Inputs)
                {
                    if (input.Address == address)
                    {
                        txModel.Inputs.Clear();
                        addressModel.TotalSent += input.Value;
                        inputsFromAddress += input.Value;
                    }
                    txModel.Inputs.Add(input.Address);
                }

                bool coinstake = inputsFromAddress != 0 && transaction.Outputs.Any(x => x.Address == null);
                if (coinstake)
                {
                    foreach (var output in transaction.Outputs)
                    {
                        if (output.Address == address)
                        {
                            addressModel.TotalReceived += output.Value;
                            if (txModel.Outputs.Count == 0)
                            {
                                var coinstakeOutput = new OutputModel(output.Address, output.Value - inputsFromAddress);
                                txModel.Outputs.Add(coinstakeOutput);
                            }
                            txModel.OutputsValue += output.Value;
                        }
                    }
                }
                else
                {
                    bool hasInputs = inputsFromAddress != 0;
                    foreach (var output in transaction.Outputs)
                    {
                        if (output.Address == address)
                        {
                            addressModel.TotalReceived += output.Value;
                            txModel.Outputs.Add(new OutputModel(output.Address, hasInputs ? output.Value : output.Value));
                            txModel.OutputsValue += output.Value;
                        }
                        else
                        {
                            txModel.Outputs.Add(new OutputModel(output.Address, hasInputs ? -output.Value : output.Value));
                            txModel.OutputsValue += output.Value;
                        }
                    }
                }
                //OutputModel coinstakeOutput = null;
                //foreach (var output in transaction.Outputs)
                //{
                //    if (output.Address == address)
                //    {
                //        addressModel.TotalReceived += output.Value;
                //        if (inputsFromAddress != 0) //it is coinstake tx
                //        {
                //            if (coinstakeOutput == null)
                //            {
                //                txModel.Outputs.Clear();
                //                coinstakeOutput = new OutputModel(output.Address, output.Value - inputsFromAddress);
                //                txModel.Outputs.Add(coinstakeOutput);
                //            }
                //            else
                //            {
                //                coinstakeOutput.Value += output.Value;
                //            }
                //            txModel.OutputsValue = coinstakeOutput.Value;
                //            continue;
                //        }
                //        else
                //        {
                //            txModel.Outputs.Clear();
                //            txModel.Outputs.Add(new OutputModel(output.Address, output.Value));
                //            txModel.OutputsValue = output.Value;
                //            break;
                //        }
                //    }

                //    if (coinstakeOutput == null)
                //    {
                //        txModel.Outputs.Add(new OutputModel(output.Address, output.Value));
                //        txModel.OutputsValue += output.Value;
                //    }
                //}
                addressModel.Transactions.Add(txModel);
            }

            var orphanBlocks = new HashSet<string>();
            var blocks = rpcClient.GetBlocks(suspectedOrphan);
            foreach (var block in blocks)
            {
                int confirmations = block["confirmations"].Value<int>();
                if (confirmations == -1)
                    orphanBlocks.Add(block["hash"].Value<string>());
            }

            foreach (var transaction in addressModel.Transactions)
            {
                if (transaction.BlockHash != null && orphanBlocks.Contains(transaction.BlockHash))
                    transaction.Confirmations = -1;
            }
            return addressModel;
        }
    }
}