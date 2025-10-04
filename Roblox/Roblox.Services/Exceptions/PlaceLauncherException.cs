namespace Roblox.Services.Exceptions;
using System.Net;
using Roblox.Models.GameServer;
public class PlaceLauncherException : System.Exception
{
    public int statusCode { get; set; }
    public string errorMessage { get; set; }
    public PlaceLauncherException() : base()
    {

    }
    public PlaceLauncherException(string errorMessage = "") : base("PlaceLauncher Exception: " + errorMessage)
    {
        this.statusCode = (int)JoinStatus.Error;
        this.errorMessage = errorMessage;
    }
    public PlaceLauncherException(JoinStatus errorCode, string errorMessage = "") : base("PlaceLauncher Exception: " + errorCode  + ": " + errorMessage)
    {
        this.statusCode = (int)errorCode;
        this.errorMessage = errorMessage;
    }
}