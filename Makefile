all: FFImageLoading Transformations Forms

package:
	nuget pack source/Xamarin.FFImageLoading.nuspec
	nuget pack source/Xamarin.FFImageLoading.Transformations.nuspec
	nuget pack source/Xamarin.FFImageLoading.Forms.nuspec

FFImageLoading:
	xbuild source/FFImageLoading.Common/FFImageLoading-contrib.csproj /p:Configuration=Release /p:BuildingInsideVisualStudio=true
	xbuild source/FFImageLoading.Touch/FFImageLoading-contrib.Touch.csproj /p:Configuration=Release /p:BuildingInsideVisualStudio=true
	xbuild source/FFImageLoading.Droid/FFImageLoading-contrib.Droid.csproj /p:Configuration=Release /p:BuildingInsideVisualStudio=true

Transformations:
	xbuild source/FFImageLoading.Transformations/FFImageLoading.Transformations.csproj /p:Configuration=Release
	xbuild source/FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj /p:Configuration=Release
	xbuild source/FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj /p:Configuration=Release


Forms:
	xbuild source/FFImageLoading.Forms/FFImageLoading.Forms.csproj /p:Configuration=Release
	xbuild source/FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj /p:Configuration=Release
	xbuild source/FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj /p:Configuration=Release

clean:
	rm -rf */bin
	rm -rf */obj
	rm -rf */*/bin
	rm -rf */*/obj
	rm -rf */*/*/bin
	rm -rf */*/*/obj
