using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading {
    [XmlRoot("HPLMapTrack_DetailMeshes")]
    public class DetailMeshes
    {
        [XmlElement("DetailMeshes")]
        public Meshes meshes { get; set; }

        public class Meshes {
            [XmlArray("Sections")]
            [XmlArrayItem("Section")]
            public Section[] sections { get; set; }

            public class Section {
                [XmlElement("DetailMesh")]
                public DetailMesh[] DetailMeshes { get; set; }

                public class DetailMesh
                {
                    [XmlAttribute("File")]
                    public string FilePath { get; set; }
                    [XmlAttribute("NumOfInstances")]
                    public int NumInstances { get; set; }

                    [XmlElement("DetailMeshEntityPositions")]
                    public string Positions { get; set; }
                    [XmlElement("DetailMeshEntityRotations")]
                    public string Rotations { get; set; }
                    [XmlElement("DetailMeshEntityRadii")]
                    public string Radii { get; set; }

                    public IEnumerable<Vector3> GetPositions()
                    {
                        var floats = Positions.Split(' ').Select(x => float.Parse(x)).ToArray();
                        for (var i=0; i<floats.Length;)
                            yield return new Vector3(floats[i++], floats[i++], floats[i++]);
                    }

                    public IEnumerable<Quaternion> GetRotations()
                    {
                        var floats = Rotations.Split(' ').Select(x => float.Parse(x)).ToArray();
                        for (var i = 0; i < floats.Length; ) {
                            var w = floats[i++];
                            var z = floats[i++];
                            var y = floats[i++];
                            var x = floats[i++];
                            yield return new Quaternion(x,y,z,w);
                        }
                    }
                }
            }
        }
    }
}
