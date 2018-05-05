#addin nuget:?package=Cake.Android.SdkManager

var TARGET = Argument ("target", Argument ("t", "Default"));
var TAG = EnvironmentVariable ("APPVEYOR_REPO_TAG") == "true";
var BUILD_NUMBER = EnvironmentVariable ("APPVEYOR_BUILD_NUMBER");
var VERSION = EnvironmentVariable ("APPVEYOR_BUILD_VERSION") ?? Argument("version", "0.0.9999");
var NUGET_VERSION = TAG ? VERSION : (VERSION + "-pre");
var CONFIG = Argument("configuration", EnvironmentVariable ("CONFIGURATION") ?? "Release");
var SLN = "./FFImageLoading.sln";

var ANDROID_HOME = EnvironmentVariable ("ANDROID_HOME") ?? Argument ("android_home", "");

Task("Libraries").Does(()=>
{
	NuGetRestore (SLN);
	MSBuild (SLN, c => {
		c.Configuration = CONFIG;
		c.MSBuildPlatform = Cake.Common.Tools.MSBuild.MSBuildPlatform.x86;
	});
});

Task ("AndroidSDK")
	.Does (() =>
{
	Information ("ANDROID_HOME: {0}", ANDROID_HOME);

	var androidSdkSettings = new AndroidSdkManagerToolSettings { 
		SdkRoot = ANDROID_HOME,
		SkipVersionCheck = true
	};

	try { AcceptLicenses (androidSdkSettings); } catch { }

	AndroidSdkManagerInstall (new [] { 
			"platforms;android-26"
		}, androidSdkSettings);
});

Task ("NuGet")
	.IsDependentOn("AndroidSDK")
	.IsDependentOn ("Libraries")
	.Does (() =>
{
    if(!DirectoryExists("./Build/nuget/"))
        CreateDirectory("./Build/nuget");

    ReplaceTextInFiles("./nuget/*.nuspec", "@version", NUGET_VERSION)
    var nuspecFiles = GetFiles("./nuget/*.nuspec");
    foreach(var file in nuspecFiles)
    {
        NuGetPack (file.FullPath, new NuGetPackSettings { 
            Version = NUGET_VERSION,
            OutputDirectory = "./Build/nuget/",
            BasePath = "./"
        });
    }
});

Task ("Default").IsDependentOn("NuGet");

Task ("Clean").Does (() => 
{
	CleanDirectory ("./component/tools/");
	CleanDirectories ("./Build/");
	CleanDirectories ("./**/bin");
	CleanDirectories ("./**/obj");
});

RunTarget (TARGET);