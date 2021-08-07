using System;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.ReportGenerator;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Coverlet;
using Cake.Frosting;

// Ignore all warnings relating to namespaces as the build program doesn't need one.
#pragma warning disable CA1050 // Declare types in namespaces.
#pragma warning disable RCS1110 // Declare type inside namespace.
#pragma warning disable RCS1060 // Declare each type in a separate file.
#pragma warning disable S3903 // Types should be defined in named namespaces.
#pragma warning disable SA1402 // File may only contain a single type.

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .SetToolPath(new DirectoryPath("../").Combine("tools"))
            .InstallTool(new Uri("nuget:?package=ReportGenerator&version=4.8.11"))
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
    }

    public string ProjectName { get; } = "TotalMixVC";

    public string CoverageDirectoryName { get; } = ".coverage";

    public string LcovReportFileName { get; } = "lcov.info";

    public string BuildConfiguration { get; }

    public ConvertableDirectoryPath ProjectRoot { get; }

    public ConvertableFilePath SolutionPath { get; }
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
        context.Log.Information("Deleting bin and obj directories under source");
        context.CleanDirectories(
            new GlobPattern(
                context.ProjectRoot
                + context.Directory("source/**/bin")
                + context.Directory(context.BuildConfiguration)));
        context.CleanDirectories(
            new GlobPattern(context.ProjectRoot + context.Directory("source/**/obj")));
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
        ConvertableFilePath lcovReportPath =
            coveragePath + context.File(context.LcovReportFileName);

        context.Log.Information("Running unit tests and collecting test coverage with Coverlet");
        context.DotNetCoreTest(
            project: context.SolutionPath,
            settings: new DotNetCoreTestSettings
            {
                Configuration = context.BuildConfiguration,
                Verbosity = DotNetCoreVerbosity.Normal,
                NoBuild = true,
                NoRestore = true
            },
            coverletSettings: new CoverletSettings
            {
                CollectCoverage = true,
                CoverletOutputFormat = CoverletOutputFormat.lcov,
                CoverletOutputDirectory = coveragePath,
                CoverletOutputName = context.LcovReportFileName
            });

        context.Log.Information("Generating coverage report using ReportGenerator");
        context.ReportGenerator(
            report: lcovReportPath,
            targetDir: coveragePath,
            settings: new ReportGeneratorSettings
            {
                ReportTypes = new[] { ReportGeneratorReportType.Html }
            });
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(TestTask))]
public class DefaultTask : FrostingTask
{
}
