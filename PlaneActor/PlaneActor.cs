using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using PlaneActor.Interfaces;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace PlaneActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class PlaneActor : Actor, IPlaneActor
    {
        private const string StateKey = "state";
        private IActorTimer timer;

        /// <summary>
        /// Initializes a new instance of PlaneActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public PlaneActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activate {0}", this.Id.GetLongId());
            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor deactivate {0}", this.Id.GetLongId());
            if (timer != null)
                UnregisterTimer(timer);

            return base.OnDeactivateAsync();
        }

        public async Task StartPlane()
        {
            timer = RegisterTimer(Update
                , null, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(5));

            await this.StateManager.SetStateAsync(StateKey, PlaneStatus.Incoming);
        }

        public async Task NotifyCanLand()
        {
            var v = await this.StateManager.TryGetStateAsync<int>(StateKey);
            if (v.Value == (int)PlaneStatus.Waiting)
                await this.StateManager.SetStateAsync(StateKey, PlaneStatus.Landing);
        }

        private async Task Update(object work)
        {
            ActorEventSource.Current.ActorMessage(this, "Actor update");
            var v = await this.StateManager.TryGetStateAsync<int>(StateKey);
            ActorEventSource.Current.ActorMessage(this, "Actor update {0} : {1}", this.Id.GetLongId(), v.Value);

            if (v.Value == (int)PlaneStatus.Waiting)
                return;

            int nextVal = (int)v.Value + 1;
            if (Enum.IsDefined(typeof(PlaneStatus), nextVal))
            {
                await this.StateManager.SetStateAsync(StateKey, nextVal);

                var proxy = ServiceProxy.Create<IPlaneController>(new Uri("fabric:/Airport/PlaneController"));
                await proxy.NotifyPlaneStatus(this.Id.GetLongId(), (PlaneStatus)nextVal);
            }
            else
            { 
                ActorEventSource.Current.ActorMessage(this, "Actor noty {0}", this.Id.GetLongId());
                await this.StateManager.SetStateAsync(StateKey, nextVal);
                UnregisterTimer(timer);

                var proxy = ServiceProxy.Create<IPlaneController>(new Uri("fabric:/Airport/PlaneController"));
                await proxy.NotifyPlaneLeft(this.Id.GetLongId());
            }

        }

        public async Task Ping()
        {
            var v = await this.StateManager.TryGetStateAsync<PlaneStatus>(StateKey);

            if (timer == null && Enum.IsDefined(typeof(PlaneStatus), v.Value))
                timer = RegisterTimer(Update
                    , null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4));
        }
    }
}
