using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Base;

namespace UIApplication
{
    public partial class Form1 : Form
    {
        private Dictionary<int, ListBox> partitionToListbox;

        public Form1()
        {
            InitializeComponent();

            partitionToListbox = new Dictionary<int, ListBox>() { 
                { 0, this.listBox2 }, 
                { 1, this.listBox3 }, 
                { 2, this.listBox4 },
                { 3, this.listBox5 } };
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetLanes();

            GetPlanes();

            GetLaneRecords();
        }

        private async void GetLanes()
        {
            var proxy = ServiceProxy.Create<IAirportLanes>(new Uri("fabric:/Airport/AirportLanes"));
            List<int> lanes = new List<int>();

            try
            {
                lanes = await proxy.GetLanes();
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

            listView1.Items.Clear();

            foreach (int lane in lanes)
            {
                listView1.Items.Add(lane.ToString());
            }
        }

        private async void GetPlanes()
        {
            var proxyPlanes = ServiceProxy.Create<IPlaneController>(new Uri("fabric:/Airport/PlaneController"));
            Dictionary<int, PlaneInfo> dict = new Dictionary<int, PlaneInfo>();

            try
            {
                dict = await proxyPlanes.GetPlanes(new System.Threading.CancellationToken());
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }

            listBox1.Items.Clear();

            foreach (KeyValuePair<int, PlaneInfo> pair in dict)
            {
                listBox1.Items.Add(String.Format("{0}  :  {1}", pair.Key, pair.Value.Status.ToString()));
            }
        }

        private async void GetLaneRecords()
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();

            foreach (KeyValuePair<int, ListBox> pair in partitionToListbox)
            {
                var proxyLanesData = ServiceProxy.Create<ILaneData>(new Uri("fabric:/Airport/LaneData"),
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(pair.Key));

                try
                {
                    dict = await proxyLanesData.GetRecords(new System.Threading.CancellationToken());
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.ToString());
                }
                pair.Value.Items.Clear();

                foreach (KeyValuePair<int, List<int>> lanePair in dict)
                {
                    pair.Value.Items.Add(String.Format("{0}  :  {1}", lanePair.Key, string.Join(",", lanePair.Value.ToArray())));
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var proxyPlanes = ServiceProxy.Create<IPlaneController>(new Uri("fabric:/Airport/PlaneController"));


            try
            {
                await proxyPlanes.SendPlane();
            }
            catch (System.Fabric.FabricServiceNotFoundException error)
            {
                Console.WriteLine(error.ToString());
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        { 
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ClearPartition(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ClearPartition(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ClearPartition(2);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ClearPartition(3);
        }

        private async void ClearPartition(int partID)
        {
            var proxyLanesData = ServiceProxy.Create<ILaneData>(new Uri("fabric:/Airport/LaneData"),
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partID));
            try
            {
                await proxyLanesData.ClearRecords();
            }
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }
    }
}
