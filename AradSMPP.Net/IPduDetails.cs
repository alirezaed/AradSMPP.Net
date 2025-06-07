namespace AradSMPP.Net;

/// <summary> IPduDetails Interface </summary>
public interface IPduDetails
{
    /// <summary> Returns details about the PDU </summary>
    /// <returns> List PduPropertyDetail </returns>
    List<PduPropertyDetail> Details();
}