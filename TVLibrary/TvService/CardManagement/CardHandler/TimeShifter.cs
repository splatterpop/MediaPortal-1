#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using TvLibrary;
using TvLibrary.Implementations;
using TvLibrary.Interfaces;
using TvLibrary.Interfaces.Analyzer;
using TvLibrary.Implementations.DVB;
using TvLibrary.Log;
using TvControl;
using TvDatabase;

namespace TvService
{
  public class TimeShifter : TimeShifterBase
  {
    private readonly bool _linkageScannerEnabled;
    private readonly ChannelLinkageGrabber _linkageGrabber;
    private bool _tuneInProgress;    
    private DateTime _timeAudioEvent;
    private DateTime _timeVideoEvent;    

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeShifter"/> class.
    /// </summary>
    /// <param name="cardHandler">The card handler.</param>
    public TimeShifter(ITvCardHandler cardHandler) : base(cardHandler)
    {
      

      _cardHandler = cardHandler;
      var layer = new TvBusinessLayer();
      _linkageScannerEnabled = (layer.GetSetting("linkageScannerEnabled", "no").Value == "yes");

      _linkageGrabber = new ChannelLinkageGrabber(cardHandler.Card);
      _timeshiftingEpgGrabberEnabled = (layer.GetSetting("timeshiftingEpgGrabberEnabled", "no").Value == "yes");

      _timeAudioEvent = DateTime.MinValue;
      _timeVideoEvent = DateTime.MinValue;
    }    

    /// <summary>
    /// Gets the name of the time shift file.
    /// </summary>
    /// <value>The name of the time shift file.</value>
    public string FileName(ref IUser user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false)
          return "";

