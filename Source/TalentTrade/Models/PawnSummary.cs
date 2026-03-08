using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
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

        /// <summary>
        /// Extract a PawnSummary from an actual Pawn object.
        /// </summary>
        public static PawnSummary FromPawn(Pawn pawn)
        {
            var summary = new PawnSummary();
            if (pawn == null) return summary;

            // Name
            summary.Name = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort;

            // Gender
            summary.Gender = pawn.gender.ToString();

            // Age
            summary.BiologicalAge = (int)pawn.ageTracker.AgeBiologicalYearsFloat;

            // Race
            summary.RaceDefName = pawn.def != null ? pawn.def.defName : "Human";

            // Skills
            if (pawn.skills != null)
            {
                var skillParts = new List<string>();
                foreach (var skill in pawn.skills.skills)
                {
                    if (skill.TotallyDisabled) continue;
                    string passion = "";
                    if (skill.passion == Passion.Minor) passion = "*";
                    else if (skill.passion == Passion.Major) passion = "**";
                    skillParts.Add(skill.def.skillLabel + " " + skill.Level + passion);
                }
                summary.SkillsSummary = string.Join(", ", skillParts.ToArray());
            }

            // Traits
            if (pawn.story != null && pawn.story.traits != null)
            {
                var traitParts = new List<string>();
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    traitParts.Add(trait.LabelCap);
                }
                summary.TraitsSummary = string.Join(", ", traitParts.ToArray());
            }

            // Health
            if (pawn.health != null && pawn.health.hediffSet != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs;
                if (hediffs.Count == 0)
                {
                    summary.HealthSummary = "Healthy";
                }
                else
                {
                    var healthParts = new List<string>();
                    foreach (var hediff in hediffs)
                    {
                        healthParts.Add(hediff.LabelCap);
                    }
                    summary.HealthSummary = string.Join(", ", healthParts.ToArray());
                }
            }

            return summary;
        }

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
