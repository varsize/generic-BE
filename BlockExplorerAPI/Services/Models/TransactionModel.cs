using BlockExplorerAPI.DAL.DTO;

namespace BlockExplorerAPI.Services.Models
{
    public class TransactionModel : AddressTransactionModel
    {
        public int? BlockNumber { get; private set; }
        public decimal InputsValue { get; private set; }
                
        public TransactionModel(Transaction transaction)
            : base(transaction)
        {
            BlockNumber = transaction.BlockNumber;
            foreach (var input in transaction.Inputs)
            {
                Inputs.Add(input.Address);
                InputsValue += input.Value;
            }
            foreach (var output in transaction.Outputs)
            {
                Outputs.Add(new OutputModel(output.Address, output.Value));
                OutputsValue += output.Value;
            }
        }
    }
}