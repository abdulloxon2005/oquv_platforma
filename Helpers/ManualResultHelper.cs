using System;
using System.Text.Json;

namespace talim_platforma.Helpers
{
    public static class ManualResultHelper
    {
        public sealed record ManualResultSummary(int? CorrectAnswers, int? TotalQuestions);

        public static ManualResultSummary? TryParse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (IsManualEntry(root))
                    {
                        return new ManualResultSummary(
                            TryGetInt(root, "CorrectAnswers"),
                            TryGetInt(root, "TotalQuestions"));
                    }

                    if (root.TryGetProperty("ManualSummary", out var summaryElement))
                    {
                        return new ManualResultSummary(
                            TryGetInt(summaryElement, "CorrectAnswers"),
                            TryGetInt(summaryElement, "TotalQuestions"));
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    // Online results: count correct answers and total questions.
                    int? total = null;
                    int? correct = null;

                    try
                    {
                        total = root.GetArrayLength();
                        correct = 0;
                        foreach (var element in root.EnumerateArray())
                        {
                            if (element.TryGetProperty("Togri", out var correctProp) && correctProp.ValueKind == JsonValueKind.True)
                            {
                                correct++;
                            }
                        }
                    }
                    catch
                    {
                        total = null;
                        correct = null;
                    }

                    if (correct.HasValue || total.HasValue)
                    {
                        return new ManualResultSummary(correct, total);
                    }
                }
            }
            catch
            {
                // ignore malformed json
            }

            return null;
        }

        public static string BuildPayload(int ball, int? correct, int? total)
        {
            var payload = new ManualResultPayload
            {
                ManualEntry = true,
                CorrectAnswers = correct,
                TotalQuestions = total,
                Score = ball
            };

            return JsonSerializer.Serialize(payload);
        }

        private static bool IsManualEntry(JsonElement element)
        {
            if (!element.TryGetProperty("ManualEntry", out var flag))
            {
                return false;
            }

            return flag.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => flag.TryGetInt32(out var numeric) && numeric != 0,
                JsonValueKind.String => bool.TryParse(flag.GetString(), out var parsed) && parsed,
                _ => false
            };
        }

        private static int? TryGetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetInt32(out var value))
            {
                return value;
            }

            return null;
        }

        private sealed class ManualResultPayload
        {
            public bool ManualEntry { get; set; }
            public int? CorrectAnswers { get; set; }
            public int? TotalQuestions { get; set; }
            public int Score { get; set; }
        }
    }
}

