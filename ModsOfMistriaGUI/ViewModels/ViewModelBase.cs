using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaGUI.Services;

namespace Garethp.ModsOfMistriaGUI.ViewModels;

public class ViewModelBase : ObservableObject
{
    public LocalizationService Localization => LocalizationService.Instance;
    public LocalizedTexts Texts => LocalizedTexts.Instance;
}
