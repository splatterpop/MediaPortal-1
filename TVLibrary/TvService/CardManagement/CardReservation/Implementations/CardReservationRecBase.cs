#region Copyright (C) 2005-2010 Team MediaPortal

// Copyright (C) 2005-2010 Team MediaPortal
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
using TvControl;
using TvLibrary.Log;

namespace TvService
{
  public abstract class CardReservationRecBase : CardReservationBase
  {    
    private CardDetail _cardInfo;
    private RecordingDetail _recDetail;    

    protected CardReservationRecBase(TVController tvController) : base(tvController) { }

  

    public CardDetail CardInfo
    {
      get { return _cardInfo; }
      set { _cardInfo = value; }
    }

    public RecordingDetail RecDetail
    {
      get { return _recDetail; }
      set { _recDetail = value; }
    }  
    
    protected override bool OnStartTune(IUser user)
    {
      bool startRecordingOnDisc = true;
      if (_tvController.SupportsSubChannels(_cardInfo.Card.IdCard) == false)
      {
        Log.Write("Scheduler : record, now start timeshift");
        string timeshiftFileName = String.Format(@"{0}\live{1}-{2}.ts", _cardInfo.Card.TimeShiftFolder, _cardInfo.Id,
                                                 user.SubChannel);
        startRecordingOnDisc = (TvResult.Succeeded == _tvController.StartTimeShifting(ref user, ref timeshiftFileName));
      }

      if (startRecordingOnDisc)
      {
        _recDetail.MakeFileName(_cardInfo.Card.RecordingFolder);
        _recDetail.CardInfo = _cardInfo;
        Log.Write("Scheduler : record to {0}", _recDetail.FileName);
        string fileName = _recDetail.FileName;
        startRecordingOnDisc = (TvResult.Succeeded == _tvController.StartRecording(ref user, ref fileName, false, 0));

        if (startRecordingOnDisc)
        {
          _recDetail.FileName = fileName;
          _recDetail.RecordingStartDateTime = DateTime.Now;
        }
      }
      if (!startRecordingOnDisc && _tvController.AllCardsIdle)
      {
        _tvController.EpgGrabberEnabled = true;
      }

      return startRecordingOnDisc;
    }

  }
}
