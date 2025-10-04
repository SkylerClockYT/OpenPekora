using System.Net;

namespace Roblox.Exceptions
{
    public class HttpException : HttpBaseException, IHttpException
    {
        public HttpException(HttpStatusCode code) : base()
        {
            statusCode = code;
        }
        
        public HttpException(HttpStatusCode code, int errorCode, string errorMessage = "") : base(errorCode, errorMessage)
        {
            statusCode = code;
        }
    }
}