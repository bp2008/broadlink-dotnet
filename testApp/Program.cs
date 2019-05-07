using Broadlink.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

class Program
{
	static void Main(string[] args)
	{
		Client client = new Client();
		List<BroadlinkDevice> discoveredDevices = client.DiscoverAsync(returnAfterOne: true).Result;
		Log("Discovered " + discoveredDevices.Count + " devices");

		if (discoveredDevices.Any())
		{
			BroadlinkDevice deviceToUse = discoveredDevices.FirstOrDefault();
			if (deviceToUse != null)
			{
				RMDevice rmDevice = deviceToUse as RMDevice;
				rmDevice.AuthorizeAsync().Wait();
				Log("Authorization obtained for device " + rmDevice.EndPoint.Address + " (" + BitConverter.ToString(rmDevice.MacAddress) + ")");

				float temp = rmDevice.GetTemperatureAsync().Result;
				Log("Temperature: " + temp);

				for (int i = 0; i < 100; i++)
				{
					try
					{
						rmDevice.EnterLearningModeAsync().Wait();

						Log("Starting Learn #" + i);
						Task.Delay(5000).Wait();

						Log("Ending Learn #" + i);
						var data = rmDevice.ReadLearningDataAsync().Result;

						Log("Data for #" + i + ": " + BitConverter.ToString(data));

						rmDevice.SendRemoteCommandAsync(data).Wait();

						Log("Sent #" + i);
					}
					catch (Exception ex)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Log(ex.Message);
						Console.ResetColor();
					}
				}
			}

		}
		Console.ReadLine();
	}
	static void Log(string msg)
	{
		Console.WriteLine(DateTime.Now.ToString() + " " + msg);
	}
}