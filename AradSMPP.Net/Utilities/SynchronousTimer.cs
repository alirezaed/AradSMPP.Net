namespace AradSMPP.Net.Utilities;

/// <summary> Maintains a synchronous timer event </summary>
public class SynchronousTimer : IDisposable
{
    #region Delegates
        
    /// <summary> Called by the timer </summary>
    public delegate void SynchronousTimerHandler(object state, SynchronousTimer theTimer);
        
    #endregion
        
    #region Private Properties
        
    /// <summary> Flag that determines whether this instance has been disposed or not yet </summary>
    private bool _disposed;
        
    /// <summary> Thread waits on this event on the timer interval </summary>
    private readonly ManualResetEvent _timerEventInterval = new(false);
        
    /// <summary> Thread waits on this event for thread to shut down </summary>
    private readonly AutoResetEvent _timerWaitShutdown = new(false);
        
    /// <summary> The interval the timer should fire </summary>
    private readonly int _timerInterval = 1000;
        
    /// <summary> State to be passed in </summary>
    private readonly object _timerState;

    /// <summary> Handle to the timer function </summary>
    private readonly SynchronousTimerHandler _timerMethod;
        
    #endregion
        
    #region Constructor
        
    /// <summary> Constructor </summary>
    /// <param name="timerMethod"></param>
    /// <param name="timerInterval"></param>
    /// <param name="timerState"></param>
    /// <param name="timerName"></param>
    public SynchronousTimer(SynchronousTimerHandler timerMethod, object timerState, int timerInterval, string? timerName = null)
    {
        _timerMethod = timerMethod;
        _timerState = timerState;
        _timerInterval = timerInterval;
            
        Thread timerThread = new(PerformTimerEvent) { Name = (timerName == null) ? "SynchronousTimer" : $"SynchronousTimer-{timerName}" };
        timerThread.Start();
    }
        
    /// <summary> Constructor </summary>
    /// <param name="timerMethod"></param>
    /// <param name="timerInterval"></param>
    /// <param name="timerState"></param>
    /// <param name="threadPriority"></param>
    /// <param name="timerName"></param>
    public SynchronousTimer(SynchronousTimerHandler timerMethod, object timerState, int timerInterval, ThreadPriority threadPriority, string? timerName = null)
    {
        _timerMethod = timerMethod;
        _timerState = timerState;
        _timerInterval = timerInterval;
            
        Thread timerThread = new(PerformTimerEvent) { Name = (timerName == null) ? "SynchronousTimer" : $"SynchronousTimer-{timerName}", Priority = threadPriority };
        timerThread.Start();
    }

    /// <summary> Constructor that will set off the timer every minute on the minute </summary>
    /// <param name="timerMethod"></param>
    /// <param name="timerState"></param>
    /// <param name="timerName"></param>
    public SynchronousTimer(SynchronousTimerHandler timerMethod, object timerState, string? timerName = null)
    {
        _timerMethod = timerMethod;
        _timerState = timerState;
        _timerInterval = 60000;
            
        Thread timerThread = new(PerformMinuteTimerEvent) { Name = (timerName == null) ? "SynchronousTimer" : $"SynchronousTimer-{timerName}" };
        timerThread.Start();
    }
        
    /// <summary> Dispose </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
        
    /// <summary> Dispose </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // Note disposing has been done.
            _disposed = true;

            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Wake up the thread so it can shut down
                _timerEventInterval.Set();
                    
                // Wait for the thread to shut down
                _timerWaitShutdown.WaitOne(10000);
            }
        }
    }
        
    #endregion
        
    #region Private Methods
        
    /// <summary> Called to implement the timer </summary>
    private void PerformTimerEvent()
    {
        for (;;)
        {
            try
            {
                // Wait here for the timer to expire
                if (_timerEventInterval.WaitOne(_timerInterval))
                {
                    if (_disposed)
                    {
                        // Tell dispose we are done
                        _timerWaitShutdown.Set();
                        
                        // We are shutting down. This should always expire
                        return;
                    }

                    // Reset the event
                    _timerEventInterval.Reset();
                }
                    
                // Call the timer method
                _timerMethod(_timerState, this);
            }

            catch
            {
                // ignored
            }
        }
    }

    /// <summary> Called to implement the timer every minute on the second </summary>
    private void PerformMinuteTimerEvent()
    {
        for (;;)
        {
            try
            {
                // Try to adjust to the nearest second
                DateTime now = DateTime.UtcNow;
                    
                // Calculate the number of milliseconds to wait
                int diff = (60 - now.Second) * 1000;

                // Wait for the clock to sync
                if (_timerEventInterval.WaitOne(diff))
                {
                    if (_disposed)
                    {
                        // Tell dispose we are done
                        _timerWaitShutdown.Set();
                        
                        // We are shutting down. This should always expire
                        return;
                    }

                    // Reset the event
                    _timerEventInterval.Reset();
                }

                // Call the timer method
                _timerMethod(_timerState, this);
            }

            catch
            {
                // ignored
            }
        }
    }
         
    #endregion
}