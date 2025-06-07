#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Represents the response to the submit_multi PDU </summary>
public class SubmitMultiSmResp : Header, IPacket, IPduDetails
{
    #region Public Properties
        
    /// <summary> The id of the message </summary>
    public string? MessageId { get; set; }
        
    /// <summary> A list of destination addresses that were unsuccessful </summary>
    public List<UnsuccessDestinationAddress> UnsuccessDestinationAddresses { get; set; }
        
    /// <summary> Optional Parameters </summary>
    public TlvCollection Optional { get; set; }
        
    #endregion

    #region Constructor

    /// <summary> Constructor </summary>
    /// <param name="defaultEncoding"></param>
    private SubmitMultiSmResp(DataCodings defaultEncoding) : base(defaultEncoding, CommandSet.SubmitMultiSmResp)
    {
        UnsuccessDestinationAddresses = [];
        Optional = [];
    }

    /// <summary> Constructor </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="commandStatus"></param>
    /// <param name="sequence"></param>
    private SubmitMultiSmResp(DataCodings defaultEncoding, CommandStatus commandStatus, uint sequence) : base(defaultEncoding, CommandSet.SubmitMultiSmResp, commandStatus, sequence)
    {
        UnsuccessDestinationAddresses = [];
        Optional = [];
    }

    /// <summary> Constructor </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="commandStatus"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    private SubmitMultiSmResp(DataCodings defaultEncoding, CommandStatus commandStatus, uint sequence, string? messageId) : base(defaultEncoding, CommandSet.SubmitMultiSmResp, commandStatus, sequence)
    {
        MessageId = messageId;

        UnsuccessDestinationAddresses = [];
        Optional = [];
    }
        
    #endregion
        
    #region Factory Methods

    /// <summary> Called to create a SubmitMultiResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <returns> SubmitMultiResp </returns>
    public static SubmitMultiSmResp Create(DataCodings defaultEncoding)
    {
        return new(defaultEncoding);
    }

    /// <summary> Called to create a SubmitMultiResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="status"></param>
    /// <param name="sequence"></param>
    /// <returns> SubmitMultiResp </returns>
    public static SubmitMultiSmResp? Create(DataCodings defaultEncoding, CommandStatus status, uint sequence)
    {
        return new(defaultEncoding, status, sequence);
    }

    /// <summary> Called to create a DataSmResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="submitMultiSm"></param>
    /// <param name="commandStatus"></param>
    /// <param name="messageId"></param>
    /// <returns> SubmitMultiSmResp </returns>
    public static SubmitMultiSmResp? Create(DataCodings defaultEncoding, SubmitMultiSm? submitMultiSm, CommandStatus commandStatus, string? messageId)
    {
        if (submitMultiSm.SourceAddr == string.Empty || submitMultiSm.SourceAddr == null)
        {
            commandStatus = CommandStatus.EsmeRinvsrcadr;
        }

        return new(defaultEncoding, commandStatus, submitMultiSm.Sequence, messageId);
    }

    /// <summary> Called to create a SubmitMultiResp object </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="buf"></param>
    /// <param name="offset"></param>
    /// <returns> SubmitMultiResp </returns>
    public static SubmitMultiSmResp? Create(DataCodings defaultEncoding, SmppBuffer buf, ref int offset)
    {
        SubmitMultiSmResp? submitMultiResp = new(defaultEncoding);
            
        try
        {
            int startOffset = offset;

            buf.ExtractHeader(submitMultiResp, ref offset);
            
            if (submitMultiResp.Length > Header.HeaderLength)
            {
                submitMultiResp.MessageId = buf.ExtractCString(ref offset);
                submitMultiResp.UnsuccessDestinationAddresses = buf.ExtractUnsuccessDestinationAddresses(ref offset);

                while (offset - startOffset < submitMultiResp.Length)
                {
                    submitMultiResp.Optional.Add(buf.ExtractTlv(ref offset));
                }
            }
        }
            
        catch
        {
            submitMultiResp = null;
        }

        return submitMultiResp;
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
        tmpBuff.AddUnsuccessDestinationAddresses(UnsuccessDestinationAddresses);
                
        if (Optional.Count > 0)
        {
            tmpBuff.AddTlvCollection(Optional);
        }

        tmpBuff.AddFinalLength();

        return tmpBuff.Buffer;
    }

    #endregion
}