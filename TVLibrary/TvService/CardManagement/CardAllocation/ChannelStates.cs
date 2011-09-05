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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;
using TvLibrary.Interfaces;
using TvLibrary.Log;
using TvControl;
using TvDatabase;

namespace TvService
{
  public class ChannelStates : CardAllocationBase
  {
    #region private members   
    
    private readonly object _lock = new object();


    public ChannelStates(TvBusinessLayer businessLayer, IController controller) : base(businessLayer, controller)
    {
      LogEnabled = false;
    }    

    private void UpdateChannelStateUsers(IEnumerable<IUser> allUsers, ChannelState chState, int channelId)
    {
      //lock (_lock)
      {
        foreach (IUser t in allUsers)
        {
          IUser u = null;
          try
          {
            u = t;
          }
          catch (NullReferenceException) {}

          if (u == null)
            continue;
          if (u.IsAdmin)
            continue; //scheduler users do not need to have their channelstates set.

          try
          {
            UpdateChannelStateUser(u, chState, channelId);
          }
          catch (NullReferenceException) {}
        }
      }
    }

    private static void UpdateChannelStateUser(IUser user, ChannelState chState, int channelId)
    {
      ChannelState currentChState;

      bool stateExists = user.ChannelStates.TryGetValue(channelId, out currentChState);

      if (stateExists)
      {
        if (chState == ChannelState.nottunable)
        {
          return;
        }
        bool recording = (currentChState == ChannelState.recording);
        if (!recording)
        {
          user.ChannelStates[channelId] = chState;
          //add key if does not exist, or update existing one.                            
        }
      }
      else
      {
        user.ChannelStates[channelId] = chState;
        //add key if does not exist, or update existing one.                          
      }
    }

    private static IList<IUser> GetActiveUsers(IDictionary<int, ITvCardHandler> cards)
    {
      // find all users
      IList<IUser> allUsers = new List<IUser>();

      try
      {
        ICollection<ITvCardHandler> cardHandlers = cards.Values;
        foreach (ITvCardHandler cardHandler in cardHandlers)
        {
          //get a list of all users for this card
          IUser[] usersAvail = cardHandler.Users.GetUsers();
          if (usersAvail != null)
          {            
            foreach (IUser tmpUser in usersAvail.Where(tmpUser => !tmpUser.IsAdmin)) 
            {
              tmpUser.ChannelStates = new Dictionary<int, ChannelState>();
              allUsers.Add(tmpUser);
            }
          }
        }
      }
      catch (InvalidOperationException tex)
      {
        Log.Error("ChannelState: Possible race condition occured when getting users - {0}", tex.StackTrace);
      }

      return allUsers;
    }
    
