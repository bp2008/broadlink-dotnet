# Broadlink.Net
.Net library for Broadlink devices.

Ported from https://github.com/mjg59/python-broadlink/.

Currently only supports RM devices (tested with RM3 Mini Blackbean).

Updates by bp2008
--------------------------

I've fixed several bugs/slow performance problems and added a copy of the [Broadlink Protocol](https://github.com/bp2008/broadlink-dotnet/blob/master/BroadlinkProtocol.md).

Usage
--------------------------

```csharp
var client = new Client();
var discoveredDevices = await client.DiscoverAsync();
        
if (discoveredDevices.Any())
{
    var deviceToUse = discoveredDevices.FirstOrDefault();
    if(deviceToUse != null)
    {
        // cast to RMDevice to use RM specific commands
        var rmDevice = deviceToUse as RMDevice;
        
        // authorize before calling further methods
        await rmDevice.AuthorizeAsync();
        
        // get the temperature as a float
        var temp = await rmDevice.GetTemperatureAsync();
        
        // enter learning mode
        await rmDevice.EnterLearningModeAsync();
        
        // give the user 3 seconds to push a remote button and read the data from it
        await Task.Delay(3000);
        var data = await rmDevice.ReadLearningDataAsync();
        
        // send a remote control command that was learned before
        await rmDevice.SendRemoteCommandAsync(data);
    }
}
```
