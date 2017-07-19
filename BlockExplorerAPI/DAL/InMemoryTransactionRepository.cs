using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.DAL.DTO;

namespace BlockExplorerAPI.DAL
{
    [Serializable]
    public class InMemoryTransactionRepository : IExtendedTransactionRepository
    {
        [NonSerialized]
        private ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();

        private readonly Dictionary<string, Transaction> transactionsIndex = new Dictionary<string, Transaction>();
        private readonly Dictionary<string, List<Input>> inputsIndex = new Dictionary<string, List<Input>>();
        private readonly Dictionary<string, List<Output>> outputsIndex = new Dictionary<string, List<Output>>();
        private readonly HashSet<string> deletedTransactions = new HashSet<string>(); 

        public void Add(Transaction tx)
        {
            rwls.EnterWriteLock();
            try
            {
                if (deletedTransactions.Contains(tx.Id)) return;
                transactionsIndex.Add(tx.Id, tx);
                foreach (var input in tx.Inputs)
                {
                    List<Input> inputs;
                    if (inputsIndex.TryGetValue(input.Address, out inputs))
                    {
                        inputs.Add(input);
                    }
                    else
                    {
                        inputs = new List<Input>() {input};
                        inputsIndex.Add(input.Address, inputs);
                    }
                }
                foreach (var output in tx.Outputs)
                {                    
                    if (output.Address != null)
                    {
                        List<Output> outputs;
                        if (outputsIndex.TryGetValue(output.Address, out outputs))
                        {
                            outputs.Add(output);
                        }
                        else
                        {
                            outputs = new List<Output>() { output };
                            outputsIndex.Add(output.Address, outputs);
                        }
                    }
                }
            }
            finally
            {
                rwls.ExitWriteLock();
            }
        }

        public Transaction Find(string txid)
        {
            rwls.EnterReadLock();
            try
            {
                Transaction tx;
                transactionsIndex.TryGetValue(txid, out tx);
                return tx;
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public List<Transaction> FindAll(IEnumerable<string> txids)
        {
            rwls.EnterReadLock();
            try
            {
                var transactions = new List<Transaction>();
                foreach (var txid in txids)
                {
                    Transaction tx;
                    if (transactionsIndex.TryGetValue(txid, out tx))
                        transactions.Add(tx);
                }
                return transactions;
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public List<Transaction> FindByAddress(string address)
        {
            rwls.EnterReadLock();
            try
            {
                var transactions = new Dictionary<string, Transaction>();
                List<Input> inputs;
                if (inputsIndex.TryGetValue(address, out inputs))
                {
                    foreach (var input in inputs)
                    {
                        if (!transactions.ContainsKey(input.TxId))
                            transactions.Add(input.TxId, input.Transaction);
                    }
                }
                List<Output> outputs;
                if (outputsIndex.TryGetValue(address, out outputs))
                {
                    foreach (var output in outputs)
                    {
                        if (!transactions.ContainsKey(output.TxId))
                            transactions.Add(output.TxId, output.Transaction);
                    }
                }
                return transactions.Values.ToList();
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public List<Transaction> GetLast(int count)
        {
            var transactions = transactionsIndex.Values
                .OrderByDescending(tx => tx.Time)
                .Take(count)
                .ToList();
            return transactions;
        }

        public int GetBlockNumber()
        {
            rwls.EnterReadLock();
            try
            {
                if (transactionsIndex.Count == 0)
                    return 0;
                return transactionsIndex.Values.Max(x => x.BlockNumber) ?? 0;
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public HashSet<string> GetTransactionIDs()
        {
            rwls.EnterReadLock();
            try
            {
                return new HashSet<string>(transactionsIndex.Keys);
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public bool Contains(string txid)
        {
            rwls.EnterReadLock();
            try
            {
                return transactionsIndex.ContainsKey(txid) || deletedTransactions.Contains(txid);
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        public void Delete(string txid)
        {
            rwls.EnterWriteLock();
            try
            {
                Transaction tx;
                if (transactionsIndex.TryGetValue(txid, out tx))
                {
                    transactionsIndex.Remove(txid);
                    foreach (var input in tx.Inputs)
                        inputsIndex.Remove(input.Address);
                    foreach (var output in tx.Outputs)
                        outputsIndex.Remove(output.Address);
                }
            }
            finally
            {
                rwls.ExitWriteLock();
            }
        }

        public void Clear()
        {
            rwls.EnterWriteLock();
            try
            {
                foreach (var txid in transactionsIndex.Keys)
                {
                    deletedTransactions.Add(txid);
                }
                transactionsIndex.Clear();
                inputsIndex.Clear();
                outputsIndex.Clear();
            }
            finally
            {
                rwls.ExitWriteLock();
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            rwls = new ReaderWriterLockSlim();
        }

        public void AddRange(IEnumerable<Transaction> transactions)
        {
            foreach (var tx in transactions)
            {
                Add(tx);
            }
        }

        public List<Transaction> FindByBlock(string blockHash)
        {
            rwls.EnterReadLock();
            try
            {
                return transactionsIndex.Values.Where(x => x.BlockHash == blockHash).ToList();
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }
    }
}