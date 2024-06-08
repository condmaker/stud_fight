# STUD FIGHT
A simple online turn-based game with a simple server connecting to clients 
via TCP.

![stud-fight-selection]

## Technical Implementation

Stud Fight works with **2 clients** connecting to a **server** via **TCP**. 
This server will then manage each client by opening two threads for each
of them, and after each **client** has reacted / made a move, the main thread
then observes each of their messages and reacts accordingly (be it moving to
the next turn, ending the game since a player has died, etc...). Each client
will then be notified of this state.

The server was made with sockets that run with the **TCP protocol**-- so we can keep
track and send information reliably to both clients. As refereed above each
thread also runs on their separate sockets to improve reactions to client
messaging.

The clients, then, attempt to connect to this server, and after first connection 
they send their moves and player status (HP, ST, etc...) to the server via
messages. If it's not their turn, however, they will not update the server,
seeing that there will be no change to the player itself as he can't move.

Both server and client use most socket calls with **blocking turned off**,
since timeouts need to be checked.

The game itself, which is the client, was made on **Unity** with basic
functionality from the engine itself, with the player and turn logic coded via
scripts that attach themselves to the UI. If the game suddenly looses connection
to the server, or times out, a pop-up will appear in-game prompting the player
to leave.

## Network messages & bandwidth

One of the goals in this project was to keep bandwidth low-- with a message
that consumes the lowest space possible. Here is the final result:

![message]

This message is, in it's entirety, **35 bytes**. An effort was made to keep
game numbers (health, stamina, etc...) low so that they could be converted to 
bytes without issues. Here's what each of them mean:

- `isReady`
    - A simple bool useful for the server. It tells that the client has choosen
a player and is ready.
- `isInGame`
    - Another bool, normally triggered by the server whenever the player wins /
loses or something goes wrong-- the main thread checks this to see if it should
end the game or not.
- `hasWon`
    - Self explanatory. Sent by the client, this tells if said client won the
game on their side.
- `isTurn`
    - The server signifies to the client if it's their turn.
- `moveId`
    - Identifier of the move used. The ID is linked to a move on the client's 
code.
- `moveDmg`
    - Damage of the move. This needs to be here because the move's damage is
dependant not only on the move, but also on stats and effects.
- `studShield`
    - Current shield value of the client.  
- `studHealth`
    - Current health value of the client.  
- `studStamina`
    - Current stamina value of the client. 

For the rest of the values, a little bit of background is needed.

A specific problem that appeared was related with the **effects**, which
on the client are represented by a dictionary of ``MoveEffect`` and ``int``-- 
meaning that each effect could have a modifier. To move this to the message
raw would bloat the file size, so the `Flag` enum operator was employed here.

This made so that the `effects` section showed all the possible effects that
the player had without putting it on a big collection-- though the specific
values still had to be stored on separate bytes, that are ignored if said
effect is not active.

This lightweight message makes the server/client communication process very
fast and cheap, even though it's arguably not needed on a turn-based game,
which does not need to be fast.

## Network Diagram

### Server-Client

![network-arch-full]

### Server

![network-arch]

### Client

![network-arch-player]

## Bibliography

Professor Diogo de Andrade's Slides

C# Socket docs: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services

C# Thread docs: https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread?view=net-8.0

C# CancellationToken docs: https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-8.0 

[network-arch-full]: /Images/networkfulldiag.png
[network-arch-player]: /Images/networkarchplayer.png
[network-arch]: /Images/networkdiag.png
[stud-fight-selection]: /Images/StudFight_gcMUlXeMfe.jpg
[message]: /Images/devenv_MDUypilvlt.jpg