﻿/*
MIT License

Copyright (c) 2019 Digital Ruby, LLC - https://www.digitalruby.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DigitalRuby.IPBan
{
    /// <summary>
    /// Extension methods for IPBan
    /// </summary>
    public static class IPBanExtensionMethods
    {
        private static readonly Encoding utf8EncodingNoPrefix = new UTF8Encoding(false);
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Throw ArgumentNullException if obj is null
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="name">Parameter name</param>
        /// <param name="message">Message</param>
        public static void ThrowIfNull(this object obj, string name = null, string message = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name ?? string.Empty, message);
            }
        }

        /// <summary>
        /// Convert an object to string using invariant culture
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="defaultValue">Default value if null</param>
        /// <returns>String</returns>
        public static string ToStringInvariant(this object obj, string defaultValue = "")
        {
            return Convert.ToString(obj, CultureInfo.InvariantCulture) ?? defaultValue;
        }

        /// <summary>
        /// Convert utf-8 bytes to string
        /// </summary>
        /// <param name="bytes">Bytes</param>
        /// <returns>Utf-8 decoded string</returns>
        public static string ToStringUTF8(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Url encode a string
        /// </summary>
        /// <param name="text">String to url encode</param>
        /// <returns>Url encoded string</returns>
        public static string UrlEncode(this string text)
        {
            return HttpUtility.UrlEncode(text ?? string.Empty);
        }

        /// <summary>
        /// Write 7 bit encoded int
        /// </summary>
        /// <param name="writer">BinaryWriter</param>
        /// <param name="value">Value</param>
        public static void Write7BitEncodedInt32(this BinaryWriter writer, int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }

        /// <summary>
        /// Read 7 bit encoded int
        /// </summary>
        /// <param name="reader">BinaryReader</param>
        /// <returns>Value</returns>
        public static int Read7BitEncodedInt32(this BinaryReader reader)
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException();

                // ReadByte handles end of stream cases for us.
                b = reader.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        /// <summary>
        /// Covnert a secure string to a non-secure string
        /// </summary>
        /// <param name="s">SecureString</param>
        /// <returns>Non-secure string</returns>
        public static string ToUnsecureString(this SecureString s)
        {
            if (s == null)
            {
                return null;
            }
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(s);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        /// <summary>
        /// Convert a secure string to non-secure binary data using utf-8 encoding
        /// </summary>
        /// <param name="s">SecureString</param>
        /// <returns>Binary data</returns>
        public static byte[] ToUnsecureBytes(this SecureString s)
        {
            if (s == null)
            {
                return null;
            }
            return Encoding.UTF8.GetBytes(ToUnsecureString(s));
        }

        /// <summary>
        /// Convert a string to a secure string
        /// </summary>
        /// <param name="unsecure">Plain text string</param>
        /// <returns>SecureString</returns>
        public static SecureString ToSecureString(this string unsecure)
        {
            if (unsecure == null)
            {
                return null;
            }
            SecureString secure = new SecureString();
            foreach (char c in unsecure)
            {
                secure.AppendChar(c);
            }
            return secure;
        }

        /// <summary>
        /// Get utf-8 bytes from a string
        /// </summary>
        /// <param name="s">String</param>
        /// <returns>UTF8 bytes or null if s is null</returns>
        public static byte[] ToBytesUTF8(this string s)
        {
            if (s == null)
            {
                return null;
            }
            return utf8EncodingNoPrefix.GetBytes(s);
        }

        /// <summary>
        /// Convert an object to an http header value string
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Http header value string</returns>
        public static string ToHttpHeaderString(this object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            else if (obj is SecureString secureString)
            {
                return secureString.ToUnsecureString();
            }
            return obj.ToStringInvariant();
        }

        /// <summary>
        /// Get a UTC date time from a unix epoch in milliseconds
        /// </summary>
        /// <param name="unixTimeStampSeconds">Unix epoch in milliseconds</param>
        /// <returns>UTC DateTime</returns>
        public static DateTime UnixTimeStampToDateTimeMilliseconds(this double unixTimeStampMilliseconds)
        {
            return unixEpoch.AddMilliseconds(unixTimeStampMilliseconds);
        }

        /// <summary>
        /// Get a unix timestamp in milliseconds from a DateTime
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <returns>Unix timestamp in milliseconds</returns>
        public static double UnixTimestampFromDateTimeMilliseconds(this DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                dt = dt.ToUniversalTime();
            }
            return (dt - unixEpoch).TotalMilliseconds;
        }

        /// <summary>
        /// An extension method to determine if an IP address is internal, as specified in RFC1918
        /// </summary>
        /// <param name="ip">The IP address that will be tested</param>
        /// <returns>Returns true if the IP is internal, false if it is external</returns>
        public static bool IsInternal(this System.Net.IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] bytes = ip.GetAddressBytes();
                switch (bytes[0])
                {
                    case 10:
                    case 127:
                        return true;
                    case 172:
                        return bytes[1] >= 16 && bytes[1] < 32;
                    case 192:
                        return bytes[1] == 168;
                    default:
                        return (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0);
                }
            }

            string addressAsString = ip.ToString();
            string firstWord = addressAsString.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0];

            // equivalent of 127.0.0.1 in IPv6
            if (addressAsString == "::1")
            {
                return true;
            }

            // The original IPv6 Site Local addresses (fec0::/10) are deprecated. Unfortunately IsIPv6SiteLocal only checks for the original deprecated version:
            else if (ip.IsIPv6SiteLocal)
            {
                return true;
            }

            // These days Unique Local Addresses (ULA) are used in place of Site Local. 
            // ULA has two variants: 
            //      fc00::/8 is not defined yet, but might be used in the future for internal-use addresses that are registered in a central place (ULA Central). 
            //      fd00::/8 is in use and does not have to registered anywhere.
            else if (firstWord.Length >= 4 && firstWord.Substring(0, 2) == "fc")
            {
                return true;
            }
            else if (firstWord.Length >= 4 && firstWord.Substring(0, 2) == "fd")
            {
                return true;
            }
            // Link local addresses (prefixed with fe80) are not routable
            else if (firstWord == "fe80")
            {
                return true;
            }
            // Discard Prefix
            else if (firstWord == "100")
            {
                return true;
            }

            // Any other IP address is not Unique Local Address (ULA)
            return false;
        }

        /// <summary>
        /// Get a UInt32 from an ipv4 address. By default, the UInt32 will be in the byte order of the CPU.
        /// </summary>
        /// <param name="ip">IPV4 address</param>
        /// <param name="swap">Whether to make the uint in the byte order of the cpu (true) or network host order (false)</param>
        /// <returns>UInt32</returns>
        /// <exception cref="InvalidOperationException">Not an ipv4 address</exception>
        public static uint ToUInt32(this IPAddress ip, bool swap = true)
        {
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new InvalidOperationException("Not an ipv4 address");
            }

            byte[] bytes = ip.GetAddressBytes();
            if (swap && BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }
            return BitConverter.ToUInt32(bytes);
        }

        /// <summary>
        /// Get a UInt128 from an ipv6 address. The UInt128 will be in the byte order of the CPU.
        /// </summary>
        /// <param name="ip">IPV6 address</param>
        /// <returns>UInt128</returns>
        /// <exception cref="ArgumentOutOfRangeException">Not an ipv6 address</exception>
        public static UInt128 ToUInt128(this IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes().Reverse().ToArray();
            ulong l1 = BitConverter.ToUInt64(bytes, 0);
            ulong l2 = BitConverter.ToUInt64(bytes, 8);
            return new UInt128(l2, l1);
        }

        /// <summary>
        /// Get an ip address from a UInt32. By default, the value is assumed to be in the byte order of the CPU.
        /// </summary>
        /// <param name="value">UInt32</param>
        /// <param name="swap">Whether to swap to network host order if needed</param>
        /// <returns>IPAddress</returns>
        public static IPAddress ToIPAddress(this uint value, bool swap = true)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (swap && BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }
            return new IPAddress(bytes);
        }

        /// <summary>
        /// Get an ip address from a UInt128. The UInt128 is assumed to be in the byte order of the CPU.
        /// </summary>
        /// <param name="value">UInt128</param>
        /// <returns>IPAddress</returns>
        public static IPAddress ToIPAddress(this UInt128 value)
        {
            byte[] bytes1 = BitConverter.GetBytes(value.MostSignificant);
            byte[] bytes2 = BitConverter.GetBytes(value.LeastSignificant);
            byte[] finalBytes;
            if (BitConverter.IsLittleEndian)
            {
                bytes1 = bytes1.Reverse().ToArray();
                bytes2 = bytes2.Reverse().ToArray();
                finalBytes = bytes1.Concat(bytes2).ToArray();
            }
            else
            {
                finalBytes = bytes2.Concat(bytes1).ToArray();
            }
            return new IPAddress(finalBytes);
        }

        [DllImport("libc")]
        public static extern uint getuid();

        /// <summary>
        /// Asks for administrator privileges upgrade if the platform supports it, otherwise does nothing
        /// </summary>
        public static void RequireAdministrator()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                        {
                            throw new InvalidOperationException("Application must be run as administrator");
                        }
                    }
                }
                else if (getuid() != 0)
                {
                    throw new InvalidOperationException("Application must be run as administrator");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to determine administrator status", ex);
            }
        }

        /// <summary>
        /// Make a task execute synchronously
        /// </summary>
        /// <param name="task">Task</param>
        public static void Sync(this Task task)
        {
            task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Make a task execute synchronously
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns>Result</returns>
        public static T Sync<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
