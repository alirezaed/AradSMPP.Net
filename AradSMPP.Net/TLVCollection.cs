#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Collection of optional parametersin TLV format </summary>
public class TlvCollection : List<Tlv>
{
    #region Operator Properties

    /// <summary> Access to the collection by tag </summary>
    /// <param name="tag"></param>
    /// <returns> A TLV object </returns>
    public Tlv this[ushort tag] { get { return this.FirstOrDefault(k => k.Tag == tag); } }

    /// <summary> Access to the collection by an optional tag </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public Tlv this[OptionalTags tag] => this[Convert.ToUInt16(tag)];
    #endregion
        
    #region Public Methods

    /// <summary> Adds the SAR reference number </summary>
    /// <param name="val"></param>
    public void AddSarReferenceNumber(ushort val)
    {
        SmppBuffer tmpBuff = new(DataCodings.Default);
        tmpBuff.AddShort(val);

        this.Add(Tlv.Create(OptionalTags.SarMsgRefNum, Convert.ToUInt16(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds the SAR sequence number </summary>
    /// <param name="val"></param>
    public void AddSarSequenceNumber(byte val)
    {
        SmppBuffer tmpBuff = new(DataCodings.Default);
        tmpBuff.AddByte(val);

        Add(Tlv.Create(OptionalTags.SarSegmentSeqnum, Convert.ToUInt16(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds the SAR total segments </summary>
    /// <param name="val"></param>
    public void AddSarTotalSegments(byte val)
    {
        SmppBuffer tmpBuff = new(DataCodings.Default);
        tmpBuff.AddByte(val);

        Add(Tlv.Create(OptionalTags.SarTotalSegments, Convert.ToUInt16(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds the more messages to send </summary>
    /// <param name="val"></param>
    public void AddMoreMessagesToSend(bool val)
    {
        byte b = 0;
        if (val)
        {
            b = 1;
        }

        SmppBuffer tmpBuff = new(DataCodings.Default);
        tmpBuff.AddByte(b);

        Add(Tlv.Create(OptionalTags.MoreMessagesToSend, Convert.ToUInt16(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds the more message pay load </summary>
    /// <param name="data"></param>
    public void AddMessagePayload(byte[] data)
    {
        SmppBuffer tmpBuff = new(DataCodings.Default);
        tmpBuff.AddBytes(data);

        Add(Tlv.Create(OptionalTags.MessagePayload, Convert.ToUInt16(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds the specified optional tag value </summary>
    /// <param name="tag"></param>
    /// <param name="val"></param>
    public void Add(OptionalTags tag, byte[] val)
    {
        Add(Tlv.Create(tag, Convert.ToUInt16(val.Length), val));
    }

    #endregion
}