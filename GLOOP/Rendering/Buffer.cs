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
        public readonly int SizeInBytes;

        private Buffer(BufferTarget type, BufferUsageHint usage, int length, string name)
        {
            Type = type;
            Usage = usage;
            SizeInBytes = length;

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
            GL.NamedBufferData(Handle, SizeInBytes, ref data, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public Buffer(T[] data, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, Marshal.SizeOf<T>() * data.Length, name)
        {
            GL.NamedBufferData(Handle, SizeInBytes, data, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public Buffer(int count, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, Marshal.SizeOf<T>() * count, name)
        {
            GL.NamedBufferData(Handle, SizeInBytes, (IntPtr)0, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public void Update(T data, uint start = 0)
        {
            var length = Marshal.SizeOf<T>();
            GL.NamedBufferSubData(Handle, (IntPtr)start, length, ref data);
            Metrics.BufferWrites += (ulong)length;
        }

        public void Update(T[] data, uint start = 0)
        {
            Update(data, data.Length, start);
        }

        public void Update(T[] data, int numElements, uint startElement = 0)
        {
            var elemSize = Marshal.SizeOf<T>();
            var length = elemSize * numElements;
            var start = elemSize * startElement;
            var endByte = length + start;
            System.Diagnostics.Debug.Assert(endByte <= SizeInBytes, $"Wrote {endByte - SizeInBytes} bytes to unmanaged memory");
            GL.NamedBufferSubData(Handle, (IntPtr)start, (int)(length - start), data);
            Metrics.BufferWrites += (ulong)length;
        }

        public void Read(ref T data, uint start = 0) 
        {
            var length = Marshal.SizeOf<T>();
            GL.GetNamedBufferSubData(Handle, (IntPtr)start, length, ref data);
            Metrics.BufferReads += (ulong)length;
        }

        public void Read(ref T[] data, uint start = 0)
        {
            var length = Marshal.SizeOf<T>() * data.Length;
            GL.GetNamedBufferSubData(Handle, (IntPtr)start, length, data);
            Metrics.BufferReads += (ulong)length;
        }

        public void Bind()
        {
            GL.BindBuffer(Type, Handle);
            Metrics.BufferBinds++;
        }
        public void Bind(int index)
        {
            GL.BindBufferBase((BufferRangeTarget)Type, index, Handle);
            Metrics.BufferBinds++;
        }
        public void Bind(int index, int start) => Bind(index, SizeInBytes, start);
        public void Bind(int index, int length, int start = 0)
        {
            System.Diagnostics.Debug.Assert(Enum.GetName(typeof(BufferRangeTarget), Type) != null, $"Cannot bind range for type {Type}");
            GL.BindBufferRange((BufferRangeTarget)Type, index, Handle, (IntPtr)(start * Marshal.SizeOf<T>()), length * Marshal.SizeOf<T>());
            Metrics.BufferBinds++;
        }
    }
}
