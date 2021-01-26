using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GLOOP.Rendering
{
    public class Shader : IDisposable
    {
        public static readonly SingleColorShader SingleColorShader = new SingleColorShader();

        private static int currentShaderHandle;

        public int Handle { get; protected set; }

        private readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        public Shader(int handle, string name = null)
        {
            Handle = handle;

            Use();
            if (!string.IsNullOrEmpty(name))
            {
                name = name[..Math.Min(name.Length, Globals.MaxLabelLength)];
                GL.ObjectLabel(ObjectLabelIdentifier.Program, Handle, name.Length, name);
            }

            LoadUniforms();
            ResourceManager.Add(this);
        }

        protected static int load(string vertPath, string fragPath, IDictionary<string, string> defines = null)
        {
            var hashDefines = (defines == null) ? string.Empty : createDefines(defines);

            var vertexShaderSource = File.ReadAllText(vertPath);
            var fragmentShaderSource = File.ReadAllText(fragPath);
            vertexShaderSource = insertDefines(vertexShaderSource, hashDefines);
            fragmentShaderSource = insertDefines(fragmentShaderSource, hashDefines);

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            CompileShader(vertexShader);

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            CompileShader(fragmentShader);

            var handle = GL.CreateProgram();
            GL.AttachShader(handle, vertexShader);
            GL.AttachShader(handle, fragmentShader);

            LinkProgram(handle);

            GL.DetachShader(handle, vertexShader);
            GL.DetachShader(handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            return handle;
        }

        private static string createDefines(IDictionary<string, string> keyValuePairs)
        {
            var sb = new StringBuilder(keyValuePairs.Count * 50);
            foreach (var pair in keyValuePairs)
              sb.Append($"#define {pair.Key} {pair.Value}{Environment.NewLine}");

            return sb.ToString();
        }

        private static string insertDefines(string shaderSource, string defines)
        {
            var versionStart = shaderSource.IndexOf("#version");
            var afterVersion = shaderSource.IndexOf("\n", versionStart, 30) + 1;

            return shaderSource[..afterVersion] + defines + shaderSource[afterVersion..];
        }

        public virtual void Use()
        {
            Use(Handle);
        }

        public static void Use(int handle)
        {
            if (currentShaderHandle != handle)
            {
                GL.UseProgram(handle);
                currentShaderHandle = handle;
            }
        }

        public bool TryGetUniformLocaiton(string name, out int location) => uniformLocations.TryGetValue(name, out location);

        public void Set(string name, TextureUnit unit)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform1(location, unit - TextureUnit.Texture0);
        }
        public void Set(string name, int data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform1(location, data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }
        public void Set(string name, float data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform1(location, data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }
        public void Set(string name, Matrix4 data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.UniformMatrix4(location, false, ref data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }
        public void Set(string name, Vector4 data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform4(location, data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }
        public void Set(string name, Vector3 data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform3(location, data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }
        public void Set(string name, Vector2 data)
        {
            if (uniformLocations.TryGetValue(name, out var location))
                GL.Uniform2(location, data);
            else
                Console.Error.WriteLine($"Cannot find uniform {name}");
        }

        protected static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            // Check for compilation errors
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var error = GL.GetShaderInfoLog(shader);
                Console.WriteLine(error);
                throw new Exception($"Error occurred whilst compiling Shader({shader})");
            }
        }

        protected static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            // Check for linking errors
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                var error = GL.GetProgramInfoLog(program);
                Console.WriteLine(error);
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        protected void LoadUniforms()
        {
            uniformLocations.Clear();

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var uniformName = GL.GetActiveUniform(Handle, i, out var size, out var type);

                for (var j = 0; j < size; j++)
                {
                    var indexName = uniformName.Replace("[0]", $"[{j}]");
                    var location = GL.GetUniformLocation(Handle, indexName);

                    uniformLocations.Add(indexName, location);
                }
            }
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}
