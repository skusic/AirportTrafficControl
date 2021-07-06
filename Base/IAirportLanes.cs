using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Base
{
    // [DataContract]
    // [DataMember]

    public interface IAirportLanes : IService
    {
        Task<List<int>> GetFreeLanes();

        Task<int> AcquireFreeLane(int planeID);

        Task FreeLane(int laneID);

        Task<List<int>> GetLanes();

        Task MarkLaneBusy(int laneID);

        Task UnMarkLaneBusy(int laneID);
    }

    public enum PlaneStatus { Incoming, Waiting, Landing, Landed };

    [DataContract]
    public class PlaneInfo
    {
        public PlaneInfo(int laneID, PlaneStatus status)
        {
            this.LaneID = laneID;
            this.Status = status;
        }
        [DataMember]
        public int LaneID { get; set; }
        [DataMember]
        public PlaneStatus Status { get; set; }
    }
}
