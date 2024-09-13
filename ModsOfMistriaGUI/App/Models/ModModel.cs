using CommunityToolkit.Mvvm.ComponentModel;

namespace Garethp.ModsOfMistriaGUI.App.Models;

public partial class ModModel: ObservableObject
{
    public string _name;
    
    public string _author;
    
    public string Full => $"{_name} by {_author}";
}