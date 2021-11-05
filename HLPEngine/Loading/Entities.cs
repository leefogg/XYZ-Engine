using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    [XmlRoot("HPLMapTrack_Entity")]
    public class Entities {
        [XmlElement("Section")]
        public Section[] sections { get; set; }

        public class Section {
            [XmlArray("FileIndex_Entities")]
            [XmlArrayItem("File")]
            public File[] Files { get; set; }

            [XmlArray("Objects")]
            [XmlArrayItem("Entity")]
            public Entity[] Objects { get; set; }

            public class File {
                [XmlAttribute("Id")]
                public int Id { get; set; }
                [XmlAttribute("Path")]
                public string Path { get; set; }
            }

            public class Entity {
                [XmlAttribute("FileIndex")]
                public int Index { get; set; }
                [XmlAttribute("Name")]
                public string Name { get; set; }
                [XmlAttribute("WorldPos")]
                public string Position { get; set; }
                [XmlAttribute("Rotation")]
                public string Rotation { get; set; }
                [XmlAttribute("Scale")]
                public string Scale { get; set; }

                [XmlArray("UserVariables")]
                [XmlArrayItem("Var", typeof(Variable))]
                public Variable[] Variables { get; set; }

                public Dictionary<string, string> GetProperties(Loading.Entity ent)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var variable in ent.Variables)
                        dict.Add(variable.Name, variable.Value);

                    foreach (var variable in Variables)
                    {
                        if (dict.ContainsKey(variable.Name))
                            dict[variable.Name] = variable.Value;
                        else
                            dict.Add(variable.Name, variable.Value);
                    }

                    return dict;
                }
            }
        }
    }
}
