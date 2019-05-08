using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Broadlink.Net
{
	/// <summary>
	/// Represents a remote control (IR/RF) device
	/// </summary>
	public class RMDevice : BroadlinkDevice
	{
		/// <summary>
		/// Start the remote control command learning mode.
		/// </summary>
		/// <returns></returns>
		public async Task EnterLearningModeAsync()
		{
			byte[] packet = PacketGenerator.GenerateStartLearningModePacket(this);
			await SendAsync(packet);
		}

		/// <summary>
		/// Read the data for a remote control command.  This takes the device out of learning mode.
		/// </summary>
		/// <returns>Byte array containing the packet for <see cref="SendRemoteCommandAsync(byte[])" /></returns>
		public async Task<RMCommand> ReadLearningDataAsync()
		{
			byte[] packet = PacketGenerator.GenerateReadLearningModePacket(this);
			byte[] encryptedResponse = await SendAndWaitForResponseAsync(packet);

			short errorCode = BitConverter.ToInt16(encryptedResponse, 0x22);
			if (errorCode != 0)
			{
				throw new Exception($"Error {errorCode} in learning response");
			}

			byte[] encryptedPayload = encryptedResponse.Slice(0x38);

			byte[] payload = encryptedPayload.Decrypt(EncryptionKey);

			return RMCommand.FromBinary(payload);
		}

		/// <summary>
		/// Execute a remote control command
		/// </summary>
		/// <param name="data">Packet obtained using <see cref="ReadLearningDataAsync()" /></param>
		/// <returns></returns>
		public async Task SendRemoteCommandAsync(RMCommand cmd)
		{
			byte[] packet = PacketGenerator.GenerateSendDataPacket(this, cmd.ToBinary());
			await SendAsync(packet);
		}

		/// <summary>
		/// Execute a remote control command
		/// </summary>
		/// <param name="data">Packet obtained using <see cref="ReadLearningDataAsync()" /></param>
		/// <returns></returns>
		public async Task SendRemoteCommandAsync(byte[] data)
		{
			byte[] packet = PacketGenerator.GenerateSendDataPacket(this, data);
			await SendAsync(packet);
		}

		/// <summary>
		/// Get the temperature
		/// </summary>
		/// <returns>temperature in degrees Celsius</returns>
		public async Task<float> GetTemperatureAsync()
		{
			byte[] packet = PacketGenerator.GenerateReadTemperaturePacket(this);
			byte[] encryptedResponse = await SendAndWaitForResponseAsync(packet);

			short errorCode = BitConverter.ToInt16(encryptedResponse, 0x22);
			if (errorCode != 0)
			{
				throw new Exception($"Error {errorCode} in temperature response");
			}

			byte[] encryptedPayload = encryptedResponse.Slice(0x38);

			byte[] payload = encryptedPayload.Decrypt(EncryptionKey);
			byte[] temperatureData = payload.Slice(0x04, 0x05);
			float temperature = temperatureData[0] + (float)temperatureData[1] / 10;
			return temperature;
		}
	}
}
