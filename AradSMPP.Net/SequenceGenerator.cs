#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Generates sequence numbers </summary>
internal class SequenceGenerator
{
    #region Private Properties

    /// <summary> Provided to lock the shared resource </summary>
    private static readonly object _locker = new();

    /// <summary> Sequence counter </summary>
    private static uint _sequence;

    /// <summary> Sequence byte counter </summary>
    private static byte _byteSequence;

    /// <summary> Random generator </summary>
    private static readonly Random _rnd = new();

    #endregion

    #region Public Properties

    /// <summary> Called to return the next counter </summary>
    public static uint Counter
    {
        get
        {
            lock (_locker)
            {
                if (_sequence == 0)
                {
                    _sequence = Convert.ToUInt32(_rnd.Next(0, Convert.ToInt32(0x7FFFFFFF)));
                }
                    
                if (_sequence == 0x7FFFFFFF)
                {
                    _sequence = 1;
                }
                    
                _sequence++;
            }
				
            return _sequence;
        }
    }

    /// <summary> Called to return the next byte counter </summary>
    public static byte ByteCounter
    {
        get
        {
            lock (_locker)
            {
                if (_byteSequence == 0)
                {
                    _byteSequence = Convert.ToByte(_rnd.Next(0, Convert.ToInt32(byte.MaxValue)));
                }
                    
                if (_byteSequence == byte.MaxValue)
                {
                    _byteSequence = 1;
                }
                    
                _byteSequence++;
            }
				
            return _byteSequence;
        }
    }

    #endregion
}