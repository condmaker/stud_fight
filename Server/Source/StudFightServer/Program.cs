using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace StudFightServer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Message
    {
        // Server info
        public bool isReady;
        public bool isInGame;
        public bool hasWon;

        // Data for the stud and the selected move
        public bool isTurn;
        public byte moveId;
        public byte studId;
        public byte moveDmg;
        public byte studShield;
        public byte studHealth;
        public byte studStamina;

        public MoveEffect effects;
        public byte atkUpVal;
        public byte doubleCostVal;
        public byte extraDmgHealthVal;
        public byte shieldVal;
        public byte stunVal;

        // The following methods were made by Diogo de Andrade.
        public static Message FromBytes(byte[] bytes)
        {
            Message message = new Message();

            int size = Marshal.SizeOf(message);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(bytes, 0, ptr, size);
            message = (Message) Marshal.PtrToStructure(ptr, typeof(Message));
            Marshal.FreeHGlobal(ptr);

            return message;
        }

        public static byte[] ToBytes(Message message)
        {
            int size = Marshal.SizeOf(message);
            byte[] bytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(message, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return bytes;
        }
    }

    [Flags]
    enum MoveEffect
    {
        None           = 0,
        AtkUp          = 1,
        DoubleCost     = 2,
        ExtraDmgHealth = 4,
        Shield         = 8,
        Stun           = 16
    }

    class Program
    {
        private static object playerLock  = new();
        private static uint   playerCheck = 0;

        private static object turnLock = new();
        private static Turn   turn     = Turn.P1;

        private static ManualResetEvent startEvent = new ManualResetEvent(false);

        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;

        static void Main(string[] args)
        {
            Socket listener, firstClient, secondClient;
            Thread? t1 = null, t2 = null;

            ManualResetEvent p1TurnEvent = new ManualResetEvent(false);
            ManualResetEvent p2TurnEvent = new ManualResetEvent(false);

            Message p1Msg = new Message();
            Message p2Msg = new Message();

            bool p1Turn = false;
            bool p2Turn = false;
            bool game   = true;

            Console.WriteLine("Starting server.");

            try
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 1234);

                listener = new Socket(AddressFamily.InterNetwork, 
                                      SocketType.Stream,
                                      ProtocolType.Tcp);

                listener.Bind(localEP);
                listener.Listen(2);

                Console.WriteLine($"Linked to IP Adress. Waiting for 2 players.");

                firstClient = listener.Accept();

                Console.WriteLine($"P1 connected @ remote " 
                    + firstClient.RemoteEndPoint.ToString());

                t1 = new Thread(() => PoolClient(firstClient, 
                                                 Turn.P1,  
                                                 p1TurnEvent,
                                                 ref p1Msg,
                                                 ref p1Turn,
                                                 token));
                t1.Name = "P1Thread";
                t1.Start();

                secondClient = listener.Accept();

                Console.WriteLine($"P2 connected @ remote "
                    + secondClient.RemoteEndPoint.ToString());

                t2 = new Thread(() => PoolClient(secondClient, 
                                                 Turn.P2, 
                                                 p2TurnEvent,
                                                 ref p2Msg,
                                                 ref p2Turn,
                                                 token));
                t2.Name = "P2Thread";
                t2.Start();

                Console.WriteLine($"P1 and P2 connected. " 
                    + "Waiting until both are ready.");

                while (playerCheck != 2)
                {
                    Thread.Sleep(1000);
                }

                Console.WriteLine($"Both players ready. Starting game.");

                // First assignment so that 
                firstClient.Send(Message.ToBytes(p2Msg));
                secondClient.Send(Message.ToBytes(p1Msg));

                startEvent.Set();

                while (game)
                {
                    if (!t1.IsAlive || !t2.IsAlive)
                    {
                        Console.Write("Player disconnection detected. Aborting.");
                        break;
                    }

                    // Wait until both threads have finished their turns
                    while ((!p1Turn || !p2Turn))
                    {
                        Thread.Sleep(1000);
                    }

                    // Death check.
                    // Server is final say on who won (shouldn't matter because
                    // they are not able to die at the same turn, but yeah)
                    if (p1Msg.studHealth <= 0 || p2Msg.studHealth <= 0)
                    {
                        // P1 has priority
                        if (p1Msg.studHealth <= 0)
                            p1Msg.hasWon = true;
                        else if (p2Msg.studHealth <= 0)
                            p2Msg.hasWon = true;

                        game = false;
                    }


                    firstClient.Send(Message.ToBytes(p2Msg));
                    secondClient.Send(Message.ToBytes(p1Msg));

                    Console.WriteLine("Both players moved. Moving to next turn.");

                    turn = (turn == Turn.P1) ? Turn.P2 : Turn.P1;

                    p1Turn = false;
                    p2Turn = false;

                    p1TurnEvent.Set();
                    p2TurnEvent.Set();

                    p1TurnEvent.Reset();
                    p2TurnEvent.Reset();
                }

                if (game)
                {
                    Console.WriteLine("A player disconnected suddenly. Killing server.");
                }
                else
                {
                    if (p1Msg.hasWon)
                        Console.Write("P1");
                    else
                        Console.Write("P2");

                    Console.WriteLine(" has won! Killing server.");
                }

                source.Cancel();
                t1.Join();
                t2.Join();
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Something went wrong. Killing server.");
                if (t1 != null) t1.Join();
                if (t2 != null) t2.Join();
            }
        }

        public static void PoolClient(Socket clientSocket, 
                                      Turn player, 
                                      ManualResetEvent turnEvent,
                                      ref Message message,
                                      ref bool turnTick,
                                      CancellationToken token)
        {
            byte[] buffer     = new byte[35];

            const int rpTimes = 1_000_000;
            int   sleepTime   = 100;
            int   rp          = rpTimes;
            bool  ready       = false;
            bool  isTurn      = false;

            // We need that in order to kill the thread
            clientSocket.Blocking = false;

            // Await first "isAlive" message with selected stud, showing that
            // the player is ready
            Console.WriteLine($"Awaiting stud selection @"
                + clientSocket.RemoteEndPoint.ToString());

            while (!ready && rp > 0)
            {
                try
                {
                    clientSocket.Receive(buffer);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                    {
                        Thread.Sleep(sleepTime);
                        rp--;
                        continue;
                    }

                    Console.WriteLine("Something went wrong when trying to " +
                        "connect with " + clientSocket.RemoteEndPoint.ToString() + ".");
                    return;
                }

                // Convert and interpret message
                message = Message.FromBytes(buffer);
                ready = message.isReady;
            }

            rp = rpTimes;

            // Stud selected 
            Console.WriteLine($"Stud ID " + message.studId.ToString()
                + " selected.");

            // Increment ready players
            lock (playerLock)
            {
                playerCheck++;

                if (playerCheck > 2)
                    playerCheck = 2;
                else
                {
                    Console.WriteLine("Player @"
                        + clientSocket.RemoteEndPoint.ToString() + " ready.");
                }
            }

            // Wait until main thread signals everything is ready
            message.isInGame = true;
            
            // First move always needs to be a dummy
            message.moveId = 15;

            lock (turnLock)
            {
                message.isTurn = (turn == player);
            }

            try
            {
                startEvent.WaitOne(Timeout.Infinite);
            }
            catch 
            {
                Console.WriteLine("Something went wrong when trying to " +
                    "connect with " + clientSocket.RemoteEndPoint.ToString() + ".");
                return;
            }

            // Game loop
            while (!message.hasWon && message.studHealth > 0 
                   && !token.IsCancellationRequested && rp > 0)
            {
                turnTick = false;

                // Check current turn
                lock (turnLock)
                {
                    isTurn = (turn == player);
                }

                if (isTurn)
                {
                    try
                    {
                        clientSocket.Receive(buffer);
                        Console.WriteLine("Received move from " +
                            clientSocket.RemoteEndPoint.ToString());

                        message = Message.FromBytes(buffer);
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.WouldBlock)
                        {
                            Thread.Sleep(sleepTime);
                            rp--;
                            continue;
                        }

                        Console.WriteLine("No response from " +
                            clientSocket.RemoteEndPoint.ToString() + ".");

                        source.Cancel();
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("@" +
                        clientSocket.RemoteEndPoint.ToString() 
                        + " is waiting for opponent.");
                }

                message.isInGame = true;

                // Inverse for next turn
                message.isTurn = !isTurn;

                // End turn-- wait until main thread tells us that we can continue
                turnTick = true;
                try
                {
                    turnEvent.WaitOne(Timeout.Infinite);
                }
                catch
                {
                    Console.WriteLine("@" +
                    clientSocket.RemoteEndPoint.ToString()
                    + " interruped. Killing thread.");
                }

                rp = rpTimes;
            }

            if (rp <= 0)
            {
                Console.WriteLine("@" +
                clientSocket.RemoteEndPoint.ToString()
                + " timed out. Killing thread.");
            }

            message.isInGame = false;
        }
    }

    public enum Turn
    {
        P1,
        P2,
        NONE
    }
}
