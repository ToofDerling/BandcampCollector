﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace BandcampCollector.Shared.Buffers
{
    /// <summary>
    /// Represents a heap-based, array-backed output sink into which data can be written.
    /// Adapted from ArrayBufferWriter: 
    /// https://github.com/dotnet/runtime/blob/bd6709248295deefa1956da3aeb9b0e086fbaca5/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
    /// </summary>
    public sealed class ArrayPoolBufferWriter<T> : IBufferWriter<T>
    {
        // Copy of Array.MaxLength.
        // Used by projects targeting .NET Framework.
        private const int ArrayMaxLength = 0x7FFFFFC7;

        private const int DefaultInitialBufferSize = 256;

        private T[] _buffer;
        private int _index;

        /// <summary>
        /// Creates an instance of an <see cref="ArrayPoolBufferWriter{T}"/>, in which data can be written to,
        /// with the default initial capacity.
        /// </summary>
        public ArrayPoolBufferWriter()
        {
            _buffer = Array.Empty<T>();
            _index = 0;
        }

        /// <summary>
        /// Creates an instance of an <see cref="ArrayPoolBufferWriter{T}"/>, in which data can be written to,
        /// with an initial capacity specified.
        /// </summary>
        /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
        /// </exception>
        public ArrayPoolBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException(null, nameof(initialCapacity));

            _buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
            _index = 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sharedArrayPoolBuffer"></param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sharedArrayPoolBuffer"/> is null.
        /// </exception>
        public ArrayPoolBufferWriter(T[] sharedArrayPoolBuffer)
        {
            ArgumentNullException.ThrowIfNull(sharedArrayPoolBuffer);

            _buffer = sharedArrayPoolBuffer;
            _index = 0;
        }

        /// <summary>
        /// Returns the underlying buffer (any data written to the buffer is not cleared).
        /// </summary>
        public T[] Buffer => _buffer;

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

        /// <summary>
        /// Returns the amount of data written to the underlying buffer so far.
        /// </summary>
        public int WrittenCount => _index;

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => _buffer.Length - _index;

        /// <summary>
        /// Clears the data written to the underlying buffer and resets the 
        /// <see cref="ArrayPoolBufferWriter{T}"/> so it can be re-used.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Reset"/> if you don't need to clear the data before re-using.
        /// </remarks>
        public void Clear()
        {
            Debug.Assert(_buffer.Length >= _index);
            _buffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        /// <summary>
        /// Reset the <see cref="ArrayPoolBufferWriter{T}"/> so it can be re-used.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Clear"/> if you need to clear the data before re-using.
        /// </remarks>
        public void Reset()
        {
            _index = 0;
        }

        /// <summary>
        /// Closes the <see cref="ArrayPoolBufferWriter{T}"/> by returning the underlying buffer 
        /// to the shared <see cref="ArrayPool{T}"/>. After returning the buffer is set to null,  
        /// and any attempt to use the <see cref="ArrayPoolBufferWriter{T}"/> will throw an exception.
        /// </summary>
        /// <param name="clearData">Clear the data before returning the buffer to the pool.</param>
        public void Close(bool clearData = false)
        {
            if (_buffer.Length > 0)
            {
                ArrayPool<T>.Shared.Return(_buffer, clearData);
            }

            _buffer = null!;
            _index = -1;
        }

        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to advance past the end of the underlying buffer.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index > _buffer.Length - count)
                ThrowInvalidOperationException_AdvancedTooFar(_buffer.Length);

            _index += count;
        }

        /// <summary>
        /// Notifies <see cref="ArrayPoolBufferWriter{T}"/> that the last <paramref name="count"/> amount 
        /// of data is no longer considered written.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the amount withdrawn is larger than the <see cref="Capacity"/>
        /// </exception>
        public void Withdraw(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index - count < 0)
                throw new InvalidOperationException("Buffer index below 0");

            _index -= count;
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Memory{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsMemory(_index);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Span{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                int currentLength = _buffer.Length;

                // Attempt to grow by the larger of the sizeHint and double the current size.
                int growBy = Math.Max(sizeHint, currentLength);

                if (currentLength == 0)
                {
                    growBy = Math.Max(growBy, DefaultInitialBufferSize);
                }

                int newSize = currentLength + growBy;

                if ((uint)newSize > int.MaxValue)
                {
                    // Attempt to grow to ArrayMaxLength.
                    uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                    Debug.Assert(needed > currentLength);

                    if (needed > ArrayMaxLength)
                    {
                        ThrowOutOfMemoryException(needed);
                    }

                    newSize = ArrayMaxLength;
                }

                var newBuffer = ArrayPool<T>.Shared.Rent(newSize);

                if (_buffer.Length > 0)
                {
                    Array.Copy(_buffer, 0, newBuffer, 0, _index);

                    #region copy test
                    //var testIte = 5;
                    //var copyIte = 100000;

                    //var sw = new Stopwatch();
                    //long elapsed = 0;

                    //for (int j = 0; j < testIte; j++)
                    //{
                    //    sw.Restart();
                    //    for (int i = 0; i < copyIte; i++)
                    //    {
                    //        Array.Copy(_buffer, 0, newBuffer, 0, _index);
                    //    }
                    //    sw.Stop();
                    //    elapsed += sw.ElapsedMilliseconds;
                    //    Console.WriteLine(sw.ElapsedMilliseconds);
                    //}
                    //Console.WriteLine($"Array.Copy: {elapsed / testIte}");
                    //elapsed = 0;

                    //for (int j = 0; j < testIte; j++)
                    //{
                    //    sw.Restart();
                    //    unsafe
                    //    {
                    //        var tSize = sizeof(T);
                    //        for (int i = 0; i < copyIte; i++)
                    //        {
                    //            fixed (T* pinnedDestination = newBuffer)
                    //            fixed (T* pinnedSource = _buffer)
                    //            {
                    //                var dataSize = tSize * _buffer.Length;
                    //                System.Buffer.MemoryCopy(pinnedSource, pinnedDestination, dataSize, dataSize);
                    //            }
                    //        }
                    //    }
                    //    sw.Stop();
                    //    elapsed += sw.ElapsedMilliseconds;
                    //    Console.WriteLine(sw.ElapsedMilliseconds);
                    //}
                    //Console.WriteLine($"Buffer.MemoryCopy: {elapsed / testIte}");
                    //elapsed = 0;

                    //for (int j = 0; j < testIte; j++)
                    //{
                    //    sw.Restart();
                    //    for (int i = 0; i < copyIte; i++)
                    //    {
                    //        System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _index);
                    //    }
                    //    sw.Stop();
                    //    elapsed += sw.ElapsedMilliseconds;
                    //    Console.WriteLine(sw.ElapsedMilliseconds);
                    //}
                    //Console.WriteLine($"Buffer.BlockCopy: {elapsed / testIte}");
                    #endregion

                    ArrayPool<T>.Shared.Return(_buffer);
                }

                _buffer = newBuffer;
            }

            Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
        }

        private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity)
        {
            throw new InvalidOperationException(capacity.ToString());
        }

        private static void ThrowOutOfMemoryException(uint capacity)
        {
            throw new OutOfMemoryException(capacity.ToString());
        }
    }
}