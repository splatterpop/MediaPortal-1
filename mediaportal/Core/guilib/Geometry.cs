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
using System.Drawing;
using MediaPortal.Player;
using MediaPortal.Profile;
using MediaPortal.Configuration;

namespace MediaPortal.GUI.Library
{
  /// <summary>
  /// Class which can do transformations for video windows
  /// currently it supports Zoom, Zoom 14:9, normal, stretch, original, letterbox 4:3 and non linear stretching
  /// </summary>
  public class Geometry
  {
    public enum Type
    {
      Normal, // pan scan
      Original, // original source format
      Zoom, // widescreen
      Zoom14to9, // 4:3 on 16:9 screens
      Stretch, // letterbox
      LetterBox43, // letterbox 4:3
      NonLinearStretch // stretch and crop
    }

    private int _imageWidth = 100; // width of the video window or image
    private int _imageHeight = 100; // height of the height window or image
    private int m_ScreenWidth = 100; // width of the screen
    private int m_ScreenHeight = 100; // height of the screen
    private Type m_eType = Type.Normal; // type of transformation used
    private float m_fPixelRatio = 1.0f; // pixelratio correction 
    private bool m_bUseNonLinearStretch = false; //AR uses non-linear stretch or not
    private float nlsZoomY = 1.10f; //  0.083f;
    private float nlsVertPos = 0.3f;


