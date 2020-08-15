using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Newtonsoft.Json;
//using System.Text.json;

namespace server2
{

    class Room
    {
        public string ownerName { set; get; }

        string ownerIP;
        int roomID;
        int busyFlag;
        string clientsNames; //array of two strings(names) of the two players in the room

        public static int roomsCount; //3shan myrg3sh el count b zero tany m3 kol run... y updaate .
        public string player2IP;
        public string[] watchingIPs;

        public Room()      
        {
            roomsCount++; 
        }
        public string ClientsNames
        {
            set
            {
                clientsNames = value;   //while creating the room
            }
            get
            {
                return clientsNames; //when it is created .
            }
        }

        public string OwnerIP
        {
            set
            {
                ownerIP = value;  //ip of the room's owner
            }
            get
            {
                return ownerIP;    
            }
        }
        public int RoomID
        {
            set
            {
                roomID = value;
            }
            get
            {
                return roomID;
            }
        }
        public int BusyFlag
        {
            set
            {
                busyFlag = value;  //2 players in the room (full)
            }
            get
            {
                return busyFlag; 
            }
        }
    }

    class Server
    {
        int numClients = 0;
        List<TcpClient> listConnectedClients = new List<TcpClient>(); 

        List<Room> roomsList = new List<Room>();
        string JSONString;        //similar l xml (store data as string datatype and send it after that and convert it to the required data type as u want)
        public List<TcpClient> ListConnectedClients
        {
            get
            {
                return listConnectedClients;
            }
        }

        TcpListener server = null;  //server still not created(declaration)

        ProcessThreadCollection currentThreads;       //int numThreads; //equals number of rooms

        NetworkStream stream; //Is it only one stream for all clients back and forth
        public NetworkStream Stream
        {
            get
            {
                return stream; //????????????????????ask
            }
        }

        int rooms;
        public int Rooms
        {
            get
            {
                return rooms;
            }
        }
        public Server() { }   //default constructor
        public Server(string ip, int port)    //
        {
            rooms = 0;   //count

            IPAddress localAddr = IPAddress.Parse(ip);        
            server = new TcpListener(localAddr, port);
            server.Start();
      
            StartListener();
        }
        public void StartListener()
        {
            try
            {
                while (true) 
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient(); // server listens for clients

                    listConnectedClients.Add(client);
                    numClients = listConnectedClients.Count;
                    Console.WriteLine("Number of clients: {0}", numClients);

                    Console.WriteLine("Connected!");

                    // new thread for each client
                    Thread t = new Thread(() =>
                    {
                        stream = client.GetStream(); //

                        string clientFlag = null;    // 
                        Byte[] data = new Byte[256]; // array of bytes ?(size of array)
                        Int32 bytes;          // ?  
                        try
                        {
                            while ((bytes = stream.Read(data, 0, data.Length)) != 0) // server reading as long as the client is sendng to the server
                            {   //
                                //bytes = stream.Read(data, 0, data.Length);

                                /* 
                                 * According to the "clientFlag" (message from the cleint), 
                                 * the server takes action
                                 * 0: client connects to the server & the server sends all the created rooms
                                 * 1: client is creating new room 
                                 * 2: client closing so remove his room if he was alone
                                 * 3: player2 sends "3" to the main server when connecting to player1
                                 *    so the room has 2 players (busy flag = 1)
                                */
                                clientFlag = Encoding.ASCII.GetString(data, 0, bytes);//(array,st.index,ending)


                                Console.WriteLine("Client flag: {0}", clientFlag);

                                if (clientFlag.Split(':')[0] == "1") // client is creating new room
                                {
                                    Room r = new Room();

                                    // add IP of player1 to the room (object)
                                    r.OwnerIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                                    //remotendpoint==gets ip address of the client

                                    // Player's user name (player 1)
                                    r.ownerName = clientFlag.Split(':')[1];
                                    //Console.WriteLine("hereee: "+r.ownerName+":"+r.OwnerIP);

                                    // room is given a unique ID (the thread id to gurantee uniqueness)
                                    r.RoomID = Thread.CurrentThread.ManagedThreadId;

                                    // add the new room to the list
                                    roomsList.Add(r);

                                    // convert object to json to send to the client
                                    JSONString = JsonConvert.SerializeObject(r, Formatting.Indented);//takes obj r and converts to jason string

                                    Byte[] JSONString_byte = System.Text.Encoding.ASCII.GetBytes(JSONString); //convert string to bytes as stream takes only bytes

                                    stream.Write(JSONString_byte, 0, JSONString_byte.Length);
                                }
                                else if (clientFlag == "0") // client connects to the server & the server sends all the created rooms
                                {
                                    // convert roomsList to JSON and send it to the client
                                    JSONString = JsonConvert.SerializeObject(roomsList);
                                    Console.WriteLine("one room: " + roomsList.Count.ToString());
                                    Console.WriteLine(JSONString);
                                    Byte[] JSONString_byte = System.Text.Encoding.ASCII.GetBytes(JSONString);
                                    stream.Write(JSONString_byte, 0, JSONString_byte.Length);
                                }
                                else if (clientFlag == "2") //client closing so remove his room if he was alone
                                {
                                    Console.WriteLine("client message: 2");

                                    foreach (Room r in roomsList)
                                    {
                                        if (r.OwnerIP == ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString())
                                        { //check between 2 ips the owner and if other one is trying to close the app , if they are identical..

                                            //Console.WriteLine("The removed room: " + r);

                                            roomsList.Remove(r);
                                            listConnectedClients.Remove(client);
                                            Room.roomsCount--;
                                            Console.WriteLine("Room with id: {} is removed", r.RoomID);
                                        }
                                    }
                                }
                                else if (clientFlag == "3") //player2 sends "3" to the main server when connecting to player1 (2 players inside the room)
                                {
                                    /* 
                                     * received message is in the form of:
                                     * 
                                     * */
                                    string player2ID = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                                    //remote end point  (ip of the other player)
                                    bytes = stream.Read(data, 0, data.Length);//(mn elbdaya lel nehaya)
                                    string roomInfo = Encoding.ASCII.GetString(data, 0, bytes);//owner's ip(get his room)
                                    foreach (Room r in roomsList)
                                    {
                                        if (r.OwnerIP == roomInfo.Split(':')[1])
                                        {
                                            r.player2IP = player2ID;
                                            r.BusyFlag = 1;  //room is busy(2 players )
                                            Console.WriteLine("Busy Room: ", r.OwnerIP + ":" + r.player2IP + ":" + r.RoomID);
                                        }
                                    }
                                }

                                /*foreach (Room rr in roomsList)
                                    {
                                        Console.WriteLine("OwnerIP: {0} of room/threadID: {1}", rr.OwnerIP, rr.RoomID);
                                    }*/
                                Console.WriteLine("Number of rooms: {0}", Room.roomsCount);
                                Console.WriteLine(JsonConvert.SerializeObject(roomsList));//(list to string )

                            }
                            //}
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("Exception: {0}", e.ToString());
                            Console.WriteLine("Client left");
                            client.Close();
                        }
                    });//end thread
                    t.Start();
                }
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                //Console.WriteLine("Client left");
                server.Stop();
            }
        }
        
    }
    class Program
    {
        static void Main(string[] args)
        {
            Server myserver;
            Thread t = new Thread(delegate () //(fnction el bynfzha el tread)
            {
                // Server IP Address and port...
                //myserver = new Server("172.16.4.45", 13000);
                myserver = new Server("192.168.1.10", 13000);
            });
            t.Start();

            Console.WriteLine("Server Started...!"); //in the main thread

        }
    }
}
