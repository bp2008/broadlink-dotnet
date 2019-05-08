using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace Broadlink.Net
{
	public static class PacketGenerator
	{
		public static byte[] GenerateDiscoveryPacket(IPAddress localIp, short sourcePort)
		{
			byte[] packet = new byte[48];

			TimeZoneInfo timezone = TimeZoneInfo.Local;
			int offsetFromGmt = timezone.BaseUtcOffset.Hours;

			byte[] offsetFromGmtBytes = offsetFromGmt.ToLittleEndianBytes();
			offsetFromGmtBytes.CopyTo(packet, 0x08);

			DateTime now = DateTime.Now;
			byte[] yearBytes = ((short)now.Year).ToLittleEndianBytes();
			yearBytes.CopyTo(packet, 0x0c);

			packet[0x0e] = (byte)now.Minute;
			packet[0x0f] = (byte)now.Hour;
			packet[0x10] = (byte)int.Parse(now.Year.ToString().Substring(2));
			packet[0x11] = (byte)now.DayOfWeek;
			packet[0x12] = (byte)now.Day;
			packet[0x13] = (byte)now.Month;

			localIp.MapToIPv4().GetAddressBytes().CopyTo(packet, 0x18);
			sourcePort.ToLittleEndianBytes().CopyTo(packet, 0x1c);

			packet[0x26] = 6;

			GenerateChecksum(packet).CopyTo(packet, 0x20);

			return packet;
		}

		private static byte[] GenerateChecksum(byte[] packet)
		{
			int checksum = 0xbeaf;

			for (int i = 0; i < packet.Length; i++)
			{
				checksum += packet[i];
			}

			checksum &= 0xffff;

			return checksum.ToLittleEndianBytes();
		}

		public static byte[] GenerateAuthorizationPacket(BroadlinkDevice device)
		{
			byte[] payload = GenerateAuthorizationPayload();

			short command = (short)0x0065;
			byte[] packet = GenerateCommandPacket(command, payload, device);
			return packet;
		}

		private static byte[] GenerateAuthorizationPayload()
		{
			byte[] payload = new byte[80];

			IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
			string hostname = properties.HostName;
			byte[] hostnameBytes = Encoding.ASCII.GetBytes(hostname);

			// addresses 0x04-0x12
			ByteArrayExtensions.FillFrom(hostnameBytes, payload, 0x04, 0x12);

			// TODO conflict between protocol documentation and python implementation
			payload[0x13] = 0x01;
			//payload[0x1e] = 0x01;

			payload[0x2d] = 0x01;

			// addresses 0x30-0x7f  (this would require a 128 byte (0x80) payload, but documentation says 80 byte (0x50).  Argh)
			ByteArrayExtensions.FillFrom(hostnameBytes, payload, 0x30, 79);

			return payload;
		}

		public static byte[] GenerateReadTemperaturePacket(RMDevice device)
		{
			byte[] payload = new byte[16];
			payload[0x00] = 1;

			short command = (short)0x006a;

			byte[] packet = GenerateCommandPacket(command, payload, device);
			return packet;
		}

		private static byte[] GenerateCommandPacket(short commandCode, byte[] payload, BroadlinkDevice device)
		{
			byte[] header = new byte[56];

			header[0x00] = 0x5a;
			header[0x01] = 0xa5;
			header[0x02] = 0xaa;
			header[0x03] = 0x55;
			header[0x04] = 0x5a;
			header[0x05] = 0xa5;
			header[0x06] = 0xaa;
			header[0x07] = 0x55;

			header[0x24] = 0x2a;
			header[0x25] = 0x27;

			commandCode.ToLittleEndianBytes().CopyTo(header, 0x26);
			device.PacketCount.ToLittleEndianBytes().CopyTo(header, 0x28);

			device.MacAddress.CopyTo(header, 0x2a);

			byte[] deviceId = device.DeviceId;
			if (deviceId == null)
			{
				deviceId = new byte[4];
			}
			deviceId.CopyTo(header, 0x30);

			GenerateChecksum(payload).CopyTo(header, 0x34);

			byte[] encryptedPayload = payload.Encrypt(device.EncryptionKey);

			byte[] packet = new byte[header.Length + encryptedPayload.Length];
			header.CopyTo(packet, 0);
			encryptedPayload.CopyTo(packet, 0x38);

			GenerateChecksum(packet).CopyTo(packet, 0x20);

			return packet;
		}

		public static byte[] GenerateStartLearningModePacket(BroadlinkDevice device)
		{
			byte[] payload = new byte[16];
			payload[0x00] = 0x03;

			short command = (short)0x006a;
			byte[] packet = GenerateCommandPacket(command, payload, device);
			return packet;
		}

		public static byte[] GenerateReadLearningModePacket(BroadlinkDevice device)
		{
			byte[] payload = new byte[16];
			payload[0x00] = 0x04;

			short command = (short)0x006a;
			byte[] packet = GenerateCommandPacket(command, payload, device);
			return packet;
		}

		public static byte[] GenerateSendDataPacket(BroadlinkDevice device, byte[] data)
		{
			byte[] payload = new byte[0x04 + data.Length];
			payload[0x00] = 0x02;
			data.CopyTo(payload, 0x04);

			short command = (short)0x006a;
			byte[] packet = GenerateCommandPacket(command, payload, device);
			return packet;
		}
	}
}
