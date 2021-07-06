using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Base;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommunicationController : ControllerBase
    {
        [HttpGet]
        [Route("getFreeLanes")]
        public async Task<List<int>> GetFreeLanes()
        {
            var proxy = ServiceProxy.Create<Base.IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));

            var result = await proxy.GetFreeLanes();

            return result;
        }

        [HttpGet]
        [Route("getLanes")]
        public async Task<List<int>> GetLanes()
        {
            var proxy = ServiceProxy.Create<Base.IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));

            var result = await proxy.GetLanes();

            return result;
        }

        [HttpGet]
        [Route("GetLaneRecords")]
        public async Task<List<int>> GetLaneRecords([FromQuery] int laneID)
        {
            var proxy = ServiceProxy.Create<Base.ILaneData>(new Uri("fabric:/Airport/LaneData"), 
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(laneID % 4));

            var result = await proxy.GetLaneRecords(laneID);

            return result;
        }

        [HttpGet]
        [Route("MarkBusy")]
        public async Task MarkBusy([FromQuery] int laneID)
        {
            var proxy = ServiceProxy.Create<Base.IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));

            await proxy.MarkLaneBusy(laneID);
        }

        [HttpGet]
        [Route("UnMarkBusy")]
        public async Task UnMarkBusy([FromQuery] int laneID)
        {
            var proxy = ServiceProxy.Create<Base.IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));

            await proxy.UnMarkLaneBusy(laneID);
        }
    }
}
