using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using System.IO;
using System.Xml.Serialization;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace GLOOP.HPL
{
    [XmlRoot]
    public class Entity {
        [XmlElement("ModelData")]
        public ModelData Model { get; set; }

        [XmlArray("UserDefinedVariables")]
        [XmlArrayItem("Var", typeof(Variable))]
        public Variable[] Variables { get; set; }

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

        public HPLEntity Load(AssimpContext context, DeferredRenderingGeoMaterial shader) {
            var SOMAHome = @"C:\Program Files (x86)\Steam\steamapps\common\SOMA";
            var fullPath = Path.Combine(SOMAHome, Model.Mesh.FileName);
            var model = new HPLEntity(fullPath, context, shader);
            if (Model.Mesh.SubMeshes != null)
            {
                if (Model.Mesh.SubMeshes.Length == model.Models.Count)
                {
                    for (int i = 0; i < Model.Mesh.SubMeshes.Length; i++)
                    {
                        var mesh = Model.Mesh.SubMeshes[i];
                        var renderable = model.Models[0];
                        renderable.Transform.Position += mesh.Position.ParseVector3();
                        renderable.Transform.Rotation += new Quaternion(mesh.Rotation.ParseVector3().Negated());
                        renderable.Transform.Scale *= mesh.Scale.ParseVector3();
                    }
                }
                else
                {
                    //Console.WriteLine("Inconsistent meshes in model");
                }
            }
            //TODO: Implement UserDefinedVariables posOffset, rotOffset, scaleOffset

            return model;
        }
    }
}
