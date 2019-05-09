using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Broadlink.Net
{
	/// <summary>
	/// Represents a command for a BroadLink RM module.
	/// </summary>
	public class RMCommand
	{
		public const double BroadlinkPulseDurationInMicroSeconds = 30.5175781;
		public RMCommandType Type;
		public byte RepeatCount;
		public byte[] RawPulseData;
		public RMCommand() { }
		/// <summary>
		/// Creates a command from the binary data received from a Broadlink RM module.
		/// </summary>
		/// <param name="raw"></param>
		/// <returns></returns>
		public static RMCommand FromBinary(byte[] raw)
		{
			try
			{
				if (raw.Length < 8 || raw[0] != 4 || raw[1] != 0 || raw[2] != 0 || raw[3] != 0)
					return null;
				RMCommand cmd = new RMCommand();
				cmd.Type = (RMCommandType)raw[4];
				cmd.RepeatCount = raw[5];
				ushort dataLength = ByteArrayExtensions.ReadUInt16LE(raw, 6);
				cmd.RawPulseData = new byte[dataLength];
				Array.Copy(raw, 8, cmd.RawPulseData, 0, dataLength);
				return cmd;
			}
			catch
			{
				return null;
			}
		}
		/// <summary>
		/// Gets the binary form of the command suitable for sending to a BroadLink RM module.
		/// </summary>
		/// <returns></returns>
		public byte[] ToBinary()
		{
			byte[] data = new byte[RawPulseData.Length + 4]; // But 4 bytes of the header do not come from here.
			data[0] = (byte)Type;
			data[1] = RepeatCount;
			Array.Copy(((short)RawPulseData.Length).ToLittleEndianBytes(), 0, data, 2, 2);
			Array.Copy(RawPulseData, 0, data, 4, RawPulseData.Length);
			return data;
		}
		/// <summary>
		/// Returns an array of pulse lengths.  1 pulse is expected to be (2 ^ -15) seconds, or 30.5175781 microseconds.  So a pulse length of 4 would be approximately 122 microseconds.
		/// </summary>
		/// <returns></returns>
		public ushort[] GetPulses()
		{
			List<ushort> broadlinkPulseData = new List<ushort>(RawPulseData.Length);
			for (int i = 0; i < RawPulseData.Length; i++)
			{
				if (RawPulseData[i] == 0)
				{
					if (i + 2 < RawPulseData.Length && RawPulseData[i + 1] != 0 && RawPulseData[i + 2] != 0)
					{
						ushort mostSignificantByte = RawPulseData[++i];
						ushort leastSignificantByte = RawPulseData[++i];
						broadlinkPulseData.Add((ushort)((mostSignificantByte << 8) | leastSignificantByte));
					}
					else
						break;
				}
				else
					broadlinkPulseData.Add((ushort)RawPulseData[i]);
			}
			return broadlinkPulseData.ToArray();
		}
		/// <summary>
		/// Returns an array of pulse lengths with each value in microseconds, to aid in interoperability with other IR systems.
		/// </summary>
		/// <returns></returns>
		public double[] GetMicrosecondPulses()
		{
			return GetPulses().Select(p => p * BroadlinkPulseDurationInMicroSeconds).ToArray();
		}
		/// <summary>
		/// Sets the RawPulseData array from an array of broadlink pulse data (e.g. from the GetPulses method).
		/// </summary>
		/// <param name="broadlinkPulseData">An array of pulse lengths in broadlink units.</param>
		public void SetPulses(ushort[] broadlinkPulseData)
		{
			List<byte> raw = new List<byte>(broadlinkPulseData.Length * 2);
			bool pulseDataEvenLength = broadlinkPulseData.Length % 2 == 0;
			for (int i = 0; i < broadlinkPulseData.Length; i++)
			{
				ushort p = broadlinkPulseData[i];
				if (i + 1 == broadlinkPulseData.Length && p != 3333 && pulseDataEvenLength)
					p = 3333; // Broadlink often has a value of 3333 (0x50D) at the end of learned commands.  I don't know if it is actually necessary.
				ConvertBroadlinkPulseToBytes(raw, Clamp(p, (ushort)1, (ushort)short.MaxValue)); // Clamp to range [1-32767]
			}
			if (!pulseDataEvenLength)
			{
				// We ended without an "off" pulse.  
				ConvertBroadlinkPulseToBytes(raw, 3333); // End with Broadlink's sentinel value
			}
			RawPulseData = raw.ToArray();
		}

		private static void ConvertBroadlinkPulseToBytes(List<byte> raw, ushort p)
		{
			if (p <= byte.MaxValue)
				raw.Add((byte)p);
			else
			{
				// If the length exceeds one byte then it is stored big endian with a leading 0.
				raw.Add(0);
				raw.Add((byte)((p & 0b1111_1111_0000_0000) >> 8));
				raw.Add((byte)(p & 0b0000_0000_1111_1111));
			}
		}

		/// <summary>
		/// Sets the RawPulseData array from an array of pulse lengths in microseconds (e.g. from the GetMicrosecondPulses method)).
		/// </summary>
		/// <param name="microsecondPulseData">An array of pulse lengths in microsecond units.</param>
		public void SetPulses(double[] microsecondPulseData)
		{
			ushort[] myShorts = microsecondPulseData.Select(p =>
			{
				return (ushort)Math.Round(p / BroadlinkPulseDurationInMicroSeconds);
			}
			).ToArray();
			SetPulses(myShorts);
		}
		private static T Clamp<T>(T v, T min, T max) where T : IComparable<T>
		{
			if (v.CompareTo(min) < 0)
				return min;
			else if (v.CompareTo(max) > 0)
				return max;
			else
				return v;
		}
	}
}
