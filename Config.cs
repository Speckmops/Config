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

        public Config()
        {
            _Sections = new List<ConfigSection>();
            _Encoding = Encoding.UTF8;
        }

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

        public bool Add(string SectionName, string EntryName, string Value)
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

        public bool Save()
        {
            StringBuilder output = new StringBuilder();

            output.AppendLine($"# Saved ({DateTime.Now.ToString()})");

            foreach (ConfigSection section in _Sections)
            {
                output.AppendLine();
                output.AppendLine($"[{section.Name}]");
                output.AppendLine();

                foreach(ConfigEntry entry in section.Entrys)
                {
                    output.AppendLine($"{entry.Name} = {entry.Value}");
                }
            }

            try
            {
                File.WriteAllText(_Path, output.ToString(), _Encoding);
                return true;
            }
            catch {
                return false;
            }
        }

        public bool Save(string Path)
        {
            _Path = Path;
            return Save();
        }

        public IConfigSection this[string SectionName]
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
        
        public bool HasSection(string SectionName)
        {
            return (from x in _Sections where x.Name.ToLower() == SectionName.ToLower() select x).Count() == 1;
        }

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

        public interface IConfigSection
        {
            string Name { get; set; }
            bool Add(string EntryName, string Value);
            bool HasEntry(string EntryName);

            bool Delete(string EntryName);

            IConfigEntry[] Entrys { get; }
            IConfigEntry this[string EntryName]
            {
                get;
            }

        }

        protected class ConfigSection : IConfigSection
        {
            private string _Name;
            private List<ConfigEntry> _Entrys;

            public string Name
            {
                get { return _Name; }
                set { _Name = value.ToLower(); }
            }


            public IConfigEntry[] Entrys
            {
                get { return _Entrys.ToArray(); }
            }

            public ConfigSection(string Name)
            {
                _Name = Name.ToLower();
                _Entrys = new List<ConfigEntry>();
            }

            public bool Add(string EntryName, string Value)
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

            public IConfigEntry this[string EntryName]
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

        public interface IConfigEntry
        {
            string Name { get; set; }
            string Value { get; set; }
            string ToString();
            int ToInt();
            bool ToBool();
            long ToLong();
            short ToShort();
            DateTime ToDateTime();
        }

        protected class ConfigEntry : IConfigEntry
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

            public ConfigEntry(string Name, string Value)
            {
                _Name = Name.ToLower();
                _Value = Value;
            }

            public override string ToString()
            {
                return _Value;
            }

            public int ToInt()
            {
                return int.Parse(_Value);
            }

            public long ToLong()
            {
                return long.Parse(_Value);
            }

            public bool ToBool()
            {
                return _Value.ToLower() == "true" || _Value.ToLower() == "on" || _Value.ToLower() == "1" ? true : false;
            }

            public short ToShort()
            {
                return short.Parse(_Value);
            }

            public DateTime ToDateTime()
            {
                return DateTime.Parse(_Value);
            }
        }
        
    }

}
