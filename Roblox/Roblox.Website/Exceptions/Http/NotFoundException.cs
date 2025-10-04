using System.Net;

namespace Roblox.Exceptions
{
    public class NotFoundException : HttpBaseException, IHttpException
    {
        public NotFoundException() : base()
        {
            statusCode = HttpStatusCode.NotFound;
        }
        
        public NotFoundException(int errorCode, string errorMessage = "") : base(errorCode, errorMessage)
        {
            statusCode = HttpStatusCode.NotFound;
        }
    }
}