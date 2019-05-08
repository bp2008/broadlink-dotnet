using Broadlink.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
	static void Main(string[] args)
	{
		try
		{
			Client client = new Client();
			List<BroadlinkDevice> discoveredDevices = client.DiscoverAsync(returnAfterOne: true).Result;
			Log("Discovered " + discoveredDevices.Count + " devices");

			SortedList<char, RMCommand> commands = Load();
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

					while (true)
					{
						Log("Press R to record a command, or press a number key to emit a saved command");
						ConsoleKeyInfo cki = Console.ReadKey(true);
						if (cki.Key == ConsoleKey.R)
						{
							Log("Press a number key to save a command to that slot");
							cki = Console.ReadKey(true);
							if (cki.KeyChar >= '0' && cki.KeyChar <= '9')
							{
								// Save a command.
								try
								{
									rmDevice.EnterLearningModeAsync().Wait();
									Log("#" + cki.KeyChar + ": Press a remote control button within 3 seconds");

									Task.Delay(3000).Wait();

									RMCommand cmd = rmDevice.ReadLearningDataAsync().Result;

									Log("#" + cki.KeyChar + ": Learned");

									Log("Data for #" + cki.KeyChar + ": " + JsonConvert.SerializeObject(cmd));
									Log("Pulse Count #" + cki.KeyChar + ": " + cmd.GetPulses().Length);
									if (BitConverter.ToString(cmd.RawPulseData) != BitConverter.ToString(cmd.ToBinary().Skip(4).ToArray()))
									{
										Log("Data Source #" + cki.KeyChar + ": Serialization Error");
										Log("Data Source #" + cki.KeyChar + ": " + BitConverter.ToString(cmd.RawPulseData));
										Log("Data Output #" + cki.KeyChar + ": " + BitConverter.ToString(cmd.ToBinary().Skip(4).ToArray()));
										continue;
									}
									commands[cki.KeyChar] = cmd;
									Save(commands);
								}
								catch (Exception ex)
								{
									Log(ex.Message);
								}
							}
						}
						else if (cki.KeyChar >= '0' && cki.KeyChar <= '9')
						{
							// Emit saved command.
							if (commands.TryGetValue(cki.KeyChar, out RMCommand cmd))
							{
								Log("#" + cki.KeyChar + ": " + BitConverter.ToString(cmd.ToBinary().Skip(4).ToArray()));
								rmDevice.SendRemoteCommandAsync(cmd).Wait();
								Log("" + cki.KeyChar + ": Sent");
							}
							else
								Log("" + cki.KeyChar + ": Command Not Found");
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log(ex);
		}
		Console.ReadLine();
	}
	static void Log(string msg)
	{
		Console.WriteLine(DateTime.Now.ToString() + " " + msg);
	}
	static void Log(Exception ex)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Log(ex.ToString());
		Console.ResetColor();
	}
	static SortedList<char, RMCommand> Load()
	{
		if (File.Exists("commands.json"))
		{
			string json = File.ReadAllText("commands.json");

			try
			{
				return JsonConvert.DeserializeObject<SortedList<char, RMCommand>>(json);
			}
			catch { }
		}
		return new SortedList<char, RMCommand>();
	}
	static void Save(SortedList<char, RMCommand> commands)
	{
		File.WriteAllText("commands.json", JsonConvert.SerializeObject(commands));
	}
}