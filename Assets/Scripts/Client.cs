using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjs/Client")]
public class Client : ScriptableObjectWithCoroutines
{
    // Server (localhost) will store some info like current turn based on
    // connection order for the correct distribution and gathering of messages.
    // This also needs to be on the server as-is.
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
            message = (Message)Marshal.PtrToStructure(ptr, typeof(Message));
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

    [SerializeField]
    public string serverIpAddress = "localhost";

    private Socket clientSocket;
    private WaitForSeconds seconds = new WaitForSeconds(.01f);

    // So for some godforsaken reason this persists through process death
    // and ignores the default value (maybe it doesn't happen on build tho)
    public Connectivity CurrentStatus
    {
        get => currentStatus;
    }
    private Connectivity currentStatus = Connectivity.DISCONNECTED;

    public GameState CurrentTurn
    {
        get => currentTurn;
    }
    private GameState currentTurn = GameState.INTERRUPTED;

    public Socket ClientSocket
    {
        get => clientSocket;
    }

    public Stud PlayerStud
    {
        get => playerStud;
    }
    private Stud playerStud;

    public Stud OpponentStud
    {
        get => opponentStud;
    }
    private Stud opponentStud;

    public bool Connect()
    {
        connectivityEvent -= UpdateCurrentConnectivity;
        gameEvent -= UpdateCurrentTurn;

        connectivityEvent += UpdateCurrentConnectivity;
        gameEvent += UpdateCurrentTurn;

        IPHostEntry ipHost    = Dns.GetHostEntry(serverIpAddress);
        IPAddress   ipAddress = null;

        for (int i = 0; i < ipHost.AddressList.Length; i++)
        {
            if (ipHost.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = ipHost.AddressList[i];
            }
        }

        IPEndPoint remoteEp = new IPEndPoint(ipAddress, 1234);

        clientSocket
            = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        clientSocket.Blocking = false;

        StartCoroutine(RunSocket(remoteEp));

        return true;
    }

    private IEnumerator RunSocket(IPEndPoint remoteEp)
    {
        byte retries = 200;

        connectivityEvent.Invoke(Connectivity.CONNECTING);

        while (retries > 0)
        {
            yield return seconds;

            try
            {
                clientSocket.Connect(remoteEp);
                Debug.Log("Connected to server @" + serverIpAddress);
                retries = 0;
                break;
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.WouldBlock:
                    case SocketError.AlreadyInProgress:
                        retries--;
                        if (retries == 0)
                        {
                            Debug.Log("Timed out. Server does not respond.");
                            connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                            yield break;
                        }
                        break;
                    case SocketError.IsConnected:
                        Debug.Log("Connected to server @" + serverIpAddress);
                        retries = 0;
                        break;
                    case SocketError.ConnectionRefused:
                    case SocketError.NotConnected:
                        Debug.LogError("Connection Refused.");
                        connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                        yield break;
                    default:
                        Debug.LogError("Unknown connection error: " + e.SocketErrorCode);
                        connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                        yield break;
                }
            }
        }

        clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        connectivityEvent.Invoke(Connectivity.CONNECTED);

