using System.ComponentModel;
using Colossal.UI.Binding;
using Newtonsoft.Json;

namespace C2VM.TrafficToolEssentials.Systems.UI;

public static class UITypes
{
    public struct ItemDivider
    {
        [JsonProperty]
        const string itemType = "divider";
    }

    public struct ItemRadio
    {
        [JsonProperty]
        const string itemType = "radio";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    // V166: Dropdown for phase mode selection
    public struct ItemDropdown
    {
        [JsonProperty]
        const string itemType = "dropdown";

        public string key;
        public string selectedValue;
        public string engineEventName;
        public DropdownOption[] options;
    }
    
    public struct DropdownOption
    {
        public string value;
        public string label;
    }

    public struct ItemTitle
    {
        [JsonProperty]
        const string itemType = "title";

        public string title;
    }

    public struct ItemMessage
    {
        [JsonProperty]
        const string itemType = "message";

        public string message;
    }

    public struct ItemCheckbox
    {
        [JsonProperty]
        const string itemType = "checkbox";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemButton
    {
        [JsonProperty]
        const string itemType = "button";

        public string type;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemNotification
    {
        [JsonProperty]
        const string itemType = "notification";

        [JsonProperty]
        const string type = "c2vm-tle-panel-notification";

        public string label;

        public string notificationType;

        public string key;

        public string value;

        public string engineEventName;
    }

    public struct ItemRange {
        [JsonProperty]
        const string itemType = "range";

        public string key;

        public string label;

        public float value;

        public string valuePrefix;

        public string valueSuffix;

        public float min;

        public float max;

        public float step;

        public float defaultValue;

        public bool enableTextField;

        public string textFieldRegExp;

        public string engineEventName;
    }

    public struct ItemCustomPhase
    {
        [JsonProperty]
        const string itemType = "customPhase";

        public int activeIndex;

        public int activeViewingIndex;

        public int currentSignalGroup;

        public int manualSignalGroup;

        public int index;

        public int length;

        public uint timer;

        public ushort turnsSinceLastRun;

        public ushort lowFlowTimer;

        public float carFlow;

        public ushort carLaneOccupied;

        public ushort publicCarLaneOccupied;

        public ushort trackLaneOccupied;

        public ushort pedestrianLaneOccupied;

        public ushort bicycleLaneOccupied;

        public float weightedWaiting;

        public float targetDuration;

        public int priority;

        public ushort minimumDuration;

        public ushort maximumDuration;

        public float targetDurationMultiplier;

        public float laneOccupiedMultiplier;

        public float intervalExponent;

        public bool prioritiseTrack;

        public bool prioritisePublicCar;

        public bool prioritisePedestrian;

        public bool prioritiseBicycle;

        public bool linkedWithNextPhase;

        public bool endPhasePrematurely;
    }

    public struct UpdateCustomPhaseData
    {
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int index;

        public string key;

        public double value;
    }

    public struct WorldPosition : IJsonWritable
    {
        public float x;

        public float y;

        public float z;

        public string key { get => $"{x.ToString("0.0")},{y.ToString("0.0")},{z.ToString("0.0")}"; }

        public static implicit operator WorldPosition(float pos) => new WorldPosition{x = pos, y = pos, z = pos};

        public static implicit operator WorldPosition(Unity.Mathematics.float3 pos) => new WorldPosition{x = pos.x, y = pos.y, z = pos.z};

        public static implicit operator Unity.Mathematics.float3(WorldPosition pos) => new Unity.Mathematics.float3(pos.x, pos.y, pos.z);

        public static implicit operator UnityEngine.Vector3(WorldPosition pos) => new UnityEngine.Vector3(pos.x, pos.y, pos.z);

        public static implicit operator string(WorldPosition pos) => pos.key;

        public override bool Equals(object obj)
        {
            if (obj is not WorldPosition)
            {
                return false;
            }
            return Equals((WorldPosition)obj);
        }

        public bool Equals(WorldPosition other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(WorldPosition).FullName);
            writer.PropertyName("x");
            writer.Write(x);
            writer.PropertyName("y");
            writer.Write(y);
            writer.PropertyName("z");
            writer.Write(z);
            writer.PropertyName("key");
            writer.Write(key);
            writer.TypeEnd();
        }
    }

