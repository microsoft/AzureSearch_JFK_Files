import * as React from "react";
import { searchPath } from "../search-page";
import { LogoComponent } from "../../common/components/logo";
import { SearchButton } from "./components/search";
import { CaptionComponent } from "./components/caption";
import { SearchInput } from "./components/search";

const style = require("./home-page.style.scss");


interface HomePageProps {
  searchValue: string;
  onSearchSubmit: () => void;
  onSearchUpdate: (newValue: string) => void;
}

export const HomePageComponent: React.StatelessComponent<HomePageProps> = (props) => {
  return (
    <div className={style.container}>
      <LogoComponent classes={{container: style.logoContainer, object: style.logoObject}} />
      <div className={style.main}>
        <CaptionComponent />
        <SearchInput         
          searchValue={props.searchValue}
          onSearchSubmit={props.onSearchSubmit}
          onSearchUpdate={props.onSearchUpdate}
        />
        <SearchButton onClick={props.onSearchSubmit}/>
      </div>
    </div>
  )
};


