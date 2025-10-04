using System.Net;

namespace Roblox.Exceptions;

public class UnauthorizedException : HttpBaseException, IHttpException
{
    public UnauthorizedException() : base(0, "Authorization has been denied for this request.")
    {
        statusCode = HttpStatusCode.Unauthorized;
    }
        
    public UnauthorizedException(int errorCode = 0, string errorMessage = "") : base(errorCode, errorMessage)
    {
        statusCode = HttpStatusCode.Unauthorized;
    }
}