    public struct ScreenPoint : System.IEquatable<ScreenPoint>, IJsonWritable
    {
        public int top;

        public int left;

        public ScreenPoint(int topPos, int leftPos)
        {
            left = leftPos;
            top = topPos;
        }

        public ScreenPoint(UnityEngine.Vector3 pos, int screenHeight)
        {
            left = (int)pos.x;
            top = (int)(screenHeight - pos.y);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ScreenPoint).FullName);
            writer.PropertyName("top");
            writer.Write(top);
            writer.PropertyName("left");
            writer.Write(left);
            writer.TypeEnd();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScreenPoint other){
                return Equals(other);
            }
            return false;
        }

        public bool Equals(ScreenPoint other)
        {
            return other.top == top && other.left == left;
        }

        public override int GetHashCode() => (top, left).GetHashCode();
    }

    public struct ToolTooltipMessage : IJsonWritable
    {
        public string image;

        public string message;

        public ToolTooltipMessage(string image, string message)
        {
            this.image = image;
            this.message = message;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ToolTooltipMessage).FullName);
            writer.PropertyName("image");
            writer.Write(image);
            writer.PropertyName("message");
            writer.Write(message);
            writer.TypeEnd();
        }
    }

    // V138: Traffic Summary for intersection overview (corrected naming)
    public struct ItemTrafficSummary
    {
        [JsonProperty]
        const string itemType = "trafficSummary";

        /// <summary>Total vehicles per hour passing through this intersection</summary>
        public float vehiclesPerHour;
        
        /// <summary>Average vehicles per minute</summary>
        public float vehiclesPerMinute;
        
        /// <summary>V163: Number of physical incoming lanes</summary>
        public int incomingLanes;
        
        /// <summary>V163: Total number of lane directions (Left+Straight+Right) - for tooltip</summary>
        public int totalDirections;
        
        /// <summary>Number of connected edges/approaches</summary>
        public int approaches;
        
        /// <summary>Current traffic flow status: "low", "medium", "high"</summary>
        public string flowStatus;
        
        /// <summary>V163: Max lanes occupied in any single phase (not sum!)</summary>
        public int occupiedLanes;
        
        /// <summary>V138: Number of pedestrian lanes currently occupied</summary>
        public int occupiedPedestrianLanes;
        
        /// <summary>Whether we have actual flow data (CustomPhase) or just estimates</summary>
        public bool hasFlowData;
        
        /// <summary>V147: Whether CustomPhase mode is active (enables data collection)</summary>
        public bool isCustomPhase;
        
        /// <summary>V148: Current real-time vehicles per hour (for live graph point)</summary>
        public float currentVPH;
        
        /// <summary>V172: Current game time (0.0-1.0 normalized day time) for chart time labels</summary>
        public float currentGameTime;
        
        /// <summary>V179: Current number of waiting vehicles at this intersection (EMA smoothed)</summary>
        public int waitingVehicles;
        
        /// <summary>V141: Historical traffic data for sparkline visualization (48 data points, 30-min intervals)</summary>
        public float[] historyData;
    }

    public static ItemRadio MainPanelItemPattern(string label, uint pattern, uint selectedPattern)
    {
        return new ItemRadio{label = label, key = "pattern", value = pattern.ToString(), engineEventName = "C2VM.TLE.CallMainPanelUpdatePattern", isChecked = (selectedPattern & 0xFFFF) == pattern};
    }

    public static ItemCheckbox MainPanelItemOption(string label, uint option, uint selectedPattern)
    {
        return new ItemCheckbox{label = label, key = option.ToString(), value = ((selectedPattern & option) != 0).ToString(), isChecked = (selectedPattern & option) != 0, engineEventName = "C2VM.TLE.CallMainPanelUpdateOption"};
    }
}