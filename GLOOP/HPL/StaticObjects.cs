using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL {
    [XmlRoot("HPLMapTrack_StaticObject")]
    public class StaticObjects {
        [XmlElement("Section")]
        public Section[] sections { get; set; }

        public class Section {
            [XmlArray("FileIndex_StaticObjects")]
            [XmlArrayItem("File")]
            public File[] Files { get; set; }

            [XmlArray("Objects")]
            [XmlArrayItem("StaticObject")]
            public StaticObject[] Objects { get; set; }

            public class File {
                [XmlAttribute("Id")]
                public int Id { get; set; }
                [XmlAttribute("Path")]
                public string Path { get; set; }
            }

            public class StaticObject {
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

                [XmlAttribute("ColorMul")]
                public string ColourMultiplier { get; set; }
                [XmlAttribute("IllumColor")]
                public string IlluminationColor { get; set; }
                [XmlAttribute("IllumBrightness")]
                public float IlluminationBrightness { get; set; } = 1;

                [XmlAttribute("IsOccluder")]
                public bool IsOccluder { get; set; }
            }
        }
    }
}
