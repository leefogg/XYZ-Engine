using GLOOP.Extensions;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GLOOP.Rendering
{
    public class Buffer<T> where T : struct
    {
        public readonly int Handle = GL.GenBuffer();
        private readonly BufferTarget Type;
        private readonly BufferUsageHint Usage;
        public readonly int ItemSize = Marshal.SizeOf<T>();
        public readonly int SizeInBytes;
        public int SizeInItems => SizeInBytes / ItemSize;

        private Buffer(BufferTarget type, BufferUsageHint usage, int numElements, string name)
        {
            Type = type;
            Usage = usage;
            SizeInBytes = numElements * ItemSize;

            Bind();

            if (!string.IsNullOrEmpty(name))
            {
                name = name.TrimLabelLength();
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, name.Length, name);
            }
        }

        public Buffer(T data, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, 1, name)
        {
            GL.NamedBufferData(Handle, SizeInBytes, ref data, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public Buffer(T[] data, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, data.Length, name)
        {
            GL.NamedBufferData(Handle, SizeInBytes, data, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public Buffer(int count, BufferTarget type, BufferUsageHint usage, string name)
            : this(type, usage, count, name)
        {
            GL.NamedBufferData(Handle, SizeInBytes, (IntPtr)0, usage);
            Metrics.BufferWrites += (ulong)SizeInBytes; 
        }

        public void Update(T data, uint start = 0)
        {
            GL.NamedBufferSubData(Handle, (IntPtr)start, ItemSize, ref data);
            Metrics.BufferWrites += (ulong)SizeInBytes;
        }

        public void Update(T[] data, uint start = 0)
        {
            Update(data, data.Length, start);
        }

        public void Update(T[] data, int numElements, uint startElement = 0)
        {
            var startByte = ItemSize * startElement;
            Debug.Assert(startByte < SizeInBytes, $"{nameof(startByte)} must be less than {SizeInBytes - ItemSize}");
            var length = ItemSize * numElements;
            var endByte = length + startByte;
            Debug.Assert(endByte <= SizeInBytes, $"Wrote {endByte - SizeInBytes} bytes to unmanaged memory");

            GL.NamedBufferSubData(Handle, (IntPtr)startByte, (int)(length - startByte), data);
            Metrics.BufferWrites += (ulong)length;
        }

        public void Read(ref T data, uint start = 0) 
        {
            GL.GetNamedBufferSubData(Handle, (IntPtr)start, ItemSize, ref data);
            Metrics.BufferReads += (ulong)ItemSize;
        }

        public void Read(ref T[] data, uint start = 0)
        {
            GL.GetNamedBufferSubData(Handle, (IntPtr)start, SizeInBytes, data);
            Metrics.BufferReads += (ulong)SizeInBytes;
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
        public void Bind(int index, int start) => Bind(index, SizeInItems, start);
        public void Bind(int index, int numElements, int startElement = 0)
        {
            var startByte = ItemSize * startElement;
            if (Type == BufferTarget.UniformBuffer)
                Debug.Assert(startByte % Globals.UniformBufferOffsetAlignment == 0, "Binding UBO out of alignment.");
            Debug.Assert(startByte <= SizeInBytes, $"{nameof(startByte)} must be less than {SizeInBytes - ItemSize}");
            var endByte = (startElement + numElements) * ItemSize;
            var length = ItemSize * numElements;
            Debug.Assert(endByte <= SizeInBytes, $"Wrote {endByte - SizeInBytes} bytes to unmanaged memory");
            Debug.Assert(Enum.GetName(typeof(BufferRangeTarget), Type) != null, $"Cannot bind range for type {Type}");

            GL.BindBufferRange((BufferRangeTarget)Type, index, Handle, (IntPtr)(startByte), length);
            Metrics.BufferBinds++;
        }
    }
}
