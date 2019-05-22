using System;
using bitprim.insight.DTOs;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using bitprim.insight.Websockets;

namespace bitprim.insight.Controllers
{
    /// <summary>
    /// Peer/Bitprim node related operations.
    /// </summary>
    [Route("[controller]")]
    public class PeerController : Controller
    {
        /// <summary>
        /// Get bitprim-insight API version.
        /// </summary>
        /// <returns> See GetApiVersionResponse DTO. </returns>
        [HttpGet("version")]
        [ResponseCache(CacheProfileName = Constants.Cache.SHORT_CACHE_PROFILE_NAME)]
        [SwaggerOperation("GetApiVersion")]
        [SwaggerResponse((int)System.Net.HttpStatusCode.OK, typeof(GetApiVersionResponse))]
        public ActionResult GetApiVersion()
        {
            //TODO Implement versioning (RA-6)
            return Json(new GetApiVersionResponse
            {
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Get peer/Bitprim node status information.
        /// </summary>
        /// <returns> See GetPeerStatusResponse DTO. </returns>
        [HttpGet("peer")]
        [ResponseCache(CacheProfileName = Constants.Cache.SHORT_CACHE_PROFILE_NAME)]
        [SwaggerOperation("GetPeerStatus")]
        [SwaggerResponse((int)System.Net.HttpStatusCode.OK, typeof(GetPeerStatusResponse))]
        public ActionResult GetPeerStatus()
        {
            //TODO Get this information from node-cint
            return Json(new GetPeerStatusResponse
            {
                connected = true,
                host = "127.0.0.1",
                port = null
            });
        }

        /// <summary>
        /// Get websocket stats.
        /// </summary>
        /// <returns> See WebSocketStatsDto. </returns>
        [HttpGet("stats")]
        [ResponseCache(CacheProfileName = Constants.Cache.SHORT_CACHE_PROFILE_NAME)]
        [ApiExplorerSettings(IgnoreApi=true)]
        [SwaggerOperation("GetStats")]
        [SwaggerResponse((int)System.Net.HttpStatusCode.OK, typeof(WebSocketStatsDto))]
        public ActionResult GetStats()
        {
            return Json(new WebSocketStatsDto
            {
                wss_input_messages = WebSocketStats.InputMessages
                ,wss_output_messages=WebSocketStats.OutputMessages
                ,wss_pending_queue_size=WebSocketStats.PendingQueueSize
                ,wss_sent_messages=WebSocketStats.SentMessages
                ,wss_subscriber_count=WebSocketStats.SubscriberCount
            });
        }

        /// <summary>
        /// Get websocket stats.
        /// </summary>
        /// <returns> See WebSocketStatsDto. </returns>
        [HttpGet("memstats")]
        [ApiExplorerSettings(IgnoreApi=true)]
        [SwaggerOperation("GetMemStats")]
        [SwaggerResponse((int)System.Net.HttpStatusCode.OK, typeof(MemoryStatsDto))]
        public ActionResult GetMemStats()
        {
            return Json(new MemoryStatsDto());
        }

        /// <summary>
        /// Force GC.
        /// </summary>
        [HttpGet("gc")]
        [ApiExplorerSettings(IgnoreApi=true)]
        [SwaggerOperation("GetGc")]
        public ActionResult Gc(int generation = 0, bool forced = false, bool block = false)
        {
            GC.Collect(generation,forced ?  GCCollectionMode.Forced: GCCollectionMode.Default, block);
            return Json("OK");
        }
    }
}
