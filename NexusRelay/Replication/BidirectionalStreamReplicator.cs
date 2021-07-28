/*
 * TheNexusAvenger
 *
 * Replicates data between 2 streams bi-directionally.
 */

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusRelay.Replication
{
    public class BidirectionalStreamReplicator
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
        public BidirectionalStreamReplicator(Stream sourceStream, Stream targetStream)
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
            await Task.WhenAny(new[]
            {
                new StreamReplicator(this._sourceStream, this._targetStream).ReplicateStream(cancellationToken),
                new StreamReplicator(this._targetStream, this._sourceStream).ReplicateStream(cancellationToken),
            });
        }
    }
}