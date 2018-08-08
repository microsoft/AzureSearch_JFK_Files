import * as React from "react";
import { RouteComponentProps } from "react-router";
import { HomePageComponent } from "./home-page.component";
import { searchPath } from "../search-page";
import analytics from "../../common/analytics/analytics";
var qs = require("qs");

const style = require("./home-page.style.scss");
interface HomePageState {
    searchValue: string;
}

export class HomePageContainer extends React.Component<RouteComponentProps<any>, HomePageState> {
    constructor(props) {
        super(props);

        this.state = {
            searchValue: "oswald",
        };
    }

    private handleSearchSubmit = () => {
        const params = qs.stringify({ term: this.state.searchValue });

        this.props.history.push({
            pathname: searchPath,
            search: `?${params}`,
        });
    };

    private handleSearchUpdate = (newSearch: string) => {
        this.setState({ ...this.state, searchValue: newSearch });
    };

    public render() {
        return (
            <div>
                <HomePageComponent
                    searchValue={this.state.searchValue}
                    onSearchSubmit={this.handleSearchSubmit}
                    onSearchUpdate={this.handleSearchUpdate}
                />
            </div>
        );
    }
}
