:: (C) Copyright 2014 by Autodesk, Inc.
::
:: Permission to use, copy, modify, and distribute this software in
:: object code form for any purpose and without fee is hereby granted, 
:: provided that the above copyright notice appears in all copies and 
:: that both that copyright notice and the limited warranty and
:: restricted rights notice below appear in all supporting 
:: documentation.
::
:: AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
:: AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
:: MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
:: DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
:: UNINTERRUPTED OR ERROR FREE.

:: Written by Cyrille Fauvel, Autodesk Developer Network (ADN)
:: http://www.autodesk.com/joinadn
:: December 30th, 2013
::
@echo off

SET CSCPATH=%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319
:: SET MSBUILD=%SYSTEMROOT%\Microsoft.NET\Framework64\v4.0.30319
SET MSBUILD=%ProgramFiles(x86)%\MSBuild\%VisualStudioVersion%\bin\amd64

if not exist ".\nuget.exe" powershell -Command "(new-object System.Net.WebClient).DownloadFile('https://nuget.org/nuget.exe', '.\nuget.exe')"

goto real_build

.\nuget.exe install MayaWpfTheme\packages.config -o packages
::if not exist ".\bin" mkdir bin
:: copy packages\Newtonsoft.Json.8.0.3\lib\net45\Newtonsoft.Json.dll bin\Newtonsoft.Json.dll
:: copy packages\RestSharp.105.1.0\lib\net45\RestSharp.dll bin\RestSharp.dll

:real_build
if not exist ".\MayaWpfTheme\bin" mkdir MayaWpfTheme\bin
if not exist ".\MayaWpfTheme\bin\Release" mkdir MayaWpfTheme\bin\Release

:: %CSCPATH%\csc /target:library /out:MayaWpfTheme\bin\Release\MayaTheme.dll /recurse:MayaWpfTheme\*.cs /recurse:MayaWpfTheme\*.xaml
"%MSBUILD%\msbuild" MayaWpfTheme\MayaWpfTheme.csproj /property:Configuration=Release /property:Platform=AnyCpu

.\nuget pack MayaWpfTheme\MayaWpfTheme.csproj -Prop Platform=AnyCPU -Prop Configuration=Release

:publishtonuget
echo .
echo ".\nuget push Autodesk.Maya.Theme.0.1.0.nupkg %NUGETKEY% -Source https://www.nuget.org/api/v2/package"
:: .\nuget push Autodesk.Maya.Theme.0.1.0.nupkg %NUGETKEY% -Source https://www.nuget.org/api/v2/package
