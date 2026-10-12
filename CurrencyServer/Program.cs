
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly object logLock = new object();
    static readonly string logFile = "server.log";

    static readonly System.Collections.Generic.Dictionary<string, double> rates =
        new System.Collections.Generic.Dictionary<string, double>
        {
            { "USD", 1.0 },
            { "EURO", 0.92 },
            { "UAH", 41.0 },
            { "GBP", 0.77 }
        };

    static async Task Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, 5000);
        server.Start();

        Console.WriteLine("Сервер курсов валют запущен. Порт 5000.");

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }

    static async Task HandleClient(TcpClient client)
    {
        string who = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        WriteLog($"Подключился: {who}");

        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
            {
                writer.AutoFlush = true;

                string? request;

                while ((request = await reader.ReadLineAsync()) != null)
                {
                    if (request.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                        break;

                    string[] parts = request.Trim().ToUpper().Split(
                        new[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                    {
                        await writer.WriteLineAsync("Помилка: введіть дві валюти, наприклад USD EURO");
                        continue;
                    }

                    string from = parts[0];
                    string to = parts[1];

                    if (!rates.ContainsKey(from) || !rates.ContainsKey(to))
                    {
                        await writer.WriteLineAsync("Помилка: доступні USD, EURO, UAH, GBP");
                        continue;
                    }

                    double result = rates[to] / rates[from];

                    string response =
                        $"1 {from} = {result:F4} {to}";

                    await writer.WriteLineAsync(response);

                    WriteLog($"{who} | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {request} | {response}");
                }
            }
        }
        catch (Exception ex)
        {
            WriteLog($"Помилка клієнта {who}: {ex.Message}");
        }
        finally
        {
            WriteLog($"Відключився: {who} | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Клієнт відключився: {who}");
        }
    }

    static void WriteLog(string message)
    {
        lock (logLock)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            Console.WriteLine(line);
            File.AppendAllText(logFile, line + Environment.NewLine);
        }
    }
}
