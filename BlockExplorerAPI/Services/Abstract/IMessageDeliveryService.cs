using BlockExplorerAPI.Models.Responses;
using BlockExplorerAPI.Services.Models;

namespace BlockExplorerAPI.Services.Abstract
{
    public interface IMessageDeliveryService
    {
        void NewTransaction(TransactionModel transaction);
        void NewBlock(BlockItemModel block);
    }
}
