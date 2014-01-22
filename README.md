Copyright (c) Autodesk, Inc. All rights reserved 

Maya .Net API - Wpf Dark Scheme
by Cyrille Fauvel - Autodesk Developer Network (ADN)
January 2014

Permission to use, copy, modify, and distribute this software in
object code form for any purpose and without fee is hereby granted, 
provided that the above copyright notice appears in all copies and 
that both that copyright notice and the limited warranty and
restricted rights notice below appear in all supporting 
documentation.

AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
UNINTERRUPTED OR ERROR FREE.
 
 
Maya-Net-Wpf-DarkScheme
=======================
Maya .Net Dark Scheme for WPF
[see also the blog post here](http://around-the-corner.typepad.com/adn/2014/01/applying-the-maya-dark-color-scheme-to-wpf.html)


The MayaTheme assembly provides the XAML definition to replicate the Maya Dark Color Scheme on WPF Window/Control.
It is not error proof, so feel free to contact me to get them fixed.

The code is designed to change all WPF window/controls color scheme at once and at runtime. That means all already running 
windows will be affected as well. You just need to initialize the system in your C# plug-in (or standalone application) using this code

	MayaTheme.Initialize (null);
	
The MayaWpfTheme assembly has no dependency on Maya so it can run anywhere. There was the option to bind the WPF resources
to QT in real-time, but the performance was not good. Instead this assembly does provide a definition of the Maya Dark Scheme and will
not take into account changes you made in Maya. You can still apply your color changes at runtime by merging a new dictionary after the
Maya Theme is loaded and initialized. You can retrieve the Maya Theme dictionary using

	Application.ResourceAssembly = typeof (MayaTheme).Assembly;

Due to a [bug in WPF](http://stackoverflow.com/questions/2642220/wpf-window-style-not-working-at-runtime), there is no way to
redefine the Window style. Instead, you will need to either modify the icon and background color by code, or to include a style or 
background statement  in your XAML window definition.

The XAML options
------------------------
a) the background option

	`<Window x:Class="MayaWpfThemeTest.WpfThemeTestWindow"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		Title="Maya Theme Test" 
		Height="900"
		Width="656"
		Background="{DynamicResource WindowBrush}"
		>

b) the style option (preferred)

	`<Window x:Class="MayaWpfThemeTest.WpfThemeTestWindow"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		Title="Maya Theme Test" 
		Height="900"
		Width="656"
		Style="{DynamicResource MayaStyle}"
		>`

The Code approach
---------------------------
c) changing the background color

		WpfThemeTestWindow wnd;
		wnd = new WpfThemeTestWindow ();
		var bc = new BrushConverter();
		wnd.Background =(Brush)bc.ConvertFrom("#FF444444");
		wnd.Show ();

d) changing the window icon for the Maya icon (optional)

		WpfThemeTestWindow wnd;
		wnd = new WpfThemeTestWindow ();
		MayaTheme.SetMayaIcon (wnd);
		wnd.Show ();


The DAG Explorer samples
========================

These 2 samples (MayaWpfPlugin and MayaWpfStandalone) are a reworked version of the Maya devkit plug-in wpfexamples.
The plug-in version was enhanced to preview multiple objects, with camera, shaders and lights. The standalone version 
is to demonstrate how to write a Maya standalone application using the Maya .NET API (they both use the Maya 
Dark Scheme assembly present on this repo).

For the standalone sample.
<b>*Important!*</b> because Maya .NET assemblies aren;t signed and not in the GAC the executable should be in 
the Maya directory where resides the Maya API .NET assemblies (I.e. openmayacpp.dll / openmayacs.dll).
For example, create a PostBuild event like this:

    copy "$(TargetPath)" "C:\Program Files\Autodesk\Maya2014\bin"
	

--------
Written by Cyrille Fauvel (Autodesk Developer Network)  
http://www.autodesk.com/adn  
http://around-the-corner.typepad.com/  

<b>Note:</b> Maya 2014 requires using the VC 10.0 Service Pack 1 runtime and the .Net Framework 4.0. You can use Visual Studio 2012/2013 to develop C# plug-ins, but have to use the .Net 4.0 framework.
