import * as React from "react";
import { getNodeOptions, bboxToPosSize, getNodeId, composeId } from "../util/common-util";

/**
 * HOCR Node SVG
 */

interface SvgRectProps {
  node: Element;
  className: string;
  idSuffix: string;
  onHover?: (id: string) => void; 
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
      onMouseEnter={props.onHover && (() => props.onHover(id))}
      onMouseLeave={props.onHover && (() => props.onHover(null))}
    />
  );
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