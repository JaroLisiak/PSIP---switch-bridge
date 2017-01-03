using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSIP
{
    class Status
    {
        volatile private int outArp, outUdp, outIp,outIcmp,outAll,outTcp;
        volatile private int inArp, inUdp, inIp, inIcmp, inAll, inTcp;

        public int OutArp
        {
            get
            {
                return outArp;
            }

            set
            {
                outArp = value;
            }
        }

        public int OutUdp
        {
            get
            {
                return outUdp;
            }

            set
            {
                outUdp = value;
            }
        }

        public int OutIp
        {
            get
            {
                return outIp;
            }

            set
            {
                outIp = value;
            }
        }

        public int OutIcmp
        {
            get
            {
                return outIcmp;
            }

            set
            {
                outIcmp = value;
            }
        }

        public int OutAll
        {
            get
            {
                return outAll;
            }

            set
            {
                outAll = value;
            }
        }

        public int OutTcp
        {
            get
            {
                return outTcp;
            }

            set
            {
                outTcp = value;
            }
        }

        public int InTcp
        {
            get
            {
                return inTcp;
            }

            set
            {
                inTcp = value;
            }
        }

        public int InArp
        {
            get
            {
                return inArp;
            }

            set
            {
                inArp = value;
            }
        }

        public int InUdp
        {
            get
            {
                return inUdp;
            }

            set
            {
                inUdp = value;
            }
        }

        public int InIp
        {
            get
            {
                return inIp;
            }

            set
            {
                inIp = value;
            }
        }

        public int InIcmp
        {
            get
            {
                return inIcmp;
            }

            set
            {
                inIcmp = value;
            }
        }

        public int InAll
        {
            get
            {
                return inAll;
            }

            set
            {
                inAll = value;
            }
        }


        public Status()
        {
            outAll = 0;
            outArp = 0;
            outIp = 0;
            outIcmp = 0;
            outUdp = 0;
            outTcp = 0;
            inAll = 0;
            inArp = 0;
            inIp = 0;
            inIcmp = 0;
            inUdp = 0;
            inTcp = 0;
        }
    }
}
