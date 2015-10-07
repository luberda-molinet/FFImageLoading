all: FFImageLoading Transformations Forms

package:
	nuget pack FFImageLoading.nuspec
	nuget pack FFImageLoading.Transformations.nuspec
	nuget pack FFImageLoading.Forms.nuspec

FFImageLoading:
	xbuild FFImageLoading.Common/FFImageLoading-contrib.csproj /p:Configuration=Release
	xbuild FFImageLoading.Touch/FFImageLoading-contrib.Touch.csproj /p:Configuration=Release
	xbuild FFImageLoading.Droid/FFImageLoading-contrib.Droid.csproj /p:Configuration=Release

Transformations:
	xbuild FFImageLoading.Transformations/FFImageLoading.Transformations.csproj /p:Configuration=Release
	xbuild FFImageLoading.Transformations.Touch/FFImageLoading.Transformations.Touch.csproj /p:Configuration=Release
	xbuild FFImageLoading.Transformations.Droid/FFImageLoading.Transformations.Droid.csproj /p:Configuration=Release


Forms:
	xbuild FFImageLoading.Forms/FFImageLoading.Forms.csproj /p:Configuration=Release
	xbuild FFImageLoading.Forms.Touch/FFImageLoading.Forms.Touch.csproj /p:Configuration=Release
	xbuild FFImageLoading.Forms.Droid/FFImageLoading.Forms.Droid.csproj /p:Configuration=Release

clean:
	rm -rf */bin
	rm -rf */obj
	rm -rf */*/bin
	rm -rf */*/obj
	rm -rf */*/*/bin
	rm -rf */*/*/obj