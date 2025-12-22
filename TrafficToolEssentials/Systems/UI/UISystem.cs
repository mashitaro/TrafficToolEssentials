using System.Collections.Generic;
using System.Globalization;
using C2VM.TrafficToolEssentials.Components;
using C2VM.TrafficToolEssentials.Systems.Overlay;
using C2VM.TrafficToolEssentials.Systems.Update;
using C2VM.TrafficToolEssentials.Utils;
using Game;
using Game.Common;
using Game.Rendering;
using Game.SceneFlow;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficToolEssentials.Systems.UI;

public partial class UISystem : UISystemBase
{
    public enum MainPanelState : int
    {
        Hidden = 0,
        FunctionSelection = 1,
        Empty = 2,
        Main = 3,
        CustomPhase = 4,
        IntersectionMonitor = 5,
    }

    private bool m_ShowNotificationUnsaved;

    public MainPanelState m_MainPanelState { get; private set; }

    public Entity m_SelectedEntity { get; private set; }

    private CustomTrafficLights m_CustomTrafficLights;

    private Game.City.CityConfigurationSystem m_CityConfigurationSystem;

    // V172: TimeSystem for current game time
    private Game.Simulation.TimeSystem m_TimeSystem;

    private RenderSystem m_RenderSystem;

    private Tool.ToolSystem m_ToolSystem;

    private Update.ModificationUpdateSystem m_ModificationUpdateSystem;

    private SimulationUpdateSystem m_SimulationUpdateSystem;

    // Green Wave Sync System
    internal TrafficLightSystems.Simulation.SyncGroupSystem m_SyncGroupSystem;

    private Camera m_Camera;

    private int m_ScreenHeight;

    private CameraUpdateSystem m_CameraUpdateSystem;

    private float3 m_CameraPosition;

    private List<UITypes.WorldPosition> m_WorldPositionList;

    private Dictionary<Entity, NativeArray<NodeUtils.EdgeInfo>> m_EdgeInfoDictionary;

    private int m_DebugDisplayGroup;

    // Delayed redraw after paste (500ms)
    private float m_PendingRedrawTime = -1f;
    
    // Live dashboard update counter
    private int m_DashboardUpdateCounter = 0;
    private const int DASHBOARD_UPDATE_INTERVAL = 15; // Update every 15 frames (~4x per second at 60fps)
    public bool m_GreenWaveDashboardActive { get; set; } = false;

    private UITypes.ScreenPoint m_MainPanelPosition;

    // V228: Cached EntityQuery for RedrawIcon - LAZY INIT to avoid null during game load!
    private EntityQuery m_IconQuery;
    
    // V231: Cached EntityQuery for UIBindings operations (RemoveFromGroup, SetOffset, etc.)
    // This query is ONLY CustomTrafficLights (no Node component)
    private EntityQuery m_TrafficLightsQuery;

    public TypeHandle m_TypeHandle;
    
