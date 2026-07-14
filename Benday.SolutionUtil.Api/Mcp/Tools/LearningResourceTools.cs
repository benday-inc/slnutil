using System.ComponentModel;
using System.Text;

using ModelContextProtocol.Server;

namespace Benday.SolutionUtil.Api.Mcp.Tools;

/// <summary>
/// A learning resource: a human-readable title and a URL.
/// </summary>
/// <param name="Title">Display title for the resource.</param>
/// <param name="Url">Link to the video, article, or documentation.</param>
public readonly record struct LearningResource(string Title, string Url);

/// <summary>
/// MCP tool that maps a conceptual topic to curated learning resources (videos,
/// articles, docs). Backed by a static dictionary — no network calls. Intended
/// for "what is X?" / "how does X work?" questions, not for questions about the
/// user's own project data.
/// </summary>
[McpServerToolType]
public class LearningResourceTools
{
    private static readonly IReadOnlyDictionary<string, LearningResource[]> Resources =
        new Dictionary<string, LearningResource[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["branching"] = new[]
            {
                new LearningResource(
                    "Branches That Don't Suck (playlist)",
                    "https://www.youtube.com/playlist?list=PLGxFXI4dC2sgmEG8vl1IoOVTj1y8V6Y6E"),
            },
            ["class diagram"] = new[]
            {
                new LearningResource(
                    "slnutil classdiagram command",
                    "https://github.com/benday-inc/slnutil"),
            },
            ["dotnet tools"] = new[]
            {
                new LearningResource(
                    "Writing Utilities with .NET Core Tools",
                    "https://www.benday.com"),
            },
            ["solution structure"] = new[]
            {
                new LearningResource(
                    "slnutil — Solution & Project Utilities",
                    "https://github.com/benday-inc/slnutil"),
            },
        };

    [McpServerTool(Name = "get_learning_resources", ReadOnly = true),
        Description(
            "Find video tutorials, blog posts, and documentation for a topic. Use when " +
            "someone asks a conceptual 'what is' or 'how does' question — not when they ask " +
            "for data about their own project. Known topics include: branching, class " +
            "diagram, dotnet tools, solution structure.")]
    public static string GetLearningResources(
        [Description("The topic to look up, e.g. 'branching' or 'dotnet tools'.")]
        string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return "Provide a topic. Known topics: " + string.Join(", ", Resources.Keys) + ".";
        }

        var matches = Resources
            .Where(entry => entry.Key.Contains(topic, StringComparison.OrdinalIgnoreCase)
                || topic.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            .SelectMany(entry => entry.Value)
            .Distinct()
            .ToList();

        if (matches.Count == 0)
        {
            return $"No curated resources found for '{topic}'. Known topics: "
                + string.Join(", ", Resources.Keys) + ".";
        }

        var builder = new StringBuilder();

        foreach (var resource in matches)
        {
            builder.AppendLine($"- {resource.Title}: {resource.Url}");
        }

        return builder.ToString().TrimEnd();
    }
}
