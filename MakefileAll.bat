set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
set config=Release
set platform=AnyCPU
set warnings=1591,1572,1573,1570,1000,1587
if "%CI%"=="True" (
    set logger=/l:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
)
set buildargs=/p:Configuration="%config%" /p:Platform="%platform%" /p:NoWarn="%warnings%" /v:minimal %logger%
set buildargsTests=/p:Configuration="Debug" /p:Platform="%platform%" /p:NoWarn="%warnings%" /v:minimal %logger%

echo Restoring NuGets...

nuget restore -MsbuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin"
dotnet restore

echo Building FFImageLoading...

%msbuild% source/FFImageLoading.Common/FFImageLoading.csproj %buildargs%
%msbuild% source/FFImageLoading.Mock/FFImageLoading.Mock.csproj %buildargs%
%msbuild% source/FFImageLoading.Windows/FFImageLoading.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.Touch/FFImageLoading.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Mac/FFImageLoading.Mac.csproj %buildargs%
%msbuild% source/FFImageLoading.Droid/FFImageLoading.Droid.csproj %buildargs%
%msbuild% source/FFImageLoading.Tizen/FFImageLoading.Tizen.csproj %buildargs%

echo Building FFImageLoading.Transformations...

%msbuild% source/FFImageLoading.Transformations/FFImageLoading.Transformations.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Windows/FFImageLoading.Transformations.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Mac/FFImageLoading.Transformations.Mac.csproj %buildargs%
%msbuild% source/FFImageLoading.Transformations.Tizen/FFImageLoading.Transformations.Tizen.csproj %buildargs%

echo Building FFImageLoading.Forms...

%msbuild% source/FFImageLoading.Forms.Mock/FFImageLoading.Forms.Mock.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.WinRT/FFImageLoading.Forms.WinRT.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.WinUWP/FFImageLoading.Forms.WinUWP.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Mac/FFImageLoading.Forms.Mac.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj %buildargs%
%msbuild% source/FFImageLoading.Forms.Tizen/FFImageLoading.Forms.Tizen.csproj %buildargs%

echo Building FFImageLoading.Svg...

%msbuild% source/FFImageLoading.Svg/FFImageLoading.Svg.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Touch/FFImageLoading.Svg.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Droid/FFImageLoading.Svg.Droid.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Windows/FFImageLoading.Svg.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Mac/FFImageLoading.Svg.Mac.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Tizen/FFImageLoading.Svg.Tizen.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms/FFImageLoading.Svg.Forms.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms.Touch/FFImageLoading.Svg.Forms.Touch.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms.Droid/FFImageLoading.Svg.Forms.Droid.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms.Windows/FFImageLoading.Svg.Forms.Windows.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms.Mac/FFImageLoading.Svg.Forms.Mac.csproj %buildargs%
%msbuild% source/FFImageLoading.Svg.Forms.Tizen/FFImageLoading.Svg.Forms.Tizen.csproj %buildargs%

echo Building Unit tests...

%msbuild% source/Tests/FFImageLoading.Tests/FFImageLoading.Tests.csproj %buildargs%

echo Packaging NuGets...

nuget pack source/Xamarin.FFImageLoading.nuspec
nuget pack source/Xamarin.FFImageLoading.Transformations.nuspec
nuget pack source/Xamarin.FFImageLoading.Forms.nuspec
nuget pack source/Xamarin.FFImageLoading.Svg.nuspec
nuget pack source/Xamarin.FFImageLoading.Svg.Forms.nuspec

echo All done.
