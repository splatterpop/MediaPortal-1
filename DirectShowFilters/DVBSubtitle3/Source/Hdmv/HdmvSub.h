/* 
 * $Id: HdmvSub.h 939 2008-12-22 21:31:24Z casimir666 $
 *
 * (C) 2006-2007 see AUTHORS
 *
 * This file is part of mplayerc.
 *
 * Mplayerc is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 *
 * Mplayerc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 */

#pragma once

typedef unsigned __int64 uint64_t;

#include "Rasterizer.h"
#include "..\dvbsubs\subtitle.h"
#include "..\SubdecoderObserver.h"

class CGolombBuffer;

class CHdmvSub
{
public:

	static const REFERENCE_TIME INVALID_TIME = _I64_MIN;

  typedef DWORD   COLORREF;
  typedef DWORD   *LPCOLORREF;

	enum HDMV_SEGMENT_TYPE
	{
		NO_SEGMENT			= 0xFFFF,
		PALETTE				= 0x14,
		OBJECT				= 0x15,
		PRESENTATION_SEG	= 0x16,
		WINDOW_DEF			= 0x17,
		INTERACTIVE_SEG		= 0x18,
		END_OF_DISPLAY		= 0x80,
		HDMV_SUB1			= 0x81,
		HDMV_SUB2			= 0x82
	};

	
	struct VIDEO_DESCRIPTOR
	{
		SHORT		nVideoWidth;
		SHORT		nVideoHeight;
		BYTE		bFrameRate;		// <= Frame rate here!
	};

	struct COMPOSITION_DESCRIPTOR
	{
		SHORT		nNumber;
		BYTE		bState;
	};

	struct SEQUENCE_DESCRIPTOR
	{
		BYTE		bFirstIn  : 1;
		BYTE		bLastIn	  : 1;
		BYTE		bReserved : 8;
	};

	struct HDMV_PALETTE
	{
		BYTE		entry_id;
		BYTE		Y;
		BYTE		Cr;
		BYTE		Cb;
		BYTE		T;
	};

	class CompositionObject : Rasterizer
	{
	public :
		SHORT				m_object_id_ref;
		BYTE				m_window_id_ref;
		bool				m_object_cropped_flag;
		bool				m_forced_on_flag;
		BYTE				m_version_number;

		SHORT				m_horizontal_position;
		SHORT				m_vertical_position;
		SHORT				m_width;
		SHORT				m_height;

		SHORT				m_cropping_horizontal_position;
		SHORT				m_cropping_vertical_position;
		SHORT				m_cropping_width;
		SHORT				m_cropping_height;

		REFERENCE_TIME		m_rtStart;
		REFERENCE_TIME		m_rtStop;

        BYTE                m_nObjectNumber;

		CompositionObject();
		~CompositionObject();

		void				SetRLEData(BYTE* pBuffer, int nSize, int nTotalSize);
		void				AppendRLEData(BYTE* pBuffer, int nSize);
		int					GetRLEDataSize()  { return m_nRLEDataSize; };
		bool				IsRLEComplete() { return m_nRLEPos >= m_nRLEDataSize; };
		void				Render(SubPicDesc& spd, CSubtitle &pSubtitle);
		void				WriteSeg (SubPicDesc& spd, SHORT nX, SHORT nY, SHORT nCount, SHORT nPaletteIndex);
		void				SetPalette (int nNbEntry, HDMV_PALETTE* pPalette, bool bIsHD);
        bool                HavePalette() { return m_nColorNumber > 0; }

	private :
		CHdmvSub*	m_pSub;
		BYTE*		m_pRLEData;
		int			m_nRLEDataSize;
		int			m_nRLEPos;
		int			m_nColorNumber;
		long		m_Colors[256];
	};

	CHdmvSub();
	~CHdmvSub();

  HRESULT ParsePES(const unsigned char* data, int len,
                    const unsigned char* header, int headerLen);

	int				GetActiveObjects()  { return m_pObjects.GetCount(); };

	POSITION		GetStartPosition(REFERENCE_TIME rt, double fps);
	POSITION		GetNext(POSITION pos) { m_pObjects.GetNext(pos); return pos; };

  void SetObserver( MSubdecoderObserver* pObserver );
  void RemoveObserver( MSubdecoderObserver* pObserver );
 
  CSubtitle* GetSubtitle( unsigned int place );
  CSubtitle* GetLatestSubtitle();
  int GetSubtitleCount();
  void ReleaseOldestSubtitle();

	REFERENCE_TIME	GetStart(POSITION nPos)	
	{
		CompositionObject*	pObject = m_pObjects.GetAt(nPos);
		return pObject!=NULL ? pObject->m_rtStart : INVALID_TIME; 
	};
	REFERENCE_TIME	GetStop(POSITION nPos)	
	{ 
		CompositionObject*	pObject = m_pObjects.GetAt(nPos);
		return pObject!=NULL ? pObject->m_rtStop : INVALID_TIME; 
	};

	void			Render(CompositionObject* pObject, SubPicDesc& spd, REFERENCE_TIME rt, RECT& bbox, CSubtitle &pSubtitle);
	HRESULT			GetTextureSize (POSITION pos, SIZE& MaxTextureSize, SIZE& VideoSize, POINT& VideoTopLeft);
	void			Reset();

private :


  uint64_t Get_pes_pts (const unsigned char* buf);

	HDMV_SEGMENT_TYPE m_nCurSegment;
	BYTE*             m_pSegBuffer;
	int               m_nTotalSegBuffer;
	int               m_nSegBufferPos;
	int               m_nSegSize;

	VIDEO_DESCRIPTOR  m_VideoDescriptor;

	CompositionObject*  m_pCurrentObject;
	CAtlList<CompositionObject*>  m_pObjects;
	HDMV_PALETTE*     m_pDefaultPalette;
	int               m_nDefaultPaletteNbEntry;

	int         m_nColorNumber;

  void        CreateSubtitle();

	int					ParsePresentationSegment(CGolombBuffer* pGBuffer);
	void				ParsePalette(CGolombBuffer* pGBuffer, USHORT nSize);
	void				ParseObject(CGolombBuffer* pGBuffer, USHORT nUnitSize);

	void				ParseVideoDescriptor(CGolombBuffer* pGBuffer, VIDEO_DESCRIPTOR* pVideoDescriptor);
	void				ParseCompositionDescriptor(CGolombBuffer* pGBuffer, COMPOSITION_DESCRIPTOR* pCompositionDescriptor);
	void				ParseCompositionObject(CGolombBuffer* pGBuffer, CompositionObject* pCompositionObject);

	void				AllocSegment(int nSize);

	CompositionObject*	FindObject(REFERENCE_TIME rt);

  static COLORREF YCrCbToRGB_Rec601(BYTE Y, BYTE Cr, BYTE Cb);
  static COLORREF YCrCbToRGB_Rec709(BYTE Y, BYTE Cr, BYTE Cb);

  MSubdecoderObserver* m_pObserver;
  std::vector<CSubtitle*> m_RenderedSubtitles;
  
  uint64_t m_nextPTS;
};
