using Assimp;
using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace GLOOP.HPL.Loading
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
                    [XmlAttribute("Name")]
                    public string Name { get; set; }
                    [XmlAttribute("Scale")]
                    public string Scale { get; set; }
                    [XmlAttribute("WorldPos")]
                    public string Position { get; set; }
                    [XmlAttribute("Rotation")]
                    public string Rotation { get; set; }
                }
            }
        }

        public HPLModelLoader Load(AssimpContext context, DeferredRenderingGeoMaterial shader) {
            var fullPath = Path.Combine(Constants.SOMARoot, Model.Mesh.FileName);
            var entity = new HPLModelLoader(fullPath, context, shader);

            if (bool.TryParse(Variables.First(v => v.Name == "ShowMesh")?.Value, out var visible) && !visible)
                throw new System.Exception("Entity not visible (hack)");

            if (Model.Mesh.SubMeshes != null)
            {
                if (Model.Mesh.SubMeshes.Length == entity.Models.Count)
                {
                    foreach (var submesh in Model.Mesh.SubMeshes)
                    {
                        foreach (var model in entity.Models)
                        {
                            //model.Transform.Position = submesh.Position.ParseVector3();
                            //model.Transform.Rotation = Quaternion.FromEulerAngles(-submesh.Rotation.ParseVector3());
                            //model.Transform.Scale = submesh.Scale.ParseVector3();
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Inconsistent meshes in model");
                }
            }
            //TODO: Implement UserDefinedVariables posOffset, rotOffset, scaleOffset

            return entity;
        }
    }
}
