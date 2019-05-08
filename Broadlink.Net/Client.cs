﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Broadlink.Net
{
	public class Client
	{
		private IPEndPoint LocalIPEndPoint { get; set; }

		public Client()
		{
			LocalIPEndPoint = GetLocalIpEndpoint();
		}


		/// <summary>
		/// Discovers Broadlink devices.
		/// </summary>
		/// <param name="waitLimit">Milliseconds to wait for responses to the discovery broadcast.</param>
		/// <param name="returnAfterOne">If true, this method will return as soon as one device responds without waiting for <paramref name="waitLimit"/> to elapse.</param>
		/// <returns></returns>
		public async Task<List<BroadlinkDevice>> DiscoverAsync(int waitLimit = 3000, bool returnAfterOne = false)
		{
			List<BroadlinkDevice> discoveredDevices = new List<BroadlinkDevice>();

			byte[] discoveryPacket = PacketGenerator.GenerateDiscoveryPacket(LocalIPEndPoint.Address, (short)LocalIPEndPoint.Port);

			IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, 80);

			using (UdpClient client = new UdpClient(LocalIPEndPoint))
			{
				await client.SendAsync(discoveryPacket, discoveryPacket.Length, ep);
				Debug.WriteLine("Message sent to the broadcast address");

				long waited;
				Stopwatch sw = new Stopwatch();
				sw.Start();
				while ((waited = sw.ElapsedMilliseconds) < waitLimit)
				{
					Task<UdpReceiveResult> receiveTask = client.ReceiveAsync();

					if (receiveTask.Wait((int)(waitLimit - waited)) && receiveTask.Result != null)
					{
						byte[] response = receiveTask.Result.Buffer;
						if (response != null)
						{
							byte[] macArray = new byte[6];
							Array.Copy(response, 0x3a, macArray, 0, 6);

							BroadlinkDevice discoveredDevice = CreateBroadlinkDevice(BitConverter.ToInt16(response, 0x34));
							discoveredDevice.LocalIPEndPoint = LocalIPEndPoint;
							discoveredDevice.EndPoint = receiveTask.Result.RemoteEndPoint;
							discoveredDevice.MacAddress = macArray;

							discoveredDevices.Add(discoveredDevice);
							if (returnAfterOne)
								break;
						}
					}
				}

				return discoveredDevices;
			}
		}

		private BroadlinkDevice CreateBroadlinkDevice(short deviceType)
		{
			BroadlinkDevice device;
			switch (deviceType)
			{
				case 0x2712: // RM2
				case 0x2737: // RM Mini
				case 0x273d: // RM Pro Phicomm
				case 0x2783: // RM2 Home Plus
				case 0x277c: // RM2 Home Plus GDT
				case 0x272a: // RM2 Pro Plus
				case 0x2787: // RM2 Pro Plus2
				case 0x278b: // RM2 Pro Plus BL
				case 0x278f: // RM Mini Shate
					device = new RMDevice();
					break;
				default:
					device = new BroadlinkDevice();
					break;
			}
			device.DeviceType = deviceType;
			return device;
		}

		private IPEndPoint GetLocalIpEndpoint()
		{
			using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect("8.8.8.8", 53);
				IPEndPoint localIpEndpoint = socket.LocalEndPoint as IPEndPoint;
				localIpEndpoint.Address = localIpEndpoint.Address.MapToIPv4();
				return localIpEndpoint;
			}
		}


	}
}
