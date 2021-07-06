using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Remoting;

namespace Base
{
    public interface IPlaneController : IService
    {

        public Task SendPlane();

        public Task NotifyPlaneCanLand(int planeID, int laneID);

        public Task NotifyPlaneLeft(long id);

        public Task NotifyPlaneStatus(long id, PlaneStatus status);

        public Task<int> GetWaiting(CancellationToken token);

        public Task<Dictionary<int, PlaneInfo>> GetPlanes(CancellationToken token);
    }
}
