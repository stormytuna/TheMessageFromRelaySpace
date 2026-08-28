namespace TRFDS.DataStructures;

public struct RelayMessage
{
	public short Sender;
	public short TransmissionId;
	public SignalMessage Signals;
}
