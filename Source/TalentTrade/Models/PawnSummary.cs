using System;
using System.Text;
using Verse;

namespace TalentTrade
{
    public class PawnSummary
    {
        public string Name = string.Empty;
        public string Gender = string.Empty;
        public int BiologicalAge;
        public string RaceDefName = "Human";
        public string SkillsSummary = string.Empty;
        public string TraitsSummary = string.Empty;
        public string HealthSummary = "Healthy";

        public string ToBase64()
        {
            string raw = string.Join("\n", new[]
            {
                Name,
                Gender,
                BiologicalAge.ToString(),
                RaceDefName,
                SkillsSummary,
                TraitsSummary,
                HealthSummary
            });
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public static PawnSummary FromBase64(string b64)
        {
            PawnSummary summary = new PawnSummary();
            if (string.IsNullOrEmpty(b64)) return summary;

            try
            {
                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                string[] lines = raw.Split('\n');
                if (lines.Length >= 1) summary.Name = lines[0];
                if (lines.Length >= 2) summary.Gender = lines[1];
                if (lines.Length >= 3) int.TryParse(lines[2], out summary.BiologicalAge);
                if (lines.Length >= 4) summary.RaceDefName = lines[3];
                if (lines.Length >= 5) summary.SkillsSummary = lines[4];
                if (lines.Length >= 6) summary.TraitsSummary = lines[5];
                if (lines.Length >= 7) summary.HealthSummary = lines[6];
            }
            catch
            {
                // Return default summary on parse failure
            }

            return summary;
        }

        public string GetDisplayLabel()
        {
            string genderSymbol = string.Empty;
            if (Gender == "Male") genderSymbol = "♂";
            else if (Gender == "Female") genderSymbol = "♀";

            return string.Concat(Name, " ", genderSymbol, " ", BiologicalAge.ToString(), "TalentTrade_ageUnit".Translate());
        }
    }
}
