using System;
using System.Collections.Generic;
using System.Text;

namespace Broadlink.Net
{
	public static class ByteArrayExtensions
	{
		/// <summary>
		/// Convert an <see cref="int"/> value to a <see cref="byte[]"/> with little endian ordering
		/// </summary>
		/// <param name="input"><see cref="int"/> value</param>
		/// <returns><see cref="byte[]"/> with little endian ordering</returns>
		public static byte[] ToLittleEndianBytes(this int input)
		{
			byte[] buf = BitConverter.GetBytes(input);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(buf);
			return buf;
		}
		/// <summary>
		/// Convert a <see cref="short"/> value to a <see cref="byte[]"/> with little endian ordering
		/// </summary>
		/// <param name="input"><see cref="short"/> value</param>
		/// <returns><see cref="byte[]"/> with little endian ordering</returns>
		public static byte[] ToLittleEndianBytes(this short input)
		{
			byte[] buf = BitConverter.GetBytes(input);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(buf);
			return buf;
		}

		/// <summary>
		/// Reads a Little Endian 16 bit unsigned integer from the byte array.
		/// </summary>
		/// <param name="buffer"><see cref="byte[]"/> with a value in little endian ordering</param>
		/// <param name="offset">Offset in the <see cref="byte[]"/> where the value begins</param>
		/// <returns></returns>
		public static ushort ReadUInt16LE(byte[] buffer, int offset)
		{
			if (!BitConverter.IsLittleEndian)
			{
				buffer = SubArray(buffer, offset, 2);
				Array.Reverse(buffer);
				offset = 0;
			}
			return BitConverter.ToUInt16(buffer, offset);
		}

		/// <summary>
		/// Reads a Little Endian 16 bit signed integer from the byte array.
		/// </summary>
		/// <param name="buffer"><see cref="byte[]"/> with a value in little endian ordering</param>
		/// <param name="offset">Offset in the <see cref="byte[]"/> where the value begins</param>
		/// <returns></returns>
		public static short ReadInt16LE(byte[] buffer, int offset)
		{
			if (!BitConverter.IsLittleEndian)
			{
				buffer = SubArray(buffer, offset, 2);
				Array.Reverse(buffer);
				offset = 0;
			}
			return BitConverter.ToInt16(buffer, offset);
		}
		/// <summary>
		/// Returns a new array containing the specified bytes from the source array.
		/// </summary>
		/// <param name="buf">The source byte array.</param>
		/// <param name="offset">The offset to begin copying bytes at.</param>
		/// <param name="length">The number of bytes to copy.</param>
		/// <returns></returns>
		public static byte[] SubArray(byte[] buf, int offset, int length)
		{
			byte[] dst = new byte[length];
			Array.Copy(buf, offset, dst, 0, length);
			return dst;
		}
		///// <summary>
		///// Convenience function to get the slice of a <see cref="byte[]"/> from <paramref name="input"/>
		///// </summary>
		///// <param name="input">byte array to slice</param>
		///// <param name="startIndex">start index to slice from</param>
		///// <returns>slice of <paramref name="input"/> starting from <paramref name="startIndex"/></returns>
		//public static byte[] Slice(this byte[] input, int startIndex)
		//{
		//    byte[] restArray = new byte[input.Length - startIndex];
		//    Array.Copy(input, startIndex, restArray, 0, restArray.Length);
		//    return restArray;
		//}

		/// <summary>
		/// Convenience function to get the slice of a <see cref="byte[]"/>
		/// </summary>
		/// <param name="input">byte array to slice</param>
		/// <param name="startIndex">start index to slice from</param>
		/// <param name="endIndex">endIndex to slice to. Use -1 to slice until the end</param>
		/// <returns>slice of <paramref name="input"/></returns>
		public static byte[] Slice(this byte[] input, int startIndex, int endIndex = -1)
		{
			if (endIndex <= 0)
			{
				endIndex = input.Length - 1;
			}

			if (endIndex <= startIndex || endIndex >= input.Length)
			{
				throw new ArgumentException();
			}

			byte[] restArray = new byte[endIndex - startIndex + 1];

			Array.Copy(input, startIndex, restArray, 0, restArray.Length);
			return restArray;
		}
		/// <summary>
		/// Fills the specified range of the destination <see cref="byte[]"/> with bytes from the source <see cref="byte[]"/> until the range is filled or the source is exhausted.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="destinationRangeFirstOffset"></param>
		/// <param name="destinationRangeLastOffset"></param>
		/// <returns></returns>
		public static void FillFrom(byte[] source, byte[] destination, int destinationRangeFirstOffset, int destinationRangeLastOffset)
		{
			int len = Math.Min(source.Length, (destinationRangeLastOffset - destinationRangeFirstOffset) + 1);
			Array.Copy(source, 0, destination, destinationRangeFirstOffset, len);
		}
	}
}
