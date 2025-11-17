using System;
using System.Collections.Generic;
using System.Linq;
using BigFourApp.Models.Event;

namespace BigFourApp.Models.ViewModels
{
    public static class VenueLayout
    {
        private const int DefaultSeatsPerRow = 10;
        private const int DefaultSeatCount = 60;
        private static readonly IReadOnlyList<string> LetterSections = new[] { "A", "B", "C", "D", "E", "F" };

        public static IReadOnlyList<SectionDefinition> GetSections(Venue? venue)
        {
            if (venue is null)
            {
                return Array.Empty<SectionDefinition>();
            }

            if (venue.Sections != null && venue.Sections.Any())
            {
                return venue.Sections
                    .OrderBy(s => s.Id)
                    .Select((section, index) => BuildCustomDefinition(section, index))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(venue.Name) && venue.Name.Contains("Mortgage Matchup Center", StringComparison.OrdinalIgnoreCase))
            {
                return BuildMortgageMatchupSections();
            }

            return Array.Empty<SectionDefinition>();
        }

        private static SectionDefinition BuildCustomDefinition(VenueSection section, int index)
        {
            var sectionCode = string.IsNullOrWhiteSpace(section.SectionCode)
                ? $"SEC-{index + 1:000}"
                : section.SectionCode.Trim();

            var displayName = string.IsNullOrWhiteSpace(section.DisplayName)
                ? $"Section {index + 1}"
                : section.DisplayName.Trim();

            var basePrice = section.BasePrice <= 0 ? 85m : section.BasePrice;
            var seatCount = section.SeatCount <= 0 ? DefaultSeatCount : section.SeatCount;
            var seatsPerRow = section.SeatsPerRow <= 0 ? DefaultSeatsPerRow : section.SeatsPerRow;

            return new SectionDefinition(sectionCode, displayName, basePrice, seatCount, seatsPerRow);
        }

        private static IReadOnlyList<SectionDefinition> BuildMortgageMatchupSections()
        {
            var sections = new List<SectionDefinition>();

            foreach (var section in LetterSections)
            {
                sections.Add(new SectionDefinition($"SEC-{section}", $"Section {section}", 160m, 160, DefaultSeatsPerRow));
            }

            foreach (var number in Enumerable.Range(101, 24))
            {
                sections.Add(new SectionDefinition($"SEC-{number}", $"Section {number}", 120m, 120, DefaultSeatsPerRow));
            }

            foreach (var number in Enumerable.Range(201, 32))
            {
                sections.Add(new SectionDefinition($"SEC-{number}", $"Section {number}", 95m, 95, DefaultSeatsPerRow));
            }

            return sections;
        }
    }

    public record SectionDefinition(string SectionId, string DisplayName, decimal BasePrice, int SeatCount, int SeatsPerRow);
}
