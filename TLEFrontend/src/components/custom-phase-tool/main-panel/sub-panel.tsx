import { useContext } from "react";
import styled from "styled-components";

import { call } from "cs2/api";

import { LocaleContext } from "@/context";
import { getString } from "@/localisations";

import Button from "@/components/common/button";
import Checkbox from "@/components/common/checkbox";
import Collapsible from "@/components/common/collapsible";
import Tooltip from "@/components/common/tooltip";
import TooltipIcon from "@/components/common/tooltip-icon";
import Divider from "@/components/main-panel/items/divider";
import MainPanelRange from "@/components/main-panel/items/range";
import Row from "@/components/main-panel/items/row";
import Title from "@/components/main-panel/items/title";
import TitleDim from "@/components/main-panel/items/title-dim";

const DimLabel = styled.div`
  color: var(--textColorDim);
  flex-grow: 1;
  flex-shrink: 1;
  flex-basis: auto;
  display: inline;
`;

const TooltipText = styled.div`
  max-width: 250rem;
  line-height: 1.4;
`;

const SectionSpacer = styled.div`
  height: 16rem;
`;

const OptionRow = styled.div`
  display: flex;
  align-items: center;
  gap: 8rem;
`;

const ItemTitle = (props: {title: string, secondaryText?: string, tooltip?: React.ReactNode, dim?: boolean}) => {
  const item: MainPanelItemTitle = {
    itemType: "title",
    ...props
  };
  return (
    <Row data={item}>
      {props.dim && <TitleDim {...item} />}
      {!props.dim && <Title {...item} />}
      {props.tooltip && !props.dim && <>
        <Tooltip position="right-start" tooltip={props.tooltip}>
          <TooltipIcon style={{marginLeft: "0.25em"}} />
        </Tooltip>
      </>}
    </Row>
  );
};

const EndPhaseButton = (props: {index: number, disabled?: boolean}) => {
  const clickHandler = () => {
    if (!props.disabled) {
      call("C2VM.TLE", "CallUpdateCustomPhaseData", JSON.stringify({key: "EndPhasePrematurely", index: props.index}));
    }
  };
  return (
    <Row hoverEffect={!props.disabled}>
      <Button
        label={props.disabled ? "PhaseEndRequested" : "EndPhasePrematurely"}
        disabled={props.disabled}
        onClick={clickHandler}
      />
    </Row>
  );
};

