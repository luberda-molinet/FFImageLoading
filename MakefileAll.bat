set msbuild="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe"

%msbuild% source/FFImageLoading.Common/FFImageLoading.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.BaitAndSwitch/FFImageLoading.BaitAndSwitch.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Windows/FFImageLoading.Windows.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.WinSL/FFImageLoading.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Touch/FFImageLoading.Touch.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Droid/FFImageLoading.Droid.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed

%msbuild% source/FFImageLoading.Transformations/FFImageLoading.Transformations.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Transformations.Windows/FFImageLoading.Transformations.Windows.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Transformations.WinSL/FFImageLoading.Transformations.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed

%msbuild% source/FFImageLoading.Forms.WinRT/FFImageLoading.Forms.WinRT.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Forms.WinUWP/FFImageLoading.Forms.WinUWP.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Forms.WinSL/FFImageLoading.Forms.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed
%msbuild% source/FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:detailed

nuget pack source/Xamarin.FFImageLoading.nuspec
nuget pack source/Xamarin.FFImageLoading.Transformations.nuspec
nuget pack source/Xamarin.FFImageLoading.Forms.nuspec