    private void DoSetChannelStates(Dictionary<int, ITvCardHandler> cards, ICollection<Channel> channels, IList<IUser> allUsers, TVController tvController)
    {
      lock (_lock)
      {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
          //construct list of all cards we can use to tune to the new channel
          Log.Debug("Controller: DoSetChannelStates for {0} channels", channels.Count);

          if (allUsers == null || allUsers.Count == 0)
          {
            return; // no users, no point in continuing.
          }

          IDictionary<int, ChannelState> TSandRecStates = null;
          ICollection<ITvCardHandler> cardHandlers = cards.Values;

          IDictionary<int, IList<int>> channelMapping = GetChannelMapping();
          IDictionary<int, IList<IChannel>> tuningChannelMapping = GetTuningChannels();
          foreach (Channel ch in channels)
          {
            if (!ch.VisibleInGuide)
            {
              UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
              continue;
            }

            //get the tuning details for the channel
            IList<IChannel> tuningDetails;
            tuningChannelMapping.TryGetValue(ch.IdChannel, out tuningDetails);
            bool isValidTuningDetails = IsValidTuningDetails(tuningDetails);
            if (!isValidTuningDetails)
            {
              UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
              continue;
            }

            foreach (IChannel tuningDetail in tuningDetails)
            {
              foreach (ITvCardHandler cardHandler in cardHandlers)
              {
                //check if card is enabled
                if (!cardHandler.DataBaseCard.Enabled)
                {
                  //not enabled, so skip the card
                  //Log.Info("Controller:    card:{0} type:{1} is disabled", cardId, tvcard.Type);
                  UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
                  continue;
                }

                if (!cardHandler.Tuner.CanTune(tuningDetail))
                {
                  //card cannot tune to this channel, so skip it
                  //Log.Info("Controller:    card:{0} type:{1} cannot tune to channel", cardId, tvcard.Type);
                  UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
                  continue;
                }

                //check if channel is mapped to this card and that the mapping is not for "Epg Only"
                bool isChannelMappedToCard = IsChannelMappedToCard(ch, cardHandler.DataBaseCard, channelMapping);
                if (!isChannelMappedToCard)
                {
                  UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
                  continue;
                }

                if (!tuningDetail.FreeToAir && !cardHandler.DataBaseCard.CAM)
                {
                  UpdateChannelStateUsers(allUsers, ChannelState.nottunable, ch.IdChannel);
                  continue;
                }

                //ok card could be used to tune to this channel
                //now we check if its free...                                            
                CheckTransponderAllUsers(ch, allUsers, cardHandler, tuningDetail);
              } //while card end
            } //foreach tuningdetail end              

            //only query once
            if (TSandRecStates == null)
            {
              Stopwatch stopwatchTsRec = Stopwatch.StartNew();
              TSandRecStates = tvController.GetAllTimeshiftingAndRecordingChannels();
              stopwatchTsRec.Stop();
              Log.Info("ChannelStates.GetAllTimeshiftingAndRecordingChannels took {0} msec",
                       stopwatchTsRec.ElapsedMilliseconds);
            }
            UpdateRecOrTSChannelStateForUsers(ch, allUsers, TSandRecStates);
          }

          RemoveAllTunableChannelStates(allUsers);
        }
        catch (InvalidOperationException tex)
        {
          Log.Error("ChannelState: Possible race condition occured setting channel states - {0}", tex.StackTrace);
        }
        catch (Exception ex)
        {
          Log.Error("ChannelState: An unknown error occured while setting channel states - {0}\n{1}", ex.Message,
                    ex.StackTrace);
        }
        finally
        {
          stopwatch.Stop();
          Log.Info("ChannelStates.DoSetChannelStates took {0} msec", stopwatch.ElapsedMilliseconds);
        }
      }
    }

