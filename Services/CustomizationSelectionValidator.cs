using FurniCraft.Data;
using FurniCraft.Models;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Services
{
    /// <summary>
    /// One customization option that passed server-side validation.
    /// </summary>
    public class ValidatedCustomization
    {
        public int OptionId { get; init; }
        public int GroupId { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public string OptionName { get; init; } = string.Empty;
        public decimal AdditionalPrice { get; init; }
    }

    public class CustomizationSelectionResult
    {
        public string? ErrorMessage { get; init; }

        public bool IsValid => ErrorMessage == null;

        public List<ValidatedCustomization> Options { get; init; } = new();

        /// <summary>Sorted option IDs (same format the cart uses for comparison).</summary>
        public List<int> OptionIds =>
            Options.Select(o => o.OptionId).OrderBy(id => id).ToList();

        /// <summary>Sum of the extra prices, calculated from database values.</summary>
        public decimal ExtraPrice => Options.Sum(o => o.AdditionalPrice);

        public static CustomizationSelectionResult Fail(string message) =>
            new() { ErrorMessage = message };
    }

    /// <summary>
    /// Validates the customization options chosen by the browser against what the
    /// database says the product really offers.
    ///
    /// Rules (the current data model has no per-group "required" / "multiple" flags,
    /// so these are the rules that match the existing Details page, which renders
    /// one radio-button list per group with the first option pre-selected):
    ///   1. Every selected option must exist and belong to a group of THIS product.
    ///   2. The same option cannot be sent twice.
    ///   3. A group allows exactly one option.
    ///   4. Every group that has options must have a selection.
    ///   Groups that have no options are ignored.
    ///
    /// If you later add IsRequired / AllowMultiple columns to CustomizationGroup,
    /// only IsRequired() and AllowsMultiple() below need to change.
    /// </summary>
    public static class CustomizationSelectionValidator
    {
        private static bool IsRequired(CustomizationGroup group) =>
            group.Options.Count > 0;

        private static bool AllowsMultiple(CustomizationGroup group) =>
            false;

        public static async Task<CustomizationSelectionResult> ValidateAsync(
            ApplicationDbContext context,
            int productId,
            IEnumerable<int>? selectedOptionIds,
            CancellationToken cancellationToken = default)
        {
            var ids = (selectedOptionIds ?? Enumerable.Empty<int>()).ToList();

            if (ids.Count != ids.Distinct().Count())
            {
                return CustomizationSelectionResult.Fail(
                    "تم اختيار نفس التخصيص أكثر من مرة.");
            }

            // Always read the product's real groups/options from the database.
            var groups = await context.CustomizationGroups
                .AsNoTracking()
                .Include(g => g.Options)
                .Where(g => g.ProductId == productId)
                .OrderBy(g => g.Id)
                .ToListAsync(cancellationToken);

            var lookup = new Dictionary<int, (CustomizationGroup Group, CustomizationOption Option)>();

            foreach (var group in groups)
            {
                foreach (var option in group.Options)
                {
                    lookup[option.Id] = (group, option);
                }
            }

            var selected = new List<(CustomizationGroup Group, CustomizationOption Option)>();

            foreach (var id in ids)
            {
                // Rejects unknown IDs and IDs that belong to another product.
                if (!lookup.TryGetValue(id, out var entry))
                {
                    return CustomizationSelectionResult.Fail(
                        "أحد التخصيصات المختارة غير صالح لهذا المنتج.");
                }

                selected.Add(entry);
            }

            foreach (var group in groups)
            {
                var count = selected.Count(s => s.Group.Id == group.Id);

                if (count > 1 && !AllowsMultiple(group))
                {
                    return CustomizationSelectionResult.Fail(
                        $"يمكنك اختيار خيار واحد فقط من \"{group.Name}\".");
                }

                if (count == 0 && IsRequired(group))
                {
                    return CustomizationSelectionResult.Fail(
                        $"يرجى اختيار أحد خيارات \"{group.Name}\".");
                }
            }

            return new CustomizationSelectionResult
            {
                Options = selected
                    .OrderBy(s => s.Group.Id)
                    .ThenBy(s => s.Option.Id)
                    .Select(s => new ValidatedCustomization
                    {
                        OptionId = s.Option.Id,
                        GroupId = s.Group.Id,
                        GroupName = s.Group.Name,
                        OptionName = s.Option.Name,
                        AdditionalPrice = s.Option.AdditionalPrice
                    })
                    .ToList()
            };
        }
    }
}