# Broadlink.Net
.Net library for Broadlink devices.

Ported from https://github.com/mjg59/python-broadlink/.

Currently only supports RM devices (tested with RM3 Mini Blackbean).

Updates by bp2008
--------------------------

I've fixed several bugs/slow performance problems and added a copy of the [Broadlink Protocol](https://github.com/bp2008/broadlink-dotnet/blob/master/BroadlinkProtocol.md).
Code improvements include:
* Eliminated use of `var` in Broadlink.Net library.
* Requests return when they get a response, without arbitrarily waiting 3000ms.
* Configurable wait time for device discovery, optionally returning after the first response.
* Added RMCommand class to provide a higher level interface to learning and sending commands (previously, you worked with raw byte arrays)
* Encrypted buffers are properly padded with zeros as expected by the device, improving stability.
* testApp console application extended to provide a more convenient testing interface.

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
