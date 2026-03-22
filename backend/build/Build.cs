using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// NUKE build orchestration for DevOpsSite backend.
///
/// Targets:
///   BuildAll       — Restore + build entire solution (Release by default)
///   BuildProject   — Build a single project by name
///   TestUnit       — Run all unit tests, quiet except failures/summary
///   CoverageReport — Run tests with coverage, produce Cobertura + HTML report, print percentage
///
/// Usage:
///   dotnet run --project build/_build.csproj -- BuildAll
///   dotnet run --project build/_build.csproj -- TestUnit
///   dotnet run --project build/_build.csproj -- CoverageReport
///   dotnet run --project build/_build.csproj -- BuildProject --project-name DevOpsSite.Domain
/// </summary>
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildAll);

    [Parameter("Configuration (Debug or Release). Default: Release")]
    readonly string Configuration = "Release";

    [Parameter("Project name for BuildProject target (e.g., DevOpsSite.Domain)")]
    readonly string ProjectName;

    [Solution] readonly Solution Solution;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";
    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "testresults";

    Target Clean => _ => _
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    /// <summary>
    /// Restore + build the entire solution. Release by default. Fails on errors.
    /// </summary>
    Target BuildAll => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .EnableNoLogo());
        });

    /// <summary>
    /// Build one specific project by name. Fails clearly if the project is not found.
    /// Usage: --project-name DevOpsSite.Domain
    /// </summary>
    Target BuildProject => _ => _
        .DependsOn(Restore)
        .Requires(() => ProjectName)
        .Executes(() =>
        {
            var project = Solution.GetAllProjects(ProjectName).FirstOrDefault();
            Assert.NotNull(project,
                $"Project '{ProjectName}' not found in solution. " +
                $"Available projects: {string.Join(", ", Solution.AllProjects.Select(p => p.Name))}");

            DotNetBuild(s => s
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .EnableNoLogo());
        });

    /// <summary>
    /// Run all unit tests. Quiet except failures and summary.
    /// </summary>
    Target TestUnit => _ => _
        .DependsOn(BuildAll)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetLoggers("console;verbosity=minimal"));
        });

    /// <summary>
    /// Run tests with coverage, produce Cobertura XML + HTML report under artifacts/coverage/,
    /// and print the aggregate line coverage percentage.
    /// </summary>
    Target CoverageReport => _ => _
        .DependsOn(BuildAll)
        .Executes(() =>
        {
            CoverageDirectory.CreateOrCleanDirectory();
            TestResultsDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("console;verbosity=minimal")
                .SetDataCollector("XPlat Code Coverage")
                .SetSettingsFile(RootDirectory / "build" / "coverlet.runsettings"));

            // Find all coverage files
            var coverageFiles = TestResultsDirectory
                .GlobFiles("**/coverage.cobertura.xml")
                .ToList();

            if (coverageFiles.Count == 0)
            {
                Serilog.Log.Warning("No coverage files found");
                return;
            }

            Serilog.Log.Information("Found {Count} coverage file(s)", coverageFiles.Count);

            // Merge and generate reports using ReportGenerator (local tool)
            var reportDir = CoverageDirectory / "report";

            DotNet($"tool restore", workingDirectory: RootDirectory);
            DotNet(
                $"reportgenerator " +
                $"\"-reports:{string.Join(";", coverageFiles)}\" " +
                $"\"-targetdir:{reportDir}\" " +
                $"\"-reporttypes:Cobertura;HtmlSummary;TextSummary\" " +
                $"\"-verbosity:Warning\"",
                workingDirectory: RootDirectory);

            // Copy merged Cobertura to predictable path
            var mergedCobertura = reportDir / "Cobertura.xml";
            if (mergedCobertura.FileExists())
            {
                mergedCobertura.Copy(CoverageDirectory / "merged.cobertura.xml");
            }

            // Print text summary
            var summaryFile = reportDir / "Summary.txt";
            if (summaryFile.FileExists())
            {
                Serilog.Log.Information("");
                foreach (var line in File.ReadAllLines(summaryFile))
                {
                    Serilog.Log.Information("  {Line}", line);
                }
            }

            // Extract and print final coverage percentage
            var coberturaFile = CoverageDirectory / "merged.cobertura.xml";
            if (coberturaFile.FileExists())
            {
                var doc = XDocument.Load(coberturaFile);
                var lineRate = doc.Root?.Attribute("line-rate")?.Value;
                if (double.TryParse(lineRate, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var rate))
                {
                    var pct = rate * 100;
                    Serilog.Log.Information("");
                    Serilog.Log.Information("========================================");
                    Serilog.Log.Information("  Coverage: {Pct:F2}%", pct);
                    Serilog.Log.Information("========================================");
                    Serilog.Log.Information("");
                    Serilog.Log.Information("  artifacts/coverage/merged.cobertura.xml");
                    Serilog.Log.Information("  artifacts/coverage/report/");
                }
            }
        });
}
