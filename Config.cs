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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Config
{
    public class Config
    {
        private List<ConfigSection> _Sections;
        private Encoding _Encoding;
        private string _Path = null;

        /// <summary>
        /// Initializes an empty config
        /// </summary>
        public Config()
        {
            _Sections = new List<ConfigSection>();
            _Encoding = Encoding.UTF8;
        }

        /// <summary>
        /// Initializes a config with the content of the given file
        /// </summary>
        /// <param name="Path">Path to config file</param>
        /// <param name="ConfigEncoding">Encoding of the config file</param>
        public Config(string Path, Encoding ConfigEncoding = null) : this()
        {
            _Encoding = ConfigEncoding == null ? Encoding.UTF8 : ConfigEncoding;


            string[] ConfigLines;

            try
            {
                ConfigLines = File.ReadAllLines(Path, _Encoding);
            }
            catch { throw new FileNotFoundException("File not found"); }

            _Path = Path;

            string CurrentSection = "default";

            foreach(string Line in ConfigLines)
            {
                if(Line.Contains("=") || Line.Contains("["))
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

                            if(r.IsMatch(CleanedLine))
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
        /// Initializes a config with the content of the given file
        /// </summary>
        /// <param name="Path">Path to config file</param>
        public static implicit operator Config(string Path)
        {
            return new Config(Path);
        }

        public static implicit operator string(Config config)
        {
            return config.GenerateConfigContent();
        }

        public override string ToString()
        {
            return this.GenerateConfigContent();
        }

        /// <summary>
        /// Adds a section to the config
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <returns>true on success, false if the section already exists</returns>
        public bool Add(string SectionName)
        {
            if(!HasSection(SectionName))
            {
                _Sections.Add(new ConfigSection(SectionName));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a section with entry to the config
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <param name="EntryName">Name of the entry</param>
        /// <param name="Value">Value of the entry</param>
        /// <returns>true on success, false if the section or entry already exists</returns>
        public bool Add(string SectionName, string EntryName, object Value)
        {
            if (!HasSection(SectionName))
            {
                _Sections.Add(new ConfigSection(SectionName));
                this[SectionName].Add(EntryName, Value);
                return true;
            }
            else
            {
                if(!HasEntry(SectionName, EntryName))
                {
                    this[SectionName].Add(EntryName, Value);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Deletes a section from config
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <returns>true on success, false if the section doesn't exist</returns>
        public bool Delete(string SectionName)
        {
            if(HasSection(SectionName))
            {
                _Sections.Remove((ConfigSection)this[SectionName]);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the config to the initial config path. It will return false if the config is not initialized with a path.
        /// </summary>
        /// <returns>true on success, false if the config is not initialized with a path or the config file can't be written</returns>
        public bool Save()
        {
            if(_Path == null)
            {
                return false;
            }

            string output = GenerateConfigContent();

            try
            {
                File.WriteAllText(_Path, output, _Encoding);
                return true;
            }
            catch {
                return false;
            }
        }


        /// <summary>
        /// Saves the config to the given path
        /// </summary>
        /// <returns>true on success, false if the config file can't be written</returns>
        public bool Save(string Path)
        {
            _Path = Path;
            return Save();
        }

        /// <summary>
        /// Returns the section
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <returns>Section-Object on success, null on fail</returns>
        public ConfigSection this[string SectionName]
        {
            get
            {
                if(HasSection(SectionName))
                {
                    return (from x in _Sections where x.Name.ToLower() == SectionName.ToLower() select x).First();
                }
                else
                {
                    return null;
                }
            }
            
        }
        
        /// <summary>
        /// Checks if the section exists
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <returns>true if it exists, false if not</returns>
        public bool HasSection(string SectionName)
        {
            return (from x in _Sections where x.Name.ToLower() == SectionName.ToLower() select x).Count() == 1;
        }

        /// <summary>
        /// Checks if the entry exists
        /// </summary>
        /// <param name="SectionName">Name of the section</param>
        /// <param name="EntryName">Name of the entry</param>
        /// <returns>true if it exists, false if not</returns>
        public bool HasEntry(string SectionName, string EntryName)
        {
            if ((from x in _Sections where x.Name.ToLower() == SectionName.ToLower() select x).Count() == 1)
            {
                return (from x in _Sections where x.Name.ToLower() == SectionName.ToLower() select x).First().HasEntry(EntryName);
            }
            else
            {
                return false;
            }
        }

        private string GenerateConfigContent()
        {
            StringBuilder output = new StringBuilder();

            output.AppendLine($"# Saved ({DateTime.Now.ToString()})");

            foreach (ConfigSection section in _Sections)
            {
                output.AppendLine();
                output.AppendLine($"[{section.Name}]");
                output.AppendLine();

                foreach (ConfigEntry entry in section.Entrys)
                {
                    output.AppendLine($"{entry.Name} = {entry.Value}");
                }
            }

            return output.ToString();
        }

        public class ConfigSection
        {
            private string _Name;
            private List<ConfigEntry> _Entrys;

            public string Name
            {
                get { return _Name; }
                set { _Name = value.ToLower(); }
            }

            public ConfigEntry[] Entrys
            {
                get { return _Entrys.ToArray(); }
            }

            public ConfigSection(string Name)
            {
                _Name = Name.ToLower();
                _Entrys = new List<ConfigEntry>();
            }

            public bool Add(string EntryName, object Value)
            {
                if(!HasEntry(EntryName))
                {
                    _Entrys.Add(new ConfigEntry(EntryName, Value));
                    return true;
                }
                else
                {
                    return false;
                }    
            }

            public bool Delete(string EntryName)
            {
                if (HasEntry(EntryName))
                {
                    _Entrys.Remove((ConfigEntry)this[EntryName]);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool HasEntry(string EntryName)
            {
                return (from x in _Entrys where x.Name.ToLower() == EntryName.ToLower() select x).Count() == 1;
            }

            public ConfigEntry this[string EntryName]
            {
                get
                {
                    if((from x in _Entrys where x.Name.ToLower() == EntryName.ToLower() select x).Count() == 1)
                    {
                        return (from x in _Entrys where x.Name.ToLower() == EntryName.ToLower() select x).First();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }


        public class ConfigEntry
        {
            private string _Name;
            private string _Value;
            public string Name
            {
                get { return _Name; }
                set { _Name = value.ToLower(); }
            }
            public string Value
            {
                get { return _Value; }
                set { _Value = value; }
            }

            public ConfigEntry(string Name, object Value)
            {
                _Name = Name.ToLower();
                _Value = Value.ToString();
            }

            public override string ToString()
            {
                return _Value;
            }

            public int ToInt()
            {
                int value;

                if (int.TryParse(_Value, out value))
                {
                    return value;
                }
                else
                {
                    return 0;
                }
            }

            public long ToLong()
            {
                long value;

                if(long.TryParse(_Value, out value))
                {
                    return value;
                }
                else
                {
                    return 0;
                }
            }

            public bool ToBool()
            {
                return _Value.ToLower() == "true" || _Value.ToLower() == "on" || _Value.ToLower() == "1" ? true : false;
            }

            public short ToShort()
            {
                short value;

                if (short.TryParse(_Value, out value))
                {
                    return value;
                }
                else
                {
                    return 0;
                }
            }

            public char[] ToCharArray()
            {
                return _Value.ToCharArray();
            }

            public DateTime ToDateTime()
            {
                DateTime value;

                if (DateTime.TryParse(_Value, out value))
                {
                    return value;
                }
                else
                {
                    return new DateTime();
                }
            }

            public static implicit operator bool(ConfigEntry Entry)
            {
                return Entry.ToBool();
            }

            public static implicit operator int(ConfigEntry Entry)
            {
                return Entry.ToInt();
            }

            public static implicit operator string(ConfigEntry Entry)
            {
                return Entry.ToString();
            }

            public static implicit operator short(ConfigEntry Entry)
            {
                return Entry.ToShort();
            }

            public static implicit operator DateTime(ConfigEntry Entry)
            {
                return Entry.ToDateTime();
            }

            public static implicit operator char[](ConfigEntry Entry)
            {
                return Entry.ToCharArray();
            }
        }
    }

}
