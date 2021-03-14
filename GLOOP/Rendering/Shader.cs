using GLOOP.Extensions;
using GLOOP.Rendering.Materials;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GLOOP.Rendering
{
    public class Shader : IDisposable
    {
        public static readonly SingleColorShader SingleColorShader = new SingleColorShader();

        public static Shader Current { get; private set; }

        public int Handle { get; protected set; }

        private readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        public Shader(int handle, string name = null)
        {
            Handle = handle;

            Use();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.TrimLabelLength();
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
            Use(this);
        }

        public static void Use(Shader shader)
        {
            if (Current != shader)
            {
                GL.UseProgram(shader.Handle);
                Current = shader;
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

        public ProgramIntrospection Introspect()
        {
            var blockProperties = new[] { ProgramProperty.NumActiveVariables };
            var activeVariables = new[] { ProgramProperty.ActiveVariables };
            var uniformProperties = new[] { ProgramProperty.NameLength, ProgramProperty.Type, ProgramProperty.Offset };

            GL.GetProgramInterface(Handle, ProgramInterface.UniformBlock, ProgramInterfaceParameter.ActiveResources, out int numUBOs);
            var numActiveBlocks = new int[1];
            var ubos = new List<BufferBinding>();
            {
                for (int block = 0; block < numUBOs; block++)
                {
                    GL.GetProgramResource(Handle, ProgramInterface.UniformBlock, block, blockProperties.Length, blockProperties, 1, out _, numActiveBlocks);
                    int numActiveUniforms = numActiveBlocks[0];
                    if (numActiveUniforms == 0)
                        continue;

                    var activeUniformsIds = new int[numActiveUniforms];
                    GL.GetProgramResource(Handle, ProgramInterface.UniformBlock, block, activeVariables.Length, activeVariables, numActiveUniforms, out var _, activeUniformsIds);
                    var uniforms = new List<Variable>();
                    foreach (var uniformID in activeUniformsIds)
                    {
                        var values = new int[uniformProperties.Length];
                        GL.GetProgramResource(Handle, ProgramInterface.Uniform, uniformID, values.Length, uniformProperties, uniformProperties.Length, out var _, values);
                        GL.GetProgramResourceName(Handle, ProgramInterface.Uniform, uniformID, values[0], out var _, out string name);
                        uniforms.Add(new Variable((All)values[1], name, values[2]));
                    }

                    ubos.Add(new BufferBinding(uniforms.ToArray()));
                }
            }

            GL.GetProgramInterface(Handle, ProgramInterface.ShaderStorageBlock, ProgramInterfaceParameter.ActiveResources, out int numSSBOs);
            var ssbos = new List<BufferBinding>();
            {
                for (int block = 0; block < numSSBOs; block++)
                {
                    GL.GetProgramResource(Handle, ProgramInterface.ShaderStorageBlock, block, blockProperties.Length, blockProperties, 1, out _, numActiveBlocks);
                    int numActiveUniforms = numActiveBlocks[0];
                    if (numActiveUniforms == 0)
                        continue;

                    var activeUniformsIds = new int[numActiveUniforms];
                    GL.GetProgramResource(Handle, ProgramInterface.ShaderStorageBlock, block, activeVariables.Length, activeVariables, numActiveUniforms, out var _, activeUniformsIds);
                    var variables = new List<Variable>();
                    foreach (var uniformID in activeUniformsIds)
                    {
                        var values = new int[uniformProperties.Length];
                        GL.GetProgramResource(Handle, ProgramInterface.BufferVariable, uniformID, values.Length, uniformProperties, uniformProperties.Length, out var _, values);
                        GL.GetProgramResourceName(Handle, ProgramInterface.BufferVariable, uniformID, values[0], out var _, out string name);
                        variables.Add(new Variable((All)values[1], name, values[2]));
                    }

                    ssbos.Add(new BufferBinding(variables.ToArray()));
                }
            }

            return new ProgramIntrospection(ubos.ToArray(), ssbos.ToArray());
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }

        public class ProgramIntrospection
        {
            public readonly BufferBinding[] UBOs;
            public readonly BufferBinding[] SSBOs;

            public ProgramIntrospection(BufferBinding[] uBOs, BufferBinding[] sSBOs)
            {
                UBOs = uBOs;
                SSBOs = sSBOs;
            }
        }

        public class BufferBinding
        {
            public readonly Variable[] Uniforms;

            public BufferBinding(Variable[] uniforms)
            {
                Uniforms = uniforms;
            }
        }
        public class Variable
        {
            public readonly All Type;
            public readonly string name;
            public readonly int OffsetInBytes;

            public Variable(All type, string name, int offsetInBytes)
            {
                Type = type;
                this.name = name;
                OffsetInBytes = offsetInBytes;
            }
        }
    }
}
