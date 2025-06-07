#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> User Data Header Collection </summary>
public class UserDataHeaderCollection : List<UserDataHeader>
{
    #region Public Properties
        
    /// <summary> Provides array type access </summary>
    /// <param name="iei"></param>
    /// <returns> A UserDataHeader object </returns>
    public UserDataHeader this[InformationElementIdentifiers iei] { get { return this.FirstOrDefault(k => k.Iei == iei); } }

    #endregion
        
    #region Operator Methods
        
    /// <summary> Allows the byte array to be assigned to a UserDataHeaderCollection object </summary>
    /// <param name="bytes"></param>
    /// <returns> UserDataHeaderCollection </returns>
    public static implicit operator UserDataHeaderCollection(byte[] bytes)
    {
        UserDataHeaderCollection col = [];

        SmppBuffer userData = new(DataCodings.Default, bytes);
        int offs = 0;

        byte udhLength = userData.ExtractByte(ref offs);
        int curOffset = offs;
            
        while (curOffset + udhLength > offs)
        {
            byte udhiType = userData.ExtractByte(ref offs);
            byte udhiLength = userData.ExtractByte(ref offs);
            byte[] data = userData.ExtractByteArray(ref offs, udhiLength);
            col.Add(UserDataHeader.Create(udhiType, udhiLength, data));
        }

        return col;
    }
        
    #endregion
        
    #region Public Methods
        
    /// <summary> Concatenates the specify message </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="msgRef"></param>
    /// <param name="total"></param>
    /// <param name="seqNum"></param>
    public void AddConcatenatedShortMessages8Bit(DataCodings defaultEncoding, byte msgRef, byte total, byte seqNum)
    {
        SmppBuffer tmpBuff = new(defaultEncoding);
        tmpBuff.AddByte(msgRef);
        tmpBuff.AddByte(total);
        tmpBuff.AddByte(seqNum);
                
        Add(UserDataHeader.Create(InformationElementIdentifiers.ConcatenatedShortMessages8Bit, Convert.ToByte(tmpBuff.Length), tmpBuff.Buffer));
    }

    /// <summary> Adds data to the user data header list </summary>
    /// <param name="iei"></param>
    /// <param name="data"></param>
    public void Add(InformationElementIdentifiers iei, byte[] data)
    {
        Add(UserDataHeader.Create(iei, Convert.ToByte(data.Length), data));
    }

    #endregion
}