namespace AradSMPP.Net;

/// <summary> IPacket Interface </summary>
public interface IPacket
{
    /// <summary> Interface to support processing PDU's </summary>
    byte[] GetPdu();
}