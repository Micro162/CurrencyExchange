
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        try
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 5000);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
            using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);

            writer.AutoFlush = true;

            Console.WriteLine("Клієнт підключений до сервера.");
            Console.WriteLine("Доступні валюти: USD, EURO, UAH, GBP");
            Console.WriteLine("Введіть дві валюти через пробіл або EXIT для виходу.");

            while (true)
            {
                Console.Write("\nВалюти: ");
                string? request = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(request))
                    continue;

                await writer.WriteLineAsync(request);

                if (request.Trim().Equals("EXIT", StringComparison.OrdinalIgnoreCase))
                    break;

                string? response = await reader.ReadLineAsync();

                if (response == null)
                {
                    Console.WriteLine("Сервер закрив з'єднання.");
                    break;
                }

                Console.WriteLine(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Помилка: " + ex.Message);
            Console.WriteLine("Переконайтеся, що сервер запущений.");
        }
    }
}
