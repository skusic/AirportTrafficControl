using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Base
{
    public interface ILaneData : IService
    {
        public Task RecordFlight(int laneID, int planeID);


        public Task<List<int>> GetLaneRecords(int laneID);

        public Task<Dictionary<int, List<int>>> GetRecords(CancellationToken token);

        public Task ClearRecords();
    }
}
