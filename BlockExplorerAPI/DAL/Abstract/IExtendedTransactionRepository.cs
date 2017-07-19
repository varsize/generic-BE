using System.Collections.Generic;

namespace BlockExplorerAPI.DAL.Abstract
{
    public interface IExtendedTransactionRepository : ISimpleTransactionRepository
    {
        HashSet<string> GetTransactionIDs();
        bool Contains(string txid);
        void Delete(string txid);
        void Clear();
    }
}