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
      onMouseEnter={onHover(props, true, id, props.node.textContent, nodePosSize.height)}
      onMouseLeave={onHover(props, false, null, null)}
    />
  );
};

const onHover = (props: SvgRectProps, isHover: boolean, id?: string, word?: string, height?: number) => (e) => {
  const reactangle = e.target.getBoundingClientRect();
  if (props.onHover) {
    props.onHover({
      id,
      left: reactangle.left,
      top: reactangle.top,
      height,
      word,
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
