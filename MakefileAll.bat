@echo off

set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe"
set config=Windows Release
set platform=AnyCPU
set warnings=;1591;
if "%CI%"=="True" (
    set logger=/l:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
)
set buildargs=/p:Configuration="%config%" /p:Platform="%platform%" /p:NoWarn="%warnings%" /v:minimal %logger%

echo Restoring NuGets...

nuget restore

echo Building FFImageLoading...

%msbuild% source/FFImageLoading.Common/FFImageLoading.csproj %buildargs%
%msbuild% source/FFImageLoading.BaitAndSwitch/FFImageLoading.BaitAndSwitch.csproj %buildargs%
%msbuild% source/FFImageLoading.Windows/FFImageLoading.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.WinSL/FFImageLoading.WinSL.csproj %buildargs%
%msbuild% source/FFImageLoading.Touch/FFImageLoading.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Droid/FFImageLoading.Droid.csproj %buildargs%

echo Building FFImageLoading.Transformations...

%msbuild% source/FFImageLoading.Transformations/FFImageLoading.Transformations.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Windows/FFImageLoading.Transformations.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.WinSL/FFImageLoading.Transformations.WinSL.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj %buildargs%

echo Building FFImageLoading.Forms...

%msbuild% source/FFImageLoading.Forms.WinRT/FFImageLoading.Forms.WinRT.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.WinUWP/FFImageLoading.Forms.WinUWP.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.WinSL/FFImageLoading.Forms.WinSL.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj %buildargs%

echo Packaging NuGets...

nuget pack source/Xamarin.FFImageLoading.nuspec
nuget pack source/Xamarin.FFImageLoading.Transformations.nuspec
nuget pack source/Xamarin.FFImageLoading.Forms.nuspec

echo All done.
