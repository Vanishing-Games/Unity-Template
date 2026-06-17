using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameMain.RunTime
{
    public class MainMenuController : MonoBehaviour
    {
        private UIDocument _document;

        // Screens
        private VisualElement _mainMenuScreen;
        private VisualElement _saveSlotScreen;
        private VisualElement _settingsScreen;

        // Main Menu Buttons
        private Button _startNewGameButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _quitButton;

        // Save Slot Elements
        private Button _createNewSlotButton;
        private VisualElement _slotContainer;
        private Button _backFromSaveButton;

        // Rename Popup Elements
        private VisualElement _renameOverlay;
        private TextField _renameTextField;
        private Button _renameConfirmButton;
        private Button _renameCancelButton;
        private string _slotToRename;

        private bool _isNewGameMode;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
                return;

            var root = _document.rootVisualElement;

            // Screens
            _mainMenuScreen = root.Q<VisualElement>("main-menu-container");
            _saveSlotScreen = root.Q<VisualElement>("save-slot-screen");
            _settingsScreen = root.Q<VisualElement>("settings-screen");

            // Main Menu Buttons
            _startNewGameButton = root.Q<Button>("start-new-game-button");
            _continueButton = root.Q<Button>("continue-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");

            _startNewGameButton?.RegisterCallback<ClickEvent>(evt => OnStartNewGameClicked());
            _continueButton?.RegisterCallback<ClickEvent>(evt => OnContinueClicked());
            _settingsButton?.RegisterCallback<ClickEvent>(evt => ShowScreen(_settingsScreen));
            _quitButton?.RegisterCallback<ClickEvent>(evt => OnQuitClicked());

            // Save Slot Elements
            _createNewSlotButton = root.Q<Button>("create-new-slot-button");
            _slotContainer = root.Q<VisualElement>("slot-container");
            _backFromSaveButton = root.Q<Button>("back-to-menu-from-save");

            _createNewSlotButton?.RegisterCallback<ClickEvent>(evt => OnCreateNewSlotClicked());
            _backFromSaveButton?.RegisterCallback<ClickEvent>(evt => ShowScreen(_mainMenuScreen));

            // Rename Popup Elements
            _renameOverlay = root.Q<VisualElement>("rename-overlay");
            _renameTextField = root.Q<TextField>("rename-textfield");
            _renameConfirmButton = root.Q<Button>("rename-confirm-button");
            _renameCancelButton = root.Q<Button>("rename-cancel-button");

            _renameConfirmButton?.RegisterCallback<ClickEvent>(evt =>
                OnRenameConfirmClicked().Forget()
            );
            _renameCancelButton?.RegisterCallback<ClickEvent>(evt =>
                _renameOverlay.style.display = DisplayStyle.None
            );

            // Settings Elements
            root.Q<Button>("back-to-menu-from-settings")
                ?.RegisterCallback<ClickEvent>(evt => ShowScreen(_mainMenuScreen));
        }

        private void ShowScreen(VisualElement screenToShow)
        {
            _mainMenuScreen.style.display = DisplayStyle.None;
            _saveSlotScreen.style.display = DisplayStyle.None;
            _settingsScreen.style.display = DisplayStyle.None;

            screenToShow.style.display = DisplayStyle.Flex;

            if (screenToShow == _saveSlotScreen)
            {
                RefreshSlotsUI();
            }
        }

        private void RefreshSlotsUI()
        {
            _slotContainer.Clear();
            var availableSlots = VgSaveSystem.Instance.AvailableSlots;

            foreach (var meta in availableSlots.OrderByDescending(m => m.LastSavedTime))
            {
                var slotBtn = new Button { name = meta.SlotName };
                slotBtn.AddToClassList("slot-button");

                // Content
                var content = new VisualElement();
                content.AddToClassList("slot-content");

                var nameLabel = new Label(meta.DisplayName ?? meta.SlotName);
                nameLabel.AddToClassList("slot-name-label");

                var infoLabel = new Label(
                    $"Last Saved: {meta.LastSavedTime:yyyy-MM-dd HH:mm} | Time: {TimeSpan.FromSeconds(meta.PlayTimeInSeconds):hh\\:mm\\:ss}"
                );
                infoLabel.AddToClassList("slot-info-label");

                content.Add(nameLabel);
                content.Add(infoLabel);
                slotBtn.Add(content);

                // Actions Area
                var actions = new VisualElement();
                actions.AddToClassList("slot-actions");

                var renameBtn = new Button { text = "Rename" };
                renameBtn.AddToClassList("rename-button");
                renameBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation(); // Prevent slot selection
                    OpenRenamePopup(meta.SlotName, meta.DisplayName);
                });
                actions.Add(renameBtn);
                slotBtn.Add(actions);

                slotBtn.RegisterCallback<ClickEvent>(evt =>
                    ContinueGameOnSlot(meta.SlotName).Forget()
                );
                _slotContainer.Add(slotBtn);
            }

            // Adjust visibility based on mode if needed
            if (_isNewGameMode)
            {
                _createNewSlotButton.AddToClassList("highlighted-button");
            }
            else
            {
                _createNewSlotButton.RemoveFromClassList("highlighted-button");
            }
        }

        private void OnStartNewGameClicked()
        {
            _isNewGameMode = true;
            ShowScreen(_saveSlotScreen);
        }

        private void OnContinueClicked()
        {
            _isNewGameMode = false;
            ShowScreen(_saveSlotScreen);
        }

        private void OnCreateNewSlotClicked()
        {
            string newSlotName = $"slot_{DateTime.Now:yyyyMMdd_HHmmss}";
            CLogger.LogInfo($"Creating new save: {newSlotName}", LogTag.Button);
            VgSaveSystem.Instance.SetCurrentSlot(newSlotName);

            var command = new GameFlowCommands.StartGameCommand(GameIdentifiers.SavePoints.NewGame);
            command.Execute().Forget();
        }

        private async UniTaskVoid ContinueGameOnSlot(string slotName)
        {
            CLogger.LogInfo($"Loading save: {slotName}", LogTag.Button);
            bool success = await VgSaveSystem.Instance.LoadSlotAsync(slotName);
            if (!success)
                return;

            var entry = VgSaveSystem.Instance.GetSaveValue<SaveSlotEntrySaveData>(
                SaveKeys.SaveSlotEntry
            );

            GameFlowCommands.StartGameCommand command;
            if (entry != null && !string.IsNullOrEmpty(entry.SavePointName))
            {
                command = new GameFlowCommands.StartGameCommand(entry.SavePointName);
            }
            else
            {
                CLogger.LogWarn(
                    $"Save slot {slotName} has no SaveSlotEntry, falling back to default level",
                    LogTag.Button
                );
                command = new GameFlowCommands.StartGameCommand(GameIdentifiers.SavePoints.NewGame);
            }

            command.Execute().Forget();
        }

        private void OpenRenamePopup(string slotName, string currentName)
        {
            _slotToRename = slotName;
            _renameTextField.value = currentName ?? slotName;
            _renameOverlay.style.display = DisplayStyle.Flex;
        }

        private async UniTaskVoid OnRenameConfirmClicked()
        {
            string newName = _renameTextField.value;
            if (!string.IsNullOrEmpty(newName))
            {
                bool success = await VgSaveSystem.Instance.RenameSlotAsync(_slotToRename, newName);
                if (success)
                {
                    RefreshSlotsUI();
                }
            }
            _renameOverlay.style.display = DisplayStyle.None;
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
