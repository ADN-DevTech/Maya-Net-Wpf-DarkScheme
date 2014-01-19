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
//- December 30th, 2013
//
using System;
using Autodesk.Maya;
using Autodesk.Maya.Runtime;
using Autodesk.Maya.OpenMaya;

[assembly: MPxCommandClass (typeof (MayaWpfThemeTest.WpfThemeTestCmd), "WpfThemeTest")]

namespace MayaWpfThemeTest {

	// This class is instantiated by Maya each time when a command 
	// is called by the user or a script.
	public class WpfThemeTestCmd : MPxCommand, IMPxCommand {
		public WpfThemeTestWindow wnd;

		public override void doIt (MArgList argl) {
			wnd = new WpfThemeTestWindow ();
			//MayaTheme.SetMayaIcon (wnd);
			wnd.Show ();
		}

	}

}
