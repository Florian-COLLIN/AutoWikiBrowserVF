﻿/*
Copyright (C) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

namespace WikiFunctions
{
    /// <summary>
    /// Provides access to the local computer registry below HKEY_CURRENT_USER\Software\AutoWikiBrowser only
    /// </summary>
    /// <remarks>Clients should implement their own error handling</remarks>
    public static class RegistryUtils
    {
        private const string KeyPrefix = "Software\\AutoWikiBrowser\\";
        private static Microsoft.Win32.RegistryKey registryKey = new Microsoft.VisualBasic.Devices.Computer().Registry.CurrentUser;

        /// <summary>
        /// Gets a string value from an AWB registry subkey
        /// </summary>
        /// <param name="keyNameSuffix"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetValue(string keyNameSuffix, object defaultValue)
        {
            string wantedKey = keyNameSuffix.Substring(keyNameSuffix.LastIndexOf("\\"));
            Microsoft.Win32.RegistryKey regKey = registryKey.OpenSubKey(BuildKeyName(keyNameSuffix.Replace(wantedKey, "")));
            return regKey.GetValue(wantedKey.Replace("\\", ""), defaultValue).ToString(); 
        }

        /// <summary>
        /// Writes a string value to an AWB registry subkey
        /// </summary>
        /// <param name="keyNameSuffix"></param>
        /// <param name="valueName"></param>
        /// <param name="value"></param>
        public static void SetValue(string keyNameSuffix, string valueName, string value)
        { GetWritableKey(keyNameSuffix).SetValue(valueName, value); }

        /// <summary>
        /// Opens or creates a writable registry key from the AWB registry area
        /// </summary>
        /// <param name="keyNameSuffix"></param>
        /// <returns></returns>
        public static Microsoft.Win32.RegistryKey GetWritableKey(string keyNameSuffix)
        {
            // Note that CreateSubKey() creates a new subkey *or opens an existing key for write access*
            return registryKey.CreateSubKey(BuildKeyName(keyNameSuffix));
        }

        /// <summary>
        /// Opens a read-only registry key from the AWB registry area
        /// </summary>
        /// <param name="keyNameSuffix"></param>
        /// <returns></returns>
        public static Microsoft.Win32.RegistryKey OpenSubKey(string keyNameSuffix)
        { return registryKey.OpenSubKey(BuildKeyName(keyNameSuffix)); }

        /// <summary>
        /// Deletes a sub key
        /// </summary>
        public static void DeleteSubKey(string keyNameSuffix, bool throwOnMissingSubKey)
        { registryKey.DeleteSubKey(BuildKeyName(keyNameSuffix), throwOnMissingSubKey); }

        /// <summary>
        /// Deletes a sub key
        /// </summary>
        public static void DeleteSubKey(string keyNameSuffix)
        { registryKey.DeleteSubKey(BuildKeyName(keyNameSuffix)); }

        private static string BuildKeyName(string keyNameSuffix)
        { return KeyPrefix + keyNameSuffix; }
    }

    namespace Encryption
    {
        /// <summary>
        /// Provides a friendly wrapper around the RijndaelSimple class
        /// </summary>
        public class EncryptionUtils
        {
            internal readonly string IV16Chars;
            internal readonly string PassPhrase;
            internal readonly string Salt;

            public EncryptionUtils(string InitVector, string PassPhrase, string Salt)
            {
                this.IV16Chars = InitVector;
                this.PassPhrase = PassPhrase;
                this.Salt = Salt;
            }

            /// <summary>
            /// Encrypts a string
            /// </summary>
            /// <param name="text">String to be encrypted</param>
            /// <returns>Encrypted string</returns>
            public string Encrypt(string text)
            {
                try
                {
                    if (!string.IsNullOrEmpty(text))
                        return RijndaelSimple.Encrypt(text, PassPhrase, Salt, "SHA1", 2, IV16Chars, 256);

                    return text;
                }
                catch { return text; }
            }

            /// <summary>
            /// Decrypts a string
            /// </summary>
            /// <param name="text">String to be decrypted</param>
            /// <returns>Decrypted string</returns>
            public string Decrypt(string text)
            {
                try
                {
                    if (!string.IsNullOrEmpty(text))
                        return RijndaelSimple.Decrypt(text, PassPhrase, Salt, "SHA1", 2, IV16Chars, 256);

                    return text;
                }
                catch { return text; }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="keyNameSuffix"></param>
            /// <param name="defaultValue"></param>
            /// <returns></returns>
            public string RegistryGetValueAndDecrypt(string keyNameSuffix, object defaultValue)
            { return Decrypt(RegistryUtils.GetValue(keyNameSuffix, defaultValue)); }
        }
    }
}
