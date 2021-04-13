using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Future.Utilities.Net.Sockets
{
    /// <summary>
    /// Segment buffer manager class.
    /// </summary>
    public class SegmentBufferManager : ISegmentBufferManager
    {
        #region [ Constants ]
        /// <summary>
        /// Max trials count.
        /// </summary>
        private const int TRIALS_COUNT = 100;
        #endregion

        #region [ Fields ]
        /// <summary>
        /// Segment chunks.
        /// </summary>
        private readonly int _segment_chunks;

        /// <summary>
        /// Chunk size.
        /// </summary>
        private readonly int _chunk_size;

        /// <summary>
        /// Segment size.
        /// </summary>
        private readonly int _segment_size;

        /// <summary>
        /// Is allowed to create memory?
        /// </summary>
        private readonly bool _allowed_to_create_memory;

        /// <summary>
        /// Buffers.
        /// </summary>
        private readonly ConcurrentStack<ArraySegment<byte>> _buffers = new ConcurrentStack<ArraySegment<byte>>();

        /// <summary>
        /// Segments.
        /// </summary>
        private readonly List<byte[]> _segments;

        /// <summary>
        /// Creating new segment locker.
        /// </summary>
        private readonly object _creating_new_segment_locker = new object();
        #endregion

        #region [ Properties ]
        /// <summary>
        /// Chunk size for <see cref="SegmentBufferManager" /> class's instance.
        /// </summary>
        public int ChunkSize => this._chunk_size;

        /// <summary>
        /// Segments count for <see cref="SegmentBufferManager" /> class's instance.
        /// </summary>
        public int SegmentsCount => this._segments.Count;

        /// <summary>
        /// Segment chunks count for <see cref="SegmentBufferManager" /> class's instance.
        /// </summary>
        public int SegmentChunksCount => this._segment_chunks;

        /// <summary>
        /// Available buffers count for <see cref="SegmentBufferManager" /> class's instance.
        /// </summary>
        public int AvailableBuffers => this._buffers.Count;

        /// <summary>
        /// Total buffer size for <see cref="SegmentBufferManager" /> class's instance.
        /// </summary>
        public int TotalBufferSize => (this._segments.Count * this._segment_size);
        #endregion

        #region [ Constructor ]
        /// <summary>
        /// Construct a new <see cref="SegmentBufferManager"></see> class's instance.
        /// </summary>
        /// <param name="segment_chunks">The number of chunks to create per segment.</param>
        /// <param name="chunk_size">The size of a chunk in bytes.</param>
        /// <exception cref="ArgumentException"></exception>
        public SegmentBufferManager(int segment_chunks, int chunk_size)
            : this(segment_chunks, chunk_size, 1)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="SegmentBufferManager"></see> class's instance.
        /// </summary>
        /// <param name="segment_chunks">The number of chunks to create per segment.</param>
        /// <param name="chunk_size">The size of a chunk in bytes.</param>
        /// <param name="initial_segments">The initial number of segments to create.</param>
        /// <exception cref="ArgumentException"></exception>
        public SegmentBufferManager(int segment_chunks, int chunk_size, int initial_segments)
            : this(segment_chunks, chunk_size, initial_segments, true)
        {
            // Do nothing.
        }

        /// <summary>
        /// Construct a new <see cref="SegmentBufferManager"></see> class's instance.
        /// </summary>
        /// <param name="segment_chunks">The number of chunks to create per segment.</param>
        /// <param name="chunk_size">The size of a chunk in bytes.</param>
        /// <param name="initial_segments">The initial number of segments to create.</param>
        /// <param name="allowed_to_create_memory">If false when empty and checkout is called an exception will be thrown.</param>
        /// <exception cref="ArgumentException"></exception>
        public SegmentBufferManager(int segment_chunks, int chunk_size, int initial_segments, bool allowed_to_create_memory)
        {
            if (segment_chunks <= 0)  throw new ArgumentException($"{nameof(segment_chunks)}");
            if (chunk_size <= 0)      throw new ArgumentException($"{nameof(chunk_size)}");
            if (initial_segments < 0) throw new ArgumentException($"{nameof(initial_segments)}");

            this._segment_chunks = segment_chunks;
            this._chunk_size     = chunk_size;
            this._segment_size   = this._segment_chunks * this._chunk_size;
            this._segments       = new List<byte[]>();

            this._allowed_to_create_memory = true;
            for (int i = 0; i < initial_segments; i++)
            {
                this.CreateNewSegment(true);
            }
            this._allowed_to_create_memory = allowed_to_create_memory;
        }
        #endregion

        #region [ Default ]
        /// <summary>
        /// Default to 1KB buffers.
        /// </summary>
        private static readonly Lazy<SegmentBufferManager> _default = new Lazy<SegmentBufferManager>(() =>
        {
            return new SegmentBufferManager(1024, 1024, 1);
        },
        System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Default <see cref="SegmentBufferManager" />
        /// <para> Default to 1KB buffers if people don't want to manage it on their own. </para>
        /// </summary>
        public static SegmentBufferManager Default => SegmentBufferManager._default.Value;
        #endregion

        #region [ Borrow ]
        /// <summary>
        /// Borrow Buffer.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnableToAllocateBufferException">
        /// Cannot allocate buffer after few trials.
        /// </exception>
        public ArraySegment<byte> BorrowBuffer()
        {
            int trial = 0;

            while (trial < TRIALS_COUNT)
            {
                if (this._buffers.TryPop(out ArraySegment<byte> result))
                {
                    return result;
                }
                this.CreateNewSegment(false);
                ++trial;
            }
            throw new UnableToAllocateBufferException();
        }

        /// <summary>
        /// Borrow buffers.
        /// </summary>
        /// <param name="count">Buffers count.</param>
        /// <returns></returns>
        /// <exception cref="UnableToAllocateBufferException">
        /// Cannot allocate buffer after few trials.
        /// </exception>
        public IEnumerable<ArraySegment<byte>> BorrowBuffers(int count)
        {
            var result         = new ArraySegment<byte>[count];
            var trial          = 0;
            var total_received = 0;

            try
            {
                while (trial < TRIALS_COUNT)
                {

                    while (total_received < count)
                    {
                        if (!this._buffers.TryPop(out ArraySegment<byte> piece)) break;

                        result[total_received++] = piece;
                    }
                    if (total_received == count)
                    {
                        return result;
                    }
                    this.CreateNewSegment(false);
                    ++trial;
                }
                throw new UnableToAllocateBufferException();
            }
            catch
            {
                if (total_received > 0) this.ReturnBuffers(result.Take(total_received));
                throw;
            }
        }
        #endregion

        #region [ Return ]
        /// <summary>
        /// Return buffer.
        /// </summary>
        /// <param name="buffer"></param>
        public void ReturnBuffer(ArraySegment<byte> buffer)
        {
            if (this.ValidateBuffer(buffer))
            {
                this._buffers.Push(buffer);
            }
        }

        /// <summary>
        /// Return buffers.
        /// </summary>
        /// <param name="buffers"></param>
        public void ReturnBuffers(IEnumerable<ArraySegment<byte>> buffers)
        {
            if (null == buffers) throw new ArgumentNullException("buffers");

            foreach (var buffer in buffers)
            {
                if (this.ValidateBuffer(buffer))
                {
                    this._buffers.Push(buffer);
                }
            }
        }

        /// <summary>
        /// Return buffers.
        /// </summary>
        /// <param name="buffers"></param>
        public void ReturnBuffers(params ArraySegment<byte>[] buffers)
        {
            if (null == buffers) throw new ArgumentNullException("buffers");

            foreach (var buffer in buffers)
            {
                if (this.ValidateBuffer(buffer))
                {
                    this._buffers.Push(buffer);
                }
            }
        }
        #endregion

        #region [ Internal ]
        /// <summary>
        /// Create new segment.
        /// </summary>
        /// <param name="force_creation">Is force creation?</param>
        /// <exception cref="UnableToCreateMemoryException">
        /// All buffers were in use and acquiring more memory has been disabled.
        /// </exception>
        private void CreateNewSegment(bool force_creation)
        {
            if (!this._allowed_to_create_memory) throw new UnableToCreateMemoryException();

            lock (this._creating_new_segment_locker)
            {
                if (force_creation || (this._buffers.Count < this._segment_chunks / 2))
                {
                    byte[] bytes = new byte[_segment_size];
                    this._segments.Add(bytes);
                    for (int index = 0; index < this._segment_chunks; ++index)
                    {
                        this._buffers.Push(new ArraySegment<byte>(bytes, index * this._chunk_size, this._chunk_size));
                    }
                }
            }
        }

        /// <summary>
        /// Validate buffer.
        /// </summary>
        /// <param name="buffer">Need to verify the buffer.</param>
        /// <returns>
        /// <see cref="true" />  Verification success.
        /// <see cref="false" /> Verification failed.
        /// </returns>
        private bool ValidateBuffer(ArraySegment<byte> buffer)
        {
            if ((null == buffer.Array) || (0 == buffer.Count) || (buffer.Array.Length < (buffer.Offset + buffer.Count)))
            {
                return false;
            }
            if (buffer.Count != this._chunk_size)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
