namespace BlockExplorerAPI.Models.Requests
{
    public class LastTransactionsRequest
    {
        private int count = ApplicationSettings.DefaultLastTransactionsCount;
        public int Count
        {
            get { return count; }
            set {
                if (value > 0)
                {
                    count = value > ApplicationSettings.MaxLastTransactionsCount ? ApplicationSettings.MaxLastTransactionsCount : value;
                }
            }
        }
    }
}