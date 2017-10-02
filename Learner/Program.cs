using Broadlink.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        var commandFilePath = Path.GetFullPath("../commands.json");

		if (!File.Exists(commandFilePath))
		{
            // Create a file to write to.
            var data = new Dictionary<string, string>();
            string createText = JsonConvert.SerializeObject(data);
			File.WriteAllText(commandFilePath, createText);
		}

        var json = File.ReadAllText(commandFilePath);
        
        var commandDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

        var client = new Client();
        var discoveredDevices = client.DiscoverAsync().Result;

        if (discoveredDevices.Any())
        {
            var deviceToUse = discoveredDevices.FirstOrDefault();
            if (deviceToUse != null)
            {
                var rmDevice = deviceToUse as RMDevice;
                rmDevice.AuthorizeAsync().Wait();

                Console.WriteLine("Type 'exit' to quit");
                var input = string.Empty;
                while (true)
                {
                    Console.WriteLine("Enter the command name");
                    input = Console.ReadLine();

                    if(input == "exit")
                    {
                        break;
                    }

                    if (commandDictionary.ContainsKey(input))
                    {
                        Console.WriteLine("Command already exists");
                        continue;
                    }

                    rmDevice.EnterLearningModeAsync().Wait();

                    Console.WriteLine("Teach the command");
                    Task.Delay(3000).Wait();
                    byte[] data;
                    try
                    {
                        data = rmDevice.ReadLearningDataAsync().Result;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error while learning code. Please try again.");
                        continue;
                    }
                    var base64 = Convert.ToBase64String(data);
                    Console.WriteLine($"Received {base64}");

                    commandDictionary.Add(input, base64);
                }

                json = JsonConvert.SerializeObject(commandDictionary);

                System.IO.File.Delete(commandFilePath);
                var writer = System.IO.File.CreateText(commandFilePath);

                writer.WriteLine(json);
                writer.Dispose();
            }

        }
    }
}