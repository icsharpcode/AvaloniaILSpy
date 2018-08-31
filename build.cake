var target = Argument("target", "Default");
var platform = Argument("platform", "AnyCPU");
var configuration = Argument("configuration", "Release");

var editbin86 = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.14.26428\bin\HostX64\x86\editbin.exe";
var editbin64 = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.14.26428\bin\HostX64\x64\editbin.exe";

var artifactsDir = (DirectoryPath)Directory("./artifacts");
var zipRootDir = artifactsDir.Combine("zips");

var fileZipSuffix = ".zip";

var netCoreAppsRoot= ".";
var netCoreApp = "ILSpy.Avalonia";

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
    DotNetCoreRestore(netCoreProject.Path);
 });

 Task("Build-NetCore")
     .IsDependentOn("Restore-NetCore")
     .Does(() =>
 {
    Information("Building: {0}", netCoreProject.Name);
    DotNetCoreBuild(netCoreProject.Path, new DotNetCoreBuildSettings {
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
        DotNetCorePublish(netCoreProject.Path, new DotNetCorePublishSettings {
            Framework = netCoreProject.Framework,
            Configuration = configuration,
            Runtime = runtime,
            SelfContained = true,
            OutputDirectory = outputDir.FullPath
        });

        if (IsRunningOnWindows() && (runtime == "win7-x86" || runtime == "win7-x64"))
        {
            Information("Patching executable subsystem for: {0}, runtime: {1}", netCoreProject.Name, runtime);

            var editbin = runtime == "win7-x86"? editbin86: editbin64;
            if (FileExists(editbin))
            {
                var targetExe = outputDir.CombineWithFilePath(netCoreProject.Name + ".exe");
                var exitCodeWithArgument = StartProcess(editbin, new ProcessSettings { 
                    Arguments = "/subsystem:windows " + targetExe.FullPath
                });
                Information("The editbin command exit code: {0}", exitCodeWithArgument);
            }
            else
            {
                Information("editbin for {0}, runtime: {1} does not exist", netCoreProject.Name, runtime);
            }
        }
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
        var tempDir = artifactsDir.Combine($"{netCoreProject.Name}.app");

        Information("Copying Info.plist");
        EnsureDirectoryExists(tempDir.Combine("Contents"));
        MoveFiles(workingDir.Combine("Info.plist").FullPath, tempDir.Combine("Contents"));

        Information("Copying App Icons");
        EnsureDirectoryExists(tempDir.Combine("Contents/Resources"));
        MoveFiles(workingDir.Combine("Images/ILSpy.icns").FullPath, tempDir.Combine("Contents/Resources"));

        Information("Copying executables");
        EnsureDirectoryExists(tempDir.Combine("Contents/MacOS"));
        MoveFiles(workingDir.FullPath + "/*", tempDir.Combine("Contents/MacOS"));

        CleanDirectory(workingDir);
        MoveDirectory(tempDir, workingDir.Combine($"{netCoreProject.Name}.app"));
        Information("Finish packaging");
    }
 });

 Task("Zip-NetCore")
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
 });

 Task("Default")
     .IsDependentOn("Restore-NetCore")
     .IsDependentOn("Publish-NetCore")
     .IsDependentOn("Zip-NetCore");

 RunTarget(target);