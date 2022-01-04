using GLOOP.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace GLOOP.HPL.Loading
{
    public class PointLight : Light
    {
        public Rendering.PointLight ToCommon()
        {
            return new Rendering.PointLight(
                        Position.ParseVector3(),
                        DiffuseColor.ParseVector3(),
                        Brightness,
                        Radius,
                        FalloffPower,
                        ToCommonType()
                    );
        }
    }
}
