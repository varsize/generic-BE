using System.Collections.Generic;
using BlockExplorerAPI.DAL.DTO;

namespace BlockExplorerAPI.DAL.Abstract
{
    public interface ITransactionRepository
    {
        void Add(Transaction tx);
        void AddRange(IEnumerable<Transaction> tx);
        Transaction Find(string txid);
        List<Transaction> FindAll(IEnumerable<string> txids);
        List<Transaction> FindByBlock(string blockHash);
        List<Transaction> FindByAddress(string address);
        List<Transaction> GetLast(int count); 
        int GetBlockNumber();
    }
}
