using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

/*
 * TODO:
 * co vsetko zobrazovat v statistikach? Staci pocet prijatych/ odoslanych v obidvoch smeroch na obidvoch portoch?
 * arp, udp, tcp,ip,icmp,celkovy pocet ramcov
 * Akym sposobom filtrovat - staci KARTA smer DRUH filtra(mac/IP/port) a hodnota(12:34:56:78..., 192.168...,8080)?
 * poyriet sa ako funguju filtre v cisco switchoch{routroch
 * Za kolko bodov je zakladna funkcionalita switchu
 * 
 * 
 * configurovatelnz cas timera z GUI
 * 
 * NIKDZ NEPOSIELAM TAM ODKIAL SOM PRIJAL
 * vyskusat velkost bufferu 1000 timer na 1
 * 
 * Preco pingovany pocitac po vymene kablov pise Ping( no response found)
 * 
 * TODO Nefunguje update tabulky / upravit updatetable
 */




namespace PSIP
{

    public partial class Form2 : Form
    {

        private static System.Timers.Timer aTimer, bTimer;

        private double RowInTable = 10;
        //private Packet packet;
        private List<Packet> buffer;
        private Packet lastPacket;
        private IList<LivePacketDevice> allDevices;
        private LivePacketDevice selectedDevice, sendDevice;
        private PacketCommunicator communicator, communicator2;
        private List<Row> table = new List<Row>();
        //private int pkt_cnt;
        private Thread thrReceive;
        private Thread thrReceive2;

        private bool loopbackFilter = false;
        private bool statistics = true;

        private bool capturing = false;

        private List<Packet> odoslane_z_1 = new List<Packet>();
        private List<Packet> odoslane_z_2 = new List<Packet>();

        private ulong prijate1 = 0, prijate2 = 0, odoslane1 = 0, odoslane2 = 0;

        private int TIMEOUT = -1;
        private int PACKET_SIZE = 65536;///4;//65536

        private Status one = new Status();
        private Status two = new Status();

        public Form2(LivePacketDevice device, LivePacketDevice device2)
        {
            InitializeComponent();
            Show();
            selectedDevice = device;
            sendDevice = device2;



            communicator = selectedDevice.Open(PACKET_SIZE, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, TIMEOUT);
            communicator2 = sendDevice.Open(PACKET_SIZE, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, TIMEOUT);
            //communicator.;
            //BerkeleyPacketFilter x = communicator.CreateFilter(loopbackFilter).loopbackFilter = true;
            thrReceive = new Thread(new ThreadStart(this.Receiving1));
            thrReceive.IsBackground = true;
            thrReceive2 = new Thread(new ThreadStart(this.Receiving2));
            thrReceive2.IsBackground = true;
            thrReceive.Start();
            thrReceive2.Start();



            //pkt_cnt = 0;
            //buffer = new List<Packet>();
            SetTimer();
            // sendVia1(BuildEthernetPacket());
        }

        private void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(1000);     // update MAC tabulky
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;

            bTimer = new System.Timers.Timer(1000); // vykreslovanie MAC tabulky do GUI
            // Hook up the Elapsed event for the timer. 
            bTimer.Elapsed += OnTimedEventb;
            bTimer.AutoReset = true;
            bTimer.Enabled = true;
        }
        private void OnTimedEventb(Object source, ElapsedEventArgs e)
        {
            showTable();
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            for (int i = table.Count - 1; i >= 0; i--)
            {
                if (table[i].Time <= 0)
                {
                    table.RemoveAt(i);
                }
                else
                {
                    table[i].Time -= 1;
                }
            }
            updateStats();
        }

