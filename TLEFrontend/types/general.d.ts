interface MainPanel {
  title: string,
  image: string,
  position: ScreenPoint,
  showPanel: boolean,
  showFloatingButton: boolean,
  state: number,
  items: MainPanelItem[]
}

type MainPanelItem = MainPanelItemTitle | MainPanelItemMessage | MainPanelItemDivider | MainPanelItemRadio | MainPanelItemCheckbox | MainPanelItemButton | MainPanelItemNotification | MainPanelItemRange | MainPanelItemCustomPhase | MainPanelItemTrafficSummary | MainPanelItemDropdown;

// V167: Dropdown for phase mode selection
interface MainPanelItemDropdown {
  itemType: "dropdown",
  key: string,
  selectedValue: string,
  engineEventName: string,
  options: DropdownOption[]
}

interface DropdownOption {
  value: string,
  label: string
}

interface MainPanelItemTitle {
  itemType: "title",
  title: string,
  secondaryText?: string
}

interface MainPanelItemMessage {
  itemType: "message",
  message: string
}

interface MainPanelItemDivider {
  itemType: "divider"
}

interface MainPanelItemRadio {
  itemType: "radio",
  type: string,
  isChecked: boolean,
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

interface MainPanelItemCheckbox {
  itemType: "checkbox",
  type: string,
  isChecked: boolean,
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

interface MainPanelItemButton {
  itemType: "button",
  type: "button",
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

interface MainPanelItemNotification {
  itemType: "notification",
  type: "notification",
  label: string,
  notificationType: "warning" | "notice",
  key?: string,
  value?: string,
  engineEventName?: string
}

interface MainPanelItemRange {
  itemType: "range",
  key: string,
  label: string,
  value: number,
  valuePrefix: string,
  valueSuffix: string,
  min: number,
  max: number,
  step: number,
  defaultValue: number,
  enableTextField?: boolean,
  textFieldRegExp?: string,
  engineEventName: string
}

interface MainPanelItemCustomPhase {
  itemType: "customPhase",
  activeIndex: number,
  activeViewingIndex: number,
  currentSignalGroup: number,
  manualSignalGroup: number,
  index: number,
  length: number,
  timer: number,
  turnsSinceLastRun: number,
  lowFlowTimer: number,
  carFlow: number,
  carLaneOccupied: number,
  publicCarLaneOccupied: number,
  trackLaneOccupied: number,
  pedestrianLaneOccupied: number,
  // V126: Bicycle support
  bicycleLaneOccupied?: number,
  weightedWaiting: number,
  targetDuration: number,
  priority: number,
  minimumDuration: number,
  maximumDuration: number,
  targetDurationMultiplier: number,
  laneOccupiedMultiplier: number,
  intervalExponent: number,
  prioritiseTrack: boolean,
  prioritisePublicCar: boolean,
  prioritisePedestrian: boolean,
  // V126: Bicycle support
  prioritiseBicycle?: boolean,
  linkedWithNextPhase: boolean,
  endPhasePrematurely: boolean,
}

interface WorldPosition {
  x: number,
  y: number,
  z: number,
  key: string
}

interface ScreenPoint {
  left: number,
  top: number
}

interface ScreenPointMap {
  [key: string]: ScreenPoint
}

interface CityConfiguration {
  leftHandTraffic: boolean
}

interface CustomPhaseLane {
  type: CustomPhaseLaneType,
  left: CustomPhaseSignalState,
  straight: CustomPhaseSignalState,
  right: CustomPhaseSignalState,
  uTurn: CustomPhaseSignalState,
  all: CustomPhaseSignalState
}

// V126: Added bicycleLane - single signal per approach (cyclists can go any direction when green)
type CustomPhaseLaneType = "carLane" | "publicCarLane" | "trackLane" | "bicycleLane" | "pedestrianLaneStopLine" | "pedestrianLaneNonStopLine";

type CustomPhaseLaneDirection = "left" | "straight" | "right" | "uTurn" | "all";

type CustomPhaseSignalState = "stop" | "go" | "yield" | "none";

interface GroupMaskSignal {
  m_GoGroupMask: number,
  m_YieldGroupMask: number
}

interface GroupMaskTurn {
  m_Left: GroupMaskSignal,
  m_Straight: GroupMaskSignal,
  m_Right: GroupMaskSignal,
  m_UTurn: GroupMaskSignal
}

interface EdgeGroupMask {
  m_Edge: Entity,
  m_Position: WorldPosition,
  m_Options: number,
  m_Car: GroupMaskTurn,
  m_PublicCar: GroupMaskTurn,
  m_Track: GroupMaskTurn,
  m_PedestrianStopLine: GroupMaskSignal,
  m_PedestrianNonStopLine: GroupMaskSignal,
  // V126: Bicycle support - single signal per approach
  m_Bicycle?: GroupMaskSignal
}

interface EdgeInfo {
  m_Edge: Entity,
  m_Position: WorldPosition,
  m_CarLaneLeftCount: number,
  m_CarLaneStraightCount: number,
  m_CarLaneRightCount: number,
  m_CarLaneUTurnCount: number,
  m_PublicCarLaneLeftCount: number,
  m_PublicCarLaneStraightCount: number,
  m_PublicCarLaneRightCount: number,
  m_PublicCarLaneUTurnCount: number,
  m_TrackLaneLeftCount: number,
  m_TrackLaneStraightCount: number,
  m_TrackLaneRightCount: number,
  // V126: Bicycle support - single count per approach (cyclists can go any direction)
  m_BicycleLaneCount?: number,
  m_TrainTrackCount: number,
  m_PedestrianLaneStopLineCount: number,
  m_PedestrianLaneNonStopLineCount: number,
  m_SubLaneInfoList: SubLaneInfo[],
  m_EdgeGroupMask: EdgeGroupMask
}

interface SubLaneGroupMask {
  m_SubLane: Entity,
  m_Position: WorldPosition,
  m_Options: number,
  m_Car: GroupMaskTurn,
  m_Track: GroupMaskTurn,
  m_Pedestrian: GroupMaskSignal
}

interface SubLaneInfo {
  m_SubLane: Entity,
  m_Position: WorldPosition,
  m_CarLaneLeftCount: number,
  m_CarLaneStraightCount: number,
  m_CarLaneRightCount: number,
  m_CarLaneUTurnCount: number,
  m_TrackLaneLeftCount: number,
  m_TrackLaneStraightCount: number,
  m_TrackLaneRightCount: number,
  // V126: Bicycle support - single count per approach
  m_BicycleLaneCount?: number,
  m_PedestrianLaneCount: number,
  m_SubLaneGroupMask: SubLaneGroupMask
}

interface ToolTooltipMessage {
  image: string,
  message: string
}

// V138: Traffic Summary for intersection overview (corrected naming)
interface MainPanelItemTrafficSummary {
  itemType: "trafficSummary",
  vehiclesPerHour: number,
  vehiclesPerMinute: number,
  incomingLanes: number,
  approaches: number,
  flowStatus: "low" | "medium" | "high",
  occupiedLanes: number,      // V138: Renamed - this is occupied lanes count, not vehicle count
  occupiedPedestrianLanes: number,  // V138: Renamed for clarity
  hasFlowData: boolean,
  isCustomPhase: boolean,  // V147: Whether CustomPhase mode is active (enables data collection)
  currentVPH: number,  // V148: Current real-time VPH for live graph point
  currentGameTime?: number,  // V172: Current game time (0.0-1.0 normalized) for chart time labels
  waitingVehicles?: number,  // V179: Current waiting vehicles at intersection
  historyData?: number[]  // V141: Historical traffic data for chart (48 points, 30-min intervals)
}