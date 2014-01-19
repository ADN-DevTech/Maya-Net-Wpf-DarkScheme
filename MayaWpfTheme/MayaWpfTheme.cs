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
//- January 6th, 2014
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Autodesk.Maya {

	public class MayaTheme {
		public static Application _app = null;

		public static bool Initialize (Application app) {
			 if ( _app == null && app == null )
				_app = new App ();
			else if ( app != null )
				_app = app ;

			if ( Application.ResourceAssembly == null )
				Application.ResourceAssembly = typeof (MayaTheme).Assembly;

			return (true);
		}

		public static bool SetMayaIcon (Window window) {
			//string [] test =typeof (MayaTheme).Assembly.GetManifestResourceNames () ;
			// Need to be an embedded resources
			System.IO.Stream file =typeof (MayaTheme).Assembly.GetManifestResourceStream ("MayaTheme.Resources.maya.ico") ;
			var icon =BitmapFrame.Create (file) ;
			window.Icon =icon ;
			return (true) ;
		}

	}

}
