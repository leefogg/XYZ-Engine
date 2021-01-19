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
            var SOMAHome = @"D:\Games\Steam\steamapps\common\soma";
            var fullPath = Path.Combine(SOMAHome, Model.Mesh.FileName);
            var model = new SOMAModel(fullPath, context, shader);

            for (var i = 0; i < Model.Mesh.SubMeshes.Length; i++)
            {
                var pos = new Vector3();
                var scale = new Vector3(1);
                var rot = new Quaternion();
                try
                {
                    var subMesh = Model.Mesh.SubMeshes[i];
                    pos = subMesh.Position.ParseVector3();
                    scale = subMesh.Scale.ParseVector3();
                    rot = new Quaternion(subMesh.Rotation.ParseVector3().Negated());
                } 
                catch (Exception ex)
                {

                }
                //model.Renderables[0].ModelMatrix = MathFunctions.CreateModelMatrix(pos, rot, scale);
            }

            return model;
        }
    }
}
