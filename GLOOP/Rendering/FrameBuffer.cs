using System;
using System.Linq;
using OpenTK.Graphics.OpenGL4;

namespace GLOOP.Rendering
{
    public class FrameBuffer : IDisposable
    {
        private readonly int Handle;
        private readonly int RBOHandle;
        public readonly Texture[] ColorBuffers;
        public readonly int Width, Height;


        public FrameBuffer(int width, int height, bool withDepth, PixelInternalFormat format = PixelInternalFormat.Rgba16f, int count = 1, string name = null)
            : this(width, height, Enumerable.Repeat(format, count).ToArray(), withDepth, name)
        { }
        public FrameBuffer(int width, int height, PixelInternalFormat[] pixelFormats, bool withDepth, string name = null)
            : this(width, height, pixelFormats.Select(f => new TextureParams { InternalFormat = f}).ToArray(), withDepth, name)
        {
        }
        public FrameBuffer(int width, int height, TextureParams[] settings, bool withDepth, string name = null)
        {
            Width = width;
            Height = height;

            Handle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

            if (!string.IsNullOrEmpty(name)) {
                name = name[..Math.Min(name.Length, Globals.MaxLabelLength)];
                GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, Handle, name.Length, name);
            }

            ColorBuffers = new Texture[settings.Length];
            var enums = new DrawBuffersEnum[ColorBuffers.Length];
            for (var i = 0; i < ColorBuffers.Length; i++)
            {
                var attachmentName = string.IsNullOrEmpty(name) ? null : name[..Math.Min(name.Length, Globals.MaxLabelLength - 2)] + i;
                settings[i].Name = attachmentName;
                ColorBuffers[i] = new Texture(width, height, settings[i]);
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0 + i,
                    TextureTarget.Texture2D,
                    ColorBuffers[i].Handle,
                    0
                );

                GL.BindTexture(TextureTarget.Texture2D, 0);

                enums[i] = DrawBuffersEnum.ColorAttachment0 + i;
            }

            GL.DrawBuffers(ColorBuffers.Length, enums);

            if (withDepth)
                RBOHandle = AttachDepth(width, height);

            CheckStatus();

            ResourceManager.Add(this);
        }

        private int AttachDepth(int width, int height)
        {
            var RBOHandle = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RBOHandle);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent16, width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, RBOHandle);
            return RBOHandle;
        }

        private void CheckStatus()
        {
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                throw new Exception("Framebuffer is incomplete!");
            }
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(Handle);
        }
    }
}
