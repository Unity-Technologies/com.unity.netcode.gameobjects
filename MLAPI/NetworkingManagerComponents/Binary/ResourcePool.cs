using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;

namespace MLAPI.Serialization
{
    /// <summary>
    /// Static class containing PooledBitStreams
    /// </summary>
    public static class BitStreamPool
    {
        private static byte createdStreams = 0;
        private static readonly Queue<WeakReference> overflowStreams = new Queue<WeakReference>();
        private static readonly Queue<PooledBitStream> streams = new Queue<PooledBitStream>();

        /// <summary>
        /// Retrieves an expandable PooledBitStream from the pool
        /// </summary>
        /// <returns>An expandable PooledBitStream</returns>
        public static PooledBitStream GetStream()
        {
            if (overflowStreams.Count > 0)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Retrieving PooledBitStream from overflow pool. Recent burst?");
                WeakReference weakStream;
                while (!(weakStream = overflowStreams.Dequeue()).IsAlive) ;
                if (weakStream.IsAlive) return (PooledBitStream)weakStream.Target;
            }

            if (streams.Count == 0)
            {
                if (createdStreams == 254) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("255 streams have been created. Did you forget to dispose?");
                else if (createdStreams < 255) createdStreams++;
                
                return new PooledBitStream();
            }

            PooledBitStream stream = streams.Dequeue();
            stream.SetLength(0);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Puts a PooledBitStream back into the pool
        /// </summary>
        /// <param name="stream">The stream to put in the pool</param>
        public static void PutBackInPool(PooledBitStream stream)
        {
            if (streams.Count > 16)
            {
                //The user just created lots of streams without returning them in between.
                //Streams are essentially byte array wrappers. This is valuable memory.
                //Thus we put this stream as a weak reference incase of another burst
                //But still leave it to GC
                if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("Putting PooledBitStream into overflow pool. Did you forget to dispose?");
                overflowStreams.Enqueue(new WeakReference(stream));
            }
            else
            {
                streams.Enqueue(stream);
            }
        }
    }

    /// <summary>
    /// Static class containing PooledBitWriters
    /// </summary>
    public static class BitWriterPool
    {
        private static byte createdWriters = 0;
        private static readonly Queue<PooledBitWriter> writers = new Queue<PooledBitWriter>();

        /// <summary>
        /// Retrieves a PooledBitWriter
        /// </summary>
        /// <param name="stream">The stream the writer should write to</param>
        /// <returns>A PooledBitWriter</returns>
        public static PooledBitWriter GetWriter(Stream stream)
        {
            if (writers.Count == 0)
            {
                if (createdWriters == 254) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("255 writers have been created. Did you forget to dispose?");
                    else if (createdWriters < 255) createdWriters++;
                
                return new PooledBitWriter(stream);
            }

            PooledBitWriter writer = writers.Dequeue();
            writer.SetStream(stream);
            
            return writer;
        }

        /// <summary>
        /// Puts a PooledBitWriter back into the pool
        /// </summary>
        /// <param name="writer">The writer to put in the pool</param>
        public static void PutBackInPool(PooledBitWriter writer)
        {
            if (writers.Count < 64) writers.Enqueue(writer);
            else if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("BitWriterPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }

    /// <summary>
    /// Static class containing PooledBitReaders
    /// </summary>
    public static class BitReaderPool
    {
        private static byte createdReaders = 0;
        private static readonly Queue<PooledBitReader> readers = new Queue<PooledBitReader>();

        /// <summary>
        /// Retrieves a PooledBitReader
        /// </summary>
        /// <param name="stream">The stream the reader should read from</param>
        /// <returns>A PooledBitReader</returns>
        public static PooledBitReader GetReader(Stream stream)
        {
            if (readers.Count == 0)
            {
                if (createdReaders == 254) if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("255 readers have been created. Did you forget to dispose?");
                else if (createdReaders < 255) createdReaders++;
                
                return new PooledBitReader(stream);
            }

            PooledBitReader reader = readers.Dequeue();
            reader.SetStream(stream);

            return reader;
        }

        /// <summary>
        /// Puts a PooledBitReader back into the pool
        /// </summary>
        /// <param name="reader">The reader to put in the pool</param>
        public static void PutBackInPool(PooledBitReader reader)
        {
            if (readers.Count < 64) readers.Enqueue(reader);
            else if (LogHelper.CurrentLogLevel <= LogLevel.Developer) LogHelper.LogInfo("BitReaderPool already has 64 queued. Throwing to GC. Did you forget to dispose?");
        }
    }

    /// <summary>
    /// Disposable BitStream that returns the Stream to the BitStreamPool when disposed
    /// </summary>
    public sealed class PooledBitStream : BitStream, IDisposable
    {
        private bool isDisposed = false;
        /// <summary>
        /// Gets a PooledBitStream from the static BitStreamPool
        /// </summary>
        /// <returns>PooledBitStream</returns>
        public static PooledBitStream Get()
        {
            PooledBitStream stream = BitStreamPool.GetStream();
            stream.isDisposed = false;
            return stream;
        }

        /// <summary>
        /// Returns the PooledBitStream into the static BitStreamPool
        /// </summary>
        public new void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitStreamPool.PutBackInPool(this);
            }
        }
    }

    /// <summary>
    /// Disposable BitWriter that returns the Writer to the BitWriterPool when disposed
    /// </summary>
    public sealed class PooledBitWriter : BitWriter, IDisposable
    {
        private bool isDisposed = false;
        
        internal PooledBitWriter(Stream stream) : base(stream)
        {

        }

        /// <summary>
        /// Gets a PooledBitWriter from the static BitWriterPool
        /// </summary>
        /// <returns>PooledBitWriter</returns>
        public static PooledBitWriter Get(Stream stream)
        {
            PooledBitWriter writer = BitWriterPool.GetWriter(stream);
            writer.isDisposed = false;
            return writer;
        }

        /// <summary>
        /// Returns the PooledBitWriter into the static BitWriterPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitWriterPool.PutBackInPool(this);
            }
        }
    }

    /// <summary>
    /// Disposable BitReader that returns the Reader to the BitReaderPool when disposed
    /// </summary>
    public sealed class PooledBitReader : BitReader, IDisposable
    {
        private bool isDisposed = false;
        
        internal PooledBitReader(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Gets a PooledBitReader from the static BitReaderPool
        /// </summary>
        /// <returns>PooledBitReader</returns>
        public static PooledBitReader Get(Stream stream)
        {
            PooledBitReader reader = BitReaderPool.GetReader(stream);
            reader.isDisposed = false;
            return reader;
        }

        /// <summary>
        /// Returns the PooledBitReader into the static BitReaderPool
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                BitReaderPool.PutBackInPool(this);
            }
        }
    }
}
