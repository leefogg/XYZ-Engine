using System;
using System.Collections.Generic;
using System.Text;

namespace GLOOP.Rendering
{
    public class DeferredSplatTerrainMaterial : Material
    {
        private DeferredSplatTerrainShader shader;

        public Texture2D BaseTexture = Texture.Error;

        public override Shader Shader => shader;

        public DeferredSplatTerrainMaterial(DeferredSplatTerrainShader shader)
        {
            this.shader = shader;
        }

        public override void Commit()
        {
            shader.Use();

            shader.ModelMatrix = ModelMatrix;

            BaseTexture.Use();
        }

        public override Material Clone()
        {
            throw new NotImplementedException();
        }
    }
}
