/*
 * TheNexusAvenger
 *
 * Replicates data from a stream to another.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusRelay.Replication
{
    public class StreamReplicator
    {
        /// <summary>
        /// Stream to read data from.
        /// </summary>
        private readonly Stream _sourceStream;
        
        /// <summary>
        /// Stream to write data to.
        /// </summary>
        private readonly Stream _targetStream;
        
        /// <summary>
        /// Creates the stream replicator.
        /// </summary>
        /// <param name="sourceStream">Stream to replicate from.</param>
        /// <param name="targetStream">Stream to replicate to.</param>
        public StreamReplicator(Stream sourceStream, Stream targetStream)
        {
            this._sourceStream = sourceStream;
            this._targetStream = targetStream;
        }
        
        /// <summary>
        /// Starts replicating data between the streams.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for stopping replicating.</param>
        public async Task ReplicateStream(CancellationToken cancellationToken)
        {
            try
            {
                await this._sourceStream.CopyToAsync(this._targetStream,cancellationToken);
            }
            catch (Exception)
            {
                // No exceptions thrown (such as network errors).
            }
        }
    }
}