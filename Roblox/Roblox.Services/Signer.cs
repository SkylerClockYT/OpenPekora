using Newtonsoft.Json;
using Roblox;
using Roblox.Dto.Games;
using Roblox.Models.GameServer;
using Roblox.Services;
using System.Security.Cryptography;
using System.Text;
namespace Roblox.Services.Signer;
public class SignService : ServiceBase
{
    private static RSACryptoServiceProvider? _rsaCsp;
    private static SHA1? _shaCsp;
    private static RSACryptoServiceProvider rsa = new();
    private static RSACryptoServiceProvider rsa2048 = new();
    private static readonly string newLine = "\r\n";
    private static readonly string format = "--rbxsig%{0}%{1}";
    private static readonly string format2048 = "--rbxsig2%{0}%{1}";

    public static void Setup()
    {
        try
        {
            byte[] privateKeyBlob = Convert.FromBase64String(System.IO.File.ReadAllText(@"Keys\PrivateKeyBlob.txt"));

            _shaCsp = SHA1.Create();
            _rsaCsp = new RSACryptoServiceProvider();

            _rsaCsp.ImportCspBlob(privateKeyBlob);
            rsa2048.ImportFromPem(System.IO.File.ReadAllText(@"Keys\PrivateKey2048.pem"));
        }
        catch (Exception ex)
        {
            throw new Exception("Error setting up SignatureController: " + ex.Message);
        }
    }

    public string SignJsonResponseForClientFromPrivateKey(dynamic JSONToSign)
    {
        string format = "--rbxsig%{0}%{1}";

        string json = JsonConvert.SerializeObject(JSONToSign);
        string script = Environment.NewLine + json;
        byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(script), _shaCsp!);

        return String.Format(format, Convert.ToBase64String(signature), script);
    }
    public string SignStringResponseForClientFromPrivateKey(string stringToSign, bool bUseRbxSig = false)
    {
        if (bUseRbxSig)
        {
            string format = "--rbxsig%{0}%{1}";

            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(stringToSign), _shaCsp!);
            string script = Environment.NewLine + stringToSign;

            return String.Format(format, Convert.ToBase64String(signature), script);
        }
        else
        {
            byte[] signature = _rsaCsp!.SignData(Encoding.Default.GetBytes(stringToSign), _shaCsp!);
            return Convert.ToBase64String(signature);
        }
    }
    public string SignJson2048(dynamic JSONToSign)
    {
        string script = newLine + JsonConvert.SerializeObject(JSONToSign);
        byte[] signature = rsa2048.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

        return string.Format(format2048, Convert.ToBase64String(signature), script);
    }
    public string SignString2048(string stringToSign, bool bUseRbxSig = false)
    {
        if (bUseRbxSig)
        {
            string script = newLine + stringToSign;
            byte[] signature = rsa.SignData(Encoding.Default.GetBytes(script), SHA1.Create());

            return string.Format(format, Convert.ToBase64String(signature), script);
        }
        else
        {
            byte[] signature = rsa2048.SignData(Encoding.Default.GetBytes(stringToSign), SHA1.Create());
            return Convert.ToBase64String(signature);
        }
    }
    // TODO: Fully rewrite the ticket system
    public string GenerateClientTicket(long year, long userId, string username, string characterAppearanceUrl, string membership, Guid jobId, long accountAgeDays, long placeId)
    {
        switch (year)
        {
            case 2017:
                return GenerateClientTicketV1(userId, username, jobId, characterAppearanceUrl);
            case 2018:
                return GenerateClientTicketV2(userId, username, jobId, characterAppearanceUrl);
            case 2019:
                return GenerateClientTicketV3(userId, username, jobId);
            case 2020:
            case 2021:
                return GenerateClientTicketV4(userId, username, characterAppearanceUrl, membership, jobId, accountAgeDays, placeId);
            default:
                throw new InvalidOperationException($"This year does not exist");
        }
    }

    public string GenerateClientTicketV1(long userId, string username, Guid jobId, string characterAppearanceUrl)
    {
        DateTime currentUtcDateTime = DateTime.UtcNow;
        string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
        string cticket = $"{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature = SignStringResponseForClientFromPrivateKey(cticket);
        string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{formattedDateTime}";
        string ticketSignature2 = SignStringResponseForClientFromPrivateKey(ticket2);
        string finalTicket = $"{formattedDateTime};{ticketSignature2};{ticketSignature}";
        return finalTicket;
    }

    public string GenerateClientTicketV2(long userId, string username, Guid jobId, string characterAppearanceUrl)
    {
        DateTime currentUtcDateTime = DateTime.UtcNow;
        string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
        string cticket = $"{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature = SignString2048(cticket);

        string cticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{formattedDateTime}";
        string ticketSignature2 = SignString2048(cticket2);

        string finalTicket = $"{formattedDateTime};{ticketSignature2};{ticketSignature};2";
        return finalTicket;
    }
    public string GenerateClientTicketV3(long userId, string username, Guid jobId)
    {
        DateTime currentUtcDateTime = DateTime.UtcNow;
        string dateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
        // the second userid is meant to be characterAppearanceId
        string ticket2 = $"{userId}\n{username}\n{userId}\n{jobId}\n{dateTime}";
        string ticket2Signature = SignString2048(ticket2);
        string ticket = $"{userId}\n{jobId}\n{dateTime}";
        string ticketSignature = SignString2048(ticket);
        // Final ticket
        string finalTicket = $"{dateTime};{ticket2Signature};{ticketSignature};3";
        return finalTicket;
    }
    public string GenerateClientTicketV4(long userId, string username, string characterAppearanceUrl, string membership, Guid jobId, long accountAgeDays, long placeId)
    {
        DateTime currentUtcDateTime = DateTime.UtcNow;
        string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
        string countryCode = "US";
        string ticket2 = $"{userId}\n{username}\n{characterAppearanceUrl}\n{jobId}\n{formattedDateTime}";
        string ticket2Signature = SignString2048(ticket2);
        string ticket = $"{formattedDateTime}\n{jobId}\n{userId}\n{userId}\n0\n{accountAgeDays}\nf\n{username.Length}\n{username}\n{membership.Length}\n{membership}\n{countryCode.Length}\n{countryCode}\n0\n\n{username.Length}\n{username}";
        string ticketSignature = SignString2048(ticket);
        string finalTicket = $"{formattedDateTime};{ticket2Signature};{ticketSignature};4";
        return finalTicket;
    }
}