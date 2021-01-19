using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.SOMA {
    [XmlRoot("HPLMapTrack_Primitive")]
    public class Primitives {
        [XmlElement("Section")]
        public Section[] Sections{ get; set; }

        public class Section
        {
            [XmlArray("Objects")]
            [XmlArrayItem("Plane")]
            public Plane[] Planes { get; set; }

            public class Plane
            {
                [XmlAttribute("Name")]
                public string Name { get; set; }
                [XmlAttribute("WorldPos")]
                public string Position { get; set; }
                [XmlAttribute("Rotation")]
                public string Rotation { get; set; }
                [XmlAttribute("EndCorner")]
                public string Scale { get; set; }

                [XmlAttribute("Material")]
                public string MaterialPath { get; set; }
                [XmlAttribute("TileAmount")]
                public string TileAmount { get; set; }
                [XmlAttribute("TileOffset")]
                public string TileOffset { get; set; }
                [XmlAttribute("AlignToWorldCoords")]
                public bool AlignToWorldCoords { get; set; }

                [XmlAttribute("Corner1UV")]
                public string Corner1UV { get; set; }
                [XmlAttribute("Corner2UV")]
                public string Corner2UV { get; set; }
                [XmlAttribute("Corner3UV")]
                public string Corner3UV { get; set; }
                [XmlAttribute("Corner4UV")]
                public string Corner4UV { get; set; }
            }
        }

       
    }
}
