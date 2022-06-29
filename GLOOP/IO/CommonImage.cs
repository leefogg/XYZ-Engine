using GLOOP.Rendering;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace GLOOP.IO
{
    public static class CommonImage
    {
        public static Bitmap Load(string path, TextureParams settings)
        {
            if (!System.IO.File.Exists(path))
                System.Diagnostics.Debug.Fail("File does not exist");

            var image = new Bitmap(path);
            var data = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            settings.PixelFormat = PixelFormat.Bgra;
            settings.Data = data.Scan0;

            return image;
        }
    }
}
