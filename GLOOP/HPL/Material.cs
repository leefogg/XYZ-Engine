using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL
{
    [XmlRoot("Material")]
    public class Material
    {
        [XmlElement("TextureUnits")]
        public TexturesUnits Textures { get; set; }

        public class TexturesUnits
        {
            [XmlElement("Diffuse")]
            public Texture Diffuse { get; set; }

            [XmlElement("Specular")]
            public Texture Specular { get; set; }

            [XmlElement("NMap")]
            public Texture NormalMap { get; set; }

            [XmlElement("Illumination")]
            public Texture Illumination { get; set; }

            public class Texture
            {
                [XmlAttribute("File")]
                public string Path { get; set; }
            }
        }
    }
}
