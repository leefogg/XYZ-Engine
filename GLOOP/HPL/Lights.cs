using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL
{
    [XmlRoot("HPLMapTrack_Light")]
    public class Lights
    {
        [XmlElement("Section")]
        public Section[] Sections { get; set; }

        public class Section
        {
            [XmlArray("Objects")]
            [XmlArrayItem("PointLight", typeof(PointLight))]
            [XmlArrayItem("SpotLight", typeof(SpotLight))]
            public Light[] Lights { get; set; }
        }
    }
}
