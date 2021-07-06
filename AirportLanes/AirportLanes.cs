using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using Base;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace AirportLanes
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class AirportLanes : StatefulService, IAirportLanes
    {
        private const string InitiatedKey = "initiated";
        private readonly int numberOfLanes = 16;

        private readonly string _lanesDictionary = "lanes";
        private readonly string _initiatedDict = "dict_initiation";
        private readonly int LANE_EMPTY = -1;

        private int lastAcquiredLane = 0;

        public AirportLanes(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var initiatedDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>(_initiatedDict);

            using (var tx = StateManager.CreateTransaction())
            {
                var value = await initiatedDict.TryGetValueAsync(tx, InitiatedKey);

                if (!value.HasValue || value.Value == false)
                {
                    var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);


                    for (int i = 0; i < numberOfLanes; i++)
                    {
                        await myDictionary.AddAsync(tx, i, LANE_EMPTY);
                    }
                    
                    await initiatedDict.SetAsync(tx, InitiatedKey, true);
                    await tx.CommitAsync();
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
         
        }

        async Task<List<int>> IAirportLanes.GetFreeLanes()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);

            List<int> freeLanesList = new List<int>();

            using (var tx = StateManager.CreateTransaction())
            {
                for (int i = 0; i < numberOfLanes; i++)
                {
                    var result = await myDictionary.TryGetValueAsync(tx, i);
                    if (result.HasValue && result.Value == LANE_EMPTY)
                        freeLanesList.Add(i);
                }
            }

            return freeLanesList;
        }

        async Task<int> IAirportLanes.AcquireFreeLane(int planeID)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);

            int lane = -1;
            int laneLimit = lastAcquiredLane;

            using (var tx = StateManager.CreateTransaction())
            {
                for (int laneNumber = 0; laneNumber < numberOfLanes; laneNumber++)
                {
                    int realNumber = (laneNumber + laneLimit) % numberOfLanes;
                    var result = await myDictionary.TryGetValueAsync(tx, realNumber);

                    if (result.HasValue && result.Value == LANE_EMPTY)
                    {
                        lane = realNumber;
                        await myDictionary.SetAsync(tx, realNumber, planeID);
                        await tx.CommitAsync();
                        break;
                    }
                }
            }

            if (lane != -1)
            {
                lastAcquiredLane = lane;

                var proxy = ServiceProxy.Create<ILaneData>(new Uri("fabric:/Airport/LaneData"), 
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(lane % 4));

                await proxy.RecordFlight(lane, planeID);
            }

            return lane;
        }

        async Task IAirportLanes.FreeLane(int laneID)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Lane Clear : {0}", laneID);
                var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);

                using (var tx = StateManager.CreateTransaction())
                {
                    await myDictionary.SetAsync(tx, laneID, LANE_EMPTY);
                    await tx.CommitAsync();
                }
                ServiceEventSource.Current.ServiceMessage(this.Context, "Lane Clear after: {0}", laneID);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, e.ToString());
            }
        }

        async Task<List<int>> IAirportLanes.GetLanes()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);
            List<int> lanes = new List<int>();

            using (var tx = this.StateManager.CreateTransaction())
            {
                for (int i = 0; i < numberOfLanes; i++)
                {
                    var value = await myDictionary.TryGetValueAsync(tx, i);
                    lanes.Add(value.Value);
                }
            }

            return lanes;
        }

        async Task IAirportLanes.MarkLaneBusy(int laneID)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var value = await myDictionary.TryGetValueAsync(tx, laneID);

                if (value.Value == -1)
                {
                    await myDictionary.SetAsync(tx, laneID, 999);
                    await tx.CommitAsync();
                }
            }

        }

        async Task IAirportLanes.UnMarkLaneBusy(int laneID)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, int>>(_lanesDictionary);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var value = await myDictionary.TryGetValueAsync(tx, laneID);

                if (value.Value == 999)
                {
                    await myDictionary.SetAsync(tx, laneID, -1);
                    await tx.CommitAsync();
                }
            }

        }
    }
}
