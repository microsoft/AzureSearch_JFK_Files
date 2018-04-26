import * as React from "react";
import { getNodeOptions, bboxToPosSize, getNodeId, composeId } from "../util/common-util";

/**
 * HOCR Node SVG
 */

interface SvgRectProps {
  node: Element;
  className: string;
  idSuffix: string;
  onHover?: (id: string, x: number, y: number, isHover: boolean) => void;
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
      onMouseEnter={onHover(props, id, true)}
      onMouseLeave={onHover(props, null, false)}
    />
  );
};

const onHover = (props: SvgRectProps, id: string, isHover: boolean) => (e: React.MouseEvent<SVGRectElement>) => {
  console.log("IsHover: ", isHover);
  if (props.onHover) {
    props.onHover(id, e.clientX, e.clientY, isHover);
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
