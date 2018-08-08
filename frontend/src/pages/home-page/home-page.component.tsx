import * as React from "react";
import { searchPath } from "../search-page";
import { LogoJFKComponent } from "../../common/components/logo-jfk";
import { SearchButton } from "./components/search";
import { CaptionComponent } from "./components/caption";
import { SearchInput } from "./components/search";
import { FooterComponent } from "../../common/components/footer";
import analytics from "../../common/analytics/analytics";

const style = require("./home-page.style.scss");

interface HomePageProps {
    searchValue: string;
    onSearchSubmit: () => void;
    onSearchUpdate: (newValue: string) => void;
}

export class HomePageComponent extends React.Component<HomePageProps> {

    private analyticsFunction = analytics;

    public componentWillMount() {
        this.analyticsFunction();
    }

    public render() {
        return (
            <div className={style.container}>
                <LogoJFKComponent
                    classes={{ container: style.logoContainer, svg: style.logoSvg }}
                />
                <div className={style.main}>
                    <CaptionComponent />
                    <SearchInput
                        searchValue={this.props.searchValue}
                        onSearchSubmit={this.props.onSearchSubmit}
                        onSearchUpdate={this.props.onSearchUpdate}
                    />
                    <SearchButton onClick={this.props.onSearchSubmit} />
                </div>
                <FooterComponent className={style.footer} />
            </div>
        );
    }
}
