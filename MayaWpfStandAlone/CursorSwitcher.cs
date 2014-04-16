// (C) Copyright 2014 by Autodesk, Inc.
//
// The information contained herein is confidential, proprietary
// to Autodesk, Inc., and considered a trade secret as defined
// in section 499C of the penal code of the State of California.
// Use of this information by anyone other than authorized
// employees of Autodesk, Inc. is granted only under a written
// non-disclosure agreement, expressly prescribing the scope
// and manner of such use.

//- Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
//- http://www.autodesk.com/joinadn
//- January 20th, 2014
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Autodesk.Maya.Samples.MayaWpfStandAlone {
	
	// Cursor switcher utility
	public class CursorSwitcher : IDisposable {
		private Cursor _previousCursor ;

		public CursorSwitcher (Cursor cursor) {
			_previousCursor =Mouse.OverrideCursor ;
			if ( cursor == null )
				Mouse.OverrideCursor =Cursors.Wait ;
			else
				Mouse.OverrideCursor =cursor ;
		}

		public void Dispose () {
			Mouse.OverrideCursor =_previousCursor ;
		}

	}

}