    private IDictionary<int, IList<IChannel>> GetTuningChannels()
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      var result = new Dictionary<int, IList<IChannel>>();
      IList<TuningDetail> tuningDetails = TuningDetail.ListAll();
      foreach (TuningDetail tuningDetail in tuningDetails)
      {
        IList<IChannel> tuningChannels;
        result.TryGetValue(tuningDetail.IdChannel, out tuningChannels);
        if (tuningChannels == null)
        {
          tuningChannels = new List<IChannel>();
          result.Add(tuningDetail.IdChannel, tuningChannels);
        }
        tuningChannels.Add(_businessLayer.GetTuningChannel(tuningDetail));
      }
      stopwatch.Stop();
      Log.Info("ChannelStates.GetTuningChannels took {0} msec", stopwatch.ElapsedMilliseconds);
      return result;
    }

    private static IDictionary<int, IList<int>> GetChannelMapping()
    {
      Stopwatch stopwatch = Stopwatch.StartNew();
      var result = new Dictionary<int, IList<int>>();
      IList<ChannelMap> channelMaps = ChannelMap.ListAll();
      foreach (ChannelMap map in channelMaps)
      {
        IList<int> cards;
        result.TryGetValue(map.IdChannel, out cards);
        if (cards == null)
        {
          cards = new List<int>();
          result.Add(map.IdChannel, cards);
        }
        if (!cards.Contains(map.IdCard))
        {
          cards.Add(map.IdCard);
        }
      }
      stopwatch.Stop();
      Log.Info("ChannelStates.GetChannelMapping took {0} msec", stopwatch.ElapsedMilliseconds);
      return result;
    }

    private bool IsChannelMappedToCard(Channel dbChannel, Card card, IDictionary<int, IList<int>> channelMapping)
    {
      //check if channel is mapped to this card and that the mapping is not for "Epg Only"
      IList<int> cards;
      channelMapping.TryGetValue(dbChannel.IdChannel, out cards);
      return cards != null && cards.Contains(card.IdCard);
    }

    private void RemoveAllTunableChannelStates(IEnumerable<IUser> allUsers)
    {
      //lock (_lock)
      {
        foreach (IUser user in allUsers)
        {
          var keysToDelete = new List<int>();

          foreach (KeyValuePair<int, ChannelState> kvp in user.ChannelStates)
          {
            if (kvp.Value == ChannelState.tunable)
            {
              keysToDelete.Add(kvp.Key);
            }
          }

          foreach (int key in keysToDelete)
          {
            user.ChannelStates.Remove(key);
          }
        }
      }
    }

    private void UpdateRecOrTSChannelStateForUsers(Channel ch, IEnumerable<IUser> allUsers,
                                                          IDictionary<int, ChannelState> TSandRecStates)
    {
      ChannelState cs;
      TSandRecStates.TryGetValue(ch.IdChannel, out cs);

      if (cs == ChannelState.recording)
      {
        UpdateChannelStateUsers(allUsers, ChannelState.recording, ch.IdChannel);
      }
      else if (cs == ChannelState.timeshifting)
      {
        UpdateChannelStateUsers(allUsers, ChannelState.timeshifting, ch.IdChannel);
      }
    }

    private void CheckTransponderAllUsers(Channel ch, IEnumerable<IUser> allUsers, ITvCardHandler tvcard,
                                                 IChannel tuningDetail)
    {
      //IEnumerable<IUser> allUsersCopy = GetUsersCopy(allUsers);
      foreach (IUser user in allUsers) 
      {
        //ignore admin users, like scheduler
        if (user.IsAdmin)
        {
          continue;
        }

        bool checkTransponder = CheckTransponder( user, tvcard, tuningDetail);
        if (checkTransponder)
        {
          UpdateChannelStateUser(user, ChannelState.tunable, ch.IdChannel);
        } 
        else
        {
          UpdateChannelStateUser(user, ChannelState.nottunable, ch.IdChannel);
        }
      }
    }

   /* private IEnumerable<IUser> GetUsersCopy(IEnumerable<IUser> allUsers)
    {
      List<IUser> allUsersCopy;
      lock (_lock)
      {
        allUsersCopy = allUsers.ToList();
      }
      return allUsersCopy;
    }*/

    #endregion

    #region public members

    public void SetChannelStates(Dictionary<int, ITvCardHandler> cards, IList<Channel> channels,
                                 TVController tvController)
    {
      if (channels == null)
      {
        return;
      }

      //call the real work as a thread in order to avoid slower channel changes.
      // find all users      
      IList<IUser> allUsers = GetActiveUsers(cards);
      ThreadStart starter = () => DoSetChannelStates(cards, channels, allUsers, tvController);
      Thread setChannelStatesThread = new Thread(starter);
      setChannelStatesThread.Name = "Channel state thread";
      setChannelStatesThread.IsBackground = true;
      setChannelStatesThread.Priority = ThreadPriority.Lowest;
      setChannelStatesThread.Start();
    }


    /// <summary>
    /// Gets a list of all channel states    
    /// </summary>    
    /// <returns>dictionary containing all channel states of the channels supplied</returns>
    public Dictionary<int, ChannelState> GetChannelStates(Dictionary<int, ITvCardHandler> cards, IList<Channel> channels,
                                                          ref IUser user,
                                                          TVController tvController)
    {
      if (channels == null)
      {
        return null;
      }

      List<IUser> allUsers = new List<IUser>();
      allUsers.Add(user);

      DoSetChannelStates(cards, channels, allUsers, tvController);

      if (allUsers.Count > 0)
      {
        return allUsers[0].ChannelStates;
      }
      return new Dictionary<int, ChannelState>();
    }

    #endregion
  }
}