using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    [XmlRoot("HPLMapTrack_Terrain")]
    public class Terrain
    {
        [XmlElement("Terrain")]
        public TerrainInfo Info { get; set; }

        public class TerrainInfo
        {
            [XmlArray("DetailTextures")]
            [XmlArrayItem("DetailTexture")]
            public DetailTexture[] DetailTextures { get; set; }

            [XmlElement("BlendLayers")]
            public BlendLayersInfo BlendLayers { get; set; }

            [XmlAttribute("Active")]
            public bool Active { get; set; }
            [XmlAttribute("GeometryPatchSize")]
            public int GeometryPatchSize { get; set; }
            [XmlAttribute("TexturePatchSize")]
            public int TexturePatchSize { get; set; }
            [XmlAttribute("HeightMapSize")]
            public float HeightMapSize { get; set; }
            [XmlAttribute("MaxHeight")]
            public float MaxHeight { get; set; }
            [XmlAttribute("UnitSize")]
            public float UnitSize { get; set; }
            [XmlAttribute("BaseMaterialFile")]
            public string BaseMaterialFile { get; set; }
            [XmlAttribute("BaseMaterialTileAmount")]
            public float BaseMaterialTileAmount { get; set; }
            [XmlAttribute("SpecularPower")]
            public float SpecularPower { get; set; }

            public class DetailTexture
            {
                [XmlAttribute("ID")]
                public int ID { get; set; }
                [XmlAttribute("File")]
                public string File { get; set; }
                [XmlAttribute("Scale")]
                public float Scale { get; set; }
            }

            public class BlendLayersInfo
            {
                [XmlArray("BlendLayer")]
                [XmlArrayItem("Material")]
                public MaterialInfo[] Materials { get; set; }

                public int ID { get; set; }

                public class MaterialInfo
                {
                    [XmlAttribute("File")]
                    public string File { get; set; }
                    [XmlAttribute("TileAmount")]
                    public float TileAmount { get; set; }
                    [XmlAttribute("HeightMapScale")]
                    public float HeightMapScale { get; set; }
                }
            }
        }
    }
}
