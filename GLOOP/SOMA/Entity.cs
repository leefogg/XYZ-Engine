using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace GLOOP.SOMA
{
    [XmlRoot]
    public class Entity {
        [XmlElement("ModelData")]
        public ModelData Model { get; set; }

        [XmlArray("UserDefinedVariables")]
        [XmlArrayItem("Var", typeof(EntityVariable))]
        public EntityVariable[] Variables { get; set; }

        public class ModelData {
            [XmlElement("Mesh")]
            public MeshInfo Mesh { get; set; }

            [XmlArray("Entities")]
            [XmlArrayItem("PointLight", typeof(PointLight))]
            public PointLight[] Lights { get; set; }

            [XmlType("Mesh")]
            public class MeshInfo {
                [XmlAttribute("Filename")]
                public string FileName { get; set; }

                [XmlElement("SubMesh")]
                public SubMeshInfo[] SubMeshes { get; set; }

                [XmlType("SubMesh")]
                public class SubMeshInfo
                {
                    [XmlAttribute("Scale")]
                    public string Scale { get; set; }
                    [XmlAttribute("WorldPos")]
                    public string Position { get; set; }
                    [XmlAttribute("Rotation")]
                    public string Rotation { get; set; }
                }
            }
        }

        public SOMAModel Load(AssimpContext context, DeferredRenderingGeoMaterial shader) {
            var SOMAHome = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA";
            var fullPath = Path.Combine(SOMAHome, Model.Mesh.FileName);
            var model = new SOMAModel(fullPath, context, shader);
            //TODO: Implement SubMeshInfo pos/scale/rot
            //TODO: Implement UserDefinedVariables posOffset, rotOffset, scaleOffset

            return model;
        }
    }
}