        try
        {
          RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
          if (!RemoteControl.Instance.CardPresent(_cardHandler.DataBaseCard.IdCard))
            return "";
          if (_cardHandler.IsLocal == false)
          {
            return RemoteControl.Instance.TimeShiftFileName(ref user);
          }
        }
        catch (Exception)
        {
          Log.Error("card: unable to connect to slave controller at:{0}",
                    _cardHandler.DataBaseCard.ReferencedServer().HostName);
          return "";
        }
        var context = _cardHandler.Card.Context as ITvCardContext;
        if (context == null)
          return null;
        context.GetUser(ref user);
        ITvSubChannel subchannel = GetSubChannel(user.SubChannel);
        if (subchannel == null)
          return null;
        return subchannel.TimeShiftFileName + ".tsbuffer";
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return "";
      }
    }

    /// <summary>
    /// Returns the position in the current timeshift file and the id of the current timeshift file
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="position">The position in the current timeshift buffer file</param>
    /// <param name="bufferId">The id of the current timeshift buffer file</param>
    public bool GetCurrentFilePosition(ref IUser user, ref Int64 position, ref long bufferId)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false)
          return false;

        try
        {
          RemoteControl.HostName = _cardHandler.DataBaseCard.ReferencedServer().HostName;
          if (!RemoteControl.Instance.CardPresent(_cardHandler.DataBaseCard.IdCard))
            return false;
          if (_cardHandler.IsLocal == false)
          {
            return RemoteControl.Instance.TimeShiftGetCurrentFilePosition(ref user, ref position,
                                                                          ref bufferId);
          }
        }
        catch (Exception)
        {
          Log.Error("card: unable to connect to slave controller at:{0}",
                    _cardHandler.DataBaseCard.ReferencedServer().HostName);
          return false;
        }
        var context = _cardHandler.Card.Context as ITvCardContext;
        if (context == null)
          return false;
        context.GetUser(ref user);
        ITvSubChannel subchannel = GetSubChannel(user.SubChannel);
        if (subchannel == null)
          return false;
        subchannel.TimeShiftGetCurrentFilePosition(ref position, ref bufferId);
        return (position != -1);
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        return false;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this card is recording.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this card is recording; otherwise, <c>false</c>.
    /// </value>
    public bool IsAnySubChannelTimeshifting
    {
      get
      {
        IUser[] users = _cardHandler.Users.GetUsers();
        if (users == null)
          return false;
        if (users.Length == 0)
          return false;
        return users.Any(user => IsTimeShifting(ref user));
      }
    }

    /// <summary>
    /// Returns if the card is timeshifting or not
    /// </summary>
    /// <returns>true when card is timeshifting otherwise false</returns>
    public bool IsTimeShifting(ref IUser user)
    {
      bool isTimeShifting = false;
      try
      {
        var subchannel = GetSubChannel(ref user);
        if (subchannel != null)
        {
          isTimeShifting = subchannel.IsTimeShifting;
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return isTimeShifting;
    }


    /// <summary>
    /// returns the date/time when timeshifting has been started for the card specified
    /// </summary>
    /// <returns>DateTime containg the date/time when timeshifting was started</returns>
    public DateTime TimeShiftStarted(IUser user)
    {
      DateTime timeShiftStarted = DateTime.MinValue;
      try
      {
        ITvSubChannel subchannel = GetSubChannel(ref user);
        if (subchannel != null)
        {
          timeShiftStarted = subchannel.StartOfTimeShift;
        }
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return timeShiftStarted;
    }

    protected override void AudioVideoEventHandler(PidType pidType)
    {
      if (_tuneInProgress)
      {
        Log.Info("audioVideoEventHandler - tune in progress");
        return;
      }

      // we are only interested in video and audio PIDs
      if (pidType == PidType.Audio)
      {
        TimeSpan ts = DateTime.Now - _timeAudioEvent;
        if (ts.TotalMilliseconds > 1000)
        {
          // Avoid repetitive events that are kept for next channel change, so trig only once.
          Log.Info("audioVideoEventHandler {0}", pidType);
          _eventAudio.Set();
        }
        else
        {
          Log.Info("audio last seen at {0}", _timeAudioEvent);
        }
        _timeAudioEvent = DateTime.Now;
      }

      if (pidType == PidType.Video)
      {
        TimeSpan ts = DateTime.Now - _timeVideoEvent;
        if (ts.TotalMilliseconds > 1000)
        {
          // Avoid repetitive events that are kept for next channel change, so trig only once.
          Log.Info("audioVideoEventHandler {0}", pidType);
          _eventVideo.Set();
        }
        else
        {
          Log.Info("video last seen at {0}", _timeVideoEvent);
        }
        _timeVideoEvent = DateTime.Now;
      }
    }

    /// <summary>
    /// Start timeshifting.
    /// </summary>
    /// <param name="user">User</param>
    /// <param name="fileName">Name of the timeshiftfile.</param>
    /// <returns>TvResult indicating whether method succeeded</returns>
    public TvResult Start(ref IUser user, ref string fileName)
    {
      try
      {
#if DEBUG

        if (File.Exists(@"\failts_" + _cardHandler.DataBaseCard.IdCard))
        {
          throw new Exception("failed ts on purpose");
        }
#endif
        if (IsTuneCancelled())
        {
          Stop(ref user);
          return TvResult.TuneCancelled;
        }
        _eventTimeshift.Reset();
        if (_cardHandler.DataBaseCard.Enabled == false)
        {
          return TvResult.CardIsDisabled;
        }

       
        // Let's verify if hard disk drive has enough free space before we start time shifting. The function automatically handles both local and UNC paths
        if (!IsTimeShifting(ref user))
        {
          var hasFreeDiskSpace = HasFreeDiskSpace(fileName);
          if (!hasFreeDiskSpace)              
          {                
            Stop(ref user);                
            return TvResult.NoFreeDiskSpace;
          }
        }

        Log.Write("card: StartTimeShifting {0} {1} ", _cardHandler.DataBaseCard.IdCard, fileName);

        var context = _cardHandler.Card.Context as ITvCardContext;
        if (context == null)
        {            
          Stop(ref user);
          return TvResult.UnknownChannel;
        }

        context.GetUser(ref user);
        ITvSubChannel subchannel = GetSubChannel(user.SubChannel);

        if (subchannel == null)
        {
          Stop(ref user);
          return TvResult.UnknownChannel;
        }

        _subchannel = subchannel;

        Log.Write("card: CAM enabled : {0}", _cardHandler.HasCA);

        if (subchannel is TvDvbChannel)
        {
          if (!((TvDvbChannel)subchannel).PMTreceived)
          {
            Log.Info("start subch:{0} No PMT received. Timeshifting failed", subchannel.SubChannelId);
            Stop(ref user);
            return TvResult.UnableToStartGraph;
          }
        }

        if (subchannel is BaseSubChannel)
        {
          ((BaseSubChannel)subchannel).AudioVideoEvent += AudioVideoEventHandler;
        }

        bool isScrambled;
        if (subchannel.IsTimeShifting)
        {
          if (!WaitForFile(ref user, out isScrambled))
          {              
            Stop(ref user);
            if (IsTuneCancelled())
            {                
              return TvResult.TuneCancelled;
            }
            if (isScrambled)
            {
              return TvResult.ChannelIsScrambled;
            }
            return TvResult.NoVideoAudioDetected;
          }

          context.OnZap(user);
          if (_linkageScannerEnabled)
            _cardHandler.Card.StartLinkageScanner(_linkageGrabber);
          StartTimeShiftingEPGgrabber(user);
          return TvResult.Succeeded;
        }

        bool result = subchannel.StartTimeShifting(fileName);
        if (result == false)
        {
          Stop(ref user);
          return TvResult.UnableToStartGraph;
        }

        fileName += ".tsbuffer";
        if (!WaitForFile(ref user, out isScrambled))
        {
          Stop(ref user);
          if (IsTuneCancelled())
          {
            return TvResult.TuneCancelled;
          }
          if (isScrambled)
          {
            return TvResult.ChannelIsScrambled;
          }
          return TvResult.NoVideoAudioDetected;
        }
        context.OnZap(user);
        if (_linkageScannerEnabled)
          _cardHandler.Card.StartLinkageScanner(_linkageGrabber);
        if (_timeshiftingEpgGrabberEnabled)
        {
          Channel channel = Channel.Retrieve(user.IdChannel);
          if (channel.GrabEpg)
            _cardHandler.Card.GrabEpg();
          else
            Log.Info("TimeshiftingEPG: channel {0} is not configured for grabbing epg",
                      channel.DisplayName);
        }
        return TvResult.Succeeded;
      }
      catch (Exception ex)
      {
        Log.Write(ex);
        Stop(ref user);
        return TvResult.UnknownError;
      }
      finally
      {
        _eventTimeshift.Set();
        _cancelled = false;
      }      
    }

    private static bool HasFreeDiskSpace(string fileName)
    {
      ulong freeDiskSpace = Utils.GetFreeDiskSpace(fileName);

      var layer = new TvBusinessLayer();
      UInt32 maximumFileSize = UInt32.Parse(layer.GetSetting("timeshiftMaxFileSize", "256").Value); // in MB
      ulong diskSpaceNeeded = Convert.ToUInt64(maximumFileSize);
      diskSpaceNeeded *= 1000000 * 2; // Convert to bytes; 2 times of timeshiftMaxFileSize
      bool hasFreeDiskSpace = freeDiskSpace > diskSpaceNeeded;
      return hasFreeDiskSpace;
    }

    /// <summary>
    /// Stops the time shifting.
    /// </summary>
    /// <returns></returns>    
    public bool Stop(ref IUser user)
    {
      try
      {
        if (_cardHandler.DataBaseCard.Enabled == false)
          return true;

        ITvSubChannel subchannel = GetSubChannel(user.SubChannel);
        if (subchannel is BaseSubChannel)
        {
          ((BaseSubChannel)subchannel).AudioVideoEvent -= AudioVideoEventHandler;
        }
        Log.Write("card {2}: StopTimeShifting user:{0} sub:{1}", user.Name, user.SubChannel,
                  _cardHandler.Card.Name);
        var context = _cardHandler.Card.Context as ITvCardContext;          
        if (context == null)
          return true;
        if (_linkageScannerEnabled)
          _cardHandler.Card.ResetLinkageScanner();

        if (_cardHandler.IsIdle)
        {
          _cardHandler.PauseCard(user);
        }
        else
        {
          Log.Debug("card not IDLE - removing user: {0}", user.Name);
          _cardHandler.Users.RemoveUser(user);
        }

        context.Remove(user);
        return true;        
      }
      catch (Exception ex)
      {
        Log.Write(ex);
      }
      return false;
    }

    /// <summary>
    /// Fetches the stream quality information
    /// </summary>   
    /// <param name="user">user</param>    
    /// <param name="totalTSpackets">Amount of packets processed</param>    
    /// <param name="discontinuityCounter">Number of stream discontinuities</param>
    /// <returns></returns>
    public void GetStreamQualityCounters(IUser user, out int totalTSpackets, out int discontinuityCounter)
    {
      totalTSpackets = 0;
      discontinuityCounter = 0;

      var context = _cardHandler.Card.Context as ITvCardContext;
      if (context != null)
      {
        bool userExists;
        context.GetUser(ref user, out userExists);
      }
      ITvSubChannel subchannel = GetSubChannel(user.SubChannel);

      var dvbSubchannel = subchannel as TvDvbChannel;
      if (dvbSubchannel != null)
      {
        dvbSubchannel.GetStreamQualityCounters(out totalTSpackets, out discontinuityCounter);
      }
    }

    public void OnBeforeTune()
    {
      Log.Debug("TimeShifter.OnBeforeTune: resetting audio/video events");
      _tuneInProgress = true;
      _eventAudio.Reset();
      _eventVideo.Reset();
    }

    public void OnAfterTune()
    {
      Log.Debug("TimeShifter.OnAfterTune: resetting audio/video time");
      _timeAudioEvent = DateTime.MinValue;
      _timeVideoEvent = DateTime.MinValue;

      _tuneInProgress = false;
    }      
  }
}