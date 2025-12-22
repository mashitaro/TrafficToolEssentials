using Game.Input;
using Game.UI;
using UnityEngine.InputSystem;

namespace C2VM.TrafficToolEssentials.Systems.UI;

public partial class UISystem : UISystemBase
{
    private ProxyAction m_MainPanelToggleKeyboardBinding;
    private ProxyAction m_IntersectionToolKeyboardBinding;

    private void SetupKeyBindings()
    {
        if (Mod.m_Settings == null)
        {
            Mod.LogError($"Mod.m_Settings is null, key bindings will not work.");
            return;
        }
        m_MainPanelToggleKeyboardBinding = Mod.m_Settings.GetAction(Settings.kKeyboardBindingMainPanelToggle);
        m_MainPanelToggleKeyboardBinding.shouldBeEnabled = true;
        m_MainPanelToggleKeyboardBinding.onInteraction += MainPanelToggle;

        m_IntersectionToolKeyboardBinding = Mod.m_Settings.GetAction(Settings.kKeyboardBindingIntersectionTool);
        m_IntersectionToolKeyboardBinding.shouldBeEnabled = true;
        m_IntersectionToolKeyboardBinding.onInteraction += IntersectionToolToggle;
    }

    private void MainPanelToggle(ProxyAction action, InputActionPhase phase)
    {
        if (Enabled && phase == InputActionPhase.Performed)
        {
            if (m_MainPanelState == MainPanelState.Hidden)
            {
                SetMainPanelState(MainPanelState.FunctionSelection);
            }
            else
            {
                SetMainPanelState(MainPanelState.Hidden);
            }
        }
    }

    private void IntersectionToolToggle(ProxyAction action, InputActionPhase phase)
    {
        if (Enabled && phase == InputActionPhase.Performed)
        {
            if (m_MainPanelState == MainPanelState.Hidden || m_MainPanelState == MainPanelState.FunctionSelection)
            {
                // Activate intersection click mode directly
                SetMainPanelState(MainPanelState.Empty);
            }
            else
            {
                // If already in some mode, close
                SetMainPanelState(MainPanelState.Hidden);
            }
        }
    }
}