        Debug.Log("Size of msg: " + Marshal.SizeOf(typeof(Message)));
    }

    private IEnumerator CollectMessage()
    {
        // Long timeout
        uint    retries = 2_000_000;
        byte[]  buffer  = new byte[35];
        Message message = new Message();

        while (retries > 0)
        {
            yield return seconds;

            try
            {
                clientSocket.Receive(buffer);

                message = Message.FromBytes(buffer);

                Debug.Log("Received message from server, ISTURN: " + (!message.isTurn).ToString());

                if (message.isInGame)
                {
                    // We need to know the opponent stud's status and the move
                    // he used. Other calculations
                    Stud stud = Stud.GetStudByType((StudType) message.studId);

                    stud.health  = message.studHealth;
                    stud.shield  = message.studShield;
                    stud.stamina = message.studStamina;

                    var fetchedEffects = new Dictionary<MoveEffect, int>();

                    foreach (MoveEffect effect in Enum.GetValues(typeof(MoveEffect)))
                    {
                        int currentVal = 0;

                        switch (effect)
                        {
                            case MoveEffect.AtkUp:
                                currentVal = message.atkUpVal;
                                break;
                            case MoveEffect.DoubleCost:
                                currentVal = message.doubleCostVal;
                                break;
                            case MoveEffect.ExtraDmgHealth:
                                currentVal = message.extraDmgHealthVal;
                                break;
                            case MoveEffect.Stun:
                                currentVal = message.stunVal;
                                break;
                            case MoveEffect.Shield:
                                currentVal = message.shieldVal;
                                break;
                        }

                        if (message.effects.HasFlag(effect))
                            fetchedEffects.Add(effect, currentVal);
                    }

                    stud.effects = fetchedEffects;

                    if (opponentStud == null || !message.isTurn)
                        opponentStud = stud;
                }
                // This means our thread @ server died somehow. Kill everything.
                else
                {
                    Debug.Log("Disconnected from server.");
                    connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                }

                // Update turn (negative because we're receiving the enemy's)
                if (!message.isTurn)
                {
                    // Get the opponent's move in case it's our turn again
                    StudMove receivingMove = StudMove.GetMoveById(message.moveId);
                    receivingMove.finalDmg = message.moveDmg;

                    if (message.hasWon)
                        clientEvent.Invoke(ClientState.WIN);
                    else
                        gameEvent.Invoke(GameState.ISTURN, receivingMove);
                }
                else
                {
                    if (message.hasWon)
                        clientEvent.Invoke(ClientState.DEAD);
                    else
                        gameEvent.Invoke(GameState.NOTTURN, StudMove.Skip);

                    EndTurn();
                }

                retries = 0;
                yield break;
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.WouldBlock:
                    case SocketError.AlreadyInProgress:
                        retries--;
                        if (retries == 0)
                        {
                            Debug.Log("Timed out. Server does not respond.");
                            connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                            yield break;
                        }
                        break;
                    default:
                        Debug.LogError("Unknown connection error: " + e.SocketErrorCode);
                        connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                        yield break;
                }
            }
        }

        // Kill everything
    }

    public void SendReady(Stud stud)
    {
        Message startingMessage = new Message();

        startingMessage.isReady     = true;
        startingMessage.studId      = (byte) stud.type;
        startingMessage.studHealth  = (byte) stud.health;
        startingMessage.studStamina = (byte) stud.stamina;

        try 
        {
            clientSocket.Send(Message.ToBytes(startingMessage));
        }
        catch (SocketException e)
        {
            switch (e.SocketErrorCode)
            {
                case SocketError.WouldBlock:
                case SocketError.AlreadyInProgress:
                default:
                    Debug.LogError("Unknown connection error: " + e.SocketErrorCode);
                    clientEvent.Invoke(ClientState.OFF);
                    connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                    return;
            }
        }

        playerStud = stud;

        clientEvent.Invoke(ClientState.READY);
        EndTurn();
    }

    public void SendMove(StudMove move)
    {
        Message testMessage = new Message();
        byte[]  buffer      = new byte[28];

        Debug.Log("Size of msg: " + Marshal.SizeOf(typeof(Message)));

        testMessage.isReady = true;

        // Inverse because it's the next turn that we'll be sending
        testMessage.isTurn = !(CurrentTurn == GameState.ISTURN);

        testMessage.moveId      = (byte) move.id;
        testMessage.studId      = (byte) playerStud.type;

        testMessage.moveDmg     = (byte) move.finalDmg;
        testMessage.studHealth  = (byte) playerStud.health;
        testMessage.studStamina = (byte) playerStud.stamina;
        testMessage.studShield  = (byte) playerStud.shield;

        // Convert the list to one single flagged enum
        MoveEffect flaggedMoves = 0;

        foreach (KeyValuePair<MoveEffect, int> flag in playerStud.effects)
        {
            flaggedMoves |= flag.Key;

            switch (flag.Key)
            {
                case MoveEffect.AtkUp:
                    testMessage.atkUpVal = (byte) flag.Value;
                    break;
                case MoveEffect.DoubleCost:
                    testMessage.doubleCostVal = (byte) flag.Value;
                    break;
                case MoveEffect.ExtraDmgHealth:
                    testMessage.extraDmgHealthVal = (byte) flag.Value;
                    break;
                case MoveEffect.Stun:
                    testMessage.stunVal = (byte) flag.Value;
                    break;
                case MoveEffect.Shield:
                    testMessage.shieldVal = (byte) flag.Value;
                    break;
            }
        }

        testMessage.effects = flaggedMoves;

        buffer = Message.ToBytes(testMessage);

        try
        {
            clientSocket.Send(buffer);

            OpponentStud.StartTurn(move, false);

            sendMove?.Invoke(currentTurn, move);
        }
        catch (SocketException e)
        {
            switch (e.SocketErrorCode)
            {
                case SocketError.WouldBlock:
                case SocketError.AlreadyInProgress:
                default:
                    Debug.LogError("Unknown connection error: " + e.SocketErrorCode);
                    clientEvent.Invoke(ClientState.OFF);
                    connectivityEvent.Invoke(Connectivity.DISCONNECTED);
                    return;
            }
        }
    }

    public void EndTurn()
    {
        endTurn?.Invoke();
        StartCoroutine(CollectMessage());
    }

    private bool UpdateCurrentConnectivity(Connectivity con)
    {
        currentStatus = con;
        return true;
    }

    private bool UpdateCurrentTurn(GameState turn, StudMove move)
    {
        currentTurn = turn;

        if (turn == GameState.ISTURN)
            PlayerStud.StartTurn(move, true);

        return true;
    }

    public Func<GameState, StudMove, bool> gameEvent, sendMove;
    public Func<Connectivity, bool>        connectivityEvent;
    public Func<ClientState, bool>         clientEvent;
    public Func<bool>                      connectEvent;
    public Action                          endTurn;
}

public enum GameState
{
    ISTURN,
    NOTTURN,
    INTERRUPTED
}
public enum ClientState
{
    OFF,
    READY,
    DEAD,
    WIN
}
public enum Connectivity
{
    CONNECTED,
    CONNECTING,
    DISCONNECTED
}

// https://www.reddit.com/r/Unity3D/comments/j2kt0i/i_created_a_scriptableobject_class_that_can_use/
public abstract class ScriptableObjectWithCoroutines : ScriptableObject
{
    private CoroutineSurrogate ___routiner;
    protected CoroutineSurrogate Routiner => ___routiner != null ? ___routiner : ___routiner = GetCoroutineSurrogate();

    protected Coroutine StartCoroutine(IEnumerator routine)
    {
        return Routiner.StartCoroutine(routine);
    }

    protected void StopCoroutine(Coroutine routine)
    {
        if (routine == null)
        {
            return;
        }

        Routiner.StopCoroutine(routine);
    }

    private CoroutineSurrogate GetCoroutineSurrogate()
    {
        CoroutineSurrogate routiner = new GameObject(nameof(CoroutineSurrogate))
            .AddComponent<CoroutineSurrogate>();
        DontDestroyOnLoad(routiner);
        return routiner;
    }
}

public class CoroutineSurrogate : MonoBehaviour
{

}