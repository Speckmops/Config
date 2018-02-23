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

namespace par0noid
{
    class Program
    {
        static void Main(string[] args)
        {
            //Create a normal config
            Config ConfigObject = new Config();

            //Also possible:
            //Config ConfigObject = new Config(@"C:\config.ini");
            //Config ConfigObject = @"C:\config.ini";
            //Config ConfigObject = new System.IO.FileInfo(@"C:\config.ini");

            //Add sections and values
            ConfigObject.Add("Login", "Username", "par0noid");
            ConfigObject.Add("Login", "Password", "secret");
            ConfigObject.Add("Windows", "Trayicon", true);
            ConfigObject.Add("Game", "Points", 1234);

            ConfigObject["Windows"].Add("Color", "black"); //Add entry to a specific section

            ConfigObject["Game"]["Points"].Value = 1000; //Since Value is an object setter, it calls ToString() if you want to set it
                
            if (ConfigObject["Windows"]["Trayicon"]) //automatic conversion to bool (true, on and 1)
            {
                Console.WriteLine($"{ConfigObject["Login"]["Username"]} has an activated trayicon");
            }

            //Save the config
            ConfigObject.Save($@"{Environment.CurrentDirectory}\Config.ini");

            //Use can also use an encrypted config. It is also possible to convert a normal config to an encrypted one
            EncryptedConfig ConfigEncrypted = EncryptedConfig.CreateFromConfig("secret", ConfigObject);

            //Working with the encrypted works like the normal config
            ConfigEncrypted.Add("Test", "Foo", "Bar");

            //Save the encrypted config to another file
            ConfigEncrypted.Save($@"{Environment.CurrentDirectory}\Config_encrypted.ini");

            Console.ReadKey();
        }
    }
}
