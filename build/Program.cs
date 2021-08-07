using System;
using System.Collections.Generic;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.Publish;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Common.Tools.InnoSetup;
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
#pragma warning disable SA1600 // Elements should be documented.

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .SetToolPath(new DirectoryPath("../").Combine("tools"))
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
        CoverageFormat = context.Argument<string>("coverage-format", "lcov");

        // Variables
        ProjectRoot = context.Directory("../");
        SolutionPath = ProjectRoot + context.File($"{ProjectName}.sln");
        GUIProjectName = context.Directory($"{ProjectName}.GUI");

        if (string.Compare(CoverageFormat, "cobertura", ignoreCase: true) == 0)
        {
            CoverletOutputFormat = CoverletOutputFormat.cobertura;
            CoverageReportFileName = "cobertura.xml";
        }
        else
        {
            CoverletOutputFormat = CoverletOutputFormat.lcov;
            CoverageReportFileName = "lcov.info";
        }

        InnoSetupScriptPath = ProjectRoot + context.File($"{ProjectName}.iss");
    }

    public string ProjectName { get; } = "TotalMixVC";

    public string CoverageDirectoryName { get; } = ".coverage";

    public string BuildConfiguration { get; }

    public string CoverageFormat { get; }

    public ConvertableDirectoryPath ProjectRoot { get; }

    public ConvertableFilePath SolutionPath { get; }

    public ConvertableDirectoryPath GUIProjectName { get; }

    public CoverletOutputFormat CoverletOutputFormat { get; }

    public string CoverageReportFileName { get; }

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
        ConvertableFilePath coverageReportPath =
            coveragePath + context.File(context.CoverageReportFileName);

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
                CoverletOutputFormat = context.CoverletOutputFormat,
                CoverletOutputDirectory = coveragePath,
                CoverletOutputName = context.CoverageReportFileName
            });

        context.Log.Information("Generating coverage report using ReportGenerator");
        context.ReportGenerator(
            report: coverageReportPath,
            targetDir: coveragePath,
            settings: new ReportGeneratorSettings
            {
                ReportTypes = new ReportGeneratorReportType[] { ReportGeneratorReportType.Html }
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
            project: context.ProjectRoot + context.Directory("source") + context.GUIProjectName,
            settings: new DotNetCorePublishSettings
            {
                Configuration = context.BuildConfiguration
            });

        context.Log.Information("Building the Inno Setup installer");
        context.Log.Information(context.InnoSetupScriptPath);
        context.InnoSetup(
            scriptFile: context.InnoSetupScriptPath,
            settings: new InnoSetupSettings
            {
                OutputDirectory = context.ProjectRoot + context.Directory("artifacts"),
                Defines = new Dictionary<string, string>
                {
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
