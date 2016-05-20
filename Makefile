all: FFImageLoading Transformations Forms

package:
	nuget pack source/Xamarin.FFImageLoading.nuspec
	nuget pack source/Xamarin.FFImageLoading.Transformations.nuspec
	nuget pack source/Xamarin.FFImageLoading.Forms.nuspec

FFImageLoading:
	xbuild source/FFImageLoading.Common/FFImageLoading.csproj /nologo /p:Configuration=Release /p:BuildingInsideVisualStudio=true /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Touch/FFImageLoading.Touch.csproj /nologo /p:Configuration=Release /p:BuildingInsideVisualStudio=true /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Droid/FFImageLoading.Droid.csproj /nologo /p:Configuration=Release /p:BuildingInsideVisualStudio=true /consoleloggerparameters:ErrorsOnly /verbosity:minimal

Transformations:
	xbuild source/FFImageLoading.Transformations/FFImageLoading.Transformations.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal

Forms:
	xbuild source/FFImageLoading.Forms/FFImageLoading.Forms.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.BaitAndSwitch/FFImageLoading.BaitAndSwitch.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal
	xbuild source/FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj /nologo /p:Configuration=Release /consoleloggerparameters:ErrorsOnly /verbosity:minimal

clean:
	rm -f *.nupkg
	rm -rf */bin
	rm -rf */obj
	rm -rf */*/bin
	rm -rf */*/obj
	rm -rf */*/*/bin
	rm -rf */*/*/obj
