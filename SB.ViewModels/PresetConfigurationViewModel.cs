using SB.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace SB.ViewModels
{
    public class PresetConfigurationViewModel : ObservableObject
    {
        private ConfigurationModel _configurationModel;
        private ApplicationStateModel _applicationStateModel;

        public PresetConfigurationViewModel(ApplicationStateModel applicationStateModel, ConfigurationModel configurationModel)
        {
            _applicationStateModel = applicationStateModel;
            _configurationModel = configurationModel;

            _leftPresetViewModel = new PresetViewModel(applicationStateModel, configurationModel);
            _rightPresetViewModel = new PresetViewModel(applicationStateModel, configurationModel);
        }

        private PresetViewModel _leftPresetViewModel;
        public PresetViewModel LeftPresetViewModel
        {
            get { return _leftPresetViewModel; }
        }

        private PresetViewModel _rightPresetViewModel;
        public PresetViewModel RightPresetViewModel
        {
            get { return _rightPresetViewModel; }
        }

        public ICommand CopySoundsRightCommand
        {
            get { return new RelayCommand(CopySoundsRight); }
        }
        private void CopySoundsRight()
        {
            if (_rightPresetViewModel.SelectedPreset != null)
                CopyItemsToList(_leftPresetViewModel.SelectedSounds, _rightPresetViewModel.SelectedPreset.SoundCollection);
        }

        public ICommand CopySoundsLeftCommand
        {
            get { return new RelayCommand(CopySoundsLeft); }
        }
        private void CopySoundsLeft()
        {
            if (_leftPresetViewModel.SelectedPreset != null)
                CopyItemsToList(_rightPresetViewModel.SelectedSounds, _leftPresetViewModel.SelectedPreset.SoundCollection);
        }

        private void CopyItemsToList<T>(IEnumerable<T> items, IList<T> list)
        {
            foreach (ICloneable item in items)
            {
                list.Add((T)item.Clone());
            }
        }
    }
}
