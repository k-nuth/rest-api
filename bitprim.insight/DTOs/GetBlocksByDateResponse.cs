using System;

namespace bitprim.insight.DTOs
{
    /// <summary>
    /// Blocks by date result data structure.
    /// </summary>
    public class GetBlocksByDateResponse
    {
        /// <summary>
        /// Found blocks.
        /// </summary>
        public BlockSummary[] blocks { get; set; }

        /// <summary>
        /// Amount of found blocks.
        /// </summary>
        public int length { get; set; }

        /// <summary>
        /// Pagination information.
        /// </summary>
        public Pagination pagination { get; set; }
    }

    /// <summary>
    /// Summarized block information.
    /// </summary>
    public class BlockSummary
    {
        /// <summary>
        /// Block height.
        /// </summary>
        public UInt64 height { get; set; }

        /// <summary>
        /// Serialized block size.
        /// </summary>
        public UInt64 size { get; set; }

        /// <summary>
        /// Block hash as 64-character (32 bytes) hex string.
        /// </summary>
        public string hash { get; set; }

        /// <summary>
        /// Date when the block was mined, in Unix timestamp format.
        /// </summary>
        public UInt32 time { get; set; }

        /// <summary>
        /// Amount of transactions in the block.
        /// </summary>
        public UInt64 txlength { get; set; }

        /// <summary>
        /// Block miner pool info (if applies).
        /// </summary>
        public PoolInfo poolInfo { get; set; }

      
    }

    /// <summary>
    /// Pagination information (not including results).
    /// </summary>
    public class Pagination
    {
        /// <summary>
        /// Day after search date.
        /// </summary>
        public string next { get; set; }

        /// <summary>
        /// Day before search date.
        /// </summary>
        public string prev { get; set; }

        /// <summary>
        /// Unix timestamp; marks the end of the search interval.
        /// </summary>
        public long currentTs { get; set; }

        /// <summary>
        /// Search date in configured date input format (default: yyyy-MM-dd).
        /// </summary>
        public string current { get; set; }

        /// <summary>
        /// True if and only if the block date to search corresponds to today.
        /// </summary>
        public bool isToday { get; set; }

        /// <summary>
        /// True if and only if the paginated results do not contain all the matching blocks.
        /// </summary>
        public bool more { get; set; }

        /// <summary>
        /// Unix timestamp of the first block beyond (i.e. before in the blockchain) the selected interval.
        /// </summary>
        public long? moreTs { get; set; }        
        
    }
}