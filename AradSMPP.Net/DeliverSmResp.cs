#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Represents the response to the deliver_sm PDU </summary>
public class DeliverSmResp : Header, IPacket, IPduDetails
{
    #region Public Properties

    /// <summary> In DeliverSM this is never used and needs to be NULL </summary>
    public string? MessageId { get; set; }

    /// <summary> Optional Parameters </summary>
    public TlvCollection Optional { get; set; }

    #endregion

    #region Constructor

    /// <summary> Constructor </summary>
    /// <param name="defaultEncoding"></param>
    private DeliverSmResp(DataCodings defaultEncoding) : base(defaultEncoding, CommandSet.DeliverSmResp)
    {
        Optional = [];
    }

    /// <summary> Constructor </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="commandStatus"></param>
    /// <param name="sequence"></param>
    private DeliverSmResp(DataCodings defaultEncoding, CommandStatus commandStatus, uint sequence) : base(defaultEncoding, CommandSet.DeliverSmResp, commandStatus, sequence)
    {
        Optional = [];
    }

    #endregion
        
    #region Factory Methods

    /// <summary> Called to create a DeliverSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <returns> DeliverSmResp </returns>
    public static DeliverSmResp Create(DataCodings defaultEncoding)
    {
        return new(defaultEncoding);
    }

    /// <summary> Called to create a DeliverSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="commandStatus"></param>
    /// <returns> DeliverSmResp </returns>
    internal static DeliverSmResp? Create(DataCodings defaultEncoding, CommandStatus commandStatus)
    {
        return new(defaultEncoding, commandStatus, 0);
    }

    /// <summary> Called to create a DeliverSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="commandStatus"></param>
    /// <param name="sequence"></param>
    /// <returns> DeliverSmResp </returns>
    public static DeliverSmResp? Create(DataCodings defaultEncoding, CommandStatus commandStatus, uint sequence)
    {
        return new(defaultEncoding, commandStatus, sequence);
    }

    /// <summary> Called to create a DeliverSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="deliverSm"></param>
    /// <param name="commandStatus"></param>
    /// <returns> DeliverSmResp </returns>
    public static DeliverSmResp? Create(DataCodings defaultEncoding, DeliverSm? deliverSm, CommandStatus commandStatus)
    {
        if (deliverSm.SourceAddr == string.Empty || deliverSm.SourceAddr == null)
        {
            commandStatus = CommandStatus.EsmeRinvsrcadr;
        }

        return new(defaultEncoding, commandStatus, deliverSm.Sequence);
    }

    /// <summary> Called to create a DeliverSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="buf"></param>
    /// <param name="offset"></param>
    /// <returns> DeliverSmResp </returns>
    public static DeliverSmResp? Create(DataCodings defaultEncoding, SmppBuffer buf, ref int offset)
    {
        DeliverSmResp? deliverSmResp = new(defaultEncoding);
            
        try
        {
            int startOffset = offset;

            buf.ExtractHeader(deliverSmResp, ref offset);

            if (deliverSmResp.Length > Header.HeaderLength)
            {
                deliverSmResp.MessageId = buf.ExtractCString(ref offset);

                while (offset - startOffset < deliverSmResp.Length)
                {
                    deliverSmResp.Optional.Add(buf.ExtractTlv(ref offset));
                }
            }
        }
            
        catch
        {
            deliverSmResp = null;
        }

        return deliverSmResp;
    }
        
    #endregion

    #region PDU Detail Methods

    /// <summary> Called to return a list of property details from the PDU </summary>
    /// <returns> List PduPropertyDetail </returns>
    public List<PduPropertyDetail> Details()
    {
        List<PduPropertyDetail> details = null;

        try
        {
            int offset = 0;

            details = PduData.ExtractHeaderDetails(ref offset);

            if (details[0].ValueUInt > Header.HeaderLength)
            {
                details.Add(PduData.ExtractCString("MessageId", ref offset));

                while (offset < PduData.Length)
                {
                    PduData.ExtractTlv(details, ref offset);
                }
            }
        }

        catch
        {
        }

        return details;
    }

    #endregion
        
    #region IPacket Methods

    /// <summary> Called to return the PDU for this type of object </summary>
    /// <returns> byte[] </returns>
    public byte[] GetPdu()
    {
        SmppBuffer tmpBuff = new(DefaultEncoding, this);

        tmpBuff.AddCString(MessageId);
                
        if (Optional.Count > 0)
        {
            tmpBuff.AddTlvCollection(Optional);
        }

        tmpBuff.AddFinalLength();

        return tmpBuff.Buffer;
    }

    #endregion
}