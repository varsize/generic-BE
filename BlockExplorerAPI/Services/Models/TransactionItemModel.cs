using BlockExplorerAPI.DAL.DTO;

namespace BlockExplorerAPI.Services.Models
{
    public class TransactionItemModel
    {
        public string Txid { get; private set; }
        public long Time { get; private set; }
        public int Size { get; private set; }
        public decimal OutputsValue { get; set; }

        public TransactionItemModel(Transaction transaction)
        {
            Txid = transaction.Id;
            Time = transaction.Time;
            Size = transaction.Size;
            foreach (var output in transaction.Outputs)
            {
                OutputsValue += output.Value;
            }
        }
    }
}