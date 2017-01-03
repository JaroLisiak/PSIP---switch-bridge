using PcapDotNet.Packets.Ethernet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;

namespace PSIP
{
    class Row
    {
        private int port;
        private MacAddress mac;
        private int time;

        public int Time
        {
            get
            {
                return time;
            }

            set
            {
                time = value;
            }
        }

        public MacAddress Mac
        {
            get
            {
                return mac;
            }

            set
            {
                mac = value;
            }
        }

        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
            }
        }

        public Row(int port, MacAddress address, int time) {
            this.port = port;
            this.mac = address;
            this.Time = time;
        }





        public Row()
        {

        }
    }
}
