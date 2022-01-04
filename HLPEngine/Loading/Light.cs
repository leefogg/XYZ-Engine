using GLOOP.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    public class Light
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("WorldPos")]
        public string Position { get; set; }

        [XmlAttribute("Brightness")]
        public float Brightness { get; set; }

        [XmlAttribute("DiffuseColor")]
        public string DiffuseColor { get; set; }

        [XmlAttribute("Radius")]
        public float Radius { get; set; }

        [XmlAttribute("FalloffPow")]
        public float FalloffPower { get; set; }

        [XmlAttribute("Gobo")]
        public string GoboTexture { get; set; }

        [XmlAttribute("GoboType")]
        public string Type { get; set; }

        protected LightType ToCommonType()
        {
            //if (string.IsNullOrEmpty(GoboTexture))
            //    return LightType.DiffuseAndSpecular;

            return Type switch
            {
                "Diffuse" => LightType.Diffuse,
                "Specular" => LightType.Specular,
                _ => LightType.DiffuseAndSpecular,
            };
        }
    }
}
