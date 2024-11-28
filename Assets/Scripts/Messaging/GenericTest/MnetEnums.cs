
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
// Normal means regular update and that everything seems fine on the client side
// FullWorldUpdate means the packets contain the full world snapshot so that the client might process if differently if necessary
// FailureMissingPackets is used by the client to notify the handler when there are missing packets
// FailureDelay can be used to notify that there was a detectable delay and that full snapshot might be required for a sync or a rollback
// Disconnect with the proper disconnect message string can be used to notify the receiver that the connection will shut down
public enum MessageType : Byte
{
    Normal = 1, FullWorldUpdate,  FailureMissingPackets, FailureDelay, Disconnect
}