    /// <summary>
    /// Empty constructor
    /// </summary>
    public Geometry() 
    {
        using (Settings xmlreader = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
        {
            nlsZoomY = (float)xmlreader.GetValueAsInt("nls", "zoom", 115) / 100.0f;
            nlsVertPos = (float)xmlreader.GetValueAsInt("nls", "vertpos", 30) / 100.0f;
        }
    }

    /// <summary>
    /// property to get/set the width of the video/image
    /// </summary>
    public int ImageWidth
    {
      get { return _imageWidth; }
      set { _imageWidth = value; }
    }

    /// <summary>
    /// property to get/set the height of the video/image
    /// </summary>
    public int ImageHeight
    {
      get { return _imageHeight; }
      set { _imageHeight = value; }
    }

    /// <summary>
    /// property to get/set the width of the screen
    /// </summary>
    public int ScreenWidth
    {
      get { return m_ScreenWidth; }
      set { m_ScreenWidth = value; }
    }

    /// <summary>
    /// property to get/set the height of the screen
    /// </summary>
    public int ScreenHeight
    {
      get { return m_ScreenHeight; }
      set { m_ScreenHeight = value; }
    }

    /// <summary>
    /// property to get/set the transformation type
    /// </summary>
    public Type ARType
    {
      get { return m_eType; }
      set { m_eType = value; }
    }

    /// <summary>
    /// property to get/set the pixel ratio 
    /// </summary>
    public float PixelRatio
    {
      get { return m_fPixelRatio; }
      set { m_fPixelRatio = value; }
    }

    /// <summary>
    /// Method todo the transformation.
    /// It will calculate 2 rectangles. A source and destination rectangle based on the
    /// current transformation , image width/height and screen width/height
    /// the returned source rectangle specifies which part of the image/video should be copied
    /// the returned destination rectangle specifies where the copied part should be presented on screen
    /// </summary>
    /// <param name="rSource">rectangle containing the source rectangle of the image/video</param>
    /// <param name="rDest">rectangle  containing the destination rectangle of the image/video</param>
    public void GetWindow(out Rectangle rSource, out Rectangle rDest)
    {
      float fSourceFrameRatio = CalculateFrameAspectRatio();
      CropSettings cropSettings = new CropSettings();
      GetWindow(fSourceFrameRatio, out rSource, out rDest, cropSettings);
    }

    public void GetWindow(int arVideoWidth, int arVideoHeight, out Rectangle rSource, out Rectangle rDest)
    {
      CropSettings cropSettings = new CropSettings();
      GetWindow(arVideoWidth, arVideoHeight, out rSource, out rDest, cropSettings);
    }

    // used from planescene
    public void GetWindow(int arVideoWidth, int arVideoHeight, out Rectangle rSource, out Rectangle rDest,
                          CropSettings cropSettings)
    {
      float fSourceFrameRatio = (float)arVideoWidth / (float)arVideoHeight;
      GetWindow(fSourceFrameRatio, out rSource, out rDest, cropSettings);
    }

    public void GetWindow(int arVideoWidth, int arVideoHeight, out Rectangle rSource, out Rectangle rDest,
                          out bool bUseNonLinearStretch, CropSettings cropSettings)
    {
      GetWindow(arVideoWidth, arVideoHeight, out rSource, out rDest, cropSettings);
      bUseNonLinearStretch = m_bUseNonLinearStretch;
    }

    public void GetWindow(float fSourceFrameRatio, out Rectangle rSource, out Rectangle rDest, CropSettings cropSettings)
    {
      float fOutputFrameRatio = fSourceFrameRatio / PixelRatio;

      // make sure the crop settings are acceptable
      cropSettings = cropSettings.EnsureSanity(ImageWidth, ImageHeight);

      int cropW = cropSettings.Left + cropSettings.Right;
      int cropH = cropSettings.Top + cropSettings.Bottom;

      // the source image dimensions when taking into
      // account the crop settings
      int croppedImageWidth = ImageWidth - cropW;
      int croppedImageHeight = ImageHeight - cropH;

      // suggested by ziphnor
      float fSourcePixelRatio = fSourceFrameRatio / ((float)ImageWidth / (float)ImageHeight);
      float fCroppedOutputFrameRatio = fSourcePixelRatio * ((float)croppedImageWidth / (float)croppedImageHeight) /
                                       PixelRatio;

      //Log.Debug("croppedImage W/H {0}x{1}", croppedImageWidth, croppedImageHeight);
      //Log.Debug("fOutputFrameRatio : {0}", fOutputFrameRatio);
      //Log.Debug("fCroppedOutputFrameRatio : {0}", fCroppedOutputFrameRatio);
      //Log.Debug("fSourceFrameRatio: {0}", fSourceFrameRatio);

      //don't use non linear stretch by default
      m_bUseNonLinearStretch = false;

      switch (ARType)
      {
        case Type.Stretch:
          {
            rSource = new Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
            rDest = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
          }
          break;

        case Type.Zoom:
          {
            // calculate AR compensation (see http://www.iki.fi/znark/video/conversion)
            // assume that the movie is widescreen first, so use full height
            float fVertBorder = 0;
            float fNewHeight = (float)(ScreenHeight);
            float fNewWidth = fNewHeight * fCroppedOutputFrameRatio;
            float fHorzBorder = (fNewWidth - (float)ScreenWidth) / 2.0f;
            float fFactor = fNewWidth / ((float)ImageWidth);
            fFactor *= PixelRatio;
            fHorzBorder = fHorzBorder / fFactor;

            if ((int)fNewWidth < ScreenWidth)
            {
              fHorzBorder = 0;
              fNewWidth = (float)(ScreenWidth);
              fNewHeight = fNewWidth / fCroppedOutputFrameRatio;
              fVertBorder = (fNewHeight - (float)ScreenHeight) / 2.0f;
              fFactor = fNewWidth / ((float)ImageWidth);
              fFactor *= PixelRatio;
              fVertBorder = fVertBorder / fFactor;
            }

            rSource = new Rectangle((int)fHorzBorder,
                                    (int)fVertBorder,
                                    (int)((float)ImageWidth - 2.0f * fHorzBorder),
                                    (int)((float)ImageHeight - 2.0f * fVertBorder));
            rDest = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
            AdjustSourceForCropping(ref rSource, cropSettings);
          }
          break;

        case Type.Normal:
          {
            // maximize the movie width
            float fNewWidth = (float)ScreenWidth;
            float fNewHeight = (float)(fNewWidth / fCroppedOutputFrameRatio);

            if (fNewHeight > ScreenHeight)
            {
              fNewHeight = ScreenHeight;
              fNewWidth = fNewHeight * fCroppedOutputFrameRatio;
            }

            // this shouldnt happen, but just make sure that everything still fits onscreen
            if (fNewWidth > ScreenWidth || fNewHeight > ScreenHeight)
            {
              //fNewWidth = (float)ImageWidth;
              fNewWidth = (float)croppedImageWidth;
              fNewHeight = (float)croppedImageHeight;
            }

            // Centre the movie
            float iPosY = (ScreenHeight - fNewHeight) / 2;
            float iPosX = (ScreenWidth - fNewWidth) / 2;

            rSource = new Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
            rDest = new Rectangle((int)iPosX, (int)iPosY, (int)(fNewWidth + 0.5f), (int)(fNewHeight + 0.5f));

            //AdjustForCropping(ref rSource, ref rDest, cropSettings, true);
          }
          break;

        case Type.Original:
          {
            // maximize the movie width
            float fNewWidth = (float)Math.Min(ImageWidth, ScreenWidth);
            float fNewHeight = (float)(fNewWidth / fOutputFrameRatio);

            if (fNewHeight > ScreenHeight)
            {
              fNewHeight = Math.Min(ImageHeight, ScreenHeight);
              fNewWidth = fNewHeight * fOutputFrameRatio;
            }

            // this shouldnt happen, but just make sure that everything still fits onscreen
            if (fNewWidth > ScreenWidth || fNewHeight > ScreenHeight)
            {
              Log.Error("Original Zoom Mode: 'this shouldnt happen' {0}x{1}", fNewWidth, fNewHeight);
              goto case Type.Normal;
            }

            // Centre the movie
            float iPosY = (ScreenHeight - fNewHeight) / 2;
            float iPosX = (ScreenWidth - fNewWidth) / 2;

            // The original zoom mode ignores cropping parameters:
            rSource = new Rectangle(0, 0, ImageWidth, ImageHeight);
            rDest = new Rectangle((int)iPosX, (int)iPosY, (int)(fNewWidth + 0.5f), (int)(fNewHeight + 0.5f));
          }
          break;

        case Type.LetterBox43:
          {
            // shrink movie 33% vertically
            float fNewWidth = (float)ScreenWidth;
            float fNewHeight = (float)(fNewWidth / fOutputFrameRatio);
            fNewHeight *= (1.0f - 0.33333333333f);

            if (fNewHeight > ScreenHeight)
            {
              fNewHeight = ScreenHeight;
              fNewHeight *= (1.0f - 0.33333333333f);
              fNewWidth = fNewHeight * fOutputFrameRatio;
            }

            // this shouldnt happen, but just make sure that everything still fits onscreen
            if (fNewWidth > ScreenWidth || fNewHeight > ScreenHeight)
            {
              fNewWidth = (float)ImageWidth;
              fNewHeight = (float)ImageHeight;
            }

            // Centre the movie
            float iPosY = (ScreenHeight - fNewHeight) / 2;
            float iPosX = (ScreenWidth - fNewWidth) / 2;

            rSource = new Rectangle(0, 0, ImageWidth, ImageHeight);
            rDest = new Rectangle((int)iPosX, (int)iPosY, (int)(fNewWidth + 0.5f), (int)(fNewHeight + 0.5f));
            AdjustSourceForCropping(ref rSource, cropSettings);
          }
          break;

        case Type.NonLinearStretch:
          {
              Log.Info("NLS: fSourceFrameRatio: {0}", fSourceFrameRatio);
              Log.Info("NLS: fSourcePixelRatio: {0}", fSourcePixelRatio);
              Log.Info("NLS: Image WxH {0}x{1}", ImageWidth, ImageHeight);
              Log.Info("NLS: croppedImage WxH {0}x{1}", croppedImageWidth, croppedImageHeight);
              Log.Info("NLS: fOutputFrameRatio : {0}", fOutputFrameRatio);
              Log.Info("NLS: fCroppedOutputFrameRatio : {0}", fCroppedOutputFrameRatio);
              Log.Info("NLS: fSourceFrameRatio: {0}", fSourceFrameRatio);

              //If screen is 16:9 do non-linear smart stretch, otherwise panscan
              float fScreenRatio = (float)ScreenWidth / ScreenHeight;
              fScreenRatio *= PixelRatio;
              Log.Info("NLS: fScreenRatio: {0}", fScreenRatio);

              if (fScreenRatio < 1.6)
              {
                  // we have a 4:3 monitor
                  // panscan
                  // assume that the movie is widescreen first, so use full height
                  float fVertBorder = 0;
                  float fNewHeight = (float)(ScreenHeight);
                  float fNewWidth = fNewHeight * fOutputFrameRatio * 1.66666666667f;
                  float fHorzBorder = (fNewWidth - (float)ScreenWidth) / 2.0f;
                  float fFactor = fNewWidth / ((float)ImageWidth);
                  fFactor *= PixelRatio;
                  fHorzBorder = fHorzBorder / fFactor;
                  fHorzBorder = fHorzBorder / fFactor;

                  if ((int)fNewWidth < ScreenWidth)
                  {
                      fHorzBorder = 0;
                      fNewWidth = (float)(ScreenWidth);
                      fNewHeight = fNewWidth / fOutputFrameRatio;
                      fVertBorder = (fNewHeight - (float)ScreenHeight) / 2.0f;
                      fFactor = fNewWidth / ((float)ImageWidth);
                      fFactor *= PixelRatio;
                      fVertBorder = fVertBorder / fFactor;
                      fVertBorder = fVertBorder / fFactor;
                  }

                  rSource = new Rectangle((int)fHorzBorder,
                                          (int)fVertBorder,
                                          (int)((float)ImageWidth - 2.0f * fHorzBorder),
                                          (int)((float)ImageHeight - 2.0f * fVertBorder));
                  rDest = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
                  AdjustSourceForCropping(ref rSource, cropSettings);
              }
              else
              {
                  // we have a widescreen monitor
                  int newTop;
                  int newHeight;

                  if (fCroppedOutputFrameRatio < 1.5f)
                  {
                      // 4:3 or similar aspect ratio --> perform NLS
                      m_bUseNonLinearStretch = true;
                      newTop = cropSettings.Top + (int)((1.0f - 1.0f / nlsZoomY) * ((float)croppedImageHeight) * (1.0f - nlsVertPos));
                      newHeight = (int)(((float)croppedImageHeight) * (1.0f / nlsZoomY));
                      rSource = new System.Drawing.Rectangle(cropSettings.Left, newTop, croppedImageWidth, newHeight);
                      rDest = new System.Drawing.Rectangle(0, 0, ScreenWidth, ScreenHeight);
                  }
                  else
                  {
                      // image is wider than 4.5:3 
                      // calculate 'wideness' of the image related to the screen, this indicates how good the image fits the screen
                      float wideness = fCroppedOutputFrameRatio / fScreenRatio;
                      float fa = 0.9f;
                      float fb = 1.1f;
                      float fc = 1.3f;
                      Log.Debug("NLS: image wideness: {0}", wideness);
                      //For 16:10 screens the cropping should not be done, which fixes the aspect ratio issue within tolerable accuracy
                      if ((fScreenRatio < 1.61) && (fScreenRatio > 1.59))
                      {
                          newTop = cropSettings.Top;
                          newHeight = (int)croppedImageHeight;
                      }
                      if (wideness <= fa)
                      {
                          // screen is wider than the image
                          // fit width, allow some top and bottom cropping
                          float fNewHeight = (float)croppedImageWidth / fScreenRatio * fSourcePixelRatio;

                          // Centre the movie
                          float iPosY = System.Math.Max((croppedImageHeight - fNewHeight) / 2.0f, 0.0f);

                          rSource = new System.Drawing.Rectangle(cropSettings.Left, (int)iPosY + cropSettings.Top, croppedImageWidth, (int)fNewHeight);
                          rDest = new System.Drawing.Rectangle(0, 0, ScreenWidth, ScreenHeight);
                      }
                      else if (wideness > fa && wideness <= fb)
                      {
                          // screen and image aspect are similar
                          // allow stretching
                          rSource = new System.Drawing.Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
                          rDest = new System.Drawing.Rectangle(0, 0, ScreenWidth, ScreenHeight);
                      }
                      else if (wideness > fb && wideness <= fc)
                      {
                          // image is wider than the screen
                          // allow left and right cropping
                          //float fNewHeight = croppedImageHeight;
                          float fNewWidth = croppedImageHeight * fScreenRatio / fSourcePixelRatio;

                          // Centre the movie
                          // float iPosY = (ScreenHeight - fNewHeight) / 2;
                          float iPosX = System.Math.Max(((float)croppedImageWidth - fNewWidth) / 2.0f, 0.0f);

                          rSource = new System.Drawing.Rectangle((int)iPosX, cropSettings.Top, (int)fNewWidth, croppedImageHeight);
                          rDest = new System.Drawing.Rectangle(0, 0, ScreenWidth, ScreenHeight);
                      }
                      else
                      {
                          // image is much wider than the screen
                          // allow some left and right cropping and some letterboxing 
                          float q = (wideness - 1.0f) / 2.0f + 1.0f;
                          float fNewWidth = (float)ScreenWidth * q;
                          float fNewHeight = (float)(fNewWidth / fCroppedOutputFrameRatio);
                          fNewWidth = ScreenWidth;

                          // Centre the movie
                          float iPosY = (ScreenHeight - fNewHeight) / 2;
                          float iPosX = (ScreenWidth - fNewWidth) / 2;

                          rSource = new System.Drawing.Rectangle(cropSettings.Left + (int)(croppedImageWidth * (q - 1.0f) / 2.0f), cropSettings.Top, (int)(croppedImageWidth / q), croppedImageHeight);
                          rDest = new System.Drawing.Rectangle((int)iPosX, (int)iPosY, (int)(fNewWidth + 0.5f), (int)(fNewHeight + 0.5f));
                      }
                  }
              }
          }
          break;

        case Type.Zoom14to9:
          {
              if (fCroppedOutputFrameRatio * (float)ScreenHeight <= ScreenWidth)
              {
                  Log.Debug("Zoom14to9: screen is wider than image - fall back to NORMAL mode");
                  // maximize the movie width
                  float fNewWidth = (float)ScreenWidth;
                  float fNewHeight = (float)(fNewWidth / fCroppedOutputFrameRatio);

                  if (fNewHeight > ScreenHeight)
                  {
                      fNewHeight = ScreenHeight;
                      fNewWidth = fNewHeight * fCroppedOutputFrameRatio;
                  }

                  // this shouldnt happen, but just make sure that everything still fits onscreen
                  if (fNewWidth > ScreenWidth || fNewHeight > ScreenHeight)
                  {
                      fNewWidth = (float)ImageWidth;
                      fNewWidth = (float)croppedImageWidth;
                      fNewHeight = (float)croppedImageHeight;
                  }

                  // Centre the movie
                  float iPosY = (ScreenHeight - fNewHeight) / 2;
                  float iPosX = (ScreenWidth - fNewWidth) / 2;

                  rSource = new Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
                  rDest = new Rectangle((int)iPosX, (int)iPosY, (int)(fNewWidth + 0.5f), (int)(fNewHeight + 0.5f));

              }
              else
              {
                   Log.Debug("Zoom14to9: image is wider than screen");
                  /*
                   1 crop image left and right
                   2 letterbox top and bottom
                   3 zoom = (normal + zoom)/2
                  */
                  float f1 = (float)ScreenWidth / (float)croppedImageWidth;
                  float f2 = (float)ScreenHeight / (float)croppedImageHeight * fSourcePixelRatio;
                  float f = (f1 + f2) / 2.0f;
                  float fnewIW = (float)croppedImageWidth * f;
                  float fnewIH = (float)fnewIW / fCroppedOutputFrameRatio;
                  float B = (ScreenHeight - fnewIH) / 2.0f; // letterbox in screen coordinates
                  float C = (fnewIW - ScreenWidth) / 2.0f / f; // crop in source image coordinates
                  Log.Debug("Zoom14to9: f1={0}", f1);
                  Log.Debug("Zoom14to9: f2={0}", f2);
                  Log.Debug("Zoom14to9: f ={0}", f);
                  Log.Debug("Zoom14to9: B ={0}", B);
                  Log.Debug("Zoom14to9: C ={0}", C);

                  rSource = new Rectangle(cropSettings.Left + (int)C, cropSettings.Top, croppedImageWidth - (int)(2.0f*C), croppedImageHeight);
                  rDest = new Rectangle(0, 
                                        (int)B, 
                                        ScreenWidth,
                                        ScreenHeight - (int)(2.0f * B));
                  Log.Debug("Zoom14to9: rSource=" + rSource.ToString());
                  Log.Debug("Zoom14to9: rDest  =" + rDest.ToString());
              }
          }
          break;

        default:
          {
            rSource = new Rectangle(0, 0, ImageWidth, ImageHeight);
            rDest = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
          }
          break;
      }
    }

    /// <summary>
    /// Adjusts the source and destination rectangles according to the cropping parameters, maintaining the RATIO between the source and destination
    /// aspect ratio.
    /// 
    /// Note:
    /// Only used for the normal aspect right now, so could as well be coded directly into that zoom mode. 
    /// But maybe it could be useful elsewhere as well and no changes have had to be made to the zoom mode code.
    /// </summary>
    /// <param name="rSource"></param>
    /// <param name="rDest"></param>
    /// <param name="cropSettings"></param>
    /*
        void AdjustForCropping(ref System.Drawing.Rectangle rSource, ref System.Drawing.Rectangle rDest, CropSettings cropSettings, bool strictKeepAspect)
        {
          // temp
          //return;
          float destAspect = rDest.Width / (float)rDest.Height;
          float sourceAspect = rSource.Width / (float)rSource.Height;
          float croppedSourceAspect = (rSource.Width - cropSettings.Right - cropSettings.Left) / (float)(rSource.Height - cropSettings.Top - cropSettings.Bottom);
          float originalAspectChange = destAspect / sourceAspect;

      
          //if (( Math.Abs( destAspect - sourceAspect ) < 0.001 ))
          //  return;

          Log.Debug("AdjustForCropping: rSrc =  " + rSource.ToString());
          Log.Debug("AdjustForCropping: rDest =  " + rDest.ToString());

          float newDestAspect = croppedSourceAspect * originalAspectChange;

          Log.Debug("newDestAspect: " + newDestAspect + " destAspect: " + destAspect);
          Log.Debug("sourceAspect: " + sourceAspect + " croppedSrcAspect: " + croppedSourceAspect);

          if (newDestAspect > destAspect)
          {
            Log.Debug("CROPADJUST : DEST NOT WIDE ENOUGH (ie someone cropped top or bottom)");
            // destination needs to be wider

            // width needed to preserve height
            float widthNeeded = rDest.Height * newDestAspect;

            if (widthNeeded > ScreenWidth && strictKeepAspect)
            {
              // decrease height
              Log.Info("CROPADJUST : NOT ENOUGH WIDTH, needs " + widthNeeded);
              // take all the width we can
              rDest.Width = ScreenWidth;
              // and reduce height for the rest
              rDest.Height = (int)(rDest.Width / newDestAspect); ;
              Log.Info("New height : " + rDest.Height);
            }
            else
            {
              Log.Info("CROPADJUST : ENOUGH WIDTH");
              rDest.Width = (int)widthNeeded;
            }
          }
          else if (newDestAspect < destAspect)
          {
            // destination needs to be taller
            Log.Info("CROPADJUST : DEST TOO WIDE (ie someone cropped left or right");

            int heightNeeded = (int)(rDest.Width * newDestAspect);
            int heightIncrease = heightNeeded - rDest.Height;

            Log.Info("HeightNeeded = " + heightNeeded);
            Log.Info("heightIncrease= " + heightIncrease);

            if (heightNeeded > ScreenHeight && strictKeepAspect)
            {
              Log.Info("CROPADJUST: NOT ENOUGH HEIGHT, ADJUSTING WIDTH");
              rDest.Height = ScreenHeight;
              int newWidth = (int)(newDestAspect * rDest.Height);
              rDest.Width = newWidth;
              Log.Debug("rDest.Width now " + newWidth);
            }
            else
            {
              rDest.Height = heightNeeded;
            }
          }

          rDest.Y = ScreenHeight / 2 - rDest.Height / 2;
          rDest.X = ScreenWidth / 2 - rDest.Width / 2;

          if (rDest.X < 0) Log.Error("Geometry.AdjustForCropping miscalculated, produced NEGATIVE X coordinate!");
          if (rDest.Y < 0) Log.Error("Geometry.AdjustForCropping miscalculated, produced NEGATIVE Y coordinate!");

          AdjustSourceForCropping(ref rSource, cropSettings);
        }
        */
    /// <summary>
    /// Adjusts only the source rectangle according to the cropping parameters.
    /// </summary>
    /// <param name="rSource"></param>
    /// <param name="cropSettings"></param>
    private void AdjustSourceForCropping(ref Rectangle rSource, CropSettings cropSettings)
    {
      rSource.Y += cropSettings.Top;
      rSource.Height -= cropSettings.Top;
      rSource.Height -= cropSettings.Bottom;

      rSource.X += cropSettings.Left;
      rSource.Width -= cropSettings.Left;
      rSource.Width -= cropSettings.Right;
    }

    /// <summary>
    /// Calculates the aspect ratio for the current image/video window
    /// <returns>float value containing the aspect ratio
    /// </returns>
    /// </summary>
    private float CalculateFrameAspectRatio()
    {
      return (float)ImageWidth / (float)ImageHeight;
    }
  }
}