using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGedcomLoader _gedcomLoader;
    private readonly IGedcomFilePickerService _gedcomFilePickerService;

    public MainWindowViewModel()
        : this(new GedcomLoader(), new NullGedcomFilePickerService())
    {
    }

    public MainWindowViewModel(IGedcomLoader gedcomLoader, IGedcomFilePickerService gedcomFilePickerService)
    {
        _gedcomLoader = gedcomLoader;
        _gedcomFilePickerService = gedcomFilePickerService;
    }

    [ObservableProperty]
    private PersonListItemViewModel? selectedPerson;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? selectedGedcomFilePath;

    public ObservableCollection<PersonListItemViewModel> People { get; } = [];

    [RelayCommand]
    private async Task SelectGedcomFileAsync()
    {
        var filePath = await _gedcomFilePickerService.PickGedcomFileAsync();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var familyTree = _gedcomLoader.Load(filePath);
            var people = familyTree.People
                .Select(CreatePersonListItem)
                .OrderBy(person => person.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(person => person.RecordId, StringComparer.Ordinal)
                .ToList();

            ReplacePeople(people);
            SelectedPerson = People.FirstOrDefault();
            SelectedGedcomFilePath = filePath;
            ErrorMessage = null;
        }
        catch (GedcomLoadException exception)
        {
            ErrorMessage = $"Kunne ikke indlaese GEDCOM-fil: {exception.Message}";
        }
    }

    private static PersonListItemViewModel CreatePersonListItem(Person person)
    {
        var displayName = string.IsNullOrWhiteSpace(person.FullName)
            ? $"Unavngiven ({person.RecordId})"
            : person.FullName.Trim();

        return new PersonListItemViewModel(person.RecordId, displayName);
    }

    private void ReplacePeople(IEnumerable<PersonListItemViewModel> people)
    {
        People.Clear();
        foreach (var person in people)
        {
            People.Add(person);
        }
    }

    private sealed class NullGedcomFilePickerService : IGedcomFilePickerService
    {
        public Task<string?> PickGedcomFileAsync()
        {
            return Task.FromResult<string?>(null);
        }
    }
}
