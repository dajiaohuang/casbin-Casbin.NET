#if !NET452 && !NET461 && !NET462
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Casbin.UnitTests.ModelTests;

public class ConformanceTest
{
    private class ConformanceCase
    {
        public string model { get; set; }
        public string policy { get; set; }
        public List<ConformanceRequest> tests { get; set; }
    }

    private class ConformanceRequest
    {
        public List<string> request { get; set; }
        public bool allow { get; set; }
        public List<string> explain { get; set; }
    }

    [Fact]
    public void TestConformance()
    {
        string dir = Environment.GetEnvironmentVariable("CASBIN_CONFORMANCE_DIR");
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        string[] files = Directory.GetFiles(Path.Combine(dir, "cases"), "*.json").OrderBy(f => f, StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(files);

        List<string> failures = new();
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            ConformanceCase c = JsonSerializer.Deserialize<ConformanceCase>(File.ReadAllText(file));
            Enforcer e;
            try
            {
                e = new Enforcer(Path.Combine(dir, "data", c.model), Path.Combine(dir, "data", c.policy));
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
                continue;
            }

            foreach (ConformanceRequest test in c.tests)
            {
                try
                {
                    var (allow, explains) = e.EnforceEx(test.request.ToArray());
                    List<string> explain = explains.FirstOrDefault()?.ToList() ?? new List<string>();
                    if (allow != test.allow || !explain.SequenceEqual(test.explain))
                    {
                        failures.Add($"{name} [{string.Join(", ", test.request)}]: got ({allow}, [{string.Join(", ", explain)}]), want ({test.allow}, [{string.Join(", ", test.explain)}])");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{name} [{string.Join(", ", test.request)}]: {ex.Message}");
                }
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} conformance failures:\n{string.Join("\n", failures)}");
    }
}
#endif
