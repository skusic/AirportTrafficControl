using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Base;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Actors.Query;

namespace PlaneController
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PlaneController : StatefulService, Base.IPlaneController
    {
        private int currentID = 0;
        private int maxActorNumber = 120;


        public PlaneController(StatefulServiceContext context)
            : base(context)
        { }


        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SendPlane();

                var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var list = await myDictionary.CreateEnumerableAsync(tx);
                    var enumerator = list.GetAsyncEnumerator();

                    while (await enumerator.MoveNextAsync(cancellationToken))
                    {

                        var actorID = new ActorId(enumerator.Current.Key);
                        var proxy = ActorProxy.Create<PlaneActor.Interfaces.IPlaneActor>(actorID, new Uri("fabric:/Airport/PlaneActorService"));
                        await proxy.Ping();
                    }
                }

               /* IActorService actorServiceProxy = ActorServiceProxy.Create(
                    new Uri("fabric:/Airport/PlaneActorService"), 0);

                ContinuationToken continuationToken = null;
                List<ActorInformation> activeActors = new List<ActorInformation>();

                do
                {
                    PagedResult<ActorInformation> page = await actorServiceProxy.GetActorsAsync(continuationToken, cancellationToken);

                    activeActors.AddRange(page.Items.Where(x => x.IsActive));

                    continuationToken = page.ContinuationToken;
                }
                while (continuationToken != null);*/

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        public async Task SendPlane()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");
            int planeID = 0;

            using (var tx = this.StateManager.CreateTransaction())
            {
                await myDictionary.SetAsync(tx, currentID, new PlaneInfo(-1, Base.PlaneStatus.Incoming));
                planeID = currentID++;
                currentID = currentID % maxActorNumber;
                await tx.CommitAsync();
            }

            var actorID = new ActorId(planeID);

            var proxy = ActorProxy.Create<PlaneActor.Interfaces.IPlaneActor>(actorID, new Uri("fabric:/Airport/PlaneActorService"));
            await proxy.StartPlane();
        }

        public async Task NotifyPlaneCanLand(int planeID, int laneID)
        {
            var actorID = new ActorId(planeID);
            var proxy = ActorProxy.Create<PlaneActor.Interfaces.IPlaneActor>(actorID, new Uri("fabric:/Airport/PlaneActorService"));
            await proxy.NotifyCanLand();

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>> ("dict");

            using (var tx = this.StateManager.CreateTransaction())
            {
                await myDictionary.SetAsync(tx, (int)planeID, new Base.PlaneInfo(laneID, PlaneStatus.Landing));

                await tx.CommitAsync();
            }
        }

        public async Task NotifyPlaneLeft(long id)
        {
            ServiceEventSource.Current.ServiceMessage(this.Context, "Plane Left : {0}", id);
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");
            int laneID = -1;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var temp = await myDictionary.TryGetValueAsync(tx, (int)id);
                laneID = temp.Value.LaneID;

                await myDictionary.TryRemoveAsync(tx, (int)id);
                await tx.CommitAsync();
            }

            var proxyLanes = ServiceProxy.Create<ITower>(new Uri("fabric:/Airport/TowerService"));
            await proxyLanes.NotifyLaneFreed(laneID);
        }

        public async Task<int> GetWaiting(CancellationToken token)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var list = await myDictionary.CreateEnumerableAsync(tx);
                var enumerator = list.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(token))
                {
                    if (enumerator.Current.Value.Status == Base.PlaneStatus.Waiting)
                        return enumerator.Current.Key;
                }
            }

            return -1;
        }

        public async Task NotifyPlaneStatus(long id, PlaneStatus status)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var current = await myDictionary.TryGetValueAsync(tx, (int)id);
                await myDictionary.SetAsync(tx, (int)id, new PlaneInfo(current.Value.LaneID, status));

                await tx.CommitAsync();
            }
        }

        public async Task<Dictionary<int, PlaneInfo>> GetPlanes(CancellationToken token)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Base.PlaneInfo>>("dict");
            Dictionary<int, PlaneInfo> infoDict = new Dictionary<int, PlaneInfo>();


            using (var tx = this.StateManager.CreateTransaction())
            {
                var list = await myDictionary.CreateEnumerableAsync(tx);
                var enumerator = list.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(token))
                {
                    infoDict.Add(enumerator.Current.Key, enumerator.Current.Value);
                }
            }

            return infoDict;
        }
    }
}
