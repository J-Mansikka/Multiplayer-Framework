
using System;

/*
[Flags]
public enum PacketFlag
{
    ContainsWorldSnapshot = 128
}
*/

// NotActive means no connection is established
// ContactServer is the first step for the client and is used to get handler information for the handshake
// Handshake is used both the client and their handler to verify connection before starting
// SyncWorldState means that the handler is about to send a full world snapshot for the client. Done once at start and later if necessary
// Connected means both parties should be in sync and are in regular communication and transmitting regular update packages
// MissingPackets is used when client has notified the handler that it's missing packets and needs them to be resend
// Disconnected state means that the handler will be deleted by the server and that there is no longer communication between client and handler
public enum WANHAConnectionState
{
    NotActive, ContactServer, Handshake, SyncWorldState, Connected, MissingPackets, Reconcile, Disconnected
}

public enum Ownership
{
    Auto, // Will be set automatically when filling synced object arrays
    Remote, // The object acts as a pawn that will read and replicate changes from incoming messages and cannot be affected locally
    Local, // The object only accepts local changes and sends them in outgoing messages to the remote pawn versions
    SharedRemoteAuth, // The object is controlled locally, but any incoming messages can override its state (e.g. clientside player object)
    SharedLocalAuth, //  The object is controlled by remote connection, but can be overriden locally (e.g. serverside player object)
    SharedEqually // A recipe for chaos. Should never be used. Even simple things like chat needs someone in charge to perform filtering for profanity and for removing spam messages
}

public enum ObjectInstanceAction: byte
{
    Despawned, Spawned
}

public enum ConnectionState
{
    // CLIENT: The networking system is not active and there is no connection
    // SERVER NCS: If the server is active, it will have a New Connection Socket (NCS) listening for new connection requests
    NotConnected = 0,

    // BOTH: The connection is shutting down. This can happen by choice, by request or from a time out.
    Disconnecting,

    // CLIENT: Tries to contact the new connection socket with information needed to create a new connection (address and port at minimum)
    // SERVER NCS: Responds by creating a new connection object, activating its socket with received information and sending back the needed parameters
    HandshakeRequest,
    // CLIENT: Receives the information on the created connection and updates the local socket to connect to it. Client will then send a verify message
    // SERVER: Responds through the connection and sends in the latest snapshot of the world and the future packets
    Connecting, 


    // CLIENT: Tries to receive the snapshot update and to fill the read buffer with enough ticks to last for a short time (recommend about 100ms-200ms delay)
    //         After receiving all the necessary ticks, client will send a READY message to the server and starts sending its own regular packets
    // SERVER: Sends new regular packets to the client while keeping the player object in stasis, until the READY message is received
    Initializing,

    // BOTH: If there is a long enough pause or a missing packet did not arrive in time, the read buffer will be consumed leaving no new information to use.
    //       In this case the game will either stop or extrapolate, until the connection can fix itself with a new snapshot and a refilled read buffer
    Desynced,


    // BOTH: A new snapshot arrived and the incorrect state of the game will interpolate to a correct one slowly enough to refill the read buffer.
    //       When a snapshot and a new read buffer have been received, the connection should return to connected state
    Resyncing,

    // CLIENT: The connection is established and the player receives regular delta packets from the server to keep the game states in sync
    // SERVER: The connection to the player is active and the server receives smaller packets from the player that also contain backups of older ones
    Connected,

    //!!!!!!! YHDEST ENUMIST SAA MOLEMMAT ARVOT JOS VAAN KÄYTTÄÄ NIINQ BITFLAGGIA
    // ELI BITSHIFTAA 8 >> ELI JÄTETÄÄ STEPIT POIS
    // JA STEPIT SAA KU CASTAA BYTEKS

    /*
     Miltä näyttäis update loop tällä?

    if(state >= 400)
        Reg
    if(state >= 300)
        Snap eli yritetään saada kokonainen snap ja sitten interpoloidaa. Snapin ja interpoloinnin aikana kerätään paketteja kai
    if(state >= 200)
        Yritetään luoda uusi yhteys tai reconnectata
    if(state >= 100)
        Sammutettaan tai pistetään yhteys pauselle
    Else
        Yhteys poikki tai ei ole vielä luotu


    !!!!!!!!!!!!!! IHA HAUSKA IDEA JOS ON CLIENT MUT SERVERILLÄ ON MONTA YHTEYTTÄ !!!!!!!!!!!!!!!!!!!!!!!!
    Eli nää voi yksinkertaistaa tai jättää käyttämättä boo
     
     */

    /*
     VAIHEET
    NotConnected = Ei olla yhdistety eikä yritetä yhdistää
    HandshakeAttempt = Type == new ja annetaan mukana IP ja Portti ja jotain
    HandshakeAccepted = Serveri saatiin kiinni ja lähettää takasin info mikä tarvitaan
     
     
     
     */
        

/*
    NotConnected = 0,
        Reconnect,
    SnapshotSynchronization = 10,
        Initialization,
        Syncing,
    Connected = 10,
    Disconnecting = 100,
        RemoteDisconnect, 
        LocalDisconnect, 
        TimedOut,  
        Dropout,  
*/

}

// First byte on each packet. Notifies the recipient of the type of the message
// ServerRegular: 
// Snapshot: Snapshot is a server packet that contains all the data of the world. Used once when joining and if there is a short disconnect or desync
// ServerMissedPacket: Server noticed a missing packet and needs the client to resend it. Should be rare since client packets already contain old ones
//
public enum MessageType : byte
{
    // KOSKA YHTEINEN MESSAGER POHJA JA PAKETTEJA EI PALAUTAETA VOIDAA KÄYTTÄÄ SAMAA NUMEROA MOLEMPIIN SUUNTII
    // ESIM. MISSEDPACKET VOI OLLA YHTEINEN JA REGULAR JA DISCONNECT JA EHKÄ JOKU TIMEOUT VIESTI
    Heartbeat, Regular, SnapshotRequest, SnapshotIncoming,  MissingPacketsRequest, MissingPacketsIncoming, DisconnectNotification,
}

public enum DisconnectCause
{
    RequestedByRemote, InitiatedByLocal, Timeout, UnstableConnection
}

public enum PacketPriority
{
    Regular = 1, Safe = 2, Important = 3
}
