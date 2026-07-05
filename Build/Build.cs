using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tooling.ProcessTasks;

[GitHubActions(
    "ci",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    OnPushTags = ["v*"],
    InvokedTargets = [nameof(All)],
    EnableGitHubToken = true,
    ImportSecrets = [nameof(SonarToken)],
    FetchDepth = 0,
    AutoGenerate = false)]
partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is Release")]
    readonly Configuration Configuration = Configuration.Release;

    [Solution("PopfileNet.sln")] readonly Solution? Solution;
    [GitRepository] readonly GitRepository? GitRepository;
    GitHubActions? Ci => GitHubActions.Instance;

    [Secret] [Parameter] readonly string? SonarToken;

    [Parameter] readonly string Registry = "ghcr.io";
    [Parameter] readonly string? ImageName;

    AbsolutePath SourceDirectory => RootDirectory;
    AbsolutePath TestResultsDirectory => RootDirectory / "TestResults";
    AbsolutePath PublishDirectory => RootDirectory / "publish";
    AbsolutePath AspireOutputDirectory => RootDirectory / "PopfileNet.AppHost" / "aspire-output";

    string SonarProjectKey => "blakharaz_PopfileNet";
    string SonarOrganization => "blakharaz";

    bool IsPush => Ci == null || !Ci.IsPullRequest;
    bool IsPullRequest => Ci is { IsPullRequest: true };
    bool IsMainBranch => GitRepository?.Branch == "main";
    bool IsTag => GitRepository?.Commit.StartsWith("refs/tags/") == true;
    string? TagName => IsTag ? GitRepository!.Commit.Replace("refs/tags/", "") : null;
    string ImageNameValue => (ImageName ?? Ci?.Repository ?? "popfilenet").ToLowerInvariant();
    bool ShouldPushDocker => Ci != null && !Ci.IsPullRequest;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            foreach (var dir in TestResultsDirectory.GlobDirectories("*"))
                dir.DeleteDirectory();
            foreach (var dir in PublishDirectory.GlobDirectories("*"))
                dir.DeleteDirectory();
            AspireOutputDirectory.DeleteDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target SonarBegin => _ => _
        .Before(Compile)
        .OnlyWhenStatic(() => !string.IsNullOrEmpty(SonarToken))
        .Executes(() =>
        {
            StartShell("dotnet tool install --global dotnet-sonarscanner").AssertZeroExitCode();

            var workspace = RootDirectory.ToString();
            var vscoveragePath = (TestResultsDirectory / "UnitTests" / "merged.vscoverage.xml").ToString();

            StartShell($"dotnet sonarscanner begin /k:\"{SonarProjectKey}\" /o:\"{SonarOrganization}\" /d:sonar.token=\"{SonarToken}\" /d:sonar.cs.vscoveragexml.reportsPaths=\"{vscoveragePath}\" /d:sonar.projectBaseDir=\"{workspace}\" /d:sonar.exclusions=\"**/obj/**,**/bin/**,**/*.Tests/**,**/TestResults/**,.github/**,**/*.md,**/Migrations/**\" /d:sonar.coverage.exclusions=\"**/*.Tests/**,**/Migrations/**\"").AssertZeroExitCode();
        });

    Target SonarEnd => _ => _
        .OnlyWhenStatic(() => !string.IsNullOrEmpty(SonarToken))
        .Executes(() =>
        {
            StartShell($"dotnet sonarscanner end /d:sonar.token=\"{SonarToken}\"").AssertZeroExitCode();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(SonarBegin)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true));
        });

    Target TestUnit => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetFilter("FullyQualifiedName~.UnitTests.")
                .SetProcessWorkingDirectory(RootDirectory / "Tests" / "UnitTests")
                .SetSettingsFile(RootDirectory / "Tests" / "UnitTests" / "coverlet.runsettings")
                .SetLoggers(["console;verbosity=minimal"])
                .SetNoBuild(true)
                .AddProperty("CollectCoverage", true)
                .AddProperty("CoverletOutputFormat", "cobertura")
                .AddProperty("Threshold", "0"));
        });

    Target MergeCoverage => _ => _
        .DependsOn(TestUnit)
        .Executes(() =>
        {
            StartShell("dotnet tool install --global dotnet-coverage").AssertZeroExitCode();

            var unitTestResultsDir = TestResultsDirectory / "UnitTests";
            unitTestResultsDir.CreateOrCleanDirectory();

            var coverageFiles = RootDirectory.GlobFiles("**/Tests/UnitTests/*/TestResults/*/coverage.cobertura.xml").ToArray();
            if (coverageFiles.Length == 0)
            {
                Serilog.Log.Warning("No coverage files found, skipping merge");
                return;
            }

            var coverageFilePatterns = string.Join(" ", coverageFiles.Select(x => $"\"{x}\""));

            StartShell($"dotnet-coverage merge {coverageFilePatterns} --output \"{unitTestResultsDir / "merged.vscoverage.xml"}\" --output-format xml", outputFilter: FilterDockerOutput).AssertZeroExitCode();
            StartShell($"dotnet-coverage merge {coverageFilePatterns} --output \"{unitTestResultsDir / "merged.cobertura.xml"}\" --output-format cobertura", outputFilter: FilterDockerOutput).AssertZeroExitCode();
        });

    Target TestIntegration => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(RootDirectory / "Tests" / "IntegrationTests" / "PopfileNet.IntegrationTests.csproj")
                .SetConfiguration(Configuration)
                .SetLoggers(["console;verbosity=minimal"])
                .SetResultsDirectory((TestResultsDirectory / "IntegrationTests").ToString())
                .SetNoBuild(true));
        });

    Target TestFunctional => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            InstallPlaywright();

            DotNetTest(s => s
                .SetProjectFile(RootDirectory / "Tests" / "FunctionalTests" / "PopfileNet.FunctionalTests.csproj")
                .SetConfiguration(Configuration)
                .SetLoggers(["console;verbosity=minimal"])
                .SetResultsDirectory((TestResultsDirectory / "FunctionalTests").ToString())
                .SetNoBuild(true));
        });

    Target CoverageReport => _ => _
        .DependsOn(MergeCoverage)
        .Executes(() =>
        {
            var coverageFile = TestResultsDirectory / "UnitTests" / "merged.cobertura.xml";
            if (!coverageFile.Exists())
            {
                Serilog.Log.Warning("Coverage file not found: {0}", coverageFile);
                return;
            }

            var reportDir = RootDirectory / "report";
            reportDir.CreateOrCleanDirectory();

            StartShell("dotnet tool install --global dotnet-reportgenerator-globaltool").AssertZeroExitCode();
            StartShell($"reportgenerator -reports:\"{coverageFile}\" -targetdir:\"{reportDir}\" -reporttypes:MarkdownSummaryGithub").AssertZeroExitCode();
        });

    Target PrComment => _ => _
        .DependsOn(CoverageReport)
        .OnlyWhenStatic(() => IsPullRequest)
        .Executes(() =>
        {
            var coverageFile = TestResultsDirectory / "UnitTests" / "merged.cobertura.xml";
            if (!coverageFile.Exists())
            {
                Serilog.Log.Warning("Coverage file not found, skipping PR comment");
                return;
            }

            var reportDir = RootDirectory / "report";
            reportDir.CreateOrCleanDirectory();

            StartShell($"reportgenerator -reports:\"{coverageFile}\" -targetdir:\"{reportDir}\" -reporttypes:MarkdownSummaryGithub").AssertZeroExitCode();

            var summaryContent = System.IO.File.ReadAllText(reportDir / "SummaryGithub.md");
            var prComment = $"<!-- popfilenet:coverage-unit -->\n\n## Code Coverage (Unit Tests)\n\n{summaryContent}";

            System.IO.File.WriteAllText("pr_comment.md", prComment);

            var prNumber = Ci?.PullRequestNumber;
            if (prNumber == null)
            {
                Serilog.Log.Warning("PR number not found, skipping comment");
                return;
            }

            var owner = Ci!.RepositoryOwner;
            var repo = Ci.Repository?.Split("/").Last();

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                Serilog.Log.Warning("Repository info not found, skipping comment");
                return;
            }

            var existingComments = GetExistingComments(owner, repo, prNumber.Value.ToString());
            var existingComment = existingComments
                .Where(c => c.UserType == "Bot" && c.Body.Contains("<!-- popfilenet:coverage-unit -->"))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (existingComment != null)
            {
                UpdateComment(owner, repo, existingComment.Id, prComment);
            }
            else
            {
                CreateComment(owner, repo, prNumber.Value.ToString(), prComment);
            }
        });

    Target UpdateReadme => _ => _
        .DependsOn(CoverageReport)
        .OnlyWhenStatic(() => Ci != null && IsMainBranch)
        .Executes(() =>
        {
            var coverageFile = TestResultsDirectory / "UnitTests" / "merged.cobertura.xml";
            if (!coverageFile.Exists())
            {
                Serilog.Log.Warning("Coverage file not found, skipping README update");
                return;
            }

            var reportDir = RootDirectory / "report";
            var summaryContent = System.IO.File.ReadAllText(reportDir / "SummaryGithub.md");

            var match = Regex.Match(summaryContent, @"(\d+(?:\.\d+)?)%");
            if (!match.Success)
            {
                Serilog.Log.Warning("Could not extract coverage percentage");
                return;
            }

            var coveragePct = match.Groups[1].Value;
            var readmePath = RootDirectory / "README.md";
            var readmeContent = System.IO.File.ReadAllText(readmePath);

            var coverageSection = $@"<!-- coverage-table-start -->
![Code Coverage](https://img.shields.io/badge/coverage-{coveragePct}%25-green)
<!-- coverage-table-end -->";

            var updatedReadme = Regex.Replace(
                readmeContent,
                @"<!-- coverage-table-start -->.*?<!-- coverage-table-end -->",
                coverageSection,
                RegexOptions.Singleline);

            System.IO.File.WriteAllText(readmePath, updatedReadme);

            StartShell("git config --local user.email \"github-actions[bot]@users.noreply.github.com\"").AssertZeroExitCode();
            StartShell("git config --local user.name \"github-actions[bot]\"").AssertZeroExitCode();
            StartShell("git add README.md").AssertZeroExitCode();

            var diffResult = StartShell("git diff --staged --quiet", logOutput: false, logInvocation: false);
            if (diffResult.ExitCode != 0)
            {
                StartShell("git commit -m \"docs: update code coverage in README [skip ci]\"").AssertZeroExitCode();
                StartShell("git push").AssertZeroExitCode();
            }
        });

    Target DockerBuild => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var backendPublishDir = PublishDirectory / "backend";
            var uiPublishDir = PublishDirectory / "ui";

            backendPublishDir.CreateOrCleanDirectory();
            uiPublishDir.CreateOrCleanDirectory();

            DotNetPublish(s => s
                .SetProject(RootDirectory / "PopfileNet.Backend" / "PopfileNet.Backend.csproj")
                .SetConfiguration(Configuration)
                .SetOutput(backendPublishDir));

            DotNetPublish(s => s
                .SetProject(RootDirectory / "PopfileNet.Ui" / "PopfileNet.Ui.csproj")
                .SetConfiguration(Configuration)
                .SetOutput(uiPublishDir));

            var tags = GetDockerTags();

            var backendArgs = ShouldPushDocker
                ? $"--push --cache-from type=gha --cache-to type=gha,mode=max"
                : $"--load --cache-from type=gha";
            StartShell($"docker buildx build {backendArgs} -f Dockerfile.backend {string.Join(" ", tags.Select(t => $"-t {t}"))} ./publish/backend", outputFilter: FilterDockerOutput).AssertZeroExitCode();

            var uiTags = tags.Select(t => t.Replace("-backend", "-ui")).ToArray();
            var uiArgs = ShouldPushDocker
                ? $"--push --cache-from type=gha --cache-to type=gha,mode=max"
                : $"--load --cache-from type=gha";
            StartShell($"docker buildx build {uiArgs} -f Dockerfile.ui {string.Join(" ", uiTags.Select(t => $"-t {t}"))} ./publish/ui", outputFilter: FilterDockerOutput).AssertZeroExitCode();
        });

    Target CommitEnv => _ => _
        .DependsOn(DockerBuild)
        .Executes(() =>
        {
            var appHostProject = RootDirectory / "PopfileNet.AppHost" / "PopfileNet.AppHost.csproj";

            var aspireOutputDir = RootDirectory / "PopfileNet.AppHost" / "aspire-output";
            aspireOutputDir.CreateOrCleanDirectory();

            DotNetRestore(s => s.SetProjectFile(appHostProject));

            StartShell($"dotnet run --project \"{appHostProject}\" -- aspire publish -o \"{aspireOutputDir}\"").AssertZeroExitCode();

            var envPath = aspireOutputDir / ".env";
            var composePath = aspireOutputDir / "docker-compose.yaml";

            // Generate .env.example from Aspire output (strip image refs and port overrides)
            var templateEnvPath = RootDirectory / ".env.example";
            var envContent = System.IO.File.ReadAllText(envPath);
            envContent = Regex.Replace(envContent, @"POPFILENET_BACKEND_IMAGE=.*", $"POPFILENET_BACKEND_IMAGE=<image>");
            envContent = Regex.Replace(envContent, @"POPFILENET_UI_IMAGE=.*", $"POPFILENET_UI_IMAGE=<image>");
            envContent = Regex.Replace(envContent, @"POPFILENET_BACKEND_PORT=.*", "POPFILENET_BACKEND_PORT=8000");
            envContent = Regex.Replace(envContent, @"POPFILENET_UI_PORT=.*", "POPFILENET_UI_PORT=8001");

            // Replace sensitive values with placeholders
            var template = new System.Text.StringBuilder();
            foreach (var line in envContent.Split('\n'))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    template.AppendLine(line);

                var parts = trimmedLine.Split('=', 2);
                var key = parts[0];

                if (key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("_USERNAME", StringComparison.OrdinalIgnoreCase))
                    template.AppendLine($"{key}=<value>");
                else
                    template.AppendLine(line);
            }

            var envHeader = "# PopfileNet Environment Configuration\n";
            envHeader += "# Copy this file as .env and fill in your values\n# Generated by the CI/CD pipeline from Aspire output\n\n";
            System.IO.File.WriteAllText(templateEnvPath, envHeader + template.ToString());

            // Generate docker-compose.yaml.example (strip dashboard, OTEL; replace versioned tags)
            var composeContent = System.IO.File.ReadAllText(composePath);
            composeContent = Regex.Replace(composeContent, @"popfilenet-dashboard:.*?restart: ""always""\n", "", RegexOptions.Singleline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_EXPORTER_OTLP_ENDPOINT:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_EXPORTER_OTLP_PROTOCOL:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_SERVICE_NAME:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_DOTNET_EXPERIMENTAL:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(
                composeContent,
                @"image:\s+ghcr\.io/\S+-backend:(?:v\d+\.\d+(?:\.\d+)?)?",
                "image: ghcr.io/blakharaz/popfilenet-backend:<image>",
                RegexOptions.Multiline);
            composeContent = Regex.Replace(
                composeContent,
                @"image:\s+ghcr\.io/\S+-ui:(?:v\d+\.\d+(?:\.\d+)?)?",
                "image: ghcr.io/blakharaz/popfilenet-ui:<image>",
                RegexOptions.Multiline);

            var templateComposePath = RootDirectory / "compose" / "docker-compose.yaml.example";
            System.IO.Directory.CreateDirectory(RootDirectory / "compose");
            System.IO.File.WriteAllText(templateComposePath, composeContent);

            // Stage both, then commit and push on main or release branches
            StartShell($"git add \"{templateEnvPath}\"").AssertZeroExitCode();
            StartShell($"git add \"{templateComposePath}\"").AssertZeroExitCode();

            var currentBranch = GitRepository?.Branch;
            var isReleaseBranch = ReleaseBranches.Any(pattern =>
                currentBranch != null && pattern.Replace("*", "") != "" && currentBranch.StartsWith(pattern.Replace("*", "")));

            if (IsMainBranch || isReleaseBranch)
            {
                StartShell("git -c user.name=ci-bot -c user.email=ci@bot commit --allow-empty -m \"[skip ci] update .env.example and docker-compose.yaml.example\"").AssertZeroExitCode();
                StartShell("git push origin HEAD:main --force-with-lease").AssertZeroExitCode();
            }
        });

    Target GenerateRelease => _ => _
        .DependsOn(CommitEnv)
        .OnlyWhenStatic(() => IsTag)
        .Executes(() =>
        {
            var appHostProject = RootDirectory / "PopfileNet.AppHost" / "PopfileNet.AppHost.csproj";

            var aspireOutputDir = RootDirectory / "PopfileNet.AppHost" / "aspire-output";
            DotNetRestore(s => s.SetProjectFile(appHostProject));

            StartShell($"dotnet run --project \"{appHostProject}\" -- aspire publish -o \"{aspireOutputDir}\"").AssertZeroExitCode();

            var composePath = aspireOutputDir / "docker-compose.yaml";
            var envPath = aspireOutputDir / ".env";

            var composeContent = System.IO.File.ReadAllText(composePath);
            composeContent = Regex.Replace(composeContent, @"popfilenet-dashboard:.*?restart: ""always""\n", "", RegexOptions.Singleline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_EXPORTER_OTLP_ENDPOINT:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_EXPORTER_OTLP_PROTOCOL:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_SERVICE_NAME:.*\n", "", RegexOptions.Multiline);
            composeContent = Regex.Replace(composeContent, @"^\s*OTEL_DOTNET_EXPERIMENTAL:.*\n", "", RegexOptions.Multiline);
            System.IO.File.WriteAllText(composePath, composeContent);

            var envContent = System.IO.File.ReadAllText(envPath);
            envContent = Regex.Replace(envContent, @"POPFILENET_BACKEND_IMAGE=.*", $"POPFILENET_BACKEND_IMAGE={Registry}/{ImageNameValue}-backend:v{TagName}");
            envContent = Regex.Replace(envContent, @"POPFILENET_UI_IMAGE=.*", $"POPFILENET_UI_IMAGE={Registry}/{ImageNameValue}-ui:v{TagName}");
            envContent = Regex.Replace(envContent, @"POPFILENET_BACKEND_PORT=.*", "POPFILENET_BACKEND_PORT=8000");
            envContent = Regex.Replace(envContent, @"POPFILENET_UI_PORT=.*", "POPFILENET_UI_PORT=8001");
            envContent = Regex.Replace(envContent, @"POSTGRES_PASSWORD=.*", "POSTGRES_PASSWORD=");
            System.IO.File.WriteAllText(envPath, envContent);

            StartShell($"gh release create \"{TagName}\" \"{composePath}\" \"{envPath}\" --title \"PopfileNet Release {TagName}\"").AssertZeroExitCode();
        });

    Target PrValidation => _ => _
        .DependsOn(TestUnit)
        .DependsOn(TestIntegration)
        .DependsOn(TestFunctional)
        .DependsOn(SonarEnd)
        .DependsOn(CoverageReport)
        .DependsOn(PrComment);

    Target All => _ => _
        .DependsOn(TestUnit)
        .DependsOn(MergeCoverage)
        .DependsOn(TestIntegration)
        .DependsOn(TestFunctional)
        .DependsOn(SonarEnd)
        .DependsOn(CoverageReport)
        .DependsOn(PrComment)
        .DependsOn(UpdateReadme)
        .DependsOn(DockerBuild)
        .DependsOn(CommitEnv);
    [Parameter("Release branches pattern")] readonly string[] ReleaseBranches = ["release/*", "releases/*"];

    static string? FilterDockerOutput(string text)
    {
        if (text.StartsWith("#") || text.StartsWith(" ") || string.IsNullOrWhiteSpace(text))
            return null;
        if (text.StartsWith("Error:"))
            return null;
        if (text.StartsWith("WARNING:"))
            return null;
        return text;
    }

    void InstallPlaywright()
    {
        StartShell("dotnet tool install Microsoft.Playwright.CLI --local").AssertZeroExitCode();
        StartShell("NEEDRESTART_MODE=a dotnet playwright install --with-deps chromium").AssertZeroExitCode();
    }

    string[] GetDockerTags()
    {
        var tags = new List<string>();

        if (!string.IsNullOrEmpty(TagName))
        {
            tags.Add($"{Registry}/{ImageNameValue}-backend:{TagName}");
            var versionParts = TagName.TrimStart('v').Split('.');
            if (versionParts.Length >= 2)
                tags.Add($"{Registry}/{ImageNameValue}-backend:v{versionParts[0]}.{versionParts[1]}");
            if (versionParts.Length >= 1)
                tags.Add($"{Registry}/{ImageNameValue}-backend:v{versionParts[0]}");
        }
        else if (IsMainBranch)
        {
            tags.Add($"{Registry}/{ImageNameValue}-backend:main");
        }
        else if (IsPullRequest)
        {
            var prNumber = Ci?.PullRequestNumber;
            if (prNumber != null)
                tags.Add($"{Registry}/{ImageNameValue}-backend:pr-{prNumber}");
        }

        var sha = Ci?.Sha;
        if (string.IsNullOrEmpty(sha))
            sha = GitRepository!.Commit.Substring(0, Math.Min(7, GitRepository.Commit.Length));
        else if (sha.Length > 7)
            sha = sha.Substring(0, 7);

        tags.Add($"{Registry}/{ImageNameValue}-backend:{sha}");

        return tags.ToArray();
    }

    GitHubComment[] GetExistingComments(string owner, string repo, string prNumber)
    {
        var result = StartShell($"gh api repos/{owner}/{repo}/issues/{prNumber}/comments --jq '.[] | {{id: .id, body: .body, user_type: .user.type, created_at: .created_at}}'");
        var comments = new List<GitHubComment>();
        foreach (var line in result.Output.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try
            {
                var json = JsonSerializer.Deserialize<GitHubCommentJson>(line);
                if (json != null)
                    comments.Add(new GitHubComment { Id = json.Id, Body = json.Body, UserType = json.UserType, CreatedAt = json.CreatedAt });
            }
            catch { }
        }
        return comments.ToArray();
    }

    void UpdateComment(string owner, string repo, string commentId, string body)
    {
        var tempFile = "comment-body.json";
        System.IO.File.WriteAllText(tempFile, JsonSerializer.Serialize(new { body }));
        StartShell($"gh api repos/{owner}/{repo}/issues/comments/{commentId} -X PATCH --input {tempFile}").AssertZeroExitCode();
        System.IO.File.Delete(tempFile);
    }

    void CreateComment(string owner, string repo, string prNumber, string body)
    {
        var tempFile = "comment-body.json";
        System.IO.File.WriteAllText(tempFile, JsonSerializer.Serialize(new { body }));
        StartShell($"gh api repos/{owner}/{repo}/issues/{prNumber}/comments -X POST --input {tempFile}").AssertZeroExitCode();
        System.IO.File.Delete(tempFile);
    }

    class GitHubComment
    {
        public string Id { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    class GitHubCommentJson
    {
        public string Id { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
