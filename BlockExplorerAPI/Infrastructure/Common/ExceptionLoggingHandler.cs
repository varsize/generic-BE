using System.Web.Http.ExceptionHandling;
using NLog;

namespace BlockExplorerAPI.Infrastructure.Common
{
    public class ExceptionLoggingHandler : ExceptionHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public override void Handle(ExceptionHandlerContext context)
        {
            Logger.Error(context.Exception, "Request url: {0}", context.ExceptionContext.Request.RequestUri.PathAndQuery);
        }

        public override bool ShouldHandle(ExceptionHandlerContext context)
        {
            return true;
        }
    }
}