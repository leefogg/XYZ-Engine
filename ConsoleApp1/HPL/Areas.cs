using GLOOP.Extensions;
using OpenTK.Mathematics;
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

                [XmlArray("UserVariables")]
                [XmlArrayItem("Var")]
                public Variable[] Variables { get; set; }

                public Box3 GetBoundingBox()
                {
                    var centre = WorldPos.ParseVector3();
                    var scale = Scale.ParseVector3();
                    return new Box3()
                    {
                        Center = centre,
                        Size = scale
                    };
                }

                public Dictionary<string, string> GetProperties()
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var variable in Variables)
                        dict.Add(variable.Name, variable.Value);

                    return dict;
                }

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
                    Sit,
                    Zoom,
                    Crawl,
                    Climb,
                    AgentRepel,
                    Sticky,
                    EyeTrackingZoom,
                    Distortion,
                    Hide
                }
            }
        }
    }
}
