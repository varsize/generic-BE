namespace BlockExplorerAPI.Models.Requests
{
    public class PaginationRequest
    {
        protected int _offset = 0;
        protected int _limit = 25;

        public int Limit
        {
            get { return _limit; }
            set
            {
                if (value > 0) _limit = value;
            }
        }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (value >= 0) _offset = value;
            }
        }
    }
}