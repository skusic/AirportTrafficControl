using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Base
{
    public interface ITower : IService
    {
        public Task NotifyLaneFreed(int laneID);
    }
}
