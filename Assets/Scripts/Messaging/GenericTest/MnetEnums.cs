
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
public enum ConnectionState
{
    NotActive, ContactServer, Handshake, SyncWorldState, Connected, MissingPackets, Reconcile, Disconnected
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
    Regular = 1, Snapshot,  RequestMissedPacket, IncomingMissedPacket, Disconnect,
}