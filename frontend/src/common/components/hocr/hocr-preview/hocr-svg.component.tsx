import * as React from "react";
import { getNodeOptions, bboxToPosSize, getNodeId, composeId, PosSize } from "../util/common-util";
import { RectangleProps } from "./rectangleProps";

/**
 * HOCR Node SVG
 */

interface SvgRectProps {
  node: Element;
  className: string;
  idSuffix: string;
  onHover?: (rectangleProps: RectangleProps) => void;
}

export const SvgRectComponent: React.StatelessComponent<SvgRectProps> = (props) => {
  const nodeOptions = getNodeOptions(props.node);
  if (!nodeOptions || !nodeOptions.bbox) return null;

  const nodePosSize = bboxToPosSize(nodeOptions.bbox);
  const id = getNodeId(props.node);
  const suffixedId = composeId(id, props.idSuffix);

  return (
    <rect
      className={props.className}
      id={suffixedId}
      x={nodePosSize.x}
      y={nodePosSize.y}
      width={nodePosSize.width}
      height={nodePosSize.height}
      onMouseEnter={onHover(props, id, true, nodePosSize)}
      onMouseLeave={onHover(props, null, false, nodePosSize)}
    />
  );
};

const onHover = (props: SvgRectProps, id: string, isHover: boolean, nodePosSize: PosSize) => (e) => {
  console.log("IsHover: ", isHover, e.clientX, nodePosSize.x, e.clientY, nodePosSize.y);
  const reactangle = e.target.getBoundingClientRect();
  if (props.onHover) {
    props.onHover({
      id,
      left: reactangle.left,
      top: reactangle.top,
      isHover,
    });
  }
}

interface SvgGroupProps {
  className: string;
}

export const SvgGroupComponent: React.StatelessComponent<SvgGroupProps> = (props) => {
  return (
    <g className={props.className}>
      {props.children}
    </g>
  );
};
