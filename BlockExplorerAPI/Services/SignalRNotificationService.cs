using Microsoft.AspNet.SignalR;
using BlockExplorerAPI.Models.Responses;
using BlockExplorerAPI.Services.Abstract;
using BlockExplorerAPI.Services.Models;

namespace BlockExplorerAPI.Services
{
    public class SignalRNotificationService : IMessageDeliveryService
    {
        private IHubContext hubContext;
        public SignalRNotificationService(IHubContext hubContext)
        {
            this.hubContext = hubContext;
        }

        public void NewTransaction(TransactionModel transaction)
        {
            hubContext.Clients.All.newTransaction(transaction);
        }

        public void NewBlock(BlockItemModel block)
        {
            hubContext.Clients.All.newBlock(block);
        }
    }
}