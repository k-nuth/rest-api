using System;

namespace bitprim.insight.DTOs
{
    /// <summary>
    /// Address history response data structure.
    /// </summary>
    public class GetAddressHistoryResponse
    {
        /// <summary>
        /// Address string; for BCH, it can be in cashaddr format.
        /// </summary>
        public string addrStr { get; set; }

        /// <summary>
        /// Current wallet balance in coin units.
        /// </summary>
        public decimal balance { get; set; }

        /// <summary>
        /// Current wallet balance in Satoshis.
        /// </summary>
        public UInt64 balanceSat { get; set; }

        /// <summary>
        /// Total amount of money received from the beginning of the chain, in coin units.
        /// </summary>
        public decimal totalReceived { get; set; }

        /// <summary>
        /// Total amount sent in Satoshis.
        /// </summary>
        public UInt64 totalReceivedSat { get; set; }

        /// <summary>
        /// Total amount of money sent from this wallet, from the beginning of the chain, in coin units.
        /// </summary>
        public decimal totalSent { get; set; }

        /// <summary>
        /// Total amount sent in Satoshis.
        /// </summary>
        public UInt64 totalSentSat { get; set; }


        /// <summary>
        /// Balance computed considering only the currently unconfirmed transactions involving this address, in coin units.
        /// </summary>
        public decimal unconfirmedBalance { get; set; }

        /// <summary>
        /// Unconfirmed balance in Satoshis.
        /// </summary>
        public Int64 unconfirmedBalanceSat { get; set; }

        /// <summary>
        /// Current amount of distinct unconfirmed transactions involving this address.
        /// Correctly spelled version, for those who prefer it.
        /// </summary>
        public UInt64 unconfirmedTxAppearances { get; set; }

        /// <summary>
        /// Current amount of distinct unconfirmed transactions involving this address.
        /// The spelling error (apperances) is intentional, for compatibility with insight.
        /// TODO Remove this property as soon as no one uses it
        /// </summary>
        public UInt64 unconfirmedTxApperances { get; set; }

        /// <summary>
        /// Amount of distinct transactions (i.e. don't count the same tx more than once) involving this address.
        /// Correctly spelled version, for those who prefer it.
        /// </summary>
        public uint txAppearances { get; set; }

        /// <summary>
        /// Amount of distinct transactions (i.e. don't count the same tx more than once) involving this address.
        /// The spelling error (apperances) is intentional, for compatibility with insight.
        /// TODO Remove this property as soon as no one uses it
        /// </summary>
        public uint txApperances { get; set; }
        
        /// <summary>
        /// Selected transaction ids (using from and to parameters) from the full history.
        /// </summary>
        public string[] transactions { get; set; }   
    }
}
