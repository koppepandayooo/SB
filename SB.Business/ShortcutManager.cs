using SB.Models;
using SB.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace SB.Business
{
    public class ShortcutManager
    {
        private ApplicationStateModel _applicationStateModel;
        private ConfigurationModel _configurationModel;
        private SoundManager _soundManager;

        /// <summary>Raised when the selected sound was changed with the mouse wheel.</summary>
        public event Action<Sound> SelectionChangedByWheel;

        /// <summary>Raised when the preset was changed with a key (its own key, or next / previous preset).</summary>
        public event Action<Preset> PresetSwitchedByKey;

        public ShortcutManager(ApplicationStateModel applicationStateModel, ConfigurationModel configurationModel, SoundManager soundManager)
        {
            _applicationStateModel = applicationStateModel;
            _configurationModel = configurationModel;
            _soundManager = soundManager;
        }

        public void KeyUp(VKey key, List<VKey> pressedKeys)
        {
            if (_applicationStateModel.ModifiedBindable != null)
            {
                _applicationStateModel.ModifiedBindable.Keys.Clear();
                foreach (VKey pressedKey in pressedKeys)
                    _applicationStateModel.ModifiedBindable.Keys.Add(pressedKey);

                _applicationStateModel.BindKeysOpened = false;
                _applicationStateModel.ModifiedBindable = null;
            }
        }

        public void KeyDown(VKey key, List<VKey> pressedKeys)
        {
            Shortcut shortcut = ResolveShortcut(key, pressedKeys, new List<Shortcut>(){
                _configurationModel.SoundShortcuts.Pause,
                _configurationModel.SoundShortcuts.Continue,
                _configurationModel.SoundShortcuts.Stop
            });
            if (shortcut != null && shortcut.Command.CanExecute(null))
            {
                shortcut.Command.Execute(null);
            }
        }

        public void KeyPress(VKey key, List<VKey> pressedKeys)
        {
            // Don't trigger anything while the user is choosing keys for a binding
            if (_applicationStateModel.ModifiedBindable != null) return;

            Shortcut playSelected = _configurationModel.SoundShortcuts.PlaySelected;
            Sound sound = ResolveShortcut(key, pressedKeys, _configurationModel.SelectedPreset.SoundCollection.Where(x => x.Files.Count != 0));
            bool playSelectedMatches = playSelected.Keys.Count != 0 && ResolveShortcut(key, pressedKeys, new[] { playSelected }) != null;

            // Prefer the more specific binding when a sound key and the play-selected key overlap
            if (playSelectedMatches && (sound == null || playSelected.Keys.Count >= sound.Keys.Count))
            {
                Sound selected = _configurationModel.SelectedPreset.SelectedSound;
                if (selected != null)
                {
                    _soundManager.Play(selected);
                }
            }
            else if (sound != null)
            {
                _configurationModel.SelectedPreset.SelectedSound = sound;
                _soundManager.Play(sound);
            }

            Preset preset = ResolveShortcut(key, pressedKeys, _configurationModel.PresetCollection);
            if (preset != null)
            {
                _configurationModel.SelectedPreset = preset;
                PresetSwitchedByKey?.Invoke(preset);
                return;
            }

            Shortcut presetShortcut = ResolveShortcut(key, pressedKeys, new List<Shortcut>()
            {
                _configurationModel.SoundShortcuts.NextPreset,
                _configurationModel.SoundShortcuts.PreviousPreset
            });
            if (presetShortcut != null)
            {
                SwitchPreset(presetShortcut == _configurationModel.SoundShortcuts.NextPreset ? 1 : -1);
            }
        }

        /// <summary>Selects the next (1) or previous (-1) preset, wrapping around.</summary>
        public void SwitchPreset(int step)
        {
            var presets = _configurationModel.PresetCollection;
            if (presets.Count < 2) return;

            int index = presets.IndexOf(_configurationModel.SelectedPreset);
            int next = ((index + step) % presets.Count + presets.Count) % presets.Count;
            _configurationModel.SelectedPreset = presets[next];
            PresetSwitchedByKey?.Invoke(presets[next]);
        }

        public void Wheel(int delta, List<VKey> pressedKeys)
        {
            if (_applicationStateModel.ModifiedBindable != null) return;

            switch (_configurationModel.WheelMode)
            {
                case WheelMode.Disabled:
                    return;
                case WheelMode.WithModifier:
                    IList<VKey> modifier = _configurationModel.SoundShortcuts.WheelModifier.Keys;
                    if (modifier.Count == 0 || !modifier.All(pressedKeys.Contains)) return;
                    break;
            }

            Preset preset = _configurationModel.SelectedPreset;
            // Follow the order shown in the list, which may be sorted by a column header
            IList<Sound> sounds = GetDisplayedSounds(preset);
            if (sounds.Count == 0 || delta == 0) return;

            // Wheel down selects the next sound, wheel up the previous one, wrapping around
            int step = delta < 0 ? 1 : -1;
            int index = preset.SelectedSound == null ? -1 : sounds.IndexOf(preset.SelectedSound);
            if (index < 0) index = step > 0 ? -1 : 0;
            int next = ((index + step) % sounds.Count + sounds.Count) % sounds.Count;
            preset.SelectedSound = sounds[next];

            SelectionChangedByWheel?.Invoke(preset.SelectedSound);
        }

        /// <summary>
        /// The sounds in the order the list shows them. Sorting by clicking a column header only
        /// changes the view of the collection, not the collection itself.
        /// </summary>
        public static IList<Sound> GetDisplayedSounds(Preset preset)
        {
            return CollectionViewSource.GetDefaultView(preset.SoundCollection).Cast<Sound>().ToList();
        }

        private T ResolveShortcut<T>(VKey key, IEnumerable<VKey> pressedKeys, IEnumerable<T> items) where T : IBindable
        {
            return items.Where(x => x.Keys.Contains(key) && x.Keys.All(y => pressedKeys.Contains(y))).OrderBy(x => x.Keys.Count).LastOrDefault();
        }
    }
}
