using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

using Base;

namespace TowerService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TowerService : StatelessService, ITower
    {
        public TowerService(StatelessServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            //await Task.Delay(TimeSpan.FromMilliseconds(2000), cancellationToken);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await HandleWaitingPlanes();
                }
                catch (Exception e)
                { 
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }
        }

        private async Task HandleWaitingPlanes()
        {
            var proxyLanes = ServiceProxy.Create<IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));
            var proxyPlanes = ServiceProxy.Create<IPlaneController>(new Uri("fabric:/Airport/PlaneController"));

            var waiting = await proxyPlanes.GetWaiting(new CancellationToken());
            if (waiting == -1)
                return;

            var lane = await proxyLanes.AcquireFreeLane(waiting);
            if (lane == -1)
                return;

            await proxyPlanes.NotifyPlaneCanLand(waiting, lane);
        }

        public async Task NotifyLaneFreed(int laneID)
        {
            var proxyLanes = ServiceProxy.Create<IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));
            await proxyLanes.FreeLane(laneID);
        }
    }
}