    /// <summary>
    /// V228: SAFE lazy initialization for m_IconQuery.
    /// This prevents crashes when RedrawIcon is called BEFORE OnCreate completes!
    /// The old V219 approach crashed because m_IconQuery was null during game load.
    /// </summary>
    private EntityQuery GetOrCreateIconQuery()
    {
        if (m_IconQuery == default)
        {
            m_IconQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Net.Node>(), 
                ComponentType.ReadOnly<CustomTrafficLights>()
            );
        }
        return m_IconQuery;
    }
    
    /// <summary>
    /// V231: SAFE lazy initialization for m_TrafficLightsQuery.
    /// Used by UIBindings for intersection operations (RemoveFromGroup, SetOffset).
    /// Prevents memory leak from GetEntityQuery() allocating on every call!
    /// </summary>
    internal EntityQuery GetOrCreateTrafficLightsQuery()
    {
        if (m_TrafficLightsQuery == default)
        {
            m_TrafficLightsQuery = GetEntityQuery(
                ComponentType.ReadOnly<CustomTrafficLights>()
            );
        }
        return m_TrafficLightsQuery;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_TypeHandle.AssignHandles(ref base.CheckedStateRef);

        m_Camera = Camera.main;
        m_ScreenHeight = Screen.height;
        m_MainPanelPosition = new(-999999, -999999);

        m_WorldPositionList = [];
        m_EdgeInfoDictionary = [];

        m_DebugDisplayGroup = -1;

        m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
        m_CityConfigurationSystem = World.GetOrCreateSystemManaged<Game.City.CityConfigurationSystem>();
        m_TimeSystem = World.GetOrCreateSystemManaged<Game.Simulation.TimeSystem>();  // V172
        m_RenderSystem = World.GetOrCreateSystemManaged<RenderSystem>();
        m_ToolSystem = World.GetOrCreateSystemManaged<Tool.ToolSystem>();
        m_ModificationUpdateSystem = World.GetOrCreateSystemManaged<Update.ModificationUpdateSystem>();
        m_SimulationUpdateSystem = World.GetOrCreateSystemManaged<SimulationUpdateSystem>();
        m_SyncGroupSystem = World.GetOrCreateSystemManaged<TrafficLightSystems.Simulation.SyncGroupSystem>();

        m_ModificationUpdateSystem.Enabled = false;
        m_SimulationUpdateSystem.Enabled = false;

        AddUIBindings();
        SetupKeyBindings();
        UpdateLocale();

        GameManager.instance.localizationManager.onActiveDictionaryChanged += UpdateLocale;
    }

    protected override void OnUpdate()
    {
        if (m_WorldPositionList.Count > 0 && !m_CameraPosition.Equals(m_CameraUpdateSystem.position))
        {
            m_CameraPosition = m_CameraUpdateSystem.position;
            m_ScreenPointBinding.Update();
        }
        
        // Check for pending delayed redraw
        if (m_PendingRedrawTime > 0 && UnityEngine.Time.time >= m_PendingRedrawTime)
        {
            m_PendingRedrawTime = -1f;
            RedrawGizmo();
            Mod.LogDebug("[DelayedRedraw] Gizmo redrawn after paste");
        }
        
        // === GREEN WAVE SYNC - Called every frame ===
        // SyncGroupSystem.OnUpdate() doesn't run reliably, so we call it from here
        if (m_SyncGroupSystem != null)
        {
            m_SyncGroupSystem.UpdateSync();
        }
        
        // V145: Live update for Main Panel (Traffic Summary, Flow Status, etc.)
        // Update every ~2 seconds when panel is visible
        if (m_MainPanelState != MainPanelState.Hidden && m_MainPanelBinding != null)
        {
            m_DashboardUpdateCounter++;
            if (m_DashboardUpdateCounter >= DASHBOARD_UPDATE_INTERVAL)
            {
                m_DashboardUpdateCounter = 0;
                m_MainPanelBinding.Update();
            }
        }
    }
    
    public void ScheduleDelayedRedraw(float delaySeconds = 0.5f)
    {
        m_PendingRedrawTime = UnityEngine.Time.time + delaySeconds;
    }

    protected override void OnDestroy()
    {
        ClearEdgeInfo();
    }

    private int m_PostLoadRefreshCounter = 0;
    private const int POST_LOAD_REFRESH_FRAMES = 60; // Refresh binding after 60 frames (~1 second)
    
    protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
    {
        // Reset sync groups for new savegame - this will reload groups for the current city
        if (m_SyncGroupSystem != null)
        {
            m_SyncGroupSystem.ResetForNewGame();
        }
        
        // V150: TrafficFlowHistory now uses ISerializable with proper schema versioning
        // No need to clear - data persists correctly across save/load
        
        m_MainPanelBinding.Update();
        m_CityConfigurationBinding.Update();
        // Start counter to refresh bindings after SyncGroupSystem has loaded
        m_PostLoadRefreshCounter = POST_LOAD_REFRESH_FRAMES;
        Mod.LogDebug("[UISystem] Game loading complete, starting post-load refresh counter");
    }

    public void SimulationUpdate()
    {
        // Post-load refresh: Update binding after SyncGroupSystem has had time to load groups
        if (m_PostLoadRefreshCounter > 0)
        {
            m_PostLoadRefreshCounter--;
            if (m_PostLoadRefreshCounter == 0)
            {
                m_MainPanelBinding.Update();
                Mod.LogDebug("[UISystem] Post-load refresh: Updated MainPanelBinding for sync groups");
            }
        }
        
        if (m_MainPanelState == MainPanelState.CustomPhase)
        {
            m_MainPanelBinding.Update();
        }
        RedrawGizmo();
    }

    public void SetMainPanelState(MainPanelState state)
    {
        UpdateEntity();
        m_MainPanelState = state;
        m_MainPanelBinding.Update();
        RedrawIcon();
        UpdateManualSignalGroup(0);
        if (m_MainPanelState != MainPanelState.CustomPhase)
        {
            UpdateActiveEditingCustomPhaseIndex(-1);
            UpdateActiveViewingCustomPhaseIndex(-1);
        }
        if (m_MainPanelState == MainPanelState.Hidden)
        {
            SaveSelectedEntity();
            m_ToolSystem.Disable();
        }
        else if (m_MainPanelState == MainPanelState.FunctionSelection)
        {
            m_ToolSystem.Disable();
        }
        else if (m_MainPanelState == MainPanelState.Empty)
        {
            m_ToolSystem.Enable();
        }
        else if (m_MainPanelState == MainPanelState.IntersectionMonitor)
        {
            m_ToolSystem.Disable();
        }
        else
        {
            m_ToolSystem.Suspend();
        }
        m_ModificationUpdateSystem.Enabled = m_MainPanelState != MainPanelState.Hidden;
        m_SimulationUpdateSystem.Enabled = m_MainPanelState != MainPanelState.Hidden;
        m_RenderSystem.ClearLineMesh();
    }

    public static string GetLocaleCode()
    {
        string locale = Utils.LocalisationUtils.GetAutoLocale(GameManager.instance.localizationManager.activeLocaleId, CultureInfo.CurrentCulture.Name);
        if (Mod.m_Settings != null && Mod.m_Settings.m_Locale != "auto")
        {
            locale = Mod.m_Settings.m_Locale;
        }
        return locale;
    }

    public static void UpdateLocale()
    {
        LocalisationUtils localisationsHelper = new LocalisationUtils(GetLocaleCode());
        localisationsHelper.AddToDictionary(GameManager.instance.localizationManager.activeDictionary);
        localisationsHelper.UpdateActiveDictionary();

        if (m_LocaleBinding != null)
        {
            m_LocaleBinding.Update();
        }
    }

    public void UpdateEdgeInfo(Entity node)
    {
        if (node == Entity.Null)
        {
            return;
        }
        if (m_EdgeInfoDictionary.ContainsKey(node))
        {
            NodeUtils.Dispose(m_EdgeInfoDictionary[node]);
        }
        m_EdgeInfoDictionary[node] = NodeUtils.GetEdgeInfoList(Allocator.Persistent, node, this).AsArray();
        m_EdgeInfoBinding.Update();
        m_MainPanelBinding.Update();
    }

    public void ClearEdgeInfo()
    {
        foreach (var kV in m_EdgeInfoDictionary)
        {
            NodeUtils.Dispose(kV.Value);
        }
        m_EdgeInfoDictionary.Clear();
    }

    public void SaveSelectedEntity()
    {
        UpdateEntity();
        ChangeSelectedEntity(Entity.Null);
        m_MainPanelBinding.Update();
    }

    public void UpdateEntity(bool keepTimer = true, bool addUpdated = true)
    {
        if (m_SelectedEntity != Entity.Null)
        {
            if (!EntityManager.HasComponent<CustomTrafficLights>(m_SelectedEntity))
            {
                EntityManager.AddComponentData(m_SelectedEntity, m_CustomTrafficLights);
            }
            else
            {
                if (keepTimer)
                {
                    var customTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(m_SelectedEntity);
                    m_CustomTrafficLights.m_Timer = customTrafficLights.m_Timer;
                }
                EntityManager.SetComponentData<CustomTrafficLights>(m_SelectedEntity, m_CustomTrafficLights);
            }

            if (!EntityManager.HasComponent<Game.Net.TrafficLights>(m_SelectedEntity))
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }
            else if (m_CustomTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.ModDefault)
            {
                EntityManager.RemoveComponent<CustomTrafficLights>(m_SelectedEntity);
            }

            if (addUpdated)
            {
                EntityManager.AddComponentData(m_SelectedEntity, default(Updated));
            }
        }
    }

    public void ChangeSelectedEntity(Entity entity)
    {
        UpdateManualSignalGroup(0);

        if (entity != m_SelectedEntity && entity != Entity.Null && m_SelectedEntity != Entity.Null)
        {
            m_ShowNotificationUnsaved = true;
            m_MainPanelBinding.Update();
            return;
        }

        if (entity != m_SelectedEntity)
        {
            m_ShowNotificationUnsaved = false;
            m_RenderSystem.ClearLineMesh();
            ClearEdgeInfo();

            if (!entity.Equals(Entity.Null))
            {
                UpdateEdgeInfo(entity);
                SetMainPanelState(MainPanelState.Main);

                if (EntityManager.HasComponent<CustomTrafficLights>(entity))
                {
                    m_CustomTrafficLights = EntityManager.GetComponentData<CustomTrafficLights>(entity);
                }
                else
                {
                    m_CustomTrafficLights = new CustomTrafficLights(CustomTrafficLights.Patterns.Vanilla);
                }
            }
            else if (m_MainPanelState != MainPanelState.Hidden)
            {
                SetMainPanelState(MainPanelState.Empty);
            }

            m_SelectedEntity = entity;
        }
    }
}
