using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    public class Buffer<T> where T : struct
    {
        public readonly int Handle = GL.GenBuffer();
        private readonly BufferTarget Type;
        private readonly BufferUsageHint Usage;
        private readonly int Length;

        private Buffer(BufferTarget type, BufferUsageHint usage, int length, string name)
        {
            Type = type;
            Usage = usage;
            Length = length;

            Bind();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.TrimLabelLength();
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, name.Length, name);
            }
        }

        public Buffer(T data, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, Marshal.SizeOf<T>(), name)
        {
            GL.NamedBufferData(Handle, Length, ref data, usage);
        }

        public Buffer(T[] data, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, Marshal.SizeOf<T>() * data.Length, name)
        {
            GL.NamedBufferData(Handle, Length, data, usage);
        }

        public Buffer(int count, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, Marshal.SizeOf<T>() * count, name)
        {
            GL.NamedBufferData(Handle, Length, (IntPtr)0, usage);
        }

        public void Update(T data, uint start = 0)
        {
            GL.NamedBufferSubData(Handle, (IntPtr)start, Marshal.SizeOf<T>(), ref data);
        }

        public void Update(T[] data, int start = 0)
        {
            GL.NamedBufferSubData(Handle, (IntPtr)start, Marshal.SizeOf<T>() * data.Length - start, data);
        }

        public void Bind()
        {
            GL.BindBuffer(Type, Handle);
        }

        public void BindRange(int start, int slot)
        {
            GL.BindBufferRange((BufferRangeTarget)Type, slot, Handle, (IntPtr)start, Length);
        }

        public void BindRange(uint start, int slot, int length)
        {
            GL.BindBufferRange((BufferRangeTarget)Type, slot, Handle, (IntPtr)start, length);
        }
    }
}
