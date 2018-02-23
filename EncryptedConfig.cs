/*
                           __                           __     
                         /'__`\                  __    /\ \    
 _____      __     _ __ /\ \/\ \    ___     ___ /\_\   \_\ \   
/\ '__`\  /'__`\  /\`'__\ \ \ \ \ /' _ `\  / __`\/\ \  /'_` \  
\ \ \L\ \/\ \L\.\_\ \ \/ \ \ \_\ \/\ \/\ \/\ \L\ \ \ \/\ \L\ \ 
 \ \ ,__/\ \__/.\_\\ \_\  \ \____/\ \_\ \_\ \____/\ \_\ \___,_\
  \ \ \/  \/__/\/_/ \/_/   \/___/  \/_/\/_/\/___/  \/_/\/__,_ /
   \ \_\                                                       
    \/_/                                      addicted to code


Copyright (C) 2018  Stefan 'par0noid' Zehnpfennig

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;

namespace par0noid
{
    public class EncryptedConfig : Config
    {
        private string _Password, _Path;
        private Encoding _Encoding;

        /// <summary>
        /// Converts the password to md5-byte-array
        /// </summary>
        private byte[] HashedPWBytes => Encoding.Default.GetBytes(BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(_Password))).Replace("-", "").ToLower());

        /// <summary>
        /// Initializes an empty encrypted config
        /// </summary>
        /// <param name="Password">Password to protect the config</param>
        public EncryptedConfig(string Password) : base()
        {
            _Password = Password;
            _Encoding = Encoding.UTF8;
        }

        /// <summary>
        /// Initializes an encrypted config with the content of the given file
        /// </summary>
        /// <param name="Path">Path to config file</param>
        /// <param name="Password">Password of the config file</param>
        /// <param name="ConfigEncoding">Encoding of the config file</param>
        public EncryptedConfig(string Path, string Password, Encoding ConfigEncoding = null)
        {
            _Encoding = ConfigEncoding == null ? Encoding.UTF8 : ConfigEncoding;
            _Password = Password;
            _Path = Path;

            byte[] encryptedConfig;

            try
            {
                encryptedConfig = File.ReadAllBytes(Path);
            }
            catch { throw new FileNotFoundException("Cannot read configfile"); }

            try
            {
                encryptedConfig = Decrypt(encryptedConfig);
            }
            catch { throw new FileNotFoundException("Cannot decrypt configfile"); }

            ParseConfig(_Encoding.GetString(encryptedConfig));
        }

        /// <summary>
        /// Private constructor for static object conversion
        /// </summary>
        /// <param name="Password">Password for the encrypted config file</param>
        /// <param name="ConfigObject">Object of config class</param>
        private EncryptedConfig(string Password, Config ConfigObject)
        {
            _Password = Password;
            _Encoding = ConfigObject.Encoding;
            _Path = ConfigObject.Path;

            ParseConfig(ConfigObject.ToString());
        }

        /// <summary>
        /// Converts a normal config to an encrypted config
        /// </summary>
        /// <param name="Password">Password for the encrpted config</param>
        /// <param name="ConfigObject">Object of config class</param>
        /// <returns>EncryptedConfig-Object</returns>
        public static EncryptedConfig CreateFromConfig(string Password, Config ConfigObject) => new EncryptedConfig(Password, ConfigObject);

        /// <summary>
        /// Saves the config to the initial config path. It will return false if the config is not initialized with a path.
        /// </summary>
        /// <returns>true on success, false if the config is not initialized with a path or the config file can't be written</returns>
        public override bool Save()
        {
            if (_Path == null)
            {
                return false;
            }

            byte[] data = _Encoding.GetBytes(base.ToString());

            try
            {

                File.WriteAllBytes(_Path, Encrypt(data));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the config to the given path
        /// </summary>
        /// <returns>true on success, false if the config file can't be written</returns>
        public override bool Save(string Path)
        {
            _Path = Path;
            return Save();
        }

        /// <summary>
        /// For internal prupose Parsing the config from an string.
        /// </summary>
        /// <param name="Content">Config-Content as string</param>
        private void ParseConfig(string Content)
        {
            string[] ConfigLines = Content.Replace("\r", "").Split('\n');

            string CurrentSection = "default";

            foreach (string Line in ConfigLines)
            {
                if (Line.Contains("=") || Line.Contains("["))
                {
                    if (Line.Replace(" ", "").Replace("\t", "").StartsWith("#") || Line.Replace(" ", "").Replace("\t", "").StartsWith("//"))
                    {
                        //Auskommentiert
                    }
                    else
                    {
                        if (Line.Contains("="))
                        {
                            //Entry

                            string[] splitted = Line.Split(new char[] { '=' });

                            string Key = splitted[0].Replace(" ", "").Replace("\t", "");

                            string[] ValueSplitted = new string[splitted.Length - 1];

                            Array.Copy(splitted, 1, ValueSplitted, 0, ValueSplitted.Length);

                            string Value = string.Join("=", ValueSplitted).TrimStart(new char[] { ' ', '\t' });

                            Add(CurrentSection, Key, Value);

                        }
                        else
                        {
                            //Section

                            string CleanedLine = Line.Replace(" ", "").Replace("\t", "");

                            Regex r = new Regex(@"^\[(?<section>[\w\-\.]+)\]$", RegexOptions.None);

                            if (r.IsMatch(CleanedLine))
                            {
                                Match m = r.Match(CleanedLine);

                                Add(m.Groups["section"].ToString());
                                CurrentSection = m.Groups["section"].ToString();
                            }
                        }
                    }

                }
                else
                {
                    //Kein Eintrag
                }
            }
        }

        /// <summary>
        /// Decrypt data. For internal prupose.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Decrypt(byte[] data)
        {
            byte[] IV = new byte[16];
            byte[] EncryptedData = new byte[data.Length - 16];
            Array.Copy(data, 0, IV, 0, 16);
            Array.Copy(data, 16, EncryptedData, 0, EncryptedData.Length);

            byte[] Result = null;

            using (AesManaged AES = new AesManaged())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Key = HashedPWBytes;
                AES.IV = IV;

                ICryptoTransform aes_decryptor = AES.CreateDecryptor(AES.Key, AES.IV);

                using (MemoryStream ms = new MemoryStream(EncryptedData))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes_decryptor, CryptoStreamMode.Read))
                    {
                        byte[] buffer = new byte[1024];

                        int readed = 0;

                        do
                        {
                            readed = cs.Read(buffer, 0, buffer.Length);

                            if(readed > 0)
                            {
                                if (Result == null)
                                {
                                    Result = new byte[readed];
                                    Array.Copy(buffer, 0 , Result, 0, readed);
                                }
                                else
                                {
                                    int offset = Result.Length;
                                    Array.Resize(ref Result, Result.Length+readed);
                                    Array.Copy(buffer, 0, Result, offset, readed);
                                }
                            }
                            

                        } while (readed > 0);
                    }

                    return Result;
                }
            }
        }

        /// <summary>
        /// Encrypt data. For internal prupose.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Encrypt(byte[] data)
        {
            List<byte> result = new List<byte>();

            using (AesManaged AES = new AesManaged())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Key = HashedPWBytes;
                AES.IV = GenerateIV();
                result.AddRange(AES.IV);

                ICryptoTransform aes_encryptor = AES.CreateEncryptor(AES.Key, AES.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes_encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                    }
                    result.AddRange(ms.ToArray());
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Generates an IV. For internal prupose.
        /// </summary>
        /// <returns>IV as byte[]</returns>
        private byte[] GenerateIV()
        {
            using (AesManaged AES = new AesManaged())
            {
                AES.BlockSize = 128;
                AES.GenerateIV();
                return AES.IV;
            }
        }

    }
}
