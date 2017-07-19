using System;
using System.Collections.Generic;
using System.Linq;
using BlockExplorerAPI.DAL.Abstract;
using BlockExplorerAPI.DAL.DTO;

namespace BlockExplorerAPI.DAL
{
    public class TwoLevelTransactionRepository : ITransactionRepository
    {
        public IExtendedTransactionRepository MemoryPool { get; private set; }
        public ISimpleTransactionRepository Block { get; private set; }

        public TwoLevelTransactionRepository(IExtendedTransactionRepository memoryPoolTransactionRepository, ISimpleTransactionRepository blockTransactionRepository)
        {
            if (memoryPoolTransactionRepository == null)
                throw new ArgumentNullException("memoryPoolTransactionRepository");
            if (blockTransactionRepository == null)
                throw new ArgumentNullException("blockTransactionRepository");

            this.MemoryPool = memoryPoolTransactionRepository;
            this.Block = blockTransactionRepository;
        }

        public void Add(Transaction tx)
        {
            if (!tx.BlockNumber.HasValue)
                MemoryPool.Add(tx);
            else
                Block.Add(tx);
        }

        public Transaction Find(string txid)
        {
            var tx = MemoryPool.Find(txid);
            if (tx == null)
                tx = Block.Find(txid);
            return tx;
        }

        public List<Transaction> FindAll(IEnumerable<string> txids)
        {
            var txidsHashSet = new HashSet<string>(txids);
            var transactions = MemoryPool.FindAll(txidsHashSet);
            foreach (var transaction in transactions)
            {
                txidsHashSet.Remove(transaction.Id);
            }

            if (txidsHashSet.Count != 0)
                transactions.AddRange(Block.FindAll(txidsHashSet));
            return transactions;
        }

        public List<Transaction> FindByAddress(string address)
        {
            var transactions = MemoryPool.FindByAddress(address);
            transactions.AddRange(Block.FindByAddress(address));
            return transactions.OrderByDescending(x => x.Time).ToList();
        }

        public List<Transaction> GetLast(int count)
        {
            var transactions = MemoryPool.GetLast(count);
            if (transactions.Count != count)
            {
                var blockTransactions = Block.GetLast(count - transactions.Count);
                transactions.AddRange(blockTransactions);
            }
            return transactions;
        }

        public int GetBlockNumber()
        {
            return Block.GetBlockNumber();
        }

        public void AddRange(IEnumerable<Transaction> tx)
        {
            Block.AddRange(tx);
        }

        public List<Transaction> FindByBlock(string blockHash)
        {
            return Block.FindByBlock(blockHash);
        }
    }
}