// (C) Copyright 2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.

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

namespace Autodesk.Maya
{
    public class MayaTheme
    {
        public static Application _app = null;

        public static bool Initialize(Application app)
        {
            if (_app == null && app == null)
                _app = new App();
            else if (app != null)
                _app = app;

            if (Application.ResourceAssembly == null)
                Application.ResourceAssembly = typeof(MayaTheme).Assembly;

            return (true);
        }

        public static bool SetMayaIcon(Window window)
        {
            //string [] test =typeof (MayaTheme).Assembly.GetManifestResourceNames () ;
            // Need to be an embedded resources
            System.IO.Stream file = typeof(MayaTheme).Assembly.GetManifestResourceStream("MayaTheme.Resources.maya.ico");
            var icon = BitmapFrame.Create(file);
            window.Icon = icon;
            return (true);
        }
    }
}
