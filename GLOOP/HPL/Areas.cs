using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL
{
    [XmlRoot("HPLMapTrack_Area")]
    public class Areas
    {
        [XmlElement("Section")]
        public Section[] sections { get; set; }

        public class Section
        {
            [XmlArray("Objects")]
            [XmlArrayItem("Area")]
            public Area[] Areas { get; set; }

            public class Area
            {
                [XmlAttribute("Name")]
                public string Name { get; set; }
                [XmlAttribute("WorldPos")]
                public string WorldPos { get; set; }
                [XmlAttribute("Scale")]
                public string Scale { get; set; }
                [XmlAttribute("Active")]
                public bool Active { get; set; }
                [XmlAttribute("AreaType")]
                public AreaType Type { get; set; }

                public enum AreaType
                {
                    PlayerStart,
                    Ladder,
                    Tool,
                    MapTransfer,
                    VisibilityArea,
                    VisibilityPortal,
                    InteractAux,
                    CameraAnimation,
                    Trigger,
                    Liquid,
                    Datamine,
                    DoorwayTrigger,
                    PathNode,
                    Soundscape,
                }
            }
        }
    }
}
