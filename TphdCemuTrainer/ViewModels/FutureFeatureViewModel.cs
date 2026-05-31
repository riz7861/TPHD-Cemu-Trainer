using TphdCemuTrainer.Cheats;

namespace TphdCemuTrainer.ViewModels;

public sealed class FutureFeatureViewModel
{
    public FutureFeatureViewModel(FutureFeatureDefinition definition)
    {
        Name = definition.Name;
        Notes = definition.Notes;
    }

    public string Name { get; }

    public string Notes { get; }
}
