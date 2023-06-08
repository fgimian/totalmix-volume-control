using System;
using System.Collections.Generic;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.InnoSetup;
using Cake.Common.Tools.ReportGenerator;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Coverlet;
using Cake.Frosting;

namespace Build;

#pragma warning disable SA1600 // Elements should be documented.

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .SetToolPath(new DirectoryPath("../").Combine("tools"))
            .InstallTool(new Uri("nuget:?package=GitVersion.CommandLine&version=5.6.11"))
            .InstallTool(new Uri("nuget:?package=ReportGenerator&version=4.8.11"))
            .InstallTool(new Uri("nuget:?package=Tools.InnoSetup&version=6.2.0"))
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public BuildContext(ICakeContext context)
        : base(context)
    {
        // Arguments
        BuildConfiguration = context.Argument<string>("configuration", "Debug");

        // Variables
        ProjectRoot = context.Directory("../");
        SolutionPath = ProjectRoot + context.File($"{ProjectName}.sln");
        InnoSetupScriptPath = ProjectRoot + context.File($"{ProjectName}.iss");
    }

    public string ProjectName { get; } = "TotalMixVC";

    public string CoverageDirectoryName { get; } = ".coverage";

    public string BuildConfiguration { get; }

    public ConvertableDirectoryPath ProjectRoot { get; }

    public ConvertableFilePath SolutionPath { get; }

    public ConvertableFilePath InnoSetupScriptPath { get; }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        return context.Arguments.HasArgument("rebuild");
    }

    public override void Run(BuildContext context)
    {
        context.Log.Information("Deleting bin and obj directories under src");
        context.CleanDirectories(
            new GlobPattern(
                context.ProjectRoot
                + context.Directory("src/**/bin")
                + context.Directory(context.BuildConfiguration)));
        context.CleanDirectories(
            new GlobPattern(context.ProjectRoot + context.Directory("src/**/obj")));
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Restoring NuGet packages");
        context.DotNetCoreRestore(root: context.ProjectRoot);
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Building the solution");
        context.DotNetCoreBuild(
            project: context.SolutionPath,
            settings: new DotNetCoreBuildSettings
            {
                Configuration = context.BuildConfiguration,
                NoRestore = true
            });
    }
}

[TaskName("Test")]
[IsDependentOn(typeof(BuildTask))]
public class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        ConvertableDirectoryPath coveragePath =
            context.ProjectRoot + context.Directory(context.CoverageDirectoryName);

        context.Log.Information("Running unit tests and collecting test coverage with Coverlet");
        context.DotNetCoreTest(
            project: context.SolutionPath,
            settings: new DotNetCoreTestSettings
            {
                Configuration = context.BuildConfiguration,
                Loggers = new string[] { "xunit" },
                Verbosity = DotNetCoreVerbosity.Normal,
                NoBuild = true,
                NoRestore = true
            },
            coverletSettings: new CoverletSettings
            {
                CollectCoverage = true,
                CoverletOutputFormat = CoverletOutputFormat.opencover
            });

        context.Log.Information("Generating coverage report using ReportGenerator");
        context.ReportGenerator(
            pattern: new GlobPattern(
                context.ProjectRoot
                + context.Directory("src/**")
                + context.File("coverage.opencover.xml")),
            targetDir: coveragePath,
            settings: new ReportGeneratorSettings
            {
                ReportTypes = new ReportGeneratorReportType[]
                {
                    ReportGeneratorReportType.Cobertura,
                    ReportGeneratorReportType.lcov,
                    ReportGeneratorReportType.Html
                }
            });
    }
}

[TaskName("Distribute")]
[IsDependentOn(typeof(TestTask))]
public class DistributeTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Publishing a distrutable version of the application");
        context.DotNetCorePublish(
            project:
                context.ProjectRoot
                + context.Directory("src")
                + context.Directory(context.ProjectName),
            settings: new DotNetCorePublishSettings
            {
                Configuration = context.BuildConfiguration,
                Runtime = "win-x64",
                SelfContained = true
            });

        context.Log.Information("Obtaining the application version using GitVersion");
        GitVersion version = context.GitVersion();

        context.Log.Information($"Building the Inno Setup installer for v{version.FullSemVer}");
        context.InnoSetup(
            scriptFile: context.InnoSetupScriptPath,
            settings: new InnoSetupSettings
            {
                OutputDirectory = context.ProjectRoot + context.Directory("artifacts"),
                Defines = new Dictionary<string, string>
                {
                    { "AppVersion", version.FullSemVer },
                    { "AppBuildConfiguration", context.BuildConfiguration }
                }
            });
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(DistributeTask))]
public class DefaultTask : FrostingTask
{
}
