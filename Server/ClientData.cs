using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using ServerData;
using System.Net;


namespace Server
{
    class ClientData
    {
        public static string Version = "2.0";
        public Thread clientThread;
        public string id;
        public Socket clientSocket;
        
        public ClientData()
        {
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(Server.Data_IN);
            clientThread.Start(clientSocket);
            sendConnectPacketToClient();
        }
        public ClientData(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            id = Guid.NewGuid().ToString();

            clientThread = new Thread(Server.Data_IN);
            clientThread.Start(clientSocket);
            sendConnectPacketToClient();
        }


        /// <summary>
        /// Send packet with client id to new client registreted client
        /// </summary>
        public void sendConnectPacketToClient()
        {
            Packet p = new Packet(PacketType.Connect, "server");
            IPEndPoint IP = clientSocket.RemoteEndPoint as IPEndPoint;

            if (Server.Blacklist.Exists(Server.BySName(IP.Address + "")))
            {
                p = new Packet(PacketType.Banned, "server");
                clientSocket.Send(p.ToBytes());
                return;
            }
            
            p.data.Add(id);
            p.data.Add(Version);
            clientSocket.Send(p.ToBytes());
        }
    }
}
