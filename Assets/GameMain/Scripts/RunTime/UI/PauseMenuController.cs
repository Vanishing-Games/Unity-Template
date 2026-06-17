using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameMain.RunTime
{
    public class PauseMenuController : MonoBehaviour
    {
        private void Awake()
        {
            m_Document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
                if (m_Document == null)
                    return;
            }

            var root = m_Document.rootVisualElement;
            if (root == null)
                return;

            m_RootContainer = root.Q<VisualElement>("root-container");

            if (m_RootContainer == null && root.childCount > 0)
            {
                m_RootContainer = root[0];
            }

            if (m_RootContainer == null)
            {
                CLogger.LogError("Failed to find root-container in UIDocument.", LogTag.Game);
                return;
            }

            m_PauseMenuScreen = m_RootContainer.Q<VisualElement>("pause-menu-container");
            m_SaveSlotScreen = m_RootContainer.Q<VisualElement>("save-slot-screen");
            m_SettingsScreen = m_RootContainer.Q<VisualElement>("settings-screen");

            m_ResumeButton = m_RootContainer.Q<Button>("resume-button");
            m_SaveGameButton = m_RootContainer.Q<Button>("save-game-button");
            m_LoadGameButton = m_RootContainer.Q<Button>("load-game-button");
            m_SettingsButton = m_RootContainer.Q<Button>("settings-button");
            m_MainMenuButton = m_RootContainer.Q<Button>("main-menu-button");

            m_ResumeButton?.RegisterCallback<ClickEvent>(evt => SetPaused(false));
            m_SaveGameButton?.RegisterCallback<ClickEvent>(evt => OpenSaveScreen());
            m_LoadGameButton?.RegisterCallback<ClickEvent>(evt => OpenLoadScreen());
            m_SettingsButton?.RegisterCallback<ClickEvent>(evt => ShowScreen(m_SettingsScreen));
            m_MainMenuButton?.RegisterCallback<ClickEvent>(evt => OnQuitToMenu());

            m_CreateNewSlotButton = m_RootContainer.Q<Button>("create-new-slot-button");
            m_SlotContainer = m_RootContainer.Q<VisualElement>("slot-container");
            m_BackFromSaveButton = m_RootContainer.Q<Button>("back-to-menu-from-save");
            m_SlotScreenTitle = m_RootContainer.Q<Label>("slot-screen-title");

            m_CreateNewSlotButton?.RegisterCallback<ClickEvent>(evt =>
                OnCreateNewSlotClicked().Forget()
            );
            m_BackFromSaveButton?.RegisterCallback<ClickEvent>(evt =>
                ShowScreen(m_PauseMenuScreen)
            );

            m_RenameOverlay = m_RootContainer.Q<VisualElement>("rename-overlay");
            m_RenameTextField = m_RootContainer.Q<TextField>("rename-textfield");
            m_RenameConfirmButton = m_RootContainer.Q<Button>("rename-confirm-button");
            m_RenameCancelButton = m_RootContainer.Q<Button>("rename-cancel-button");

            m_RenameConfirmButton?.RegisterCallback<ClickEvent>(evt =>
                OnRenameConfirmClicked().Forget()
            );
            m_RenameCancelButton?.RegisterCallback<ClickEvent>(evt =>
                m_RenameOverlay.style.display = DisplayStyle.None
            );

            m_RootContainer
                .Q<Button>("back-to-menu-from-settings")
                ?.RegisterCallback<ClickEvent>(evt => ShowScreen(m_PauseMenuScreen));

            SetPaused(false);
        }

        private void Update()
        {
            if (VgInput.GetButtonDown(InputAction.Pause))
            {
                SetPaused(!m_IsPaused);
            }
        }

        private void SetPaused(bool paused)
        {
            if (m_RootContainer == null)
                return;

            m_IsPaused = paused;
            m_RootContainer.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            Time.timeScale = paused ? 0f : 1f;

            if (paused)
            {
                ShowScreen(m_PauseMenuScreen);
            }
        }

        private void ShowScreen(VisualElement screenToShow)
        {
            m_PauseMenuScreen.style.display = DisplayStyle.None;
            m_SaveSlotScreen.style.display = DisplayStyle.None;
            m_SettingsScreen.style.display = DisplayStyle.None;

            screenToShow.style.display = DisplayStyle.Flex;

            if (screenToShow == m_SaveSlotScreen)
            {
                RefreshSlotsUI();
            }
        }

        private void OpenSaveScreen()
        {
            m_IsSaveMode = true;
            m_SlotScreenTitle.text = "Save Game";
            ShowScreen(m_SaveSlotScreen);
        }

        private void OpenLoadScreen()
        {
            m_IsSaveMode = false;
            m_SlotScreenTitle.text = "Load Game";
            ShowScreen(m_SaveSlotScreen);
        }

        private void RefreshSlotsUI()
        {
            m_SlotContainer.Clear();
            var availableSlots = VgSaveSystem.Instance.AvailableSlots;

            foreach (var meta in availableSlots.OrderByDescending(m => m.LastSavedTime))
            {
                var slotBtn = new Button { name = meta.SlotName };
                slotBtn.AddToClassList("slot-button");

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

                var actions = new VisualElement();
                actions.AddToClassList("slot-actions");

                var renameBtn = new Button { text = "Rename" };
                renameBtn.AddToClassList("rename-button");
                renameBtn.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    OpenRenamePopup(meta.SlotName, meta.DisplayName);
                });
                actions.Add(renameBtn);
                slotBtn.Add(actions);

                slotBtn.RegisterCallback<ClickEvent>(evt => OnSlotSelected(meta.SlotName).Forget());
                m_SlotContainer.Add(slotBtn);
            }

            m_CreateNewSlotButton.style.display = m_IsSaveMode
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private async UniTaskVoid OnSlotSelected(string slotName)
        {
            if (m_IsSaveMode)
            {
                CLogger.LogInfo($"Saving game to: {slotName}", LogTag.Game);
                bool success = await VgSaveSystem.Instance.SaveSlotAsync(slotName);
                if (success)
                {
                    SetPaused(false);
                }
            }
            else
            {
                CLogger.LogInfo($"Loading game from: {slotName}", LogTag.Game);
                bool success = await VgSaveSystem.Instance.LoadSlotAsync(slotName);
                if (success)
                {
                    SetPaused(false);
                    var command = new GameFlowCommands.StartGameCommand(
                        GameIdentifiers.SavePoints.Chapter1Start
                    );
                    command.Execute().Forget();
                }
            }
        }

        private async UniTaskVoid OnCreateNewSlotClicked()
        {
            string newSlotName = $"slot_{DateTime.Now:yyyyMMdd_HHmmss}";
            CLogger.LogInfo($"Creating new save: {newSlotName}", LogTag.Game);
            VgSaveSystem.Instance.SetCurrentSlot(newSlotName);
            bool success = await VgSaveSystem.Instance.SaveSlotAsync(newSlotName);
            if (success)
            {
                SetPaused(false);
            }
        }

        private void OpenRenamePopup(string slotName, string currentName)
        {
            m_SlotToRename = slotName;
            m_RenameTextField.value = currentName ?? slotName;
            m_RenameOverlay.style.display = DisplayStyle.Flex;
        }

        private async UniTaskVoid OnRenameConfirmClicked()
        {
            string newName = m_RenameTextField.value;
            if (!string.IsNullOrEmpty(newName))
            {
                bool success = await VgSaveSystem.Instance.RenameSlotAsync(m_SlotToRename, newName);
                if (success)
                {
                    RefreshSlotsUI();
                }
            }
            m_RenameOverlay.style.display = DisplayStyle.None;
        }

        private void OnQuitToMenu()
        {
            Time.timeScale = 1f;
            GameCore.Instance.RequestExitToMenu();
        }

        private UIDocument m_Document;
        private VisualElement m_RootContainer;
        private VisualElement m_PauseMenuScreen;
        private VisualElement m_SaveSlotScreen;
        private VisualElement m_SettingsScreen;
        private Button m_ResumeButton;
        private Button m_SaveGameButton;
        private Button m_LoadGameButton;
        private Button m_SettingsButton;
        private Button m_MainMenuButton;
        private Button m_CreateNewSlotButton;
        private VisualElement m_SlotContainer;
        private Button m_BackFromSaveButton;
        private Label m_SlotScreenTitle;
        private VisualElement m_RenameOverlay;
        private TextField m_RenameTextField;
        private Button m_RenameConfirmButton;
        private Button m_RenameCancelButton;
        private string m_SlotToRename;
        private bool m_IsSaveMode;
        private bool m_IsPaused;
    }
}