export default function SubPanel(props: {data: MainPanelItemCustomPhase | null, statisticsOnly?: boolean}) {
  const locale = useContext(LocaleContext);
  const data = props.data;

  if (!data) {
    return <></>;
  }

  return (
    <>
      {!props.statisticsOnly && <>
        <SectionSpacer />
        
        {/* OPTIONS SECTION */}
        <Collapsible title={getString(locale, "Options")} defaultOpen={true}>
          <Row hoverEffect={true} data={{
              itemType: "checkbox",
              type: "",
              isChecked: data.prioritiseTrack,
              key: "PrioritiseTrack",
              value: "0",
              label: "",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
          >
            <OptionRow>
              <Checkbox isChecked={data.prioritiseTrack} />
              <Tooltip position="right-start" tooltip={<TooltipText>{getString(locale, "PrioritiseTrackTooltip")}</TooltipText>}>
                <TooltipIcon style={{marginRight: "0.4em"}} />
              </Tooltip>
              <DimLabel>{getString(locale, "PrioritiseTrack")}</DimLabel>
            </OptionRow>
          </Row>
          <Row hoverEffect={true} data={{
              itemType: "checkbox",
              type: "",
              isChecked: data.prioritisePublicCar,
              key: "PrioritisePublicCar",
              value: "0",
              label: "",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
          >
            <OptionRow>
              <Checkbox isChecked={data.prioritisePublicCar} />
              <Tooltip position="right-start" tooltip={<TooltipText>{getString(locale, "PrioritisePublicCarTooltip")}</TooltipText>}>
                <TooltipIcon style={{marginRight: "0.4em"}} />
              </Tooltip>
              <DimLabel>{getString(locale, "PrioritisePublicCar")}</DimLabel>
            </OptionRow>
          </Row>
          <Row hoverEffect={true} data={{
              itemType: "checkbox",
              type: "",
              isChecked: data.prioritisePedestrian,
              key: "PrioritisePedestrian",
              value: "0",
              label: "",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
          >
            <OptionRow>
              <Checkbox isChecked={data.prioritisePedestrian} />
              <Tooltip position="right-start" tooltip={<TooltipText>{getString(locale, "PrioritisePedestrianTooltip")}</TooltipText>}>
                <TooltipIcon style={{marginRight: "0.4em"}} />
              </Tooltip>
              <DimLabel>{getString(locale, "PrioritisePedestrian")}</DimLabel>
            </OptionRow>
          </Row>
          {/* V130: Bicycle Support DISABLED - bicycles follow car signals in CS2
              Backend code remains for future compatibility
          <Row hoverEffect={true} data={{
              itemType: "checkbox",
              type: "",
              isChecked: data.prioritiseBicycle ?? false,
              key: "PrioritiseBicycle",
              value: "0",
              label: "",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
          >
            <OptionRow>
              <Checkbox isChecked={data.prioritiseBicycle ?? false} />
              <Tooltip position="right-start" tooltip={<TooltipText>{getString(locale, "PrioritiseBicycleTooltip")}</TooltipText>}>
                <TooltipIcon style={{marginRight: "0.4em"}} />
              </Tooltip>
              <DimLabel>{getString(locale, "PrioritiseBicycle")}</DimLabel>
            </OptionRow>
          </Row>
          */}
        </Collapsible>

        <SectionSpacer />
        <Divider />
        <SectionSpacer />

        {/* ADJUSTMENTS SECTION */}
        <Collapsible title={getString(locale, "Adjustments")} defaultOpen={true}>
          <MainPanelRange 
            data={{
              itemType: "range",
              key: "MinimumDuration",
              label: "MinimumDuration",
              value: data.minimumDuration,
              valuePrefix: "",
              valueSuffix: "s",
              min: 0,
              max: 30,
              step: 1,
              defaultValue: 2,
              enableTextField: true,
              textFieldRegExp: "^\\d{0,4}$",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
            tooltip={<TooltipText>{getString(locale, "MinimumDurationTooltip")}</TooltipText>}
          />
          <MainPanelRange 
            data={{
              itemType: "range",
              key: "MaximumDuration",
              label: "MaximumDuration",
              value: data.maximumDuration,
              valuePrefix: "",
              valueSuffix: "s",
              min: 5,
              max: 300,
              step: 5,
              defaultValue: 300,
              enableTextField: true,
              textFieldRegExp: "^\\d{0,4}$",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
            tooltip={<TooltipText>{getString(locale, "MaximumDurationTooltip")}</TooltipText>}
          />
          <MainPanelRange 
            data={{
              itemType: "range",
              key: "TargetDurationMultiplier",
              label: "TargetDurationMultiplier",
              value: data.targetDurationMultiplier,
              valuePrefix: "",
              valueSuffix: "CustomPedestrianDurationMultiplierSuffix",
              min: 0.1,
              max: 10,
              step: 0.1,
              defaultValue: 1,
              enableTextField: true,
              textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
            tooltip={<TooltipText>{getString(locale, "TargetDurationMultiplierTooltip")}</TooltipText>}
          />
          <MainPanelRange 
            data={{
              itemType: "range",
              key: "LaneOccupiedMultiplier",
              label: "LaneOccupiedMultiplier",
              value: data.laneOccupiedMultiplier,
              valuePrefix: "",
              valueSuffix: "CustomPedestrianDurationMultiplierSuffix",
              min: 0.1,
              max: 10,
              step: 0.1,
              defaultValue: 1,
              enableTextField: true,
              textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
            tooltip={<TooltipText>{getString(locale, "LaneOccupiedMultiplierTooltip")}</TooltipText>}
          />
          <MainPanelRange 
            data={{
              itemType: "range",
              key: "IntervalExponent",
              label: "IntervalExponent",
              value: data.intervalExponent,
              valuePrefix: "",
              valueSuffix: "",
              min: 0.1,
              max: 10,
              step: 0.1,
              defaultValue: 2,
              enableTextField: true,
              textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
              engineEventName: "C2VM.TLE.CallUpdateCustomPhaseData"
            }}
            tooltip={<TooltipText>{getString(locale, "IntervalExponentTooltip")}</TooltipText>}
          />
        </Collapsible>

        <SectionSpacer />
        <Divider />
        <SectionSpacer />
      </>}

      {/* STATISTICS SECTION */}
      <Collapsible title={getString(locale, "Statistics")} defaultOpen={!props.statisticsOnly}>
        <ItemTitle 
          title="Timer" 
          secondaryText={`${data.timer} / ${Round(Math.min(Math.max(data.targetDuration, data.minimumDuration), data.maximumDuration))}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "TimerTooltip")}</TooltipText>}
        />
        <ItemTitle 
          title="Priority" 
          secondaryText={`${data.priority}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "PriorityTooltip")}</TooltipText>}
        />
        <ItemTitle 
          title="LastRun" 
          secondaryText={`${data.turnsSinceLastRun}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "LastRunTooltip")}</TooltipText>}
        />
        <ItemTitle 
          title="CarFlow" 
          secondaryText={`${Round(data.carFlow)}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "CarFlowTooltip")}</TooltipText>}
        />
        <ItemTitle 
          title="LanesOccupied" 
          secondaryText={`${data.carLaneOccupied}, ${data.publicCarLaneOccupied}, ${data.trackLaneOccupied}, ${data.pedestrianLaneOccupied}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "LanesOccupiedTooltip")}</TooltipText>}
        />
        <ItemTitle 
          title="WeightedWaiting" 
          secondaryText={`${Round(data.weightedWaiting)}`} 
          dim={true}
          tooltip={<TooltipText>{getString(locale, "WeightedWaitingTooltip")}</TooltipText>}
        />
      </Collapsible>

      <SectionSpacer />

      {data.activeIndex < 0 && data.manualSignalGroup <= 0 && data.currentSignalGroup == data.index + 1 && <EndPhaseButton index={data.index} disabled={data.endPhasePrematurely} />}
    </>
  );
}

function Round(num: number): number {
  return Math.round(num * 100) / 100;
}

