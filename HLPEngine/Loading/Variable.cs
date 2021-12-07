using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    public class Variable
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAttribute("Value")]
        public string Value { get; set; }

        public override string ToString() => $"{Name} = {Value}";
    }
}
