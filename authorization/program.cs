using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

public class Program
{
    private static readonly HttpClient client = new HttpClient();

    private static (string, string, string) ConstructInitAuthUrl()
    {
        string appKey = "Your-App-Key";
        string appSecret = "Your-App-Secret";

        string authUrl = $"https://api.schwabapi.com/v1/oauth/authorize?client_id={appKey}&redirect_uri=https:[Your-redirect-url";

        Console.WriteLine("Click to authenticate:");
        Console.WriteLine(authUrl);

        return (appKey, appSecret, authUrl);
    }

    private static (Dictionary<string, string>, Dictionary<string, string>) ConstructHeadersAndPayload(string returnedUrl, string appKey, string appSecret)
    {
        string responseCode = $"{returnedUrl.Substring(returnedUrl.IndexOf("code=") + 5, returnedUrl.IndexOf("%40") - (returnedUrl.IndexOf("code=") + 5))}@";

        string credentials = $"{appKey}:{appSecret}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Basic {base64Credentials}" },
            { "Content-Type", "application/x-www-form-urlencoded" }
        };

        var payload = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", responseCode },
            { "redirect_uri", "https://127.0.0.1:8080" }
        };

        return (headers, payload);
    }

    private static async Task<Dictionary<string, string>> RetrieveTokens(Dictionary<string, string> headers, Dictionary<string, string> payload)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.schwabapi.com/v1/oauth/token");

        foreach (var header in headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        requestMessage.Content = new FormUrlEncodedContent(payload);

        var response = await client.SendAsync(requestMessage);
        string responseBody = await response.Content.ReadAsStringAsync();

        var tokensDict = JObject.Parse(responseBody).ToObject<Dictionary<string, string>>();
        return tokensDict;
    }

    public static async Task Main(string[] args)
    {
        var (appKey, appSecret, csAuthUrl) = ConstructInitAuthUrl();
        
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = csAuthUrl,
            UseShellExecute = true
        });
        //this will open your browser to the Schwab login page, currently at https://sws-gateway.schwab.com/ui/host/#/login-one-step. 
        //you will be asked to enter your login id and password. The id and password you use needs to be for your trading account
        //not your developer account. After completing this form, you will be directed through a series of screens to confrim that
        //you know what you're doing here and really want to do it. When you finish this confirmation process, you will end up at
        //the returned URL, which will appear on the address line of your browser. It will be in the form of [the callback url for your
        //registered app with a couple of query parameters. Copy the entire url and paste it here.
        Console.WriteLine("Paste Returned URL:");
        string returnedUrl = Console.ReadLine();

        var (initTokenHeaders, initTokenPayload) = ConstructHeadersAndPayload(returnedUrl, appKey, appSecret);

        var initTokensDict = await RetrieveTokens(initTokenHeaders, initTokenPayload);

        foreach (var token in initTokensDict)
        {
            Console.WriteLine($"{token.Key}: {token.Value}");
        }

        Console.WriteLine("Done!");
    }
}
