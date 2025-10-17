using System;
using System.Collections.Generic;
using System.Linq;
using BigFourApp.Models.Event;

namespace BigFourApp.Models.ViewModels
{
    /// <summary>
    /// Definición de layouts por recinto; ahora solo contempla Mortgage Matchup Center, pero está preparado para extenderse a otros venues.
    /// </summary>
    public static class VenueLayout
    {
        private static readonly IReadOnlyList<string> LetterSections = new[] { "A", "B", "C", "D", "E", "F" };

        public static IReadOnlyList<SectionDefinition> GetSections(Venue? venue)
        {
            if (venue is null)
            {
                return Array.Empty<SectionDefinition>();
            }

            // This layout is derived from the venue description provided for the Mortgage Matchup Center.
            if (venue.Name.Contains("Mortgage Matchup Center", StringComparison.OrdinalIgnoreCase))
            {
                return BuildMortgageMatchupSections();
            }

            return Array.Empty<SectionDefinition>();
        }

        private static IReadOnlyList<SectionDefinition> BuildMortgageMatchupSections()
        {
            var sections = new List<SectionDefinition>();

            foreach (var section in LetterSections)
            {
                sections.Add(new SectionDefinition($"SEC-{section}", $"Section {section}", 160m));
            }

            foreach (var number in Enumerable.Range(101, 24))
            {
                sections.Add(new SectionDefinition($"SEC-{number}", $"Section {number}", 120m));
            }

            foreach (var number in Enumerable.Range(201, 32))
            {
                sections.Add(new SectionDefinition($"SEC-{number}", $"Section {number}", 95m));
            }

            return sections;
        }
    }

    public record SectionDefinition(string SectionId, string DisplayName, decimal BasePrice);
}
