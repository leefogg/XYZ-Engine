using GLOOP.Extensions;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.SOMA
{
    public class SpotLight : Light
    {
        [XmlAttribute("Rotation")]
        public string Rotation { get; set; }

        [XmlAttribute("SpotFalloffPow")]
        public float AngularFalloffPower { get; set; }

        [XmlAttribute("FOV")]
        public float FOV { get; set; }

        [XmlAttribute("NearClipPlane")]
        public float ZNear { get; set; }

        public GLOOP.SpotLight ToCommon()
        {
            return new GLOOP.SpotLight(
                        Position.ParseVector3(),
                        new Quaternion(Rotation.ParseVector3()),
                        DiffuseColor.ParseVector3(),
                        Brightness,
                        FalloffPower,
                        ToCommonType(),
                        AngularFalloffPower,
                        Radius,
                        MathHelper.RadiansToDegrees(FOV),
                        ZNear
                    );
        }
    }
}
