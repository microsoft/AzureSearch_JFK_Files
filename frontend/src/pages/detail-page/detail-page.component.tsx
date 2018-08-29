import * as React from "react";
import { HocrProofreaderComponent, PageIndex } from "../../common/components/hocr";
import { ZoomMode } from "../../common/components/hocr";
import { ToolbarComponent } from "./components/toolbar";
import { HorizontalSeparator } from "../../common/components/horizontal-separator";
import analytics from "../../common/analytics/analytics";

const style = require("./detail-page.style.scss");

interface DetailPageProps {
    hocr: string;
    targetWords: string[];
    zoomMode?: ZoomMode;
    pageIndex: PageIndex;
    showText?: boolean;
    onToggleTextClick: () => void;
    onZoomChange: (zoomMode: ZoomMode) => void;
    onCloseClick: () => void;
}

export class DetailPageComponent extends React.Component<DetailPageProps, {}> {
    constructor(props) {
        super(props);
    }

    private analyticsFunction = analytics;

    public componentWillMount() {
        this.analyticsFunction();
    }

    public render() {
        return (
            <div className={style.container}>
                <ToolbarComponent
                    zoomMode={this.props.zoomMode}
                    onToggleTextClick={this.props.onToggleTextClick}
                    onZoomChange={this.props.onZoomChange}
                    onCloseClick={this.props.onCloseClick}
                />
                <HorizontalSeparator className={style.separator} />
                <HocrProofreaderComponent
                    className={style.hocr}
                    hocr={this.props.hocr}
                    targetWords={this.props.targetWords}
                    zoomMode={this.props.zoomMode}
                    pageIndex={this.props.pageIndex}
                    showText={this.props.showText}
                />
            </div>
        );
    }
}
