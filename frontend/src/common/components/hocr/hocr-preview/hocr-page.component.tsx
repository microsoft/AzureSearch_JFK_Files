import * as React from "react";
import { Tooltip } from "material-ui";
import { getNodeId, getNodeOptions, WordComparator } from "../util/common-util";
import { HocrNodeProps, getNodeChildrenComponents } from "./hocr-node.component";
import { HocrPageStyleMap } from "./hocr-page.style";
import { HocrPreviewStyleMap } from "./hocr-preview.style";


/**
 * HOCR Page
 */

export type ZoomMode = "page-full" | "page-width" | "original";

export interface HocrPageProps {
  node: Element;
  key?: number;
  wordCompare: WordComparator;
  idSuffix: string;
  renderOnlyTargetWords?: boolean;
  userStyle?: HocrPreviewStyleMap;
  onWordHover?: (wordId: string) => void;
  zoomMode?: ZoomMode;
}

interface State {
  isOpenTooltip: boolean;
  tooltipX: number;
  tooltipY: number;
}

export class HocrPageComponent extends React.PureComponent<HocrPageProps, State> {
  constructor(props) {
    super(props);

    this.state = {
      isOpenTooltip: false,
      tooltipX: 0,
      tooltipY: 0,
    };
  }

  toggleTooltip = (x: number, y: number) => {
    this.setState({
      isOpenTooltip: !this.state.isOpenTooltip,
      tooltipX: x,
      tooltipY: y,
    });
  }

  onNodeHover = (wordId: string, x: number, y: number) => {
    this.toggleTooltip(x, y);
    if (this.props.onWordHover) {
      this.props.onWordHover(wordId);
    }
  }

  public render() {
    if (!this.props.node) return null;
    const pageOptions = getNodeOptions(this.props.node);

    return (
      <>
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className={this.props.userStyle.page}
          id={getNodeId(this.props.node, this.props.idSuffix)}
          viewBox={pageOptions.bbox.join(" ")}
          style={getZoomStyle(this.props.zoomMode || "original", pageOptions.bbox)}
        >
          <rect className={this.props.userStyle.background}
            x="0" y="0" width="100%" height="100%" />
          <image className={this.props.userStyle.image}
            x="0" y="0" width="100%" height="100%"
            xlinkHref={pageOptions.image} />
          <g className={this.props.userStyle.placeholders}>
            {getNodeChildrenComponents({
              node: this.props.node,
              key: this.props.key,
              wordCompare: this.props.wordCompare,
              idSuffix: this.props.idSuffix,
              renderOnlyTargetWords: this.props.renderOnlyTargetWords,
              userStyle: this.props.userStyle,
              onWordHover: this.onNodeHover,
            })}
          </g>
        </svg>

        <Tooltip
          title="Test tooltip"
          open={this.state.isOpenTooltip}
          style={{
            position: 'absolute',
            top: this.state.tooltipY,
            left: this.state.tooltipX,
          }}
        >
          <h1>test</h1>
        </Tooltip>
      </>
    );
  }
}

const getZoomStyle = (zoomMode: ZoomMode, bbox: any) => {
  return {
    width: (zoomMode === "original") ? `${(bbox[2] - bbox[0])}px`
      : (zoomMode === "page-width") ? "100%" : "",
    height: (zoomMode === "original") ? `${(bbox[3] - bbox[1])}px`
      : (zoomMode === "page-full") ? "100%" : "",
    display: "block",
    margin: "auto",
  }
}
