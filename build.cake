var target = Argument("target", "Default");
var platform = Argument("platform", "AnyCPU");
var configuration = Argument("configuration", "Release");

var artifactsDir = (DirectoryPath)Directory("./artifacts");
var zipRootDir = artifactsDir.Combine("zips");

var fileZipSuffix = ".zip";

var netCoreAppsRoot= ".";
var netCoreApp = "ILSpy";

var buildDirs = 
    GetDirectories($"{netCoreAppsRoot}/**/bin/**") + 
    GetDirectories($"{netCoreAppsRoot}/**/obj/**") + 
    GetDirectories($"{netCoreAppsRoot}/artifacts/*");

var netCoreProject = new {
        Path = $"{netCoreAppsRoot}/{netCoreApp}",
        Name = netCoreApp,
        Framework = XmlPeek($"{netCoreAppsRoot}/{netCoreApp}/{netCoreApp}.csproj", "//*[local-name()='TargetFramework']/text()"),
        Runtimes = XmlPeek($"{netCoreAppsRoot}/{netCoreApp}/{netCoreApp}.csproj", "//*[local-name()='RuntimeIdentifiers']/text()").Split(';')
    };


 Task("Clean")
 .Does(()=>{
     CleanDirectories(buildDirs);
 });

 Task("Restore-NetCore")
     .IsDependentOn("Clean")
     .Does(() =>
 {
    DotNetRestore(netCoreProject.Path);
 });

 Task("Build-NetCore")
     .IsDependentOn("Restore-NetCore")
     .Does(() =>
 {
    Information("Building: {0}", netCoreProject.Name);
    DotNetBuild(netCoreProject.Path, new DotNetBuildSettings {
        Configuration = configuration
    });
 });

 Task("Publish-NetCore")
     .IsDependentOn("Restore-NetCore")
     .Does(() =>
 {
    foreach(var runtime in netCoreProject.Runtimes)
    {
        var outputDir = artifactsDir.Combine(runtime);

        Information("Publishing: {0}, runtime: {1}", netCoreProject.Name, runtime);
        DotNetPublish(netCoreProject.Path, new DotNetPublishSettings {
            Framework = netCoreProject.Framework,
            Configuration = configuration,
            Runtime = runtime,
            SelfContained = true,
            OutputDirectory = outputDir.FullPath
        });
    }
 });


 Task("Package-Mac")
     .IsDependentOn("Publish-NetCore")
     .Does(() =>
 {
    var runtimeIdentifiers = netCoreProject.Runtimes.Where(r => r.StartsWith("osx"));
    foreach(var runtime in runtimeIdentifiers)
    {
        var workingDir = artifactsDir.Combine(runtime);
        var tempDir = artifactsDir.Combine("app");

        Information("Copying Info.plist");
        EnsureDirectoryExists(tempDir.Combine("Contents"));
        MoveFiles(workingDir.Combine("Info.plist").FullPath, tempDir.Combine("Contents"));

        Information("Copying App Icons");
        EnsureDirectoryExists(tempDir.Combine("Contents/Resources"));
        MoveFiles(workingDir.Combine("ILSpy.icns").FullPath, tempDir.Combine("Contents/Resources"));

        Information("Copying executables");
        MoveDirectory(workingDir, tempDir.Combine("Contents/MacOS"));

        Information("Finish packaging");
        EnsureDirectoryExists(workingDir);
        MoveDirectory(tempDir, workingDir.Combine($"{netCoreProject.Name}.app"));
    }
 });

 /* Task("Zip-NetCore")
     .IsDependentOn("Publish-NetCore")
     .IsDependentOn("Package-Mac")
     .Does(() =>
 {
    EnsureDirectoryExists(zipRootDir);
    foreach(var runtime in netCoreProject.Runtimes)
    {
        var workingDir = artifactsDir.Combine(runtime);
        Information("Zipping {0} artifacts to {1}", runtime, zipRootDir);
        Zip(workingDir.FullPath, zipRootDir.CombineWithFilePath(netCoreProject.Name + "-" + runtime + "-" + configuration + fileZipSuffix));
    }
 }); */

 Task("Default")
     .IsDependentOn("Restore-NetCore")
     .IsDependentOn("Publish-NetCore")
	 .IsDependentOn("Package-Mac")
     /*.IsDependentOn("Zip-NetCore")*/;

 RunTarget(target);