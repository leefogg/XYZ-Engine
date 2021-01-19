using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.DAE
{
    [XmlRoot("COLLADA")]
    public class Model
    {
        public static Model Load(string path)
        {
            var xml = File.ReadAllText(path);
            var toreplace = "xmlns=\"http://www.collada.org/2005/11/COLLADASchema\"";
            xml = xml.Replace(toreplace, "");
            using var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(xml);
            writer.Flush();
            ms.Position = 0;

            var serializer = new XmlSerializer(typeof(Model));
            var dae = (Model)serializer.Deserialize(ms);
            return dae;
        }


        [XmlElement("asset")]
        public AssetInfo Meta { get; set; }

        public class AssetInfo
        {
            [XmlElement("unit")]
            public UnitInfo Units { get; set; }

            public class UnitInfo
            {
                [XmlAttribute("name")]
                public string Name { get; set; }
                [XmlAttribute("meter")]
                public float Scale { get; set; }
            }
        }
    }
}
