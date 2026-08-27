namespace TMFRS.DataStructures;

public struct RelayMessage
{
	public short Sender;
	public short TransmissionId;
	public SignalMessage Signals;
}
