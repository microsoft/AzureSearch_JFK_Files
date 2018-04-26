import * as React from "react";
import { Tooltip } from "material-ui";
import { getNodeId, getNodeOptions, WordComparator } from "../util/common-util";
import { HocrNodeProps, getNodeChildrenComponents } from "./hocr-node.component";
import { HocrPageStyleMap } from "./hocr-page.style";
import { HocrPreviewStyleMap } from "./hocr-preview.style";
import { ENGINE_METHOD_DIGESTS } from "constants";
import { RectangleProps } from "./rectangleProps";
import { TooltipComponent } from "../tooltip";


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
  tooltipLeft: number;
  tooltipTop: number;
}

export class HocrPageComponent extends React.PureComponent<HocrPageProps, State> {
  constructor(props) {
    super(props);

    this.state = {
      isOpenTooltip: false,
      tooltipLeft: 0,
      tooltipTop: 0,
    };
  }

  updateTooltip = (rectangleProps: RectangleProps) => {
    this.setState({
      isOpenTooltip: rectangleProps.isHover,
      tooltipLeft: rectangleProps.left,
      tooltipTop: rectangleProps.top,
    });
  }

  onNodeHover = (rectangleProps: RectangleProps) => {
    this.updateTooltip(rectangleProps);
    if (this.props.onWordHover) {
      this.props.onWordHover(rectangleProps.id);
    }
  }

  public render() {
    if (!this.props.node) return null;
    const pageOptions = getNodeOptions(this.props.node);

    return (
      <>
        <TooltipComponent
          show={this.state.isOpenTooltip}
          //oswalshow={true}
          top={this.state.tooltipTop + 50}
          left={this.state.tooltipLeft + 50}
          title="Test tooltip"
        >
          <p>Lorem Ipsum es simplemente el texto de relleno de las imprentas y archivos de texto. Lorem Ipsum ha sido el texto de relleno estándar de las industrias desde el año 1500, cuando un impresor (N. del T. persona que se dedica a la imprenta) desconocido usó una galería de textos y los mezcló de tal manera que logró hacer un libro de textos especimen. No sólo sobrevivió 500 años, sino que tambien ingresó como texto de relleno en documentos electrónicos, quedando esencialmente igual al original. Fue popularizado en los 60s con la creación de las hojas "Letraset", las cuales contenian pasajes de Lorem Ipsum, y más recientemente con software de autoedición, como por ejemplo Aldus PageMaker, el cual incluye versiones de Lorem Ipsum.</p>
        </TooltipComponent>
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
