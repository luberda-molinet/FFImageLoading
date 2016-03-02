"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Common/FFImageLoading.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal

"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Windows/FFImageLoading.Windows.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal
"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.WinSL/FFImageLoading.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal

"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Transformations.Windows/FFImageLoading.Transformations.Windows.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal
"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Transformations.WinSL/FFImageLoading.Transformations.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal

"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Forms.WinRT/FFImageLoading.Forms.WinRT.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal
"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Forms.WinUWP/FFImageLoading.Forms.WinUWP.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal
"C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe" source/FFImageLoading.Forms.WinSL/FFImageLoading.Forms.WinSL.csproj /nologo /p:Configuration="Windows Release" /consoleloggerparameters:ErrorsOnly /verbosity:minimal

pause