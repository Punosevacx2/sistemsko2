using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ProjekatSP;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    private static readonly string ApiKey = "0c335201-4b5e-4834-b2df-e93be0f8a6b5";
    private static readonly HttpClient http = new HttpClient();
    private static readonly LRUCache Cache = new LRUCache();
    private static readonly string ApiBaseUrl = "http://api.airvisual.com/v2/city";

    static async Task Main()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5050/");
        listener.Start();

        Console.WriteLine("Osluskujem 5050");

        while (true)
        {
            var context = await listener.GetContextAsync();
            _ = Task.Run(() => HandleRequestAsync(context));
        }
    }

    static async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        string url = request.Url.ToString();
        Console.WriteLine($"Request: {url}");

        IQAir responseData;

        lock (Cache)
        {
            if (LRUCache.Contains(url))
            {
                responseData = LRUCache.Get(url);
            }
            else
            {
                responseData = GetDataAsync(url).GetAwaiter().GetResult();

                if (responseData == null) return;
                LRUCache.Put(url, responseData);
            }
        }
        byte[] buffer;

        if(responseData.status == "Greska")
        {
             buffer = Encoding.UTF8.GetBytes("Odabrali ste nepostojeci grad");
        }

        else
        {
            buffer = Encoding.UTF8.GetBytes(responseData.data.ToString());
        }

        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    static async Task<IQAir> GetDataAsync(string url)
    {
        if (url.Contains("favicon")) return null;
        var query = WebUtility.UrlDecode(url.Substring(url.IndexOf('?') + 1));
        var parameters = query.Split('&');
        var city = "";
        foreach (var parameter in parameters)
        {
            var parts = parameter.Split('=');

            if (parts.Length == 2)
            {
                city = parts[1];
                break;
            }
        }

        if (string.IsNullOrEmpty(city))
        {
            return new IQAir("Greska");
        }

        try
        {
            HttpResponseMessage odgovor;
            try
            {
                query = $"city={city}&state=Central Serbia&country=Serbia";
                string apiUrl = $"{ApiBaseUrl}?{query}&key={ApiKey}";
                odgovor = await http.GetAsync(apiUrl);

                if (!odgovor.IsSuccessStatusCode)
                {
                    throw new Exception(odgovor.StatusCode.ToString());
                }
            }
            catch (Exception e)
            {
                query = $"city={city}&state=Autonomna Pokrajina Vojvodina&country=Serbia";
                string apiUrl = $"{ApiBaseUrl}?{query}&key={ApiKey}";
                odgovor = await http.GetAsync(apiUrl);

                if (!odgovor.IsSuccessStatusCode)
                {
                    throw new Exception(odgovor.StatusCode.ToString());
                }
            }

            string responseContent = await odgovor.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);

            JObject nov = JObject.Parse(responseContent);
            IQAir name = new IQAir("Uspesno");
            name.data = (int)nov["data"]["current"]["pollution"]["aqius"]; //Izvlacimo podatak koji nam je potreban

            return name;
        }
        catch (Exception e)
        {
            return new IQAir("Greska");
        }
    }
}