        int getOutPut(MacAddress mac)
        {
            // v MAC tabulke najde na ktorom porte (siet. karte) sa nachadza dana MAC adresa
            foreach (var i in table.ToList())
            {
                MacAddress m = i.Mac;
                if (m.Equals(mac))
                {
                    return i.Port;
                }
            }
            return -1;  // ak sa dana mac adresa nenachadza v tabulke
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void PacketHandler(Packet packet)
        {
            bool breake = false;
            if (packet.Ethernet.Source.Equals(new MacAddress("01:01:01:01:01:01")))
            {
                //Console.Write("########## PRIJAL SOM TESTOVACI PACKET");
                //breake = false;
            }
            if (capturing == false)
            {
                //Console.WriteLine("neprijimam");
                return;
            }
            //Console.WriteLine("Prijal som paket na 1");

            //Console.WriteLine(odoslane_z_1.Count);
            if (loopbackFilter)
            {
                foreach (Packet p in odoslane_z_1)
                {
                    if (p.Equals(packet))
                    {
                        odoslane_z_1.Remove(p);
                        Console.WriteLine("Znova som prijal odoslany paket na 1! \n");
                        breake = true;
                        return;
                    }
                }
            }




            if (breake == false)
            {
                // pridavanie zaznamu do MAC tabulky
                //Console.WriteLine("Pridavam data z 1 portu");
                updateTable(1, packet.Ethernet.Source);
                if (statistics)
                {
                    prijate1++;
                    updateStatsIn(1, packet);               // update statistics
                }
                int outPut = getOutPut(packet.Ethernet.Destination);
                //Console.WriteLine(outPut);
                if (outPut == -1)
                {
                    Console.WriteLine("Port 1, cielova MAC sa nenachadza v tabulke #BROADCAST!");
                    // MAC sa nenachadza v tabulke
                    // BROADCAST???
                    sendVia2(packet);
                    return;
                }
                if (outPut == 1)
                {
                    Console.WriteLine("Port 1, cielova MAC adresa je na rovankom porte #ZAHADZUJEM!");
                    // chceme to posielat spat ak dana MAC adresa je ta ktora mi to poslala?
                    return;
                }
                if (outPut == 2)
                {
                    Console.WriteLine("Port 1, odosielam na PORT2  !!!");
                    // posielame dalej 
                    sendVia2(packet);
                    return;
                }
            }
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void PacketHandler2(Packet packet)
        {
            bool breake = false;
            if (packet.Ethernet.Source.Equals(new MacAddress("01:01:01:01:01:01")))
            {

                //Console.Write("########## PRIJAL SOM TESTOVACI PACKET");
                //breake = false;
            }


            if (capturing == false)
            {
                //Console.Write("neprijimam");
                return;
            }
            //Console.WriteLine("Prijal som paket na 2");


            //Console.WriteLine(odoslane_z_2.Count);
            if (loopbackFilter)
            {
                foreach (Packet p in odoslane_z_2)
                {
                    if (p.Equals(packet))
                    {
                        odoslane_z_2.Remove(p);
                        Console.Write("Znova som prijal odoslany paket na 2! \n");
                        breake = true;
                        return;
                    }
                }
            }


            if (breake == false)
            {

                // pridavanie zaznamu do MAC tabulky
                //Console.WriteLine("Updatujem tabulku na 2");
                updateTable(2, packet.Ethernet.Source); //update MAC table
                if (statistics)
                {
                    prijate2++;
                    updateStatsIn(2, packet);               // update statistics
                }


                int outPut = getOutPut(packet.Ethernet.Destination);
                //Console.WriteLine(outPut);
                if (outPut == -1)
                {
                    Console.WriteLine("Port 2, cielova MAC sa nenachadza v tabulke #BROADCAST!");
                    // MAC sa nenachadza v tabulke
                    // BROADCAST???
                    sendVia1(packet);
                    return;
                }
                if (outPut == 1)
                {
                    Console.WriteLine("Port 2, odosielam na PORT1  !!!");
                    // posielam dalej
                    // TODO : APPLY FILTER
                    sendVia1(packet);
                    return;

                }
                if (outPut == 2)
                {
                    Console.WriteLine("Port 2, cielova MAC adresa je na rovankom porte #ZAHADZUJEM!");
                    // posielana spat odkial som prijal????
                    return;
                }
            }

        }

        private void showTable()
        {
            Invoke(new MethodInvoker(delegate () { MacView.Items.Clear(); }));
            foreach (Row i in table.ToList())
            {
                try
                {
                    ListViewItem item = new ListViewItem(i.Port.ToString());
                    item.SubItems.Add(i.Mac.ToString());
                    item.SubItems.Add(i.Time.ToString());
                    Invoke(new MethodInvoker(delegate () { MacView.Items.Add(item); }));
                }
                catch
                {

                }

            }
        }

        private void updateTable(int v, MacAddress source)
        {

            for (int i = 0; i < table.Count(); i++)
            {
                MacAddress pom = table[i].Mac;
                if (pom.Equals(source))
                {
                    if (table[i].Port == v)
                    {
                        table[i].Time = (int)MACTime.Value;
                        return;
                    }
                    table.RemoveAt(i);
                }
            }


            //Console.Write(v);
            //Console.WriteLine(" -   Pridavam zaznam do tabulky!");

            Row n = new Row();
            n.Port = v;
            n.Mac = source;
            n.Time = (int)MACTime.Value;
            table.Add(n);



        }

        private void updateStatsIn(int port, Packet packet)
        {
            if (port == 1)
            {
                if (packet.Ethernet.EtherType == EthernetType.Arp)
                {
                    //ARP
                    one.InArp++;
                    return;
                }
                else if (packet.Ethernet.EtherType == EthernetType.IpV4)
                {
                    //IP
                    one.InIp++;
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Udp)
                    {
                        //UDP
                        one.InUdp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Tcp)
                    {
                        //TCP
                        one.InTcp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                    {
                        //ICMP
                        one.InIcmp++;
                        return;
                    }
                }
                one.InAll++;
            }
            if (port == 2)
            {

                if (packet.Ethernet.EtherType == EthernetType.Arp)
                {
                    //ARP
                    two.InArp++;
                    return;
                }
                else if (packet.Ethernet.EtherType == EthernetType.IpV4)
                {
                    //IP
                    two.InIp++;
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Udp)
                    {
                        //UDP
                        two.InUdp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Tcp)
                    {
                        //TCP
                        two.InTcp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                    {
                        //ICMP
                        two.InIcmp++;
                        return;
                    }

                }
                two.InAll++;
            }
        }
        private void updateStatsOut(int port, Packet packet)
        {
            if (port == 1)
            {
                if (packet.Ethernet.EtherType == EthernetType.Arp)
                {
                    //ARP
                    one.OutArp++;
                    return;
                }
                else if (packet.Ethernet.EtherType == EthernetType.IpV4)
                {
                    //IP
                    one.OutIp++;
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Udp)
                    {
                        //UDP
                        one.OutUdp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Tcp)
                    {
                        //TCP
                        one.OutTcp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                    {
                        //ICMP
                        one.OutIcmp++;
                        return;
                    }

                }
                one.OutAll++;
            }
            if (port == 2)
            {
                if (packet.Ethernet.EtherType == EthernetType.Arp)
                {
                    //ARP
                    two.OutArp++;
                    return;
                }
                else if (packet.Ethernet.EtherType == EthernetType.IpV4)
                {
                    //IP
                    two.OutIp++;
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Udp)
                    {
                        //UDP
                        two.OutUdp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.Tcp)
                    {
                        //TCP
                        two.OutTcp++;
                        return;
                    }
                    if (packet.Ethernet.IpV4.Protocol == IpV4Protocol.InternetControlMessageProtocol)
                    {
                        //ICMP
                        two.OutIcmp++;
                        return;
                    }

                }
                two.OutAll++;
            }

        }

        //metoda pre prijimanie packetov

        private void Receiving1()
        {

            communicator.ReceivePackets(-1, PacketHandler);
        }

        private void Receiving2()
        {
            communicator2.ReceivePackets(-1, PacketHandler2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == "Start")
            {
                capturing = true;
                buttonStart.Text = "Stop";
            }
            else if (buttonStart.Text == "Stop")
            {
                capturing = false;
                buttonStart.Text = "Start";
            }
        }

        private void count_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            table = new List<Row>();
            showTable();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void updateStats()
        {
            Invoke(new MethodInvoker(delegate ()
            {
                label7.Text = prijate1.ToString();
                label8.Text = odoslane1.ToString();
                label9.Text = prijate2.ToString();
                label10.Text = odoslane2.ToString();

                label62.Text = one.InAll.ToString();
                label21.Text = one.InArp.ToString();
                label22.Text = one.InTcp.ToString();
                label23.Text = one.InUdp.ToString();
                label24.Text = one.InIp.ToString();
                label25.Text = one.InIcmp.ToString();

                label63.Text = one.OutAll.ToString();
                label30.Text = one.OutArp.ToString();
                label29.Text = one.OutTcp.ToString();
                label28.Text = one.OutUdp.ToString();
                label27.Text = one.OutIp.ToString();
                label26.Text = one.OutIcmp.ToString();

                label64.Text = two.InAll.ToString();
                label50.Text = two.InArp.ToString();
                label49.Text = two.InTcp.ToString();
                label48.Text = two.InUdp.ToString();
                label47.Text = two.InIp.ToString();
                label46.Text = two.InIcmp.ToString();

                label65.Text = two.OutAll.ToString();
                label40.Text = two.OutArp.ToString();
                label39.Text = two.OutTcp.ToString();
                label38.Text = two.OutUdp.ToString();
                label37.Text = two.OutIp.ToString();
                label36.Text = two.OutIcmp.ToString();
            }));
        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void buttonSend_Click_1(object sender, EventArgs e)
        {
            //Console.Write("Posielam packet z 2\n");
            sendVia2(BuildEthernetPacket());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Console.Write("Posielam packet z 1\n");
            sendVia1(BuildEthernetPacket());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "ON")
            {
                loopbackFilter = true;
                button4.Text = "OFF";
            }
            else if (button4.Text == "OFF")
            {
                loopbackFilter = false;
                button4.Text = "ON";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "ON")
            {
                statistics = true;
                button5.Text = "OFF";
            }
            else if (button5.Text == "OFF")
            {
                statistics = false;
                button5.Text = "ON";
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            one = new Status();
            two = new Status();
            prijate1 = 0;
            prijate2 = 0;
            odoslane1 = 0;
            odoslane2 = 0;
        }

        private void sendVia1(Packet pp)
        {
            if (loopbackFilter)
                odoslane_z_1.Add(pp);

            if (statistics)
            {
                odoslane1++;
                updateStatsOut(1, pp);
            }

            communicator.SendPacket(pp);


        }
        private void sendVia2(Packet pp2)
        {
            if (loopbackFilter)
                odoslane_z_2.Add(pp2);


            if (statistics)
            {
                odoslane2++;
                updateStatsOut(2, pp2);
            }

            communicator2.SendPacket(pp2);

        }

        private void boxPacket_TextChanged(object sender, EventArgs e)
        {

        }

        private void listDevices_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private static Packet BuildEthernetPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("01:01:01:01:01:01"),
                    Destination = new MacAddress("02:02:02:02:02:02"),
                    EtherType = EthernetType.IpV4,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(Encoding.ASCII.GetBytes("hello world")),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }

    }
}
