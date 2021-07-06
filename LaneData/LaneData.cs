using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

using Base;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace LaneData
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class LaneData : StatefulService, ILaneData
    {
        private const string _dictionaryName = "myDictionary";

        public LaneData(StatefulServiceContext context)
            : base(context)
        { }

       
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        public async Task RecordFlight(int laneID, int planeID)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, List<int>>>(_dictionaryName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await myDictionary.AddOrUpdateAsync(tx, laneID, new List<int>() { planeID }, (key, value) => { value.Add(planeID); return value; });
                await tx.CommitAsync();
            }
        }

        public async Task<List<int>> GetLaneRecords(int laneID)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, List<int>>>(_dictionaryName);

            List<int> laneRecords;
            using (var tx = this.StateManager.CreateTransaction())
            {
                laneRecords = await myDictionary.GetOrAddAsync(tx, laneID, new List<int>());
            }

            return laneRecords;
        }

        public async Task<Dictionary<int, List<int>>> GetRecords(CancellationToken token)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, List<int>>>(_dictionaryName);
            Dictionary<int, List<int>> recordDict = new Dictionary<int, List<int>>();


            using (var tx = this.StateManager.CreateTransaction())
            {
                var list = await myDictionary.CreateEnumerableAsync(tx);
                var enumerator = list.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(token))
                {
                    recordDict.Add(enumerator.Current.Key, enumerator.Current.Value);
                }
            }

            return recordDict;
        }

        public async Task ClearRecords()
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, List<int>>>(_dictionaryName);

            await myDictionary.ClearAsync();
        }
    }